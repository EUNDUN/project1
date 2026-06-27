using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using Game.Config;
using Game.Player;
using Game.Camera;
using Game.Input;
using Game.Combat;
using Game.GameState;
using Game.Bot;
using Game.UI;

namespace Game
{
    // Builds the scene at runtime from a single GameConfig reference.
    // Attach to an empty GameObject in an otherwise empty SampleScene.
    //
    // Flow:
    //   Awake  → floor + ClassSelectionUI only (no game entities yet)
    //   User selects class → StartGame(cls) → creates player, bots, wires lists, HUD
    public class GameBootstrap : MonoBehaviour
    {
        public GameConfig config;

        // Populated during StartGame(), used by WireEnemyLists() and WireAbilityPassthrough().
        private readonly List<HealthComponent> _blueTeam = new List<HealthComponent>();
        private readonly List<HealthComponent> _redTeam  = new List<HealthComponent>();
        private readonly List<BotController>   _blueBots = new List<BotController>();
        private readonly List<BotController>   _redBots  = new List<BotController>();
        private AbilityController _playerAbility; // stored for WireAbilityPassthrough

        void Awake()
        {
            if (config == null)
            {
                Debug.LogError("[GameBootstrap] GameConfig is not assigned. " +
                    "Right-click in Project > Create > Game > GameConfig, then assign it to Bootstrap.");
                return;
            }

            CreateFloor();
            CreateClassSelectionUI();  // game entities are created inside the selection callback
        }

        // ─── Class selection ──────────────────────────────────────────────────────────

        void CreateClassSelectionUI()
        {
            GameObject go = new GameObject("ClassSelectionUI");
            ClassSelectionUI ui = go.AddComponent<ClassSelectionUI>();
            // Wire the single callback boundary between UI and game creation.
            // Swap this delegate to plug in a lobby/network selection later.
            ui.OnClassSelected = StartGame;
        }

        // Called by ClassSelectionUI exactly once when the player picks a class.
        // All game entities are created here so every system starts in a consistent state.
        void StartGame(CombatClass selectedClass)
        {
            CreatePlayer(selectedClass);
            CreateBlueBots();
            CreateRedBots();
            WireEnemyLists();
            WireAbilityPassthrough();
            CreateCrosshairUI();
            Debug.Log($"[Bootstrap] Game started. Player class: {selectedClass}");
        }

        // ─── Scene construction ───────────────────────────────────────────────────────

        void CreateFloor()
        {
            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "Floor";
            floor.transform.position = Vector3.zero;
            floor.transform.localScale = new Vector3(10f, 1f, 10f);
        }

