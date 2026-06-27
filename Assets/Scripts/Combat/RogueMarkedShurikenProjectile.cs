using UnityEngine;

namespace Game.Combat
{
    // Rogue E1 — shuriken projectile. Flies forward; on enemy hit, applies damage and
    // notifies RogueAbilityHandler to arm the E2 mark.
    // Designed for future pooling: call Init() to reset all state before (re)use.
    public class RogueMarkedShurikenProjectile : MonoBehaviour
    {
        private const float HitRadius = 0.2f;

        private Team               _ownerTeam;
        private float              _damage;
        private float              _speed;
        private float              _maxRange;
        private Vector3            _direction;
        private float              _traveled;
        private LayerMask          _hitLayerMask;
        private RogueAbilityHandler _notifyHandler; // receives OnEProjectileHit on enemy contact

        // Shared overlap buffer — one allocation at class load, reused every frame.
        private static readonly Collider[] s_buf = new Collider[8];

        public void Init(Team ownerTeam, float damage, float speed, float maxRange,
                         Vector3 direction, LayerMask hitLayerMask,
                         RogueAbilityHandler notifyHandler)
        {
            _ownerTeam     = ownerTeam;
            _damage        = damage;
            _speed         = speed;
            _maxRange      = maxRange;
            _direction     = direction.normalized;
            _traveled      = 0f;
            _hitLayerMask  = hitLayerMask;
            _notifyHandler = notifyHandler;
        }

        void Update()
        {
            float step = _speed * Time.deltaTime;
            transform.position += _direction * step;
            _traveled += step;

            if (_traveled >= _maxRange)
            {
                _notifyHandler?.OnEProjectileMissed(); // range exhausted — E1 missed
                Destroy(gameObject);
                return;
            }

            int count = Physics.OverlapSphereNonAlloc(
                transform.position, HitRadius, s_buf,
                _hitLayerMask, QueryTriggerInteraction.Ignore);

            for (int i = 0; i < count; i++)
            {
                HealthComponent hc = s_buf[i].GetComponent<HealthComponent>();
                if (hc == null) hc = s_buf[i].GetComponentInParent<HealthComponent>();

                if (hc != null)
                {
                    if (hc.Team == _ownerTeam || hc.IsDead) continue; // ally / dead — pass through

                    hc.TakeDamage(new DamageInfo
                    {
                        BaseDamage = _damage,
                        SourceTeam = _ownerTeam,
                        SourceId   = string.Empty,
                    });
                    _notifyHandler?.OnEProjectileHit(hc); // hit — arm E2 mark
                    Destroy(gameObject);
                    return;
                }

                // No HealthComponent = static geometry (wall, obstacle) — miss.
                _notifyHandler?.OnEProjectileMissed();
                Destroy(gameObject);
                return;
            }
        }
    }
}