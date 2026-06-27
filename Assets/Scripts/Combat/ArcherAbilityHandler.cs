using UnityEngine;
using Game.Config;
using Game.Commands;

namespace Game.Combat
{
    // Handles Archer-specific ability logic:
    //   RC  — Rapid Fire attack-speed buff
    //   Q   — Enhanced Shot type cycling (Shock/Fire/Ice)
    //   E   — Predictive Shield
    //   F   — Barrage Gauge: charge by hitting enemies, spend to rapid-fire while held
    //   Z   — Roll: camera-relative dash with cooldown refund on basic hits
    //   R   — Overdrive: move speed + attack speed surge; Basic shots become AoE explosive
    //
    // Follows the same Init/TickTimers/TryActivate/HandleOwnerDeath/ForceCleanup contract
    // as WarriorAbilityHandler, RogueAbilityHandler, and MageAbilityHandler.
    public class ArcherAbilityHandler : MonoBehaviour
    {
        private AbilityController _ac;
        private GameConfig        _config;
        private HealthComponent   _ownerHealth;
        private Transform         _cameraTransform;

        // ── RC — Rapid Fire buff ──────────────────────────────────────────────────
        private bool  _isRapidFiring  = false;
        private float _rapidFireTimer = 0f;

        public bool  IsRapidFiring  => _isRapidFiring;
        public float RapidFireTimer => _rapidFireTimer;

        // Combined attack speed multiplier: RC × Overdrive (each contributes 1f when inactive).
        public float AttackSpeedMultiplier =>
            (_isRapidFiring ? _config.archerRapidFireAttackSpeedMultiplier : 1f) *
            (_isOverdrive   ? _config.archerOverdriveAttackSpeedMultiplier  : 1f);

        // ── R — Overdrive ─────────────────────────────────────────────────────────
        private bool  _isOverdrive    = false;
        private float _overdriveTimer = 0f;

        public bool  ArcherIsOverdrive    => _isOverdrive;
        public float ArcherOverdriveTimer => _overdriveTimer;

        // ── Q — Enhanced Shot (pending type) ──────────────────────────────────────
        private ArcherShotType _pendingShotType = ArcherShotType.Basic;

        public bool           HasPendingShotType => _pendingShotType != ArcherShotType.Basic;
        public ArcherShotType PendingShotType    => _pendingShotType;

        public ArcherShotType ConsumePendingShotType()
        {
            var t = _pendingShotType;
            _pendingShotType = ArcherShotType.Basic;
            // Enhanced shots (Q-selected) start the Q cooldown on consumption, not on Q press.
            // This lets the player pre-select a type then lock it in by firing.
            if (t == ArcherShotType.Shock || t == ArcherShotType.Fire || t == ArcherShotType.Ice)
                _ac.SetCooldown(AbilitySlot.Q, _ac.AbilityConfig.QCooldown);
            return t;
        }

        // ── E — Predictive Shield ─────────────────────────────────────────────────
        private bool       _isShielded   = false;
        private float      _shieldTimer  = 0f;
        private GameObject _shieldVisual;
        private static Material s_shieldMat;

        public bool  IsShielded  => _isShielded;
        public float ShieldTimer => _shieldTimer;

        // ── Z — Roll ──────────────────────────────────────────────────────────────
        // _cachedMoveInput: this frame's cmd.MoveInput (set in TickHeldInputs which runs before
        //   TryActivate in AbilityController.Tick — see that file for call order).
        // _lastMoveInput:   last frame that had any non-zero move input; fallback when stationary.
        private Vector2 _cachedMoveInput = Vector2.zero;
        private Vector2 _lastMoveInput   = Vector2.zero;

        // ── F — Barrage Gauge ─────────────────────────────────────────────────────
        // _fGauge is preserved on death — the player keeps their gauge between lives.
        // _isFiring and the move speed penalty are cleared on death/cleanup.
        private float _fGauge     = 0f;
        private bool  _isFiring   = false;
        private float _fFireTimer = 0f;

        public float ArcherFGauge    => _fGauge;
        public float ArcherFMaxGauge => _config != null ? _config.archerFMaxGauge : 100f;
        public bool  ArcherIsFiring  => _isFiring;

