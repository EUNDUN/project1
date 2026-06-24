using UnityEngine;
using Game.Combat;
using Game.Config;

namespace Game.Bot
{
    // Pursues the nearest alive enemy and attacks at melee range.
    // The enemies array is injected once at startup by GameBootstrap — no per-frame scene search.
    public class BotController : MonoBehaviour
    {
        public GameConfig config;
        public HealthComponent   botHealth;
        public HealthComponent[] enemies;   // set by GameBootstrap.WireEnemyLists()

        private CharacterController _cc;
        private float _attackCooldown;
        private float _verticalVelocity;
        private float _sqDetectRange;
        private float _sqAttackRange;

        void Start()
        {
            _cc = GetComponent<CharacterController>();

            if (config == null || _cc == null || botHealth == null || enemies == null)
            {
                Debug.LogError("[BotController] Missing required reference. Disabling.", this);
                enabled = false;
                return;
            }

            _sqDetectRange = config.botDetectRange * config.botDetectRange;
            _sqAttackRange = config.botAttackRange * config.botAttackRange;

            botHealth.OnRespawned += HandleRespawned;
        }

        void OnDestroy()
        {
            if (botHealth != null) botHealth.OnRespawned -= HandleRespawned;
        }

        void Update()
        {
            if (botHealth.IsDead) return;

            if (_cc.isGrounded && _verticalVelocity < 0f)
                _verticalVelocity = -2f;
            _verticalVelocity += config.gravity * Time.deltaTime;

            Vector3 vertMotion = new Vector3(0f, _verticalVelocity, 0f);

            HealthComponent target = FindNearestAliveEnemy();
            if (target == null)
            {
                _cc.Move(vertMotion * Time.deltaTime);
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
                    _cc.Move((toTarget * config.botMoveSpeed + vertMotion) * Time.deltaTime);
                }
                else
                {
                    _cc.Move(vertMotion * Time.deltaTime);
                }
            }
            else
            {
                _cc.Move(vertMotion * Time.deltaTime);
            }

            _attackCooldown = Mathf.Max(0f, _attackCooldown - Time.deltaTime);
            if (sqDist <= _sqAttackRange && _attackCooldown <= 0f)
            {
                _attackCooldown = config.botAttackCooldown;
                var info = new DamageInfo
                {
                    BaseDamage = config.botAttackDamage,
                    SourceTeam = botHealth.Team,
                    SourceId   = gameObject.name
                };
                target.TakeDamage(info);

                if (config.debugCombatLogs)
                    Debug.Log($"[Bot] {name} -> {target.name}  HP {target.CurrentHp:F0}/{target.MaxHp:F0}");
            }
        }

        // Iterates a small fixed array — O(N) over enemies count, never searches the scene.
        // Uses sqrMagnitude to avoid sqrt in the comparison loop.
        HealthComponent FindNearestAliveEnemy()
        {
            HealthComponent nearest  = null;
            float           minSqDst = float.MaxValue;

            for (int i = 0; i < enemies.Length; i++)
            {
                HealthComponent e = enemies[i];
                if (e == null || e.IsDead) continue;

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