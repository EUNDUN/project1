using System;
using UnityEngine;

namespace Game.Combat
{
    public class HealthComponent : MonoBehaviour
    {
        public Team        Team           { get; private set; }
        public CombatClass CombatClass    { get; private set; }
        public float       MaxHp          { get; private set; }
        public float       CurrentHp      { get; private set; }
        public bool        IsDead         { get; private set; }
        // True when either respawn invulnerability or ability invulnerability is active.
        // The two layers are independent so clearing one never accidentally clears the other.
        public bool        IsInvulnerable => _respawnInvul || _abilityInvul;
        public bool        IsStunned      => (_activeEffects & StatusEffectMask.Stunned)   != 0;
        public bool        IsStealthed    => (_activeEffects & StatusEffectMask.Stealthed) != 0;
        // Single targetability gate checked by BotController and any future AI/targeting system.
        // Dead characters are always untargetable; stealthed characters are untargetable by AI.
        public bool        IsTargetable   => !IsDead && !IsStealthed;

        // Subscribers: pass (currentHp, maxHp). Fired only when the value changes.
        public event Action<float, float>    OnHealthChanged;
        public event Action<HealthComponent> OnDeath;
        public event Action<HealthComponent> OnRespawned;
        // Fired after HP is reduced. Useful for stealth reveal-on-hit and similar reactions.
        public event Action<HealthComponent> OnDamaged;
        // Fired the moment the shield absorbs a damage or CC hit (before the call returns).
        // Subscribers (e.g. ArcherAbilityHandler) use this to clear handler state and visual.
        public event Action OnShieldConsumed;

        // Read by FirstPersonMotor / BotController for move speed and knockback.
        // MoveSpeedMultiplier    = enemy debuff layer  (Rogue Q slow, etc.)
        // SelfMoveSpeedMultiplier= self ability layer  (per-source buff array * penalty)
        //                          Multiple named buff sources (RC ×1.3, Overdrive ×1.5) multiply.
        // Motors multiply both:  finalSpeed = baseSpeed * SelfMoveSpeedMultiplier * MoveSpeedMultiplier
        public float   MoveSpeedMultiplier     => _slowTimer       > 0f ? _slowMultiplier : 1f;
        public float SelfMoveSpeedMultiplier
        {
            get
            {
                float v = 1f;
                for (int i = 0; i < BuffSourceCount; i++)
                    if (_buffTimers[i] > 0f) v *= _buffMults[i];
                return v * (_selfPenTimer > 0f ? _selfPenMult : 1f);
            }
        }
        public Vector3 KnockbackVelocity       => _knockbackTimer  > 0f ? _knockbackVelocity : Vector3.zero;
        // Incoming damage multiplier — 1.0 = normal, 0.5 = 50% reduction (warrior guard, etc.)
        public float   DamageTakenMultiplier   => _damageTakenTimer > 0f ? _damageTakenMult  : 1f;

        private CharacterStats   _stats;
        private bool             _initialized;
        private StatusEffectMask _activeEffects    = StatusEffectMask.None;
        private float            _stunTimer        = 0f;
        private float            _slowMultiplier   = 1f;
        private float            _slowTimer        = 0f;
        private Vector3          _knockbackVelocity;
        private float            _knockbackTimer   = 0f;
        // One-shot vertical launch impulse: consumed once by the motor, then cleared.
        // Motors set _verticalVelocity = impulse, then normal gravity takes over.
        // Air time formula: 2 * impulse / |gravity|  (e.g. 10m/s ÷ 20 = 1.0s).
        private float            _pendingLaunchImpulse = 0f;
        // Incoming-damage multiplier: set by abilities (e.g. warrior guard).
        // Policy: stronger reduction (lower mult) wins; longer duration wins.
        private float            _damageTakenMult  = 1f;
        private float            _damageTakenTimer = 0f;
        // Self-applied move speed: per-source buff array (multiply together) + penalty layer.
        // SelfMoveSpeedMultiplier = product(active buffs) * penalty.
        // Sources are indexed by SelfMoveSpeedSource enum; BuffSourceCount must match its Count.
        private const int        BuffSourceCount   = 3;
        private readonly float[] _buffMults        = { 1f, 1f, 1f };
        private readonly float[] _buffTimers       = { 0f, 0f, 0f };
        private float            _selfPenMult      = 1f;
        private float            _selfPenTimer     = 0f;
        // External pull velocity — set by zone effects (blackhole). Independent of knockback.
        // Always overwritten by new pull requests so direction tracks zone center each tick.
        private Vector3 _pullVelocity;
        private float   _pullTimer = 0f;
        public Vector3  PullVelocity => _pullTimer > 0f ? _pullVelocity : Vector3.zero;