        // Called by AbilityController.AddArcherFGauge() from projectile hit callbacks.
        public void AddBarrageGauge(float amount)
        {
            _fGauge = Mathf.Min(_fGauge + amount, _config.archerFMaxGauge);
        }

        // ── Lifecycle ─────────────────────────────────────────────────────────────
        public void Init(AbilityController ac, GameConfig config,
                         HealthComponent ownerHealth, Transform cameraTransform)
        {
            _ac              = ac;
            _config          = config;
            _ownerHealth     = ownerHealth;
            _cameraTransform = cameraTransform;
            _fGauge          = config.archerFMaxGauge; // start at 100% for playability
            _ownerHealth.OnShieldConsumed += HandleShieldConsumed;
        }

        public void TickTimers(float dt)
        {
            if (_isRapidFiring)
            {
                _rapidFireTimer -= dt;
                if (_rapidFireTimer <= 0f)
                {
                    _isRapidFiring  = false;
                    _rapidFireTimer = 0f;
                }
            }

            if (_isShielded)
            {
                _shieldTimer -= dt;
                if (_shieldTimer <= 0f)
                    EndShield();
            }

            if (_isOverdrive)
            {
                _overdriveTimer -= dt;
                if (_overdriveTimer <= 0f)
                {
                    _isOverdrive    = false;
                    _overdriveTimer = 0f;
                    // HC move-speed buff expires on its own via the duration set at activation.
                }
            }

            // If F is firing but the owner is now dead or stunned, end barrage immediately.
            // TickHeldInputs() is only called from AbilityController.Tick(cmd), which
            // LocalPlayerController may skip during stun/death — so the cleanup guard lives here.
            if (_isFiring && (_ownerHealth.IsDead || _ownerHealth.IsStunned))
                EndFBarrage();
        }

        // Called from AbilityController.Tick(cmd) — must run BEFORE TryActivate(Z) so that
        // _cachedMoveInput holds this frame's direction when TryBeginRoll() reads it.
        // See AbilityController.Tick() for the call ordering that guarantees this.
        public void TickHeldInputs(PlayerCommand cmd)
        {
            // Cache move input for Z roll direction.
            _cachedMoveInput = cmd.MoveInput;
            if (cmd.MoveInput.sqrMagnitude >= 0.01f)
                _lastMoveInput = cmd.MoveInput;

            float dt       = Time.deltaTime;
            bool  wantFire = cmd.SkillFHeld && !_ownerHealth.IsDead && !_ownerHealth.IsStunned;

            if (!_isFiring && wantFire && _fGauge >= _config.archerFMinStartGauge)
                StartFBarrage();
            else if (_isFiring && !wantFire)
                EndFBarrage();

            if (!_isFiring) return;

            // Drain gauge each frame while firing.
            _fGauge -= _config.archerFDrainPerSecond * dt;
            if (_fGauge <= 0f)
            {
                _fGauge = 0f;
                EndFBarrage();
                return;
            }

            // Fixed 0.1s fire interval — independent of RC attack speed buff.
            _fFireTimer -= dt;
            if (_fFireTimer <= 0f)
            {
                _fFireTimer += _config.archerFFireInterval;
                FireBarrageShot();
            }
        }

        public bool TryActivate(AbilitySlot slot)
        {
            if (slot == AbilitySlot.RightClick) return TryBeginRapidFire();
            if (slot == AbilitySlot.Q)          return TryBeginQ();
            if (slot == AbilitySlot.E)          return TryBeginShield();
            if (slot == AbilitySlot.Z)          return TryBeginRoll();
            if (slot == AbilitySlot.R)          return TryBeginOverdrive();
            // F is a held skill handled by TickHeldInputs — not routed through TryActivate.
            return false;
        }

        public void HandleOwnerDeath()
        {
            CancelRapidFire();
            CancelOverdrive();
            EndShield();
            _pendingShotType = ArcherShotType.Basic;
            // Stop firing; _fGauge is intentionally kept (spec: gauge persists through death).
            EndFBarrage();
            // Reset roll direction memory so the next life starts with a clean directional state.
            _cachedMoveInput = Vector2.zero;
            _lastMoveInput   = Vector2.zero;
        }

