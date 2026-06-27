using UnityEngine;
using Game.Combat;
using Game.Commands;
using Game.Config;

namespace Game.Player
{
    // Designed to be called from a controller (local or server-replay).
    [RequireComponent(typeof(PlayerEntity))]
    public class FirstPersonMotor : MonoBehaviour
    {
        public GameConfig config;

        // Set by ability handlers (e.g. Rogue stealth). Multiplies WASD speed; ignored during dash.
        public float MoveSpeedMultiplier = 1f;

        // Debug instrumentation — written every Tick, read by MotorDebugUI every 0.15 s.
        // NonSerialized keeps them out of the Inspector and the scene file.
        [System.NonSerialized] public Vector2 DebugMoveInput;
        [System.NonSerialized] public float   DebugBaseSpeed;
        [System.NonSerialized] public float   DebugMotorMoveMultiplier;
        [System.NonSerialized] public float   DebugDebuffMultiplier;
        [System.NonSerialized] public float   DebugSelfMoveMultiplier;
        [System.NonSerialized] public float   DebugFinalSpeed;
        [System.NonSerialized] public Vector3 DebugDashVelocity;
        [System.NonSerialized] public Vector3 DebugMotion;

        private PlayerEntity    _entity;
        private float           _verticalVelocity;
        private HealthComponent _healthComponent; // cached for debuff-layer speed read

        void Awake()
        {
            _entity          = GetComponent<PlayerEntity>();
            _healthComponent = GetComponent<HealthComponent>(); // null on non-combat entities
        }

        // dashVelocity: when non-zero, replaces WASD horizontal movement for this frame.
        // Gravity, jump, and crouch still apply normally.
        public void Tick(PlayerCommand cmd, Vector3 dashVelocity = default)
        {
            HandleCrouch(cmd.CrouchHeld);
            HandleJump(cmd.JumpPressed);
            // Skip gravity accumulation during forced vertical movement (warrior Q self-movement).
            if (dashVelocity.y == 0f) ApplyGravity();
            ApplyMovement(cmd.MoveInput, dashVelocity);
        }

        // Called by RespawnController after teleport so stale velocity does not jerk on first Tick.
        public void ResetState()
        {
            _verticalVelocity  = 0f;
            _entity.IsGrounded = false;
            MoveSpeedMultiplier = 1f;
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
            // A launch impulse (e.g. warrior Q slam) overrides vertical velocity for this frame.
            // Gravity then decelerates normally, giving air time = 2 * impulse / |gravity|.
            float impulse = _healthComponent != null ? _healthComponent.ConsumeLaunchImpulse() : 0f;
            if (impulse > 0f) { _verticalVelocity = impulse; return; }

            if (_entity.IsGrounded && _verticalVelocity < 0f)
                _verticalVelocity = -2f; // keeps isGrounded stable on slopes
            else
                _verticalVelocity += config.gravity * Time.deltaTime;
        }

        void ApplyMovement(Vector2 moveInput, Vector3 dashVelocity)
        {
            DebugMoveInput    = moveInput;
            DebugDashVelocity = dashVelocity;

            Vector3 horizontal;
            if (dashVelocity.sqrMagnitude > 0.001f)
            {
                // Forced move overrides WASD. XZ and Y are separated so vertical can be
                // handled independently of gravity-driven _verticalVelocity.
                horizontal = new Vector3(dashVelocity.x, 0f, dashVelocity.z);
                // Speed multipliers don't apply in dash path — zero them so the panel is unambiguous.
                DebugBaseSpeed           = 0f;
                DebugMotorMoveMultiplier = 0f;
                DebugDebuffMultiplier    = 0f;
                DebugSelfMoveMultiplier  = 0f;
                DebugFinalSpeed          = horizontal.magnitude;
            }
            else
            {
                // MoveSpeedMultiplier = motor ability layer (Rogue stealth / F3 buff).
                // debuffMult          = HC enemy-debuff layer (Rogue Q slow, etc.)
                // selfMult            = HC self-ability layer (warrior guard penalty, etc.)
                float debuffMult = _healthComponent != null ? _healthComponent.MoveSpeedMultiplier     : 1f;
                float selfMult   = _healthComponent != null ? _healthComponent.SelfMoveSpeedMultiplier : 1f;
                float baseSpeed  = _entity.IsCrouching ? config.crouchSpeed : config.moveSpeed;
                float speed      = baseSpeed * MoveSpeedMultiplier * debuffMult * selfMult;
                horizontal = (transform.right * moveInput.x + transform.forward * moveInput.y).normalized * speed;

                DebugBaseSpeed           = baseSpeed;
                DebugMotorMoveMultiplier = MoveSpeedMultiplier;
                DebugDebuffMultiplier    = debuffMult;
                DebugSelfMoveMultiplier  = selfMult;
                DebugFinalSpeed          = speed;
            }

            // When forced move carries a Y component (warrior Q leap/slam), it overrides
            // gravity-driven _verticalVelocity. _verticalVelocity is synced so the transition
            // back to normal gravity is smooth once the forced phase ends.
            float effectiveY;
            if (dashVelocity.y != 0f) { effectiveY = dashVelocity.y; _verticalVelocity = dashVelocity.y; }
            else                       { effectiveY = _verticalVelocity; }

            Vector3 kb     = _healthComponent != null ? _healthComponent.KnockbackVelocity : Vector3.zero;
            Vector3 pull   = _healthComponent != null ? _healthComponent.PullVelocity       : Vector3.zero;
            Vector3 motion = horizontal + Vector3.up * effectiveY + kb + pull;

            DebugMotion = motion;

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