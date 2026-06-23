using UnityEngine;
using Game.Commands;
using Game.Config;

namespace Game.Camera
{
    // playerBodyTransform rotates on Y (yaw). cameraTransform rotates on X (pitch).
    // Remote players do not get this component.
    public class FirstPersonCamera : MonoBehaviour
    {
        public GameConfig config;
        public Transform playerBodyTransform;
        public Transform cameraTransform;

        private float _pitch;
        private bool _ready;

        void Start()
        {
            _ready = config != null && playerBodyTransform != null && cameraTransform != null;
            if (!_ready)
                Debug.LogError("[FirstPersonCamera] Missing required reference. Camera will not function.", this);
        }

        public void Tick(PlayerCommand cmd, bool isCrouching)
        {
            if (!_ready) return;

            float mouseX = cmd.LookInput.x * config.mouseSensitivity;
            float mouseY = cmd.LookInput.y * config.mouseSensitivity;

            _pitch = Mathf.Clamp(_pitch - mouseY, -config.maxPitchAngle, config.maxPitchAngle);
            cameraTransform.localRotation = Quaternion.Euler(_pitch, 0f, 0f);
            playerBodyTransform.Rotate(Vector3.up * mouseX);

            float targetY = isCrouching ? config.crouchCameraLocalY : config.standCameraLocalY;
            Vector3 lp = cameraTransform.localPosition;
            lp.y = Mathf.Lerp(lp.y, targetY, config.crouchTransitionSpeed * Time.deltaTime);
            cameraTransform.localPosition = lp;
        }
    }
}