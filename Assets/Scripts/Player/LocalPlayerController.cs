using UnityEngine;
using Game.Commands;
using Game.Input;
using Game.Camera;

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
        private bool _cursorLocked;

        void Start()
        {
            _inputReader = GetComponent<PlayerInputReader>();
            _fpsCamera = GetComponent<FirstPersonCamera>();
            _motor = playerEntity != null ? playerEntity.GetComponent<FirstPersonMotor>() : null;

            if (playerEntity == null || _inputReader == null || _motor == null || _fpsCamera == null)
            {
                Debug.LogError("[LocalPlayerController] Missing required component or reference. Disabling.", this);
                enabled = false;
                return;
            }

            LockCursor(true);
        }

        void Update()
        {
            if (UnityEngine.Input.GetKeyDown(KeyCode.Escape))
                LockCursor(!_cursorLocked);

            PlayerCommand cmd = _inputReader.Read();

            if (_cursorLocked)
                _fpsCamera.Tick(cmd, playerEntity.IsCrouching);

            _motor.Tick(cmd);
        }

        void LockCursor(bool locked)
        {
            _cursorLocked = locked;
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }
    }
}