        public void ForceCleanup()
        {
            if (_ownerHealth != null)
                _ownerHealth.OnShieldConsumed -= HandleShieldConsumed;
            CancelRapidFire();
            CancelOverdrive();
            EndShield();
            _pendingShotType = ArcherShotType.Basic;
            EndFBarrage();
            // _fGauge preserved per spec ("게이지는 플레이 세션 정책에 맞게 유지해도 된다").
        }

        // ── Q — Enhanced Shot ─────────────────────────────────────────────────────
        // Cycles pending shot type: Basic→Shock→Fire→Ice→Shock (wraps at Ice).
        // Cooldown starts when the selected shot is CONSUMED (fired), not on press.
        // Blocked while Q cooldown is active (i.e., after the previous enhanced shot lands).
        private bool TryBeginQ()
        {
            if (_ownerHealth.IsDead || _ownerHealth.IsStunned) return false;
            if (_ac.GetCooldownRemaining(AbilitySlot.Q) > 0f) return false;
            _pendingShotType = _pendingShotType switch
            {
                ArcherShotType.Basic => ArcherShotType.Shock,
                ArcherShotType.Shock => ArcherShotType.Fire,
                ArcherShotType.Fire  => ArcherShotType.Ice,
                _                    => ArcherShotType.Shock, // Ice wraps back to Shock
            };
            return true;
        }

        // ── Z — Roll ──────────────────────────────────────────────────────────────
        private bool TryBeginRoll()
        {
            if (_ownerHealth.IsDead || _ownerHealth.IsStunned)          return false;
            if (_ac.GetCooldownRemaining(AbilitySlot.Z) > 0f)           return false;

            // Direction priority: current frame input → last valid input → camera backward.
            Vector2 input = _cachedMoveInput.sqrMagnitude >= 0.01f ? _cachedMoveInput
                          : _lastMoveInput.sqrMagnitude   >= 0.01f ? _lastMoveInput
                          : new Vector2(0f, -1f);

            Vector3 camFwd   = _cameraTransform.forward;  camFwd.y   = 0f;
            Vector3 camRight = _cameraTransform.right;    camRight.y = 0f;
            Vector3 worldDir = (camFwd.normalized * input.y + camRight.normalized * input.x).normalized;

            // Fallback: if camera is aimed straight up/down, roll backward in world space.
            if (worldDir.sqrMagnitude < 0.001f)
                worldDir = -_ownerHealth.transform.forward;

            float speed = _config.archerRollDistance / _config.archerRollDuration;
            _ac.StartDash(worldDir * speed, _config.archerRollDuration);
            _ac.SetCooldown(AbilitySlot.Z, _config.archerRollCooldown);

            // Gauge reward — capped at max; only granted when roll actually starts.
            AddBarrageGauge(_config.archerRollGaugeGain);
            return true;
        }

        // ── R — Overdrive ─────────────────────────────────────────────────────────
        private bool TryBeginOverdrive()
        {
            if (_ownerHealth.IsDead || _ownerHealth.IsStunned) return false;
            if (_ac.GetCooldownRemaining(AbilitySlot.R) > 0f)  return false;
            if (_isOverdrive)                                   return false;

            _isOverdrive    = true;
            _overdriveTimer = _config.archerOverdriveDuration;
            _ownerHealth.SetSelfMoveSpeedBuff(SelfMoveSpeedSource.ArcherOverdrive,
                _config.archerOverdriveMoveSpeedMultiplier,
                _config.archerOverdriveDuration);
            _ac.SetCooldown(AbilitySlot.R, _config.archerOverdriveCooldown);
            return true;
        }

        private void CancelOverdrive()
        {
            if (!_isOverdrive) return;
            _isOverdrive    = false;
            _overdriveTimer = 0f;
            _ownerHealth.ClearSelfMoveSpeedBuff(SelfMoveSpeedSource.ArcherOverdrive);
        }

