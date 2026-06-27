using UnityEngine;

namespace Game.Combat
{
    // Moving X-shaped shockwave for Warrior Z (Parry Retreat).
    // Travels in the warrior's forward direction at spawn time.
    // X cross-section: two beams at ±45° relative to travel direction.
    // Each enemy is damaged at most once per wave instance.
    // Replace SpawnVisual with a particle VFX call when assets are ready.
    public class WarriorZWave : MonoBehaviour
    {
        private float           _speed;
        private float           _maxDist;
        private float           _distTraveled;
        private float           _damage;
        private float           _knockback;
        private HealthComponent _ownerHc;
        private LayerMask       _layerMask;
        private float           _armHalfSpan;  // half-length of each X arm (sideways)
        private float           _armHalfW;     // half-thickness of each X arm beam
        private float           _armHalfH;     // half-height

        // Persists across frames: each target is hit at most once per wave instance.
        private readonly HealthComponent[] _hitCache = new HealthComponent[16];
        private int                        _hitCount;

        private static readonly Collider[] s_buf = new Collider[16];
        private static Material            s_mat;

        // Called immediately after AddComponent. transform.position must already be set.
        public void Init(Vector3 forward, float speed, float travelDist,
                         float damage, float knockback,
                         HealthComponent ownerHc, LayerMask layerMask,
                         float armHalfSpan, float armHalfW, float armHalfH)
        {
            _speed       = speed;
            _maxDist     = travelDist;
            _damage      = damage;
            _knockback   = knockback;
            _ownerHc     = ownerHc;
            _layerMask   = layerMask;
            _armHalfSpan = armHalfSpan;
            _armHalfW    = armHalfW;
            _armHalfH    = armHalfH;

            transform.rotation = Quaternion.LookRotation(forward, Vector3.up);
            SpawnVisual();
        }

        void Update()
        {
            float step = _speed * Time.deltaTime;
            transform.position += transform.forward * step;
            _distTraveled += step;
            ScanHits();
            if (_distTraveled >= _maxDist)
                Destroy(gameObject);
        }

        void ScanHits()
        {
            // Rotate around the travel axis (local Z) so the X stands upright in the player's view.
            // halfExtents: X = beam thickness (thin), Y = arm half-span (long), Z = depth in travel.
            Vector3    half = new Vector3(_armHalfW, _armHalfSpan, _armHalfH);
            Quaternion r1   = transform.rotation * Quaternion.Euler(0f, 0f,  45f);
            Quaternion r2   = transform.rotation * Quaternion.Euler(0f, 0f, -45f);
            _hitCount = Collect(half, r1, _hitCount);
            _hitCount = Collect(half, r2, _hitCount);
        }

        private int Collect(Vector3 half, Quaternion rot, int count)
        {
            int found = Physics.OverlapBoxNonAlloc(
                transform.position, half, s_buf, rot, _layerMask, QueryTriggerInteraction.Ignore);

            for (int i = 0; i < found; i++)
            {
                HealthComponent hc = s_buf[i].GetComponent<HealthComponent>();
                if (hc == null) hc = s_buf[i].GetComponentInParent<HealthComponent>();
                if (hc == null || hc == _ownerHc || hc.IsDead) continue;
                if (hc.Team == _ownerHc.Team) continue;

                bool already = false;
                for (int j = 0; j < count; j++)
                    if (_hitCache[j] == hc) { already = true; break; }
                if (already) continue;
                if (count >= _hitCache.Length) continue;

                _hitCache[count++] = hc;

                hc.TakeDamage(new DamageInfo
                {
                    BaseDamage = _damage,
                    SourceTeam = _ownerHc.Team,
                    SourceId   = string.Empty,
                });

                if (_knockback > 0f)
                {
                    Vector3 dir = hc.transform.position - transform.position;
                    dir.y = 0f;
                    if (dir.sqrMagnitude < 0.001f) dir = transform.forward;
                    hc.ApplyKnockback(dir.normalized * _knockback, 0.3f, _ownerHc.Team);
                }
            }
            return count;
        }

        // Two cube children at ±45° relative to parent forward, travel with the GO automatically.
        private void SpawnVisual()
        {
            float armW    = _armHalfW    * 2f;  // beam thickness (X)
            float armSpan = _armHalfSpan * 2f;  // arm total length (Y — stands upright)
            float armH    = _armHalfH    * 2f;  // depth in travel direction (Z)

            for (int i = 0; i < 2; i++)
            {
                float zAngle = i == 0 ? 45f : -45f;
                GameObject vis = GameObject.CreatePrimitive(PrimitiveType.Cube);
                vis.transform.SetParent(transform, false);
                vis.transform.localRotation = Quaternion.Euler(0f, 0f, zAngle); // rotate around travel axis
                vis.transform.localPosition = Vector3.zero;
                vis.transform.localScale    = new Vector3(armW, armSpan, armH);
                Destroy(vis.GetComponent<Collider>());
                vis.GetComponent<Renderer>().sharedMaterial = GetMat();
            }
        }

        private static Material GetMat()
        {
            if (s_mat == null)
            {
                s_mat = new Material(Shader.Find("Standard"));
                s_mat.SetFloat("_Mode", 3f);
                s_mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                s_mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                s_mat.SetInt("_ZWrite", 0);
                s_mat.EnableKeyword("_ALPHABLEND_ON");
                s_mat.renderQueue = 3000;
                s_mat.color = new Color(0.20f, 0.80f, 1.00f, 0.55f); // cyan-blue, semi-transparent
            }
            return s_mat;
        }
    }
}