        // selectedClass comes from the ClassSelectionUI, not from GameConfig.
        // GameConfig.playerStartingClass is retained as a development annotation
        // and potential future skip-UI fallback, but is not read here.
        void CreatePlayer(CombatClass selectedClass)
        {
            Vector3 playerSpawn = new Vector3(0f, 0f, config.blueSpawnZ);

            // --- PlayerEntity: body + physics ---
            GameObject playerGo = new GameObject("PlayerEntity");
            playerGo.transform.position = playerSpawn;

            CharacterController cc = playerGo.AddComponent<CharacterController>();
            cc.height = config.standHeight;
            cc.center = new Vector3(0f, config.standHeight * 0.5f, 0f);
            cc.radius = 0.4f;
            cc.slopeLimit = 45f;
            cc.stepOffset = 0.3f;

            playerGo.AddComponent<PlayerEntity>();

            // HealthComponent must be added before FirstPersonMotor.
            // AddComponent calls Awake() immediately; FirstPersonMotor.Awake() caches
            // GetComponent<HealthComponent>(), so the component must exist at that point.
            HealthComponent playerHealth = playerGo.AddComponent<HealthComponent>();
            playerHealth.Initialize(Team.Blue, selectedClass, CharacterStats.FromConfig(config));

            FirstPersonMotor motor = playerGo.AddComponent<FirstPersonMotor>();
            motor.config = config;

            _blueTeam.Add(playerHealth);

            RespawnController playerRespawn = playerGo.AddComponent<RespawnController>();
            playerRespawn.config = config;
            playerRespawn.spawnPoint = playerSpawn;

            // --- Camera: child of PlayerEntity ---
            GameObject camGo = new GameObject("CameraRoot");
            camGo.transform.SetParent(playerGo.transform, false);
            camGo.transform.localPosition = new Vector3(0f, config.standCameraLocalY, 0f);

            UnityEngine.Camera cam = camGo.AddComponent<UnityEngine.Camera>();
            cam.fieldOfView = 75f;
            cam.nearClipPlane = 0.1f;
            cam.tag = "MainCamera";
            camGo.AddComponent<AudioListener>();
            camGo.AddComponent<UniversalAdditionalCameraData>();

            // --- LocalPlayer: input + camera (no physics) ---
            GameObject localPlayerGo = new GameObject("LocalPlayer");
            localPlayerGo.AddComponent<PlayerInputReader>();

            FirstPersonCamera fpsCamera = localPlayerGo.AddComponent<FirstPersonCamera>();
            fpsCamera.config = config;
            fpsCamera.playerBodyTransform = playerGo.transform;
            fpsCamera.cameraTransform = camGo.transform;

            LocalPlayerController controller = localPlayerGo.AddComponent<LocalPlayerController>();
            controller.playerEntity = playerGo.GetComponent<PlayerEntity>();
            controller.cameraTransform = camGo.transform;

            BasicAttackController attackCtrl = localPlayerGo.AddComponent<BasicAttackController>();
            attackCtrl.config = config;
            attackCtrl.cameraTransform = camGo.transform;
            attackCtrl.attackerHealth = playerHealth;

            AbilityController abilityCtrl = localPlayerGo.AddComponent<AbilityController>();
            abilityCtrl.config          = config;
            abilityCtrl.ownerHealth     = playerHealth;
            abilityCtrl.cameraTransform = camGo.transform;
            _playerAbility              = abilityCtrl; // stored for WireAbilityPassthrough

            DebugDamageInput debugDamage = localPlayerGo.AddComponent<DebugDamageInput>();
            debugDamage.config = config;
            debugDamage.targetHealth = playerHealth;

            // Local player HUD: HP display + damage flash + backstab feedback (bottom-left)
            GameObject hudGo = new GameObject("PlayerHUD");
            PlayerHUD hud = hudGo.AddComponent<PlayerHUD>();
            hud.config            = config;
            hud.playerHealth      = playerHealth;
            hud.attackController  = attackCtrl;

            // Ability cooldown debug UI (bottom-centre, one row above HP)
            GameObject abilityUiGo = new GameObject("AbilityDebugUI");
            AbilityDebugUI abilityUi = abilityUiGo.AddComponent<AbilityDebugUI>();
            abilityUi.abilityController = abilityCtrl;

            // Motor movement debug panel (top-left) — shows speed multiplier breakdown.
            GameObject motorDbgGo = new GameObject("MotorDebugUI");
            MotorDebugUI motorDbg = motorDbgGo.AddComponent<MotorDebugUI>();
            motorDbg.motor = motor;
        }

        void CreateBlueBots()
        {
            for (int i = 0; i < config.blueBotCount; i++)
            {
                CombatClass cls = (config.blueBotClasses != null && i < config.blueBotClasses.Length)
                    ? config.blueBotClasses[i] : CombatClass.Warrior;
                Vector3 pos = TeamSpawnPosition(i, config.blueBotCount, config.blueSpawnZ);
                BotController bot = CreateBot("BlueBot_" + i, Team.Blue, cls, pos);
                _blueTeam.Add(bot.botHealth);
                _blueBots.Add(bot);
            }
        }

