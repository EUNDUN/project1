using Game.Combat;
using Game.Config;
using Game.Input;
using UnityEngine;

namespace Game.Visuals
{
    // Local first-person weapon presentation only. It never participates in combat, physics, or targeting.
    public sealed class FirstPersonWeaponView : MonoBehaviour
    {
        private const string WarriorPath = "Weapons/warrior_sword_basic_01";
        private const string ArcherPath  = "Weapons/archer_cannon_basic_01";
        private const string RoguePath   = "Weapons/rogue_shuriken_basic_01";
        private const string MagePath    = "Weapons/mage_staff_basic_01";

        public GameConfig config;
        public HealthComponent ownerHealth;
        public Transform cameraTransform;

        private GameObject _root;
        private Transform _motionRoot;
        private GameObject _instance;
        private CombatClass _combatClass;
        private PlayerInputReader _inputReader;

        private float _idleTime;
        private float _moveBobTime;
        private float _attackMotionTime = 999f;
        private Vector2 _lookSway;
        private Vector2 _lastMoveInput;
        private Vector3 _lastCameraBobOffset;
        private Quaternion _lastCameraBobRotation = Quaternion.identity;

        void Start()
        {
            if (cameraTransform == null || ownerHealth == null)
            {
                Debug.LogWarning("[FirstPersonWeaponView] Missing cameraTransform or ownerHealth.");
                return;
            }

            _inputReader = GetComponent<PlayerInputReader>();
            _combatClass = ownerHealth.CombatClass;
            BuildWeapon(_combatClass);
        }

        void Update()
        {
            if (_root == null) return;

            if (config != null && config.debugLiveUpdateWeaponView)
                ApplyConfiguredTransform(_combatClass);

            UpdateViewMotion();
        }

        void LateUpdate()
        {
            UpdateCameraBob();
        }

        void OnDestroy()
        {
            RemoveCameraBob();
            if (_instance != null) Destroy(_instance);
            if (_root != null) Destroy(_root);
        }

        private void BuildWeapon(CombatClass combatClass)
        {
            _root = new GameObject("WeaponViewRoot");
            _root.transform.SetParent(cameraTransform, false);

            GameObject motionGo = new GameObject("WeaponMotionRoot");
            _motionRoot = motionGo.transform;
            _motionRoot.SetParent(_root.transform, false);

            GameObject prefab = Resources.Load<GameObject>(GetResourcePath(combatClass));
            if (prefab == null)
            {
                BuildFallback(combatClass);
                ApplyConfiguredTransform(combatClass);
                return;
            }

            _instance = Instantiate(prefab, _motionRoot);
            _instance.name = combatClass + "WeaponView";
            _instance.transform.localPosition = Vector3.zero;
            _instance.transform.localRotation = Quaternion.identity;
            DisableColliders(_instance.transform);
            ApplyConfiguredTransform(combatClass);
        }

        private void ApplyConfiguredTransform(CombatClass combatClass)
        {
            _root.transform.localPosition = GetRootPosition(combatClass);
            _root.transform.localRotation = Quaternion.Euler(GetRootEuler(combatClass));
            _root.transform.localScale = Vector3.one;

            if (_instance != null)
            {
                _instance.transform.localScale = Vector3.one * GetModelScale(combatClass);
            }
        }

        private void UpdateViewMotion()
        {
            if (_motionRoot == null) return;

            float dt = Time.deltaTime;
            _idleTime += dt;

            Vector2 moveInput = Vector2.zero;
            Vector2 lookInput = Vector2.zero;
            bool attackPressed = false;

            if (_inputReader != null)
            {
                var cmd = _inputReader.Read();
                moveInput = cmd.MoveInput;
                lookInput = cmd.LookInput;
                attackPressed = cmd.AttackPressed;
            }

            _lastMoveInput = moveInput;

            if (attackPressed)
                _attackMotionTime = 0f;

            float moveAmount = Mathf.Clamp01(moveInput.magnitude);
            if (moveAmount > 0.01f)
                _moveBobTime += dt * MoveBobSpeed();
            else
                _moveBobTime = Mathf.Lerp(_moveBobTime, 0f, dt * 6f);

            Vector2 lookTarget = new Vector2(-lookInput.x, -lookInput.y) * LookSwayAmount();
            _lookSway = Vector2.Lerp(_lookSway, lookTarget, dt * 12f);

            Vector3 pos = Vector3.zero;
            Vector3 euler = Vector3.zero;

            float idleBob = Mathf.Sin(_idleTime * IdleBobSpeed()) * IdleBobAmount();
            pos.y += idleBob;

            if (moveAmount > 0.01f)
            {
                float walkX = Mathf.Sin(_moveBobTime) * MoveBobXAmount() * moveAmount;
                float walkY = Mathf.Abs(Mathf.Cos(_moveBobTime)) * MoveBobYAmount() * moveAmount;
                pos.x += walkX;
                pos.y += walkY;
                euler.z += -walkX * 60f;
            }

            pos.x += _lookSway.x;
            pos.y += _lookSway.y;
            euler.y += _lookSway.x * LookSwayRotation() * 100f;
            euler.x += -_lookSway.y * LookSwayRotation() * 100f;

            if (_combatClass == CombatClass.Archer)
                ApplyGunRecoil(ref pos, ref euler, dt);
            else
                ApplyMeleeSwing(ref pos, ref euler, dt);

            if (config != null && !config.weaponViewMotionEnabled)
            {
                pos = Vector3.zero;
                euler = Vector3.zero;
            }

            _motionRoot.localPosition = pos;
            _motionRoot.localRotation = Quaternion.Euler(euler);
            _motionRoot.localScale = Vector3.one;
        }

