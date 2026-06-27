using UnityEngine;

namespace Game.Combat
{
    // Spawned by RogueAbilityHandler.TryQ().
    // Phase 1 (travelDuration s): moves forward travelDistance m.
    // Phase 2 (zoneDuration s): stays in place; damages + slows enemies every tickInterval s.
    // Init-pattern allows future object pooling without modifying callers.
    public class RogueGiantShurikenZone : MonoBehaviour
    {
        private Team    _sourceTeam;
        private float   _damagePerTick;
        private float   _tickInterval;
        private float   _travelDuration;
        private float   _travelDistance;
        private float   _zoneDuration;
        private float   _halfSize;       // half of full zone width/depth
        private float   _slowMultiplier;
        private float   _slowDuration;
        private int     _layerMask;
        private Vector3 _travelDir;
        private Vector3 _startPos;
        private float   _finalX;        // cached final X position (reused in stationary phase)
        private float   _finalY;
        private float   _finalZ;

        private float      _age;
        private float      _tickTimer;
        private bool       _initialized;
        private bool       _snapped;     // true once we've locked position at end of travel
        private GameObject _visual;

        // Shared scratch buffers — NonAlloc calls are sequential (Unity single-threaded Update).
        private static readonly Collider[]        s_buf      = new Collider[16];
        private static readonly HealthComponent[] s_hitCache = new HealthComponent[16];

        private static Material s_mat;

        public void Init(Team sourceTeam, float damagePerTick, float tickInterval,
                         float travelDuration, float travelDistance, float zoneDuration,
                         float zoneSize, float slowMultiplier, float slowDuration,
                         Vector3 direction, int layerMask)
        {
            _sourceTeam     = sourceTeam;
            _damagePerTick  = damagePerTick;
            _tickInterval   = tickInterval;
            _travelDuration = travelDuration;
            _travelDistance = travelDistance;
            _zoneDuration   = zoneDuration;
            _halfSize       = zoneSize * 0.5f;
            _slowMultiplier = slowMultiplier;
            _slowDuration   = slowDuration;
            _travelDir      = direction; // already normalised by caller
            _layerMask      = layerMask;
            _startPos       = transform.position;
            _age            = 0f;
            _tickTimer      = tickInterval; // first tick fires at tickInterval seconds
            _snapped        = false;
            _initialized    = true;

            Vector3 finalPos = _startPos + _travelDir * _travelDistance;
            _finalX = finalPos.x; _finalY = finalPos.y; _finalZ = finalPos.z;

            SpawnVisual(zoneSize);
        }

        void Update()
        {
            if (!_initialized) return;

            float dt = Time.deltaTime;
            _age += dt;

            // Destroy when total lifetime exceeded.
            if (_age >= _travelDuration + _zoneDuration)
            {
                Destroy(gameObject);
                return;
            }

            // Position update.
            if (_age <= _travelDuration)
            {
                float t = _age / _travelDuration;
                transform.position = _startPos + _travelDir * (_travelDistance * t);
            }
            else if (!_snapped)
            {
                // Snap exactly to the final position on the first stationary frame.
                transform.position = new Vector3(_finalX, _finalY, _finalZ);
                _snapped = true;
            }

            // Spin the visual for visual feedback.
            if (_visual != null)
                _visual.transform.Rotate(Vector3.up, 180f * dt, Space.World);

            // Damage tick.
            _tickTimer -= dt;
            if (_tickTimer <= 0f)
            {
                _tickTimer = _tickInterval;
                DamageTick();
            }
        }

        private void DamageTick()
        {
            // Check at body-centre height so the box reliably overlaps standing characters.
            Vector3 center      = transform.position + Vector3.up;
            Vector3 halfExtents = new Vector3(_halfSize, 1.2f, _halfSize);

            int count = Physics.OverlapBoxNonAlloc(
                center, halfExtents, s_buf, transform.rotation,
                _layerMask, QueryTriggerInteraction.Ignore);

            int hitCount = 0;
            for (int i = 0; i < count; i++)
            {
                HealthComponent hc = s_buf[i].GetComponent<HealthComponent>();
                if (hc == null) hc = s_buf[i].GetComponentInParent<HealthComponent>();
                if (hc == null || hc.IsDead) continue;
                if (hc.Team == _sourceTeam) continue; // no friendly fire

                // Dedup within this tick — same hc on multiple colliders counts once.
                bool already = false;
                for (int j = 0; j < hitCount; j++)
                    if (s_hitCache[j] == hc) { already = true; break; }
                if (already) continue;

                // Cache full: skip rather than hit without tracking (prevents repeated hits).
                if (hitCount >= s_hitCache.Length) continue;

                s_hitCache[hitCount++] = hc;
                hc.TakeDamage(new DamageInfo
                {
                    BaseDamage = _damagePerTick,
                    SourceTeam = _sourceTeam,
                    SourceId   = string.Empty,
                });
                hc.ApplySlow(_slowMultiplier, _slowDuration, _sourceTeam);
            }

            // Release hit-cache references so GC can collect dead targets.
            for (int i = 0; i < hitCount; i++) s_hitCache[i] = null;
        }

        private void SpawnVisual(float zoneSize)
        {
            _visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
            _visual.transform.SetParent(transform, false);
            _visual.transform.localPosition = Vector3.zero;
            _visual.transform.localScale    = new Vector3(zoneSize, 0.18f, zoneSize);
            Destroy(_visual.GetComponent<Collider>());
            _visual.GetComponent<Renderer>().sharedMaterial = GetMat();
        }

        private static Material GetMat()
        {
            if (s_mat == null)
            {
                s_mat = new Material(Shader.Find("Standard"));
                s_mat.color = new Color(0.15f, 0.88f, 1f); // bright cyan
            }
            return s_mat;
        }
    }
}