        // Two-layer invulnerability: respawn (RespawnController) and ability (e.g. warrior R).
        private bool             _respawnInvul     = false;
        private bool             _abilityInvul     = false;
        // One-shot predictive shield: absorbs the next enemy damage or CC application.
        // Set by ArcherAbilityHandler; consumed (cleared) here on first hit, fires OnShieldConsumed.
        private bool             _shieldActive     = false;

        public bool HasShield => _shieldActive;

        public void Initialize(Team team, CombatClass combatClass, CharacterStats stats)
        {
            Team        = team;
            CombatClass = combatClass;
            _stats      = stats;
            MaxHp       = stats.MaxHp;
            CurrentHp   = stats.MaxHp;
            IsDead         = false;
            _respawnInvul  = false;
            _abilityInvul  = false;
            _initialized   = true;
        }

        // Called by RespawnController after the respawn delay.
        public void Reinitialize()
        {
            CurrentHp      = MaxHp;
            IsDead         = false;
            _respawnInvul  = false;
            _abilityInvul  = false;
            _activeEffects  = StatusEffectMask.None;
            _stunTimer      = 0f;
            _slowMultiplier    = 1f;
            _slowTimer         = 0f;
            _knockbackVelocity    = Vector3.zero;
            _knockbackTimer       = 0f;
            _pendingLaunchImpulse = 0f;
            _damageTakenMult      = 1f;
            _damageTakenTimer     = 0f;
            for (int i = 0; i < BuffSourceCount; i++) { _buffMults[i] = 1f; _buffTimers[i] = 0f; }
            _selfPenMult          = 1f;
            _selfPenTimer         = 0f;
            _pullVelocity         = Vector3.zero;
            _pullTimer            = 0f;
            _shieldActive         = false;
            OnHealthChanged?.Invoke(CurrentHp, MaxHp);
            OnRespawned?.Invoke(this);
        }

        // ── Shield API ────────────────────────────────────────────────────────────
        // Called by ArcherAbilityHandler when the shield buff activates.
        public void ActivateShield()  { if (_initialized && !IsDead) _shieldActive = true; }

        // Called by ArcherAbilityHandler on natural expiry or cleanup (death/ForceCleanup).
        public void ClearShield()     { _shieldActive = false; }

        // Internal: consumes the shield and notifies the handler. Called by all CC/damage paths.
        private void ConsumeShield()
        {
            _shieldActive = false;
            OnShieldConsumed?.Invoke();
        }

        // True when the source is hostile (different team, or neutral-vs-neutral fight).
        // Friendly sources (same non-Neutral team) do not consume the shield.
        // Default parameter Team.Neutral is treated conservatively as hostile.
        private bool IsHostileSource(Team sourceTeam) =>
            sourceTeam != Team || Team == Team.Neutral;

        // Attempts to absorb an attack event BEFORE any damage or CC effects are applied.
        // Use when a single ability fires multiple effects (damage + CC) that must be treated
        // as one logical hit — call this once, skip ALL effects if it returns true.
        // Returns true if the shield was active and the source was hostile (shield consumed).
        // Returns false if no shield or source is friendly.
        // Guaranteed single-consume: calling again after a true return always returns false.
        public bool TryConsumeShieldFrom(Team sourceTeam)
        {
            if (!_shieldActive) return false;
            if (!IsHostileSource(sourceTeam)) return false;
            ConsumeShield();
            return true;
        }

        // Applies or refreshes a movement slow. Stronger (lower) multiplier wins; duration extends to the longer of the two.
        public void ApplySlow(float multiplier, float duration, Team sourceTeam = Team.Neutral)
        {
            if (!_initialized || IsDead) return;
            if (_shieldActive && IsHostileSource(sourceTeam)) { ConsumeShield(); return; }
            if (_slowTimer > 0f)
            {
                _slowMultiplier = Mathf.Min(_slowMultiplier, multiplier); // keep stronger slow
                _slowTimer      = Mathf.Max(_slowTimer, duration);        // keep longer duration
            }
            else
            {
                _slowMultiplier = multiplier;
                _slowTimer      = duration;
            }
        }