        void CreateRedBots()
        {
            for (int i = 0; i < config.redBotCount; i++)
            {
                CombatClass cls = (config.redBotClasses != null && i < config.redBotClasses.Length)
                    ? config.redBotClasses[i] : CombatClass.Warrior;
                Vector3 pos = TeamSpawnPosition(i, config.redBotCount, config.redSpawnZ);
                BotController bot = CreateBot("RedBot_" + i, Team.Red, cls, pos);
                _redTeam.Add(bot.botHealth);
                _redBots.Add(bot);
            }
        }

        // Collects every character's CharacterController and hands the array to the player's
        // AbilityController so it can call Physics.IgnoreCollision during dashes and Q casts.
        // Only the player AbilityController is wired — generalise to all ACs before adding bot skills or PvP.
        void WireAbilityPassthrough()
        {
            if (_playerAbility == null) return;
            var ccs = new System.Collections.Generic.List<CharacterController>(
                _blueTeam.Count + _redTeam.Count);
            foreach (HealthComponent hc in _blueTeam)
            {
                CharacterController cc = hc.GetComponent<CharacterController>();
                if (cc != null) ccs.Add(cc);
            }
            foreach (HealthComponent hc in _redTeam)
            {
                CharacterController cc = hc.GetComponent<CharacterController>();
                if (cc != null) ccs.Add(cc);
            }
            _playerAbility.dashPassthroughControllers = ccs.ToArray();
        }

        // Arrays are shared across same-team bots — no per-bot allocation.
        void WireEnemyLists()
        {
            HealthComponent[] redArray  = _redTeam.ToArray();
            HealthComponent[] blueArray = _blueTeam.ToArray();
            foreach (BotController bot in _blueBots) bot.enemies = redArray;
            foreach (BotController bot in _redBots)  bot.enemies = blueArray;
        }

        BotController CreateBot(string botName, Team team, CombatClass combatClass, Vector3 spawnPos)
        {
            GameObject botGo = new GameObject(botName);
            botGo.transform.position = spawnPos;

            CharacterController cc = botGo.AddComponent<CharacterController>();
            cc.height = config.standHeight;
            cc.center = new Vector3(0f, config.standHeight * 0.5f, 0f);
            cc.radius = 0.4f;
            cc.slopeLimit = 45f;
            cc.stepOffset = 0.3f;

            // Visual mesh — separate child so CharacterController is the sole collider.
            GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            visual.name = "Mesh";
            visual.transform.SetParent(botGo.transform, false);
            visual.transform.localPosition = new Vector3(0f, config.standHeight * 0.5f, 0f);
            Destroy(visual.GetComponent<CapsuleCollider>());

            Renderer rend = visual.GetComponent<Renderer>();
            rend.material.color = team == Team.Blue
                ? new Color(0.3f, 0.3f, 1f)
                : new Color(1f, 0.3f, 0.3f);

            HealthComponent health = botGo.AddComponent<HealthComponent>();
            health.Initialize(team, combatClass, CharacterStats.FromConfig(config));

            RespawnController respawn = botGo.AddComponent<RespawnController>();
            respawn.config = config;
            respawn.spawnPoint = spawnPos;

            BotController botCtrl = botGo.AddComponent<BotController>();
            botCtrl.config = config;
            botCtrl.botHealth = health;
            // enemies wired later by WireEnemyLists() — called after all bots are created

            GameObject uiGo = new GameObject(botName + "_UI");
            HealthDebugUI ui = uiGo.AddComponent<HealthDebugUI>();
            ui.target = health;

            return botCtrl;
        }

        Vector3 TeamSpawnPosition(int index, int count, float spawnZ)
        {
            float x = count > 1 ? (index - (count - 1) * 0.5f) * config.spawnSpreadX : 0f;
            return new Vector3(x, 0f, spawnZ);
        }

        void CreateCrosshairUI()
        {
            GameObject uiGo = new GameObject("CrosshairUI");
            uiGo.AddComponent<CrosshairUI>();
        }
    }
}