        private void ApplyMeleeSwing(ref Vector3 pos, ref Vector3 euler, float dt)
        {
            float duration = SwingDuration();
            if (_attackMotionTime >= duration) return;

            _attackMotionTime += dt;
            float t = Mathf.Clamp01(_attackMotionTime / duration);
            float swing = Mathf.Sin(t * Mathf.PI);
            float swingArc = Mathf.Sin(Mathf.Sqrt(t) * Mathf.PI);
            float posAmount = SwingPositionAmount();
            float rotAmount = SwingRotationAmount();

            pos.x += -posAmount * 0.45f * swing;
            pos.y += -posAmount * 0.65f * swingArc;
            pos.z += -posAmount * 0.30f * swing;

            euler.x += -rotAmount * 0.65f * swingArc;
            euler.y +=  rotAmount * 0.25f * swing;
            euler.z += -rotAmount * swing;
        }

        private void ApplyGunRecoil(ref Vector3 pos, ref Vector3 euler, float dt)
        {
            float duration = GunRecoilDuration();
            if (_attackMotionTime >= duration) return;

            _attackMotionTime += dt;
            float t = Mathf.Clamp01(_attackMotionTime / duration);
            float kick = 1f - t;
            float returnEase = kick * kick;
            float posAmount = GunRecoilPositionAmount();
            float rotAmount = GunRecoilRotationAmount();

            pos.z += -posAmount * returnEase;
            pos.y +=  posAmount * 0.22f * returnEase;
            pos.x +=  Mathf.Sin(t * Mathf.PI) * posAmount * 0.08f;

            euler.x += -rotAmount * returnEase;
            euler.y +=  rotAmount * 0.12f * Mathf.Sin(t * Mathf.PI);
            euler.z +=  rotAmount * 0.08f * Mathf.Sin(t * Mathf.PI);
        }

        private void UpdateCameraBob()
        {
            if (cameraTransform == null) return;
            RemoveCameraBob();

            if (config != null && (!config.weaponViewMotionEnabled || !config.cameraViewBobEnabled)) return;

            float moveAmount = Mathf.Clamp01(_lastMoveInput.magnitude);
            if (moveAmount <= 0.01f) return;

            float y = Mathf.Abs(Mathf.Cos(_moveBobTime)) * CameraBobAmount() * moveAmount;
            float x = Mathf.Sin(_moveBobTime) * CameraBobSideAmount() * moveAmount;
            float roll = -Mathf.Sin(_moveBobTime) * CameraBobRollAmount() * moveAmount;

            _lastCameraBobOffset = new Vector3(x, y, 0f);
            _lastCameraBobRotation = Quaternion.Euler(0f, 0f, roll);

            cameraTransform.localPosition += _lastCameraBobOffset;
            cameraTransform.localRotation *= _lastCameraBobRotation;
        }

        private void RemoveCameraBob()
        {
            if (cameraTransform == null) return;

            if (_lastCameraBobOffset != Vector3.zero)
            {
                cameraTransform.localPosition -= _lastCameraBobOffset;
                _lastCameraBobOffset = Vector3.zero;
            }

            if (_lastCameraBobRotation != Quaternion.identity)
            {
                cameraTransform.localRotation *= Quaternion.Inverse(_lastCameraBobRotation);
                _lastCameraBobRotation = Quaternion.identity;
            }
        }

        private Vector3 GetRootPosition(CombatClass combatClass)
        {
            if (config == null) return DefaultRootPosition(combatClass);

            return combatClass switch
            {
                CombatClass.Archer => config.archerWeaponViewLocalPosition,
                CombatClass.Rogue  => config.rogueWeaponViewLocalPosition,
                CombatClass.Mage   => config.mageWeaponViewLocalPosition,
                _                  => config.warriorWeaponViewLocalPosition,
            };
        }

        private Vector3 GetRootEuler(CombatClass combatClass)
        {
            if (config == null) return DefaultRootEuler(combatClass);

            return combatClass switch
            {
                CombatClass.Archer => config.archerWeaponViewLocalEuler,
                CombatClass.Rogue  => config.rogueWeaponViewLocalEuler,
                CombatClass.Mage   => config.mageWeaponViewLocalEuler,
                _                  => config.warriorWeaponViewLocalEuler,
            };
        }

