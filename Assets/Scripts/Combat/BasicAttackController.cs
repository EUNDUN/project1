using UnityEngine;
using Game.Commands;
using Game.Config;

namespace Game.Combat
{
    // Manages cooldown and drives AttackResolver on behalf of the local player.
    // Bots and servers call AttackResolver.TryHit directly with their own timing.
    public class BasicAttackController : MonoBehaviour
    {
        public GameConfig config;
        public Transform cameraTransform;
        public HealthComponent attackerHealth;

        private float _cooldown;

        void Start()
        {
            if (config == null || cameraTransform == null || attackerHealth == null)
            {
                Debug.LogError("[BasicAttackController] Missing required reference. Disabling.", this);
                enabled = false;
            }
        }

        public void Tick(PlayerCommand cmd)
        {
            _cooldown = Mathf.Max(0f, _cooldown - Time.deltaTime);
            if (!cmd.AttackPressed || _cooldown > 0f) return;
            if (attackerHealth.IsDead) return;

            _cooldown = config.attackCooldown;

            if (!AttackResolver.TryHit(cameraTransform.position, cameraTransform.forward,
                                        config.attackRange, attackerHealth.Team,
                                        config.attackLayerMask, out HealthComponent target))
                return;

            var info = new DamageInfo
            {
                BaseDamage = config.attackDamage,
                SourceTeam = attackerHealth.Team,
                SourceId   = string.Empty
            };
            target.TakeDamage(info);

            if (config.debugCombatLogs)
                Debug.Log($"[BasicAttack] Hit {target.name}  {target.CurrentHp:F0}/{target.MaxHp:F0} HP");
        }
    }
}