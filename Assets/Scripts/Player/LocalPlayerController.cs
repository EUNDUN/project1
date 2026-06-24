using UnityEngine;
using Game.Commands;
using Game.Input;
using Game.Camera;
using Game.Combat;

namespace Game.Player
{
    // Remote players have PlayerEntity + FirstPersonMotor but NOT this component.
    public class LocalPlayerController : MonoBehaviour
    {
        public PlayerEntity playerEntity;
        public Transform cameraTransform;

        private PlayerInputReader _inputReader;
        private FirstPersonMotor _motor;
        private FirstPersonCamera _fpsCamera;
        private BasicAttackController _attack;
        private HealthComponent _playerHealth;
        private bool _cursorLocked;
        private bool _isDead;

        void Start()
        {
            _inputReader = GetComponent<PlayerInputReader>();
            _fpsCamera   = GetComponent<FirstPersonCamera>();
            _attack      = GetComponent<BasicAttackController>();
            _motor       = playerEntity != null ? playerEntity.GetComponent<FirstPersonMotor>() : null;

            if (playerEntity == null || _inputReader == null || _motor == null
                || _fpsCamera == null || _attack == null)
            {
                Debug.LogError("[LocalPlayerController] Missing required component or reference. Disabling.", this);
                enabled = false;
                return;
            }

            _playerHealth = playerEntity.GetComponent<HealthComponent>();
            if (_playerHealth != null)
            {
                _playerHealth.OnDeath     += OnPlayerDied;
                _playerHealth.OnRespawned += OnPlayerRespawned;
            }

            LockCursor(true);
        }

        void OnDestroy()
        {
            if (_playerHealth != null)
            {
                _playerHealth.OnDeath     -= OnPlayerDied;
                _playerHealth.OnRespawned -= OnPlayerRespawned;
            }
        }

        void Update()
        {
            if (UnityEngine.Input.GetKeyDown(KeyCode.Escape))
                LockCursor(!_cursorLocked);

            PlayerCommand cmd = _inputReader.Read();

            // Camera works even when dead — player can look around while waiting to respawn.
            if (_cursorLocked)
                _fpsCamera.Tick(cmd, playerEntity.IsCrouching);

            // Movement and attack are blocked while dead.
            if (!_isDead)
            {
                if (_cursorLocked)
                    _attack.Tick(cmd);
                _motor.Tick(cmd);
            }
        }

        void LockCursor(bool locked)
        {
            _cursorLocked = locked;
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }

        void OnPlayerDied(HealthComponent hc)      => _isDead = true;
        void OnPlayerRespawned(HealthComponent hc) => _isDead = false;
    }
}