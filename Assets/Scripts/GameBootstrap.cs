using UnityEngine;
using UnityEngine.Rendering.Universal;
using Game.Config;
using Game.Player;
using Game.Camera;
using Game.Input;
using Game.Combat;
using Game.UI;

namespace Game
{
    // Builds the scene at runtime from a single GameConfig reference.
    // Attach to an empty GameObject in an otherwise empty SampleScene.
    public class GameBootstrap : MonoBehaviour
    {
        public GameConfig config;

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
            CreateDummy();
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
            // --- PlayerEntity: body + physics ---
            GameObject playerGo = new GameObject("PlayerEntity");
            // center.y = height/2 -> capsule bottom sits exactly at transform.position (feet at y=0)
            playerGo.transform.position = Vector3.zero;

            CharacterController cc = playerGo.AddComponent<CharacterController>();
            cc.height = config.standHeight;
            cc.center = new Vector3(0f, config.standHeight * 0.5f, 0f);
            cc.radius = 0.4f;
            cc.slopeLimit = 45f;
            cc.stepOffset = 0.3f;

            playerGo.AddComponent<PlayerEntity>(); // Awake caches CharacterController

            FirstPersonMotor motor = playerGo.AddComponent<FirstPersonMotor>();
            // Awake on FirstPersonMotor ran and cached PlayerEntity via GetComponent.
            // config is set here; motor only reads it inside Tick() (called from Update).
            motor.config = config;

            HealthComponent playerHealth = playerGo.AddComponent<HealthComponent>();
            playerHealth.Initialize(Team.Blue, CharacterStats.FromConfig(config));

            // --- Camera: child of PlayerEntity so it moves with the body ---
            GameObject camGo = new GameObject("CameraRoot");
            camGo.transform.SetParent(playerGo.transform, false);
            camGo.transform.localPosition = new Vector3(0f, config.standCameraLocalY, 0f);

            UnityEngine.Camera cam = camGo.AddComponent<UnityEngine.Camera>();
            cam.fieldOfView = 75f;
            cam.nearClipPlane = 0.1f;
            cam.tag = "MainCamera";
            camGo.AddComponent<AudioListener>();
            camGo.AddComponent<UniversalAdditionalCameraData>(); // required by URP

            // --- LocalPlayer: input + camera controller (no physics, no collider) ---
            GameObject localPlayerGo = new GameObject("LocalPlayer");

            localPlayerGo.AddComponent<PlayerInputReader>();

            FirstPersonCamera fpsCamera = localPlayerGo.AddComponent<FirstPersonCamera>();
            fpsCamera.config = config;
            fpsCamera.playerBodyTransform = playerGo.transform;
            fpsCamera.cameraTransform = camGo.transform;

            LocalPlayerController controller = localPlayerGo.AddComponent<LocalPlayerController>();
            // LocalPlayerController.Start() is deferred; fields set here are ready in time.
            controller.playerEntity = playerGo.GetComponent<PlayerEntity>();
            controller.cameraTransform = camGo.transform;

            DebugAttackInput debugAttack = localPlayerGo.AddComponent<DebugAttackInput>();
            debugAttack.config = config;
            debugAttack.cameraTransform = camGo.transform;
            debugAttack.attackerHealth = playerHealth;
        }

        void CreateDummy()
        {
            // Stationary target for testing the damage pipeline.
            GameObject dummyGo = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            dummyGo.name = "DummyEnemy";
            // Capsule primitive: height=2, pivot at center. position.y=1 puts bottom at y=0.
            dummyGo.transform.position = new Vector3(0f, 1f, 5f);

            HealthComponent dummyHealth = dummyGo.AddComponent<HealthComponent>();
            dummyHealth.Initialize(Team.Red, CharacterStats.FromConfig(config));

            GameObject uiGo = new GameObject("DummyHealthUI");
            HealthDebugUI ui = uiGo.AddComponent<HealthDebugUI>();
            ui.target = dummyHealth;
        }

        void CreateCrosshairUI()
        {
            GameObject uiGo = new GameObject("CrosshairUI");
            uiGo.AddComponent<CrosshairUI>();
        }
    }
}