        private static Vector3 DefaultRootPosition(CombatClass combatClass) => combatClass switch
        {
            CombatClass.Archer => new Vector3(0.45f, -0.35f, 0.75f),
            CombatClass.Rogue  => new Vector3(0.45f, -0.35f, 0.75f),
            CombatClass.Mage   => new Vector3(0.45f, -0.35f, 0.75f),
            _                  => new Vector3(0.45f, -0.35f, 0.75f),
        };

        private static Vector3 DefaultRootEuler(CombatClass combatClass) => combatClass switch
        {
            CombatClass.Archer => new Vector3(10f, 180f, 0f),
            CombatClass.Rogue  => new Vector3(10f, 180f, 0f),
            CombatClass.Mage   => new Vector3(10f, 180f, 0f),
            _                  => new Vector3(10f, 180f, 0f),
        };

        private float GetModelScale(CombatClass combatClass)
        {
            float configured = 0f;
            if (config != null)
            {
                configured = combatClass switch
                {
                    CombatClass.Archer => config.archerWeaponViewScale,
                    CombatClass.Rogue  => config.rogueWeaponViewScale,
                    CombatClass.Mage   => config.mageWeaponViewScale,
                    _                  => config.warriorWeaponViewScale,
                };
            }

            if (configured > 0f) return configured;

            return combatClass switch
            {
                CombatClass.Archer => 0.0025f,
                CombatClass.Rogue  => 0.02f,
                CombatClass.Mage   => 0.01f,
                _                  => 0.015f,
            };
        }

        private float IdleBobSpeed() => config != null && config.weaponViewIdleBobSpeed > 0f ? config.weaponViewIdleBobSpeed : 1.6f;
        private float IdleBobAmount() => config != null ? config.weaponViewIdleBobAmount : 0.012f;
        private float MoveBobSpeed() => config != null && config.weaponViewMoveBobSpeed > 0f ? config.weaponViewMoveBobSpeed : 8f;
        private float MoveBobXAmount() => config != null ? config.weaponViewMoveBobXAmount : 0.035f;
        private float MoveBobYAmount() => config != null ? config.weaponViewMoveBobYAmount : 0.045f;
        private float LookSwayAmount() => config != null ? config.weaponViewLookSwayAmount : 0.012f;
        private float LookSwayRotation() => config != null ? config.weaponViewLookSwayRotation : 2.5f;
        private float SwingDuration() => config != null && config.weaponViewSwingDuration > 0f ? config.weaponViewSwingDuration : 0.28f;
        private float SwingPositionAmount() => config != null ? config.weaponViewSwingPositionAmount : 0.16f;
        private float SwingRotationAmount() => config != null ? config.weaponViewSwingRotationAmount : 55f;
        private float GunRecoilDuration() => config != null && config.weaponViewGunRecoilDuration > 0f ? config.weaponViewGunRecoilDuration : 0.12f;
        private float GunRecoilPositionAmount() => config != null ? config.weaponViewGunRecoilPositionAmount : 0.10f;
        private float GunRecoilRotationAmount() => config != null ? config.weaponViewGunRecoilRotationAmount : 9f;
        private float CameraBobAmount() => config != null ? config.cameraViewBobAmount : 0.035f;
        private float CameraBobSideAmount() => config != null ? config.cameraViewBobSideAmount : 0.018f;
        private float CameraBobRollAmount() => config != null ? config.cameraViewBobRollAmount : 0.8f;

        private static string GetResourcePath(CombatClass combatClass) => combatClass switch
        {
            CombatClass.Archer => ArcherPath,
            CombatClass.Rogue  => RoguePath,
            CombatClass.Mage   => MagePath,
            _                  => WarriorPath,
        };

        private void BuildFallback(CombatClass combatClass)
        {
            _instance = GameObject.CreatePrimitive(PrimitiveType.Cube);
            _instance.name = combatClass + "WeaponFallback";
            _instance.transform.SetParent(_motionRoot, false);
            _instance.transform.localPosition = Vector3.zero;
            _instance.transform.localRotation = Quaternion.identity;
            _instance.transform.localScale = Vector3.one * GetModelScale(combatClass);
            DisableColliders(_instance.transform);

            Renderer renderer = _instance.GetComponent<Renderer>();
            if (renderer != null) renderer.sharedMaterial = GetFallbackMaterial(combatClass);
        }

        private static void DisableColliders(Transform root)
        {
            Collider[] colliders = root.GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < colliders.Length; i++) colliders[i].enabled = false;
        }

        private static Material GetFallbackMaterial(CombatClass combatClass)
        {
            Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.color = combatClass switch
            {
                CombatClass.Archer => new Color(1f, 0.8f, 0.2f),
                CombatClass.Rogue  => new Color(0.6f, 0.2f, 1f),
                CombatClass.Mage   => new Color(0.2f, 0.7f, 1f),
                _                  => new Color(0.9f, 0.9f, 0.9f),
            };
            return mat;
        }
    }
}