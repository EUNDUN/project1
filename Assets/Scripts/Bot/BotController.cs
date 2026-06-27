using UnityEngine;
using Game.Combat;
using Game.Config;

namespace Game.Bot
{
    // Pursues the nearest alive enemy and attacks at melee range.
    // The enemies array is injected once at startup by GameBootstrap — no per-frame scene search.
    public class BotController : MonoBehaviour
    {
        public GameConfig        config;
        public HealthComponent   botHealth;
        public HealthComponent[] enemies;   // set by GameBootstrap.WireEnemyLists()

        private CharacterController _cc;
        private float           _attackCooldown;
        private float           _verticalVelocity;
        private float           _sqDetectRange;
        private float           _sqAttackRange;
        private ClassAttackData _attackData;
        private bool            _isArcher;

        void Start()
        {
            _cc = GetComponent<CharacterController>();

            if (config == null || _cc == null || botHealth == null || enemies == null)
            {
                Debug.LogError("[BotController] Missing required reference. Disabling.", this);
                enabled = false;
                return;
            }

            _attackData = config.GetAttackData(botHealth.CombatClass);
            _isArcher   = botHealth.CombatClass == CombatClass.Archer;

            // Archer uses projectile range (30 m); other classes use ClassAttackData.Range.
            float attackRange = _isArcher ? config.archerBasicProjectileRange : _attackData.Range;
            float detectRange = Mathf.Max(config.botDetectRange, attackRange + 1f);
            _sqDetectRange = detectRange * detectRange;
            _sqAttackRange = attackRange * attackRange;

            botHealth.OnRespawned += HandleRespawned;
        }

        void OnDestroy()
        {
            if (botHealth != null) botHealth.OnRespawned -= HandleRespawned;
        }

        void Update()
        {
            if (botHealth.IsDead) return;

            float impulse = botHealth.ConsumeLaunchImpulse();
            if (impulse > 0f)
                _verticalVelocity = impulse; // launch — gravity decelerates from here
            else if (_cc.isGrounded && _verticalVelocity < 0f)
                _verticalVelocity = -2f;
            _verticalVelocity += config.gravity * Time.deltaTime;

            Vector3 vertMotion = new Vector3(0f, _verticalVelocity, 0f);
            Vector3 kb         = botHealth.KnockbackVelocity;
            Vector3 pull       = botHealth.PullVelocity;      // blackhole / zone pull (horizontal)

            // Stunned: gravity + knockback + pull — voluntary movement and attack are suppressed.
            if (botHealth.IsStunned)
            {
                _cc.Move((vertMotion + kb + pull) * Time.deltaTime);
                return;
            }

            HealthComponent target = FindNearestAliveEnemy();
            if (target == null)
            {
                _cc.Move((vertMotion + kb + pull) * Time.deltaTime);
                return;
            }

            Vector3 toTarget = target.transform.position - transform.position;
            float sqDist = toTarget.sqrMagnitude;

            if (sqDist <= _sqDetectRange && sqDist > _sqAttackRange)
            {
                toTarget.y = 0f;
                if (toTarget.sqrMagnitude > 0.001f)
                {
                    toTarget.Normalize();
                    transform.rotation = Quaternion.LookRotation(toTarget);
                    float moveSpeedMult = botHealth.MoveSpeedMultiplier * botHealth.SelfMoveSpeedMultiplier;
                    _cc.Move((toTarget * config.botMoveSpeed * moveSpeedMult + vertMotion + kb + pull) * Time.deltaTime);
                }
                else
                {
                    _cc.Move((vertMotion + kb + pull) * Time.deltaTime);
                }
            }
            else
            {
                _cc.Move((vertMotion + kb + pull) * Time.deltaTime);
            }

            _attackCooldown = Mathf.Max(0f, _attackCooldown - Time.deltaTime);
            if (sqDist <= _sqAttackRange && _attackCooldown <= 0f && target.IsTargetable)
            {
                // eye-level origin; target torso centre avoids overshooting the head.
                Vector3 origin       = transform.position + Vector3.up * (config.standHeight * 0.8f);
                Vector3 targetCenter = target.transform.position + Vector3.up * (config.standHeight * 0.5f);

                if (_isArcher)
                {
                    _attackCooldown = config.archerBasicProjectileCooldown;
                    Vector3 dir = targetCenter - origin;
                    if (dir.sqrMagnitude < 0.01f) return;
                    ArcherBasicProjectile.Spawn(botHealth, origin, dir.normalized, config, ArcherShotType.Basic);
                }
                else
                {
                    _attackCooldown = _attackData.Cooldown;
                    Vector3 rawDir = targetCenter - origin;
                    if (rawDir.sqrMagnitude < 0.01f) return;

                    if (!AttackResolver.TryHit(origin, rawDir.normalized, _attackData.Range,
                                                botHealth.Team, config.attackLayerMask,
                                                out HealthComponent hit))
                        return;

                    var info = new DamageInfo
                    {
                        BaseDamage = _attackData.Damage,
                        SourceTeam = botHealth.Team,
                        SourceId   = gameObject.name
                    };
                    hit.TakeDamage(info);

                    if (config.debugCombatLogs)
                        Debug.Log($"[Bot] {name} -> {hit.name}  HP {hit.CurrentHp:F0}/{hit.MaxHp:F0}");
                }
            }
        }

        // Iterates a small fixed array — O(N) over enemies count, never searches the scene.
        // Uses sqrMagnitude to avoid sqrt in the comparison loop.
        // IsTargetable = !IsDead && !IsStealthed; stealthed characters are never picked.
        HealthComponent FindNearestAliveEnemy()
        {
            HealthComponent nearest  = null;
            float           minSqDst = float.MaxValue;

            for (int i = 0; i < enemies.Length; i++)
            {
                HealthComponent e = enemies[i];
                if (e == null || !e.IsTargetable) continue;

                float sqDst = (transform.position - e.transform.position).sqrMagnitude;
                if (sqDst < minSqDst)
                {
                    minSqDst = sqDst;
                    nearest  = e;
                }
            }
            return nearest;
        }

        void HandleRespawned(HealthComponent _)
        {
            _verticalVelocity = 0f;
            _attackCooldown   = 0f;
        }
    }
}