        // ── RC — Rapid Fire ───────────────────────────────────────────────────────
        private bool TryBeginRapidFire()
        {
            if (_isRapidFiring) return false;
            if (_ac.GetCooldownRemaining(AbilitySlot.RightClick) > 0f) return false;
            if (_ownerHealth.IsDead || _ownerHealth.IsStunned)          return false;

            _isRapidFiring  = true;
            _rapidFireTimer = _config.archerRapidFireDuration;

            _ownerHealth.SetSelfMoveSpeedBuff(SelfMoveSpeedSource.ArcherRapidFire,
                _config.archerRapidFireMoveSpeedMultiplier,
                _config.archerRapidFireDuration);

            _ac.SetCooldown(AbilitySlot.RightClick, _config.archerRapidFireCooldown);
            return true;
        }

        private void CancelRapidFire()
        {
            if (!_isRapidFiring) return;
            _isRapidFiring  = false;
            _rapidFireTimer = 0f;
            _ownerHealth.ClearSelfMoveSpeedBuff(SelfMoveSpeedSource.ArcherRapidFire);
        }

        // ── E — Predictive Shield ─────────────────────────────────────────────────
        private bool TryBeginShield()
        {
            if (_isShielded) return false;
            if (_ac.GetCooldownRemaining(AbilitySlot.E) > 0f)    return false;
            if (_ownerHealth.IsDead || _ownerHealth.IsStunned)    return false;

            _isShielded  = true;
            _shieldTimer = _config.archerShieldDuration;
            _ownerHealth.ActivateShield();
            _ac.SetCooldown(AbilitySlot.E, _config.archerShieldCooldown);
            SetShieldVisual(true);
            return true;
        }

        private void EndShield()
        {
            if (!_isShielded) return;
            _isShielded  = false;
            _shieldTimer = 0f;
            _ownerHealth.ClearShield();
            SetShieldVisual(false);
        }

        private void HandleShieldConsumed()
        {
            _isShielded  = false;
            _shieldTimer = 0f;
            SetShieldVisual(false);
        }

        // ── F — Barrage ───────────────────────────────────────────────────────────
        private void StartFBarrage()
        {
            _isFiring   = true;
            _fFireTimer = 0f; // triggers first shot this frame
            // Apply move speed penalty for the entire duration of firing.
            // 99999f keeps it "permanent" until explicitly cleared by EndFBarrage.
            _ownerHealth.SetSelfMoveSpeedMultiplier(_config.archerFMoveSpeedMultiplier, 99999f);
        }

        private void EndFBarrage()
        {
            if (!_isFiring) return;
            _isFiring   = false;
            _fFireTimer = 0f;
            _ownerHealth.ClearSelfMovePenalty();
        }

        private void FireBarrageShot()
        {
            if (_cameraTransform == null) return;
            Vector3 dir = _cameraTransform.forward;
            ArcherBasicProjectile.SpawnBarrage(
                _ownerHealth, _cameraTransform.position + dir * 0.5f, dir, _config);
        }

        // ── Shield visual ─────────────────────────────────────────────────────────
        private void SetShieldVisual(bool show)
        {
            if (show && _shieldVisual == null)
                CreateShieldVisual();
            if (_shieldVisual != null)
                _shieldVisual.SetActive(show);
        }

        private void CreateShieldVisual()
        {
            _shieldVisual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            _shieldVisual.name = "ShieldVisual";
            _shieldVisual.transform.SetParent(_ownerHealth.transform, false);
            _shieldVisual.transform.localPosition = Vector3.up * (_config.standHeight * 0.5f);
            _shieldVisual.transform.localScale    = Vector3.one * 1.5f;
            Object.Destroy(_shieldVisual.GetComponent<Collider>());
            _shieldVisual.GetComponent<Renderer>().sharedMaterial = GetShieldMat();
        }

        private static Material GetShieldMat()
        {
            if (s_shieldMat != null) return s_shieldMat;
            s_shieldMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            s_shieldMat.SetFloat("_Surface", 1f);
            s_shieldMat.SetFloat("_Blend",   0f);
            s_shieldMat.SetFloat("_SrcBlend", 5f);
            s_shieldMat.SetFloat("_DstBlend", 10f);
            s_shieldMat.SetFloat("_ZWrite",   0f);
            s_shieldMat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            s_shieldMat.renderQueue = 3000;
            s_shieldMat.color = new Color(0.10f, 0.75f, 1.0f, 0.28f);
            return s_shieldMat;
        }
    }
}
