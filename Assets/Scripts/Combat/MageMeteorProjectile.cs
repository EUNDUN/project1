using UnityEngine;
using Game.Config;

namespace Game.Combat
{
    // A single meteor spawned by MageMeteorStorm.
    // Phase 1 (warningDuration): sits at spawn height; warning disc shows on the ground below.
    // Phase 2 (fallDuration): falls to impact point.
    // Phase 3: OverlapSphereNonAlloc damage + self-destruct.
    public class MageMeteorProjectile : MonoBehaviour
    {
        private Team            _ownerTeam;
        private HealthComponent _ownerHealth;
        private GameConfig      _config;
        private Vector3         _impactPos;
        private Vector3         _startPos;
        private float           _arcHeight;   // extra upward bulge at arc midpoint (proportional to horizontal distance)
        private float           _arcDuration; // total flight time (warningDuration + fallDuration)
        private float           _timer;
        private bool            _hasImpacted;
        private GameObject      _warningGo;

        // Static NonAlloc buffers — safe because Unity Update is single-threaded.
        // hitCount is a local variable per Impact() call, so no cross-frame contamination.
        private static readonly Collider[]        s_cols  = new Collider[16];
        private static readonly HealthComponent[] s_hcBuf = new HealthComponent[16];

        private static Material s_meteorMat;
        private static Material s_warningMat;

        public void Init(Team ownerTeam, HealthComponent ownerHealth,
                         GameConfig config, Vector3 impactPos)
        {
            _ownerTeam   = ownerTeam;
            _ownerHealth = ownerHealth;
            _config      = config;
            _impactPos   = impactPos;
            _startPos    = transform.position;
            _timer       = 0f;
            _hasImpacted = false;

            // Arc height scales with horizontal distance — far impacts get a more dramatic arc.
            var hDiff  = new Vector3(_startPos.x - impactPos.x, 0f, _startPos.z - impactPos.z);
            _arcHeight = hDiff.magnitude * 0.25f;

            // Total flight time = warning + fall config values combined.
            _arcDuration = config.mageMeteorWarningDuration + config.mageMeteorFallDuration;

            SpawnWarningIndicator();
        }

        void Update()
        {
            if (_hasImpacted) return;

            _timer += Time.deltaTime;
            float t = Mathf.Clamp01(_timer / _arcDuration);

            // Sinusoidal arc: Sin(PI*t) is 0 at both endpoints and peaks at the midpoint.
            // Combined with the Lerp from a high launch point to the ground impact,
            // this produces a mortar-like trajectory that bulges upward then drops.
            transform.position = Vector3.Lerp(_startPos, _impactPos, t)
                               + Vector3.up * Mathf.Sin(Mathf.PI * t) * _arcHeight;

            if (_timer >= _arcDuration)
                Impact();
        }

        void OnDestroy()
        {
            if (_warningGo != null) { Destroy(_warningGo); _warningGo = null; }
        }

        private void Impact()
        {
            _hasImpacted = true;

            if (_warningGo != null) { Destroy(_warningGo); _warningGo = null; }

            int count = Physics.OverlapSphereNonAlloc(
                _impactPos, _config.mageMeteorImpactRadius, s_cols,
                _config.attackLayerMask, QueryTriggerInteraction.Ignore);

            int hitCount = 0;
            for (int i = 0; i < count; i++)
            {
                HealthComponent hc = s_cols[i].GetComponent<HealthComponent>();
                if (hc == null) hc = s_cols[i].GetComponentInParent<HealthComponent>();
                if (hc == null) continue;
                if (_ownerHealth != null && hc == _ownerHealth) continue;
                if (hc.Team == _ownerTeam) continue;
                if (!hc.IsTargetable) continue;

                bool dup = false;
                for (int j = 0; j < hitCount; j++)
                    if (s_hcBuf[j] == hc) { dup = true; break; }
                if (dup) continue;

                if (hitCount < s_hcBuf.Length) s_hcBuf[hitCount++] = hc;

                hc.TakeDamage(new DamageInfo
                {
                    BaseDamage = _config.mageMeteorDamage,
                    SourceTeam = _ownerTeam,
                    SourceId   = string.Empty
                });
            }

            for (int i = 0; i < hitCount; i++) s_hcBuf[i] = null;
            Destroy(gameObject);
        }

        private void SpawnWarningIndicator()
        {
            _warningGo = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            _warningGo.name = "MeteorWarning";
            _warningGo.transform.position = _impactPos + Vector3.up * 0.05f;
            float d = _config.mageMeteorImpactRadius * 2f;
            _warningGo.transform.localScale = new Vector3(d, 0.05f, d);
            Destroy(_warningGo.GetComponent<Collider>());
            _warningGo.GetComponent<Renderer>().sharedMaterial = GetWarningMat();
        }

        // Public so MageMeteorStorm can set sharedMaterial before adding component.
        public static Material GetMeteorMat()
        {
            if (s_meteorMat != null) return s_meteorMat;
            s_meteorMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            s_meteorMat.color = new Color(1.0f, 0.30f, 0.05f);
            return s_meteorMat;
        }

        private static Material GetWarningMat()
        {
            if (s_warningMat != null) return s_warningMat;
            s_warningMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            s_warningMat.color = new Color(0.80f, 0.15f, 0.0f);
            return s_warningMat;
        }
    }
}
