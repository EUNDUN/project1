using UnityEngine;

namespace Game.Combat
{
    // Spawned by MageAbilityHandler when the player releases left-click after charging Z.
    // Travels in a straight line; applies damage and stun on the first valid hit.
    // Stun and damage scale with chargeRatio — values are baked in at spawn via Init().
    // TODO: replace Instantiate/Destroy with an object pool when projectile counts grow.
    public class MageArcaneBoltProjectile : MonoBehaviour
    {
        private Team            _ownerTeam;
        private HealthComponent _ownerHealth;
        private float           _damage;
        private float           _stunDuration;
        private float           _speed;
        private float           _rangeRemaining;
        private float           _radius;
        private Vector3         _direction;
        private LayerMask       _layerMask;

        // Shared across all instances in the same frame — safe because Update() is single-threaded.
        private static readonly RaycastHit[] s_hits = new RaycastHit[8];

        public void Init(Team ownerTeam, HealthComponent ownerHealth,
                         float damage, float stunDuration,
                         float speed, float range, float radius,
                         Vector3 direction, LayerMask layerMask)
        {
            _ownerTeam      = ownerTeam;
            _ownerHealth    = ownerHealth;
            _damage         = damage;
            _stunDuration   = stunDuration;
            _speed          = speed;
            _rangeRemaining = range;
            _radius         = radius;
            _direction      = direction.normalized;
            _layerMask      = layerMask;
        }

        void Update()
        {
            float dt   = Time.deltaTime;
            float dist = _speed * dt;

            _rangeRemaining -= dist;
            if (_rangeRemaining <= 0f) { Destroy(gameObject); return; }

            // SphereCast to catch targets at any speed without tunnelling.
            int count = Physics.SphereCastNonAlloc(
                transform.position, _radius, _direction, s_hits, dist,
                _layerMask, QueryTriggerInteraction.Ignore);

            // Find the closest valid hit — enemy or environment — skipping owner/allies/dead.
            float           bestDist = float.MaxValue;
            HealthComponent bestHC   = null;
            bool            hitFound = false;

            for (int i = 0; i < count; i++)
            {
                RaycastHit hit = s_hits[i];
                HealthComponent hc = hit.collider.GetComponent<HealthComponent>();
                if (hc == null) hc = hit.collider.GetComponentInParent<HealthComponent>();

                if (hc != null)
                {
                    if (hc == _ownerHealth) continue; // skip self
                    if (hc.Team == _ownerTeam) continue; // skip allies
                    if (hc.IsDead) continue;
                }
                // else: environment collider — always a valid blocker

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
                    bestHC.ApplyStun(_stunDuration, _ownerTeam);
                }
                Destroy(gameObject);
                return;
            }

            transform.position += _direction * dist;
        }
    }
}
