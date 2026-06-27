using UnityEngine;

namespace Game.Combat
{
    // Rogue Z — stun bomb placed at the cast origin just before the teleport.
    // Explodes after a fuse delay, dealing damage and stunning nearby enemies.
    // Designed for future pooling: call Init() to reset all state before (re)use.
    public class RogueStunBomb : MonoBehaviour
    {
        private Team      _ownerTeam;
        private float     _damage;
        private float     _blastRadius;
        private float     _stunDuration;
        private LayerMask _hitLayerMask;
        private float     _fuseTimer;

        private static readonly Collider[] s_buf = new Collider[16];
        private static Material s_blastMat;

        public void Init(Team ownerTeam, float damage, float blastRadius, float stunDuration,
                         float fuseTime, LayerMask hitLayerMask)
        {
            _ownerTeam    = ownerTeam;
            _damage       = damage;
            _blastRadius  = blastRadius;
            _stunDuration = stunDuration;
            _fuseTimer    = fuseTime;
            _hitLayerMask = hitLayerMask;
        }

        void Update()
        {
            _fuseTimer -= Time.deltaTime;
            if (_fuseTimer > 0f) return;
            Explode();
            Destroy(gameObject);
        }

        private void Explode()
        {
            int count = Physics.OverlapSphereNonAlloc(
                transform.position, _blastRadius, s_buf,
                _hitLayerMask, QueryTriggerInteraction.Ignore);

            for (int i = 0; i < count; i++)
            {
                HealthComponent hc = s_buf[i].GetComponent<HealthComponent>();
                if (hc == null) hc = s_buf[i].GetComponentInParent<HealthComponent>();
                if (hc == null || hc.IsDead || hc.Team == _ownerTeam) continue;

                hc.TakeDamage(new DamageInfo
                {
                    BaseDamage = _damage,
                    SourceTeam = _ownerTeam,
                    SourceId   = string.Empty,
                });
                hc.ApplyStun(_stunDuration, _ownerTeam);
            }

            SpawnBlastVisual();
        }

        // Brief solid-colour sphere flash (0.15 s). Replace with VFX in a later stage.
        private void SpawnBlastVisual()
        {
            GameObject vis = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            vis.transform.position   = transform.position;
            vis.transform.localScale = Vector3.one * (_blastRadius * 2f);
            Destroy(vis.GetComponent<Collider>());
            vis.GetComponent<Renderer>().sharedMaterial = GetBlastMat();
            Destroy(vis, 0.15f);
        }

        private static Material GetBlastMat()
        {
            if (s_blastMat == null)
            {
                s_blastMat = new Material(Shader.Find("Standard"));
                s_blastMat.color = new Color(1f, 0.45f, 0f); // orange burst
            }
            return s_blastMat;
        }
    }
}