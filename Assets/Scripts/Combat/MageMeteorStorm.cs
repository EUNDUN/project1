using UnityEngine;
using Game.Config;

namespace Game.Combat
{
    // Manages the meteor storm spawned by MageMeteorOrbSequence.
    // Distributes meteors randomly within the storm radius over the storm duration.
    // Lives independently of the caster — not cancelled on caster death.
    public class MageMeteorStorm : MonoBehaviour
    {
        private Team            _ownerTeam;
        private HealthComponent _ownerHealth;
        private GameConfig      _config;
        private Vector3         _launchPos;   // orb explosion point — all meteors arc FROM here

        private float _timer;
        private int   _spawned;
        private float _nextSpawnTime;
        private float _selfDestructTime;

        private static readonly RaycastHit[] s_groundHits = new RaycastHit[4];

        public void Init(Team ownerTeam, HealthComponent ownerHealth, GameConfig config, Vector3 launchPos)
        {
            _ownerTeam   = ownerTeam;
            _ownerHealth = ownerHealth;
            _config      = config;
            _launchPos   = launchPos;
            _timer       = 0f;
            _spawned     = 0;
            _nextSpawnTime = config.mageMeteorStormDelay;

            // Self-destruct after all meteors have completed their full lifecycle.
            _selfDestructTime = config.mageMeteorStormDelay
                              + config.mageMeteorStormDuration
                              + config.mageMeteorWarningDuration
                              + config.mageMeteorFallDuration
                              + 0.5f;
        }

        void Update()
        {
            _timer += Time.deltaTime;

            // Use while loop to handle large deltaTime without skipping meteors.
            if (_config.mageMeteorCount > 0)
            {
                float interval = _config.mageMeteorStormDuration / _config.mageMeteorCount;
                while (_spawned < _config.mageMeteorCount && _timer >= _nextSpawnTime)
                {
                    SpawnMeteor();
                    _spawned++;
                    _nextSpawnTime += interval;
                }
            }

            if (_timer >= _selfDestructTime)
                Destroy(gameObject);
        }

        private void SpawnMeteor()
        {
            // Uniform random position within storm radius (flat XZ disk).
            float r     = _config.mageMeteorStormRadius * Mathf.Sqrt(Random.value);
            float angle = Random.value * (2f * Mathf.PI);
            var   offset = new Vector3(Mathf.Cos(angle) * r, 0f, Mathf.Sin(angle) * r);
            Vector3 impactPos = transform.position + offset;
            // Per-meteor ground Y correction: each random XZ may sit at a different terrain height.
            if (TryFindGroundY(impactPos, out float groundY))
                impactPos.y = groundY;

            // Meteor starts at the orb explosion point and arcs outward to the impact point.
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "Meteor";
            go.transform.SetPositionAndRotation(_launchPos, Quaternion.identity);
            go.transform.localScale = Vector3.one * (_config.mageMeteorVisualRadius * 2f);
            Destroy(go.GetComponent<Collider>());
            go.GetComponent<Renderer>().sharedMaterial = MageMeteorProjectile.GetMeteorMat();

            var proj = go.AddComponent<MageMeteorProjectile>();
            proj.Init(_ownerTeam, _ownerHealth, _config, impactPos);
        }

        private static bool TryFindGroundY(Vector3 xzPos, out float groundY)
        {
            Vector3 origin = xzPos + Vector3.up * 20f;
            int count = Physics.RaycastNonAlloc(
                origin, Vector3.down, s_groundHits, 40f, ~0, QueryTriggerInteraction.Ignore);
            groundY = xzPos.y;
            float closest = float.MaxValue;
            bool  found   = false;
            for (int i = 0; i < count; i++)
            {
                RaycastHit h  = s_groundHits[i];
                HealthComponent hc = h.collider.GetComponent<HealthComponent>();
                if (hc == null) hc = h.collider.GetComponentInParent<HealthComponent>();
                if (hc != null) continue;
                if (h.distance < closest) { closest = h.distance; groundY = h.point.y; found = true; }
            }
            return found;
        }
    }
}
