using UnityEngine;
using Game.Commands;
using Game.Config;

namespace Game.Combat
{
    // Manages cooldown and drives AttackResolver on behalf of the local player.
    // Bots and servers call AttackResolver.TryHit directly with their own timing.
    public class BasicAttackController : MonoBehaviour
    {
        public GameConfig      config;
        public Transform       cameraTransform;
        public HealthComponent attackerHealth;

        // OnAttackHit fires first (before OnAttackUsed) so subscribers can check stealth state
        // before BreakStealth() is called. Both events fire only when an attack is committed
        // (not blocked by cooldown/dead/stun).
        public event System.Action<HealthComponent> OnAttackHit;      // null if raycast missed
        public event System.Action                  OnAttackUsed;     // always fires on committed attack
        // Fired by RogueAbilityHandler when a backstab bonus lands.
        // Subscribed by PlayerHUD to show combat feedback to the local player.
        public event System.Action<float>           OnBackstabLanded;

        private float             _cooldown;
        private ClassAttackData   _attackData;
        private bool              _isArcher;
        private AbilityController _abilityController; // null on bots (they don't use BasicAttackController)
        // Two callbacks cached at Start() — selected per shot type, never allocated per frame.
        // Basic shot: OnArcherBasicHit  — F gauge gain + Z cooldown refund.
        // Q/enhanced: AddArcherFGauge   — F gauge gain only; Z cooldown unchanged.
        private System.Action<float> _onBasicHitCallback;
        private System.Action<float> _addFGaugeCallback;

        void Start()
        {
            if (config == null || cameraTransform == null || attackerHealth == null)
            {
                Debug.LogError("[BasicAttackController] Missing required reference. Disabling.", this);
                enabled = false;
                return;
            }
            _attackData        = config.GetAttackData(attackerHealth.CombatClass);
            _isArcher          = attackerHealth.CombatClass == CombatClass.Archer;
            _abilityController = GetComponent<AbilityController>();
            if (_isArcher && _abilityController != null)
            {
                _onBasicHitCallback = _abilityController.OnArcherBasicHit; // gauge + Z CD refund
                _addFGaugeCallback  = _abilityController.AddArcherFGauge;  // gauge only (Q shots)
            }
        }

        public void Tick(PlayerCommand cmd)
        {
            _cooldown = Mathf.Max(0f, _cooldown - Time.deltaTime);
            if (!cmd.AttackPressed || _cooldown > 0f) return;
            if (attackerHealth.IsDead || attackerHealth.IsStunned) return;
            // Block basic attack during Mage Z aiming/charging and Archer F barrage.
            if (_abilityController != null && _abilityController.ShouldBlockBasicAttack) return;

            if (_isArcher)
            {
                // effectiveCooldown = baseCooldown / attackSpeedMultiplier (never modifies config).
                float atkSpeed = _abilityController != null
                    ? _abilityController.ArcherAttackSpeedMultiplier
                    : 1f;
                _cooldown = config.archerBasicProjectileCooldown / atkSpeed;
                // Consume the pending enhanced shot type (Basic if Q was never pressed).
                ArcherShotType shotType = _abilityController != null
                    ? _abilityController.ConsumeArcherPendingShotType()
                    : ArcherShotType.Basic;
                // Basic during Overdrive → upgraded explosive projectile.
                // Q shots (Shock/Fire/Ice) are unaffected by Overdrive.
                if (shotType == ArcherShotType.Basic && (_abilityController?.ArcherIsOverdrive ?? false))
                    shotType = ArcherShotType.OverdriveBasic;
                // Basic / OverdriveBasic: full callback (F gauge + Z cooldown refund).
                // Q/enhanced: gauge-only callback (no Z cooldown change).
                System.Action<float> hitCallback =
                    (shotType == ArcherShotType.Basic || shotType == ArcherShotType.OverdriveBasic)
                    ? _onBasicHitCallback
                    : _addFGaugeCallback;
                Vector3 dir = cameraTransform.forward;
                ArcherBasicProjectile.Spawn(attackerHealth,
                                            cameraTransform.position + dir * 0.5f,
                                            dir, config, shotType, hitCallback);
                OnAttackUsed?.Invoke();
                return;
            }

            // Non-Archer: instant raycast hit (Warrior / Rogue / Mage fallback).
            _cooldown = _attackData.Cooldown;

            bool hit = AttackResolver.TryHit(cameraTransform.position, cameraTransform.forward,
                                             _attackData.Range, attackerHealth.Team,
                                             config.attackLayerMask, out HealthComponent target);

            // OnAttackHit fires BEFORE OnAttackUsed so handlers can check their stealth state
            // (e.g. backstab bonus) before BreakStealth() is called by OnAttackUsed.
            if (hit) OnAttackHit?.Invoke(target);
            OnAttackUsed?.Invoke();

            if (!hit) return;

            var info = new DamageInfo
            {
                BaseDamage = _attackData.Damage,
                SourceTeam = attackerHealth.Team,
                SourceId   = string.Empty
            };
            target.TakeDamage(info);

            if (config.debugCombatLogs)
                Debug.Log($"[BasicAttack] Hit {target.name}  {target.CurrentHp:F0}/{target.MaxHp:F0} HP");
        }

        // Called by RogueAbilityHandler when a backstab bonus is applied.
        internal void FireBackstabLanded(float bonus) => OnBackstabLanded?.Invoke(bonus);
    }
}