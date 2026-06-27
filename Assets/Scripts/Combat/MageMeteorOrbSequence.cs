using UnityEngine;
using Game.Config;

namespace Game.Combat
{
    // Spawned by MageAbilityHandler when Mage R is activated.
    // Rises for mageMeteorOrbRiseDuration seconds, then spawns MageMeteorStorm and self-destructs.
    // Survives the caster's death — cannot be cancelled once spawned.
    public class MageMeteorOrbSequence : MonoBehaviour
    {
        private Team            _ownerTeam;
        private HealthComponent _ownerHealth;
        private GameConfig      _config;
        private Vector3         _stormCenter;
        private Vector3         _riseStart;
        private float           _riseTimer;
        private GameObject      _orbGo;

        private static Material s_orbMat;

        public void Init(Team ownerTeam, HealthComponent ownerHealth,
                         GameConfig config, Vector3 stormCenter)
        {
            _ownerTeam   = ownerTeam;
            _ownerHealth = ownerHealth;
            _config      = config;
            _stormCenter = stormCenter;
            _riseStart   = transform.position;
            _riseTimer   = 0f;
            SpawnOrbVisual();
        }

        void Update()
        {
            _riseTimer += Time.deltaTime;
            float t = Mathf.Clamp01(_riseTimer / _config.mageMeteorOrbRiseDuration);

            if (_orbGo != null)
                _orbGo.transform.position = _riseStart + Vector3.up * (_config.mageMeteorOrbRiseHeight * t);

            if (_riseTimer >= _config.mageMeteorOrbRiseDuration)
            {
                if (_orbGo != null) { Destroy(_orbGo); _orbGo = null; }
                SpawnStorm();
                Destroy(gameObject);
            }
        }

        private void SpawnOrbVisual()
        {
            _orbGo = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            _orbGo.name = "MeteorOrb";
            _orbGo.transform.position = _riseStart;
            _orbGo.transform.localScale = Vector3.one * 1.0f;
            Destroy(_orbGo.GetComponent<Collider>());
            _orbGo.GetComponent<Renderer>().sharedMaterial = GetOrbMat();
        }

        private void SpawnStorm()
        {
            // Pass the orb's peak position so meteors arc FROM here to their impact points.
            Vector3 launchPos = _riseStart + Vector3.up * _config.mageMeteorOrbRiseHeight;
            var go = new GameObject("MageMeteorStorm");
            go.transform.position = _stormCenter;
            var storm = go.AddComponent<MageMeteorStorm>();
            storm.Init(_ownerTeam, _ownerHealth, _config, launchPos);
        }

        private static Material GetOrbMat()
        {
            if (s_orbMat != null) return s_orbMat;
            s_orbMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            s_orbMat.color = new Color(1.0f, 0.15f, 0.05f);
            return s_orbMat;
        }
    }
}
