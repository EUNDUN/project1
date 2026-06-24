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
    public class GameBootstrap : MonoBehaviour
    {
        public GameConfig config;

        // Built during Create* calls, then used by WireEnemyLists().
        private readonly List<HealthComponent> _blueTeam = new List<HealthComponent>();
        private readonly List<HealthComponent> _redTeam  = new List<HealthComponent>();
        private readonly List<BotController>   _blueBots = new List<BotController>();
        private readonly List<BotController>   _redBots  = new List<BotController>();
        private HealthComponent _playerHealth;

        void Awake()
        {
            if (config == null)
            {
                Debug.LogError("[GameBootstrap] GameConfig is not assigned. " +
                    "Right-click in Project > Create > Game > GameConfig, then assign it to Bootstrap.");
                return;
            }

            CreateFloor();
            CreatePlayer();
            CreateBlueBots();
            CreateRedBots();
            WireEnemyLists();
            CreateCrosshairUI();
        }

        void CreateFloor()
        {
            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "Floor";
            floor.transform.position = Vector3.zero;
            floor.transform.localScale = new Vector3(10f, 1f, 10f);
        }

        void CreatePlayer()
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

            FirstPersonMotor motor = playerGo.AddComponent<FirstPersonMotor>();
            motor.config = config;

            HealthComponent playerHealth = playerGo.AddComponent<HealthComponent>();
            playerHealth.Initialize(Team.Blue, CharacterStats.FromConfig(config));

            _playerHealth = playerHealth;
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

            DebugDamageInput debugDamage = localPlayerGo.AddComponent<DebugDamageInput>();
            debugDamage.config = config;
            debugDamage.targetHealth = playerHealth;

            // Local player HUD: HP display + damage flash
            GameObject hudGo = new GameObject("PlayerHUD");
            PlayerHUD hud = hudGo.AddComponent<PlayerHUD>();
            hud.config = config;
            hud.playerHealth = playerHealth;
        }

        void CreateBlueBots()
        {
            for (int i = 0; i < config.blueBotCount; i++)
            {
                Vector3 pos = TeamSpawnPosition(i, config.blueBotCount, config.blueSpawnZ);
                BotController bot = CreateBot("BlueBot_" + i, Team.Blue, pos);
                _blueTeam.Add(bot.botHealth);
                _blueBots.Add(bot);
            }
        }

        void CreateRedBots()
        {
            for (int i = 0; i < config.redBotCount; i++)
            {
                Vector3 pos = TeamSpawnPosition(i, config.redBotCount, config.redSpawnZ);
                BotController bot = CreateBot("RedBot_" + i, Team.Red, pos);
                _redTeam.Add(bot.botHealth);
                _redBots.Add(bot);
            }
        }

        // Each bot receives a reference to every alive enemy at startup.
        // Arrays are shared across same-team bots — no per-bot allocation.
        void WireEnemyLists()
        {
            HealthComponent[] redArray  = _redTeam.ToArray();
            HealthComponent[] blueArray = _blueTeam.ToArray();
            foreach (BotController bot in _blueBots) bot.enemies = redArray;
            foreach (BotController bot in _redBots)  bot.enemies = blueArray;
        }

        // Shared factory: creates one bot GO with CharacterController, HealthComponent,
        // RespawnController, BotController, visual mesh, and debug HP label.
        BotController CreateBot(string botName, Team team, Vector3 spawnPos)
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

            // Team color for visual identification during testing.
            Renderer rend = visual.GetComponent<Renderer>();
            rend.material.color = team == Team.Blue
                ? new Color(0.3f, 0.3f, 1f)
                : new Color(1f, 0.3f, 0.3f);

            HealthComponent health = botGo.AddComponent<HealthComponent>();
            health.Initialize(team, CharacterStats.FromConfig(config));

            RespawnController respawn = botGo.AddComponent<RespawnController>();
            respawn.config = config;
            respawn.spawnPoint = spawnPos;

            BotController botCtrl = botGo.AddComponent<BotController>();
            botCtrl.config = config;
            botCtrl.botHealth = health;
            // enemies is set later by WireEnemyLists()

            // Debug HP label
            GameObject uiGo = new GameObject(botName + "_UI");
            HealthDebugUI ui = uiGo.AddComponent<HealthDebugUI>();
            ui.target = health;

            return botCtrl;
        }

        // Spreads N positions evenly along the X axis at the given Z depth.
        // count=1 → center (x=0). count=2, spread=3 → x = -1.5, +1.5. Etc.
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