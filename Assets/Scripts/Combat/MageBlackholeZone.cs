using UnityEngine;
using Game.Config;

namespace Game.Combat
{
    // Spawned by MageAbilityHandler after the cast delay expires.
    // Each tickInterval: slows and horizontally pulls valid targets in range.
    // No damage. Destroyed when duration expires.
    // TODO: replace Instantiate/Destroy with pool when needed.
    public class MageBlackholeZone : MonoBehaviour
    {
        private Team            _ownerTeam;
        private HealthComponent _ownerHealth;
        private GameConfig      _config;
        private float           _duration;
        private float           _tickTimer;

        // Shared across all zone instances — safe because Update() is single-threaded.
        private static readonly Collider[] s_buf = new Collider[16];

        public void Init(Team ownerTeam, HealthComponent ownerHealth, GameConfig config)
        {
            _ownerTeam   = ownerTeam;
            _ownerHealth = ownerHealth;
            _config      = config;
            _duration    = config.mageBlackholeDuration;
            _tickTimer   = 0f; // fire immediately on first Update
        }

        void Update()
        {
            float dt = Time.deltaTime;
            _duration -= dt;
            if (_duration <= 0f)
            {
                Destroy(gameObject);
                return;
            }

            _tickTimer -= dt;
            if (_tickTimer <= 0f)
            {
                _tickTimer = _config.mageBlackholeTickInterval;
                ApplyZoneEffects();
            }
        }

        private void ApplyZoneEffects()
        {
            Vector3 center = transform.position;
            float   hh     = _config.mageBlackholeCylinderHalfHeight;
            float   r      = _config.mageBlackholeRadius;
            float   rSq    = r * r;

            // OverlapCapsule covers the full vertical cylinder generously.
            // Bottom/top caps extend halfHeight above and below the zone center.
            Vector3 bottom = center - Vector3.up * hh;
            Vector3 top    = center + Vector3.up * hh;
            int count = Physics.OverlapCapsuleNonAlloc(
                bottom, top, r, s_buf, -1, QueryTriggerInteraction.Ignore);

            for (int i = 0; i < count; i++)
            {
                Collider col = s_buf[i];

                HealthComponent hc = col.GetComponent<HealthComponent>();
                if (hc == null) hc = col.GetComponentInParent<HealthComponent>();
                if (hc == null) continue;

                if (hc == _ownerHealth) continue;       // skip caster
                if (hc.Team == _ownerTeam) continue;    // skip allies
                if (!hc.IsTargetable) continue;         // dead or stealthed

                // True cylindrical check: only horizontal (XZ) distance matters.
                Vector3 delta = hc.transform.position - center;
                delta.y = 0f;
                if (delta.sqrMagnitude > rSq) continue;

                // Slow — refreshed every tick so it persists while inside zone.
                hc.ApplySlow(_config.mageBlackholeSlowMultiplier,
                             _config.mageBlackholeSlowRefreshDuration, _ownerTeam);

                // Horizontal pull toward zone center.
                // Y already zeroed in delta; reuse it as the pull direction.
                Vector3 toCenter = -delta; // center - position, Y=0
                if (toCenter.sqrMagnitude > 0.01f)
                {
                    hc.ApplyPull(toCenter.normalized * _config.mageBlackholePullSpeed,
                                 _config.mageBlackholeSlowRefreshDuration);
                }
            }
        }
    }
}
