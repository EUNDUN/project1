using UnityEngine;
using Game.Commands;
using Game.Config;

namespace Game.Player
{
    // Designed to be called from a controller (local or server-replay).
    [RequireComponent(typeof(PlayerEntity))]
    public class FirstPersonMotor : MonoBehaviour
    {
        public GameConfig config;

        private PlayerEntity _entity;
        private float _verticalVelocity;

        void Awake()
        {
            _entity = GetComponent<PlayerEntity>();
        }

        public void Tick(PlayerCommand cmd)
        {
            HandleCrouch(cmd.CrouchHeld);
            HandleJump(cmd.JumpPressed);
            ApplyGravity();
            ApplyMovement(cmd.MoveInput);
        }

        void HandleCrouch(bool crouchHeld)
        {
            if (_entity.IsCrouching == crouchHeld) return;

            if (!crouchHeld)
            {
                // Block standing if something is overhead
                float r = _entity.Controller.radius;
                Vector3 topSphere = transform.position + Vector3.up * (config.standHeight - r);
                if (Physics.CheckSphere(topSphere, r, ~0, QueryTriggerInteraction.Ignore))
                    return;
            }

            _entity.IsCrouching = crouchHeld;
        }

        void HandleJump(bool jumpPressed)
        {
            if (jumpPressed && _entity.IsGrounded && !_entity.IsCrouching)
                _verticalVelocity = Mathf.Sqrt(2f * config.jumpHeight * Mathf.Abs(config.gravity));
        }

        void ApplyGravity()
        {
            if (_entity.IsGrounded && _verticalVelocity < 0f)
                _verticalVelocity = -2f; // keeps isGrounded stable on slopes
            else
                _verticalVelocity += config.gravity * Time.deltaTime;
        }

        void ApplyMovement(Vector2 moveInput)
        {
            float speed = _entity.IsCrouching ? config.crouchSpeed : config.moveSpeed;
            Vector3 horizontal = (transform.right * moveInput.x + transform.forward * moveInput.y).normalized * speed;
            Vector3 motion = horizontal + Vector3.up * _verticalVelocity;

            _entity.Controller.Move(motion * Time.deltaTime);
            _entity.IsGrounded = _entity.Controller.isGrounded;
            _entity.Velocity = motion;

            float targetHeight = _entity.IsCrouching ? config.crouchHeight : config.standHeight;
            float t = config.crouchTransitionSpeed * Time.deltaTime;
            _entity.Controller.height = Mathf.Lerp(_entity.Controller.height, targetHeight, t);
            Vector3 center = _entity.Controller.center;
            center.y = Mathf.Lerp(center.y, targetHeight * 0.5f, t);
            _entity.Controller.center = center;
        }
    }
}
