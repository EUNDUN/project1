using UnityEngine;

namespace Game.Combat
{
    // Spawned by MageAbilityHandler. Moves in a straight line each frame.
    // Uses SphereCastNonAlloc to detect hits without tunnelling at high speed.
    // TODO: replace Instantiate/Destroy with an object pool when projectile counts grow.
    public class MageFireballProjectile : MonoBehaviour
    {
        private Team            _ownerTeam;
        private HealthComponent _ownerHealth;
        private float           _damage;
        private float           _speed;
        private float           _rangeRemaining;
        private float           _radius;
        private Vector3         _direction;
        private LayerMask       _layerMask;
        private float           _lifetime;
        // Big fireball only — zero values mean no extra effect (normal fireball path).
        private float           _stunDuration;
        private float           _knockbackSpeed;
        private float           _knockbackDuration;

        // Shared across all instances in the same frame — safe because Update() is single-threaded.
        private static readonly RaycastHit[] s_hits = new RaycastHit[8];

        // Called once immediately after AddComponent. No Inspector fields — all config comes in here.
        // Pass stunDuration/knockbackSpeed/knockbackDuration > 0 for big fireball effects.
        public void Init(Team ownerTeam, HealthComponent ownerHealth, float damage,
                         float speed, float range, float radius, Vector3 direction,
                         LayerMask layerMask,
                         float stunDuration, float knockbackSpeed, float knockbackDuration)
        {
            _ownerTeam         = ownerTeam;
            _ownerHealth       = ownerHealth;
            _damage            = damage;
            _speed             = speed;
            _rangeRemaining    = range;
            _radius            = radius;
            _direction         = direction.normalized;
            _layerMask         = layerMask;
            _lifetime          = 3f;
            _stunDuration      = stunDuration;
            _knockbackSpeed    = knockbackSpeed;
            _knockbackDuration = knockbackDuration;
        }

        void Update()
        {
            float dt   = Time.deltaTime;
            float dist = _speed * dt;

            _lifetime       -= dt;
            _rangeRemaining -= dist;

            if (_lifetime <= 0f || _rangeRemaining <= 0f)
            {
                Destroy(gameObject);
                return;
            }

            // Cast a sphere along this frame's movement step to catch targets at any speed.
            int count = Physics.SphereCastNonAlloc(
                transform.position, _radius, _direction, s_hits, dist,
                _layerMask, QueryTriggerInteraction.Ignore);

            // Find the closest valid hit — environment or enemy — skipping owner/allies/dead.
            float           bestDist = float.MaxValue;
            HealthComponent bestHC   = null;   // non-null = enemy hit; null = environment hit
            bool            hitFound = false;

            for (int i = 0; i < count; i++)
            {
                RaycastHit hit = s_hits[i];
                Collider   col = hit.collider;

                HealthComponent hc = col.GetComponent<HealthComponent>();
                if (hc == null) hc = col.GetComponentInParent<HealthComponent>();

                if (hc != null)
                {
                    // Skip owner, allies, and already-dead targets entirely.
                    if (hc == _ownerHealth) continue;
                    if (hc.Team == _ownerTeam) continue;
                    if (hc.IsDead) continue;
                }
                // else: non-character collider → environment (always a valid blocker)

                if (hit.distance < bestDist)
                {
                    bestDist = hit.distance;
                    bestHC   = hc;
                    hitFound = true;
                }
            }

            if (hitFound)
            {
                if (bestHC != null)
                {
                    bestHC.TakeDamage(new DamageInfo
                    {
                        BaseDamage = _damage,
                        SourceTeam = _ownerTeam,
                        SourceId   = string.Empty
                    });

                    // Big fireball only: apply stun and horizontal knockback.
                    if (_stunDuration > 0f)
                        bestHC.ApplyStun(_stunDuration, _ownerTeam);

                    if (_knockbackSpeed > 0f && _knockbackDuration > 0f)
                    {
                        Vector3 kbDir = _direction;
                        kbDir.y = 0f;
                        if (kbDir.sqrMagnitude < 0.01f)
                        {
                            kbDir = _ownerHealth != null ? _ownerHealth.transform.forward : Vector3.zero;
                            kbDir.y = 0f;
                            if (kbDir.sqrMagnitude < 0.01f) kbDir = Vector3.forward;
                        }
                        bestHC.ApplyKnockback(kbDir.normalized * _knockbackSpeed, _knockbackDuration, _ownerTeam);
                    }
                }
                Destroy(gameObject);
                return;
            }

            transform.position += _direction * dist;
        }
    }
}