        // External pull toward a zone center (e.g. blackhole). Always replaces current pull
        // so the direction stays current as the zone refreshes each tick.
        public void ApplyPull(Vector3 velocity, float duration)
        {
            if (!_initialized || IsDead) return;
            _pullVelocity = velocity;
            _pullTimer    = duration;
        }

        // Applies a velocity impulse read by FirstPersonMotor / BotController.
        // Stronger (larger magnitude) knockback replaces weaker; always refreshes duration.
        public void ApplyKnockback(Vector3 velocity, float duration, Team sourceTeam = Team.Neutral)
        {
            if (!_initialized || IsDead) return;
            if (_shieldActive && IsHostileSource(sourceTeam)) { ConsumeShield(); return; }
            if (_knockbackTimer > 0f && velocity.sqrMagnitude < _knockbackVelocity.sqrMagnitude)
                _knockbackTimer = Mathf.Max(_knockbackTimer, duration); // weaker — just extend
            else
            {
                _knockbackVelocity = velocity;
                _knockbackTimer    = duration;
            }
        }

        // Queues a vertical launch impulse. The motor reads this once per Tick and sets
        // its own _verticalVelocity, producing an accurate physics-based air time.
        // Strongest impulse wins when multiple sources fire in the same frame.
        public void ApplyLaunchImpulse(float speed, Team sourceTeam = Team.Neutral)
        {
            if (!_initialized || IsDead) return;
            if (_shieldActive && IsHostileSource(sourceTeam)) { ConsumeShield(); return; }
            _pendingLaunchImpulse = Mathf.Max(_pendingLaunchImpulse, speed);
        }

        // Called once per motor Tick. Returns the queued impulse and clears it atomically.
        public float ConsumeLaunchImpulse()
        {
            float v = _pendingLaunchImpulse;
            _pendingLaunchImpulse = 0f;
            return v;
        }

        // Sets a temporary incoming-damage multiplier (e.g. 0.5 = 50% reduction).
        // Stronger (lower) multiplier wins; longer duration wins when stacking.
        public void SetDamageTakenMultiplier(float multiplier, float duration)
        {
            if (!_initialized || IsDead) return;
            if (_damageTakenTimer > 0f)
            {
                _damageTakenMult  = Mathf.Min(_damageTakenMult, multiplier);
                _damageTakenTimer = Mathf.Max(_damageTakenTimer, duration);
            }
            else
            {
                _damageTakenMult  = multiplier;
                _damageTakenTimer = duration;
            }
        }

        // Legacy API: routes buffs to Generic source slot, penalties to the penalty layer.
        // New callers should use SetSelfMoveSpeedBuff / ClearSelfMoveSpeedBuff directly.
        public void SetSelfMoveSpeedMultiplier(float multiplier, float duration)
        {
            if (!_initialized || IsDead) return;
            if (multiplier >= 1f)
                SetSelfMoveSpeedBuff(SelfMoveSpeedSource.Generic, multiplier, duration);
            else
            {
                if (_selfPenTimer > 0f) { _selfPenMult = Mathf.Min(_selfPenMult, multiplier); _selfPenTimer = Mathf.Max(_selfPenTimer, duration); }
                else                    { _selfPenMult = multiplier; _selfPenTimer = duration; }
            }
        }

        // Clears all buff sources and the penalty layer — used on death or full ability cancel.
        public void ClearSelfMoveSpeedMultiplier()
        {
            for (int i = 0; i < BuffSourceCount; i++) { _buffMults[i] = 1f; _buffTimers[i] = 0f; }
            _selfPenMult  = 1f;
            _selfPenTimer = 0f;
        }

        // Sets a named-source buff. Overwrites the slot unconditionally — each source manages
        // its own lifetime. Multiple sources multiply together in SelfMoveSpeedMultiplier.
        public void SetSelfMoveSpeedBuff(SelfMoveSpeedSource source, float multiplier, float duration)
        {
            if (!_initialized || IsDead) return;
            int i = (int)source;
            _buffMults[i]  = multiplier;
            _buffTimers[i] = duration;
        }

        // Clears a single buff source without affecting any other source or the penalty layer.
        public void ClearSelfMoveSpeedBuff(SelfMoveSpeedSource source)
        {
            int i = (int)source;
            _buffMults[i]  = 1f;
            _buffTimers[i] = 0f;
        }

        // Clears only the penalty layer — used when a single held ability (e.g. F Barrage)
        // ends while a separate buff (e.g. Rapid Fire) may still be active.
        public void ClearSelfMovePenalty()
        {
            _selfPenMult  = 1f;
            _selfPenTimer = 0f;
        }

        // Immediately cancels the incoming-damage multiplier (called on guard end / death).
        public void ClearDamageTakenMultiplier()
        {
            _damageTakenMult  = 1f;
            _damageTakenTimer = 0f;
        }

        // Applies or extends a stun. Duration is clamped to the longer of existing vs new.
        public void ApplyStun(float duration, Team sourceTeam = Team.Neutral)
        {
            if (!_initialized || IsDead) return;
            if (_shieldActive && IsHostileSource(sourceTeam)) { ConsumeShield(); return; }
            _activeEffects |= StatusEffectMask.Stunned;
            _stunTimer = Mathf.Max(_stunTimer, duration);
        }

        void Update()
        {
            if (!_initialized) return;
            float dt = Time.deltaTime;
            if (_stunTimer > 0f)
            {
                _stunTimer -= dt;
                if (_stunTimer <= 0f)
                {
                    _stunTimer     = 0f;
                    _activeEffects &= ~StatusEffectMask.Stunned;
                }
            }
            if (_slowTimer > 0f)
            {
                _slowTimer -= dt;
                if (_slowTimer <= 0f) { _slowTimer = 0f; _slowMultiplier = 1f; }
            }
            if (_knockbackTimer > 0f)
            {
                _knockbackTimer -= dt;
                if (_knockbackTimer <= 0f) { _knockbackTimer = 0f; _knockbackVelocity = Vector3.zero; }
            }
            if (_damageTakenTimer > 0f)
            {
                _damageTakenTimer -= dt;
                if (_damageTakenTimer <= 0f) { _damageTakenTimer = 0f; _damageTakenMult = 1f; }
            }
            for (int i = 0; i < BuffSourceCount; i++)
            {
                if (_buffTimers[i] > 0f)
                {
                    _buffTimers[i] -= dt;
                    if (_buffTimers[i] <= 0f) { _buffTimers[i] = 0f; _buffMults[i] = 1f; }
                }
            }
            if (_selfPenTimer > 0f)
            {
                _selfPenTimer -= dt;
                if (_selfPenTimer <= 0f) { _selfPenTimer = 0f; _selfPenMult = 1f; }
            }
            if (_pullTimer > 0f)
            {
                _pullTimer -= dt;
                if (_pullTimer <= 0f) { _pullTimer = 0f; _pullVelocity = Vector3.zero; }
            }
        }

        // Respawn invulnerability — controlled by RespawnController's coroutine.
        public void SetInvulnerable(bool state) => _respawnInvul = state;

        // Ability invulnerability — controlled by ability skills (e.g. warrior R ultimate).
        // Independent of respawn invuln; clearing one never clears the other.
        public void SetAbilityInvulnerable(bool state) => _abilityInvul = state;

        // Called by RogueAbilityHandler when stealth activates or ends.
        // Sets the Stealthed flag in _activeEffects, which IsTargetable reads.
        public void SetStealthed(bool stealthed)
        {
            if (stealthed)
                _activeEffects |= StatusEffectMask.Stealthed;
            else
                _activeEffects &= ~StatusEffectMask.Stealthed;
        }

        // Single entry point for all damage sources: player, bot, server relay.
        public void TakeDamage(DamageInfo info)
        {
            if (!_initialized || IsDead || IsInvulnerable) return;
            if (info.SourceTeam == Team && Team != Team.Neutral) return; // no friendly fire
            if (TryConsumeShieldFrom(info.SourceTeam)) return;            // shield absorbs hit

            float finalDamage = info.IgnoreArmor
                ? info.BaseDamage
                : _stats.CalculateFinalDamage(info.BaseDamage);
            finalDamage *= DamageTakenMultiplier; // apply guard / damage-reduction buff
            CurrentHp = Mathf.Max(0f, CurrentHp - finalDamage);
            OnHealthChanged?.Invoke(CurrentHp, MaxHp);
            OnDamaged?.Invoke(this);

            if (CurrentHp <= 0f)
            {
                IsDead = true;
#if UNITY_EDITOR
                Debug.Log($"[HealthComponent] {name} has died.");
#endif
                OnDeath?.Invoke(this);
            }
        }
    }
}