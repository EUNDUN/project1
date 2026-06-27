using UnityEngine;
using System.Collections.Generic;
using Game.Config;

namespace Game.Combat
{
    // Fast ranged projectile for Archer basic attack, enhanced Q shots, and F Barrage.
    //
    // Shot types:
    //   Basic         — hits first enemy; single-target damage; passes through allies/self.
    //   Shock/Fire/Ice — explode at contact point via OverlapSphereNonAlloc.
    //   Barrage       — F skill rapid-fire; hits first enemy + slow; no Q explosion; no gauge gain.
    //
    // Pooling: Spawn()/SpawnBarrage() pull from s_pool (shared cap 32).
    // All GO creation occurs once in CreateNew(); reuse calls SetActive + SetPositionAndRotation + Init().
    //
    // Gauge gain: optional onHitGaugeGain callback fired when the projectile actually deals damage.
    //   Basic  → +archerFGaugeGainOnBasicHit (only if target was not shielded/invulnerable).
    //   Q shot → +archerFGaugeGainOnEnhancedHit once per explosion if ≥1 enemy was hit.
    //   Barrage → null (no gauge gain — prevents infinite loop).
    public class ArcherBasicProjectile : MonoBehaviour
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
        private ArcherShotType  _shotType;
        private GameConfig      _config;
        // Fired when this projectile successfully deals damage to at least one enemy.
        // Basic: once per hit (only if not blocked by shield/invuln).
        // Enhanced: once per explosion if ≥1 enemy reached damage step.
        // Barrage: null — no gauge gain.
        private System.Action<float> _onHitGaugeGain;

        // Per-instance glow child — created once in CreateNew(), toggled/recoloured each Init().
        private GameObject _glowGO;
        private Renderer   _glowRenderer;

        // Pool cap prevents unbounded growth during high fire-rate bursts.
        private const int PoolCapacity = 32;
        private static readonly List<ArcherBasicProjectile> s_pool =
            new List<ArcherBasicProjectile>(PoolCapacity);

        // Shared SphereCast buffer — safe: Update() is single-threaded.
        private static readonly RaycastHit[] s_hits = new RaycastHit[8];

        // Shared AoE explosion buffers — used only during ExplodeAt(), which is single-frame.
        private static readonly Collider[]        s_explodeBuf   = new Collider[16];
        private static readonly HealthComponent[] s_explodeHCBuf = new HealthComponent[16];

        // Material caches — one instance per type, shared across all pool slots.
        private static Material s_bodyMat;
        private static Material s_glowShockMat;
        private static Material s_glowFireMat;
        private static Material s_glowIceMat;

        // ── Static factory — basic attack and Q enhanced shots ────────────────────
        public static void Spawn(HealthComponent ownerHealth, Vector3 origin,
                                 Vector3 direction, GameConfig config,
                                 ArcherShotType shotType = ArcherShotType.Basic,
                                 System.Action<float> onHitGaugeGain = null)
        {
            direction = direction.normalized;
            Quaternion rot  = Quaternion.FromToRotation(Vector3.up, direction);
            var proj = GetFromPool(origin, rot);
            proj.Init(ownerHealth.Team, ownerHealth,
                      config.archerBasicProjectileDamage,
                      config.archerBasicProjectileSpeed,
                      config.archerBasicProjectileRange,
                      config.archerBasicProjectileRadius,
                      direction, config.attackLayerMask,
                      shotType, config, onHitGaugeGain);
        }

        // ── Static factory — F Barrage shot ──────────────────────────────────────
        // Applies spread, uses archerFDamage, no gauge gain.
        public static void SpawnBarrage(HealthComponent ownerHealth, Vector3 origin,
                                        Vector3 direction, GameConfig config)
        {
            // Random spread (allocation-free: Quaternion.Euler returns a value type).
            float a   = config.archerFSpreadAngle * 0.5f;
            float rx  = (Random.value * 2f - 1f) * a;
            float ry  = (Random.value * 2f - 1f) * a;
            direction = (Quaternion.Euler(rx, ry, 0f) * direction).normalized;

            Quaternion rot  = Quaternion.FromToRotation(Vector3.up, direction);
            var proj = GetFromPool(origin, rot);
            proj.Init(ownerHealth.Team, ownerHealth,
                      config.archerFDamage,
                      config.archerBasicProjectileSpeed,
                      config.archerBasicProjectileRange,
                      config.archerBasicProjectileRadius,
                      direction, config.attackLayerMask,
                      ArcherShotType.Barrage, config, null);
        }

        // ── Pool retrieval ─────────────────────────────────────────────────────────
        private static ArcherBasicProjectile GetFromPool(Vector3 origin, Quaternion rot)
        {
            ArcherBasicProjectile proj = null;
            while (s_pool.Count > 0)
            {
                int last = s_pool.Count - 1;
                var c    = s_pool[last];
                s_pool.RemoveAt(last);
                if (c != null) { proj = c; break; }
            }
            if (proj != null)
            {
                proj.gameObject.SetActive(true);
                proj.transform.SetPositionAndRotation(origin, rot);
            }
            else
            {
                proj = CreateNew(origin, rot);
            }
            return proj;
        }

        // ── Instance setup (called on first use and every pool reuse) ─────────────
        public void Init(Team ownerTeam, HealthComponent ownerHealth,
                         float damage, float speed, float range, float radius,
                         Vector3 direction, LayerMask layerMask,
                         ArcherShotType shotType, GameConfig config,
                         System.Action<float> onHitGaugeGain = null)
        {
            _ownerTeam       = ownerTeam;
            _ownerHealth     = ownerHealth;
            _damage          = damage;
            _speed           = speed;
            _rangeRemaining  = range;
            _radius          = radius;
            _direction       = direction.normalized;
            _layerMask       = layerMask;
            _lifetime        = 3f;
            _shotType        = shotType;
            _config          = config;
            _onHitGaugeGain  = onHitGaugeGain;
            // Scale the projectile (and its collision radius) for OverdriveBasic.
            // All other types use the base capsule dimensions from CreateNew().
            if (shotType == ArcherShotType.OverdriveBasic)
            {
                float s = config.archerOverdriveProjectileScaleMultiplier;
                _radius *= s;
                transform.localScale = new Vector3(0.20f * s, 0.28f * s, 0.20f * s);
            }
            else
            {
                transform.localScale = new Vector3(0.20f, 0.28f, 0.20f);
            }
            ApplyGlow(shotType);
        }

        // ── Per-frame movement + hit detection ────────────────────────────────────
        void Update()
        {
            float dt   = Time.deltaTime;
            float dist = _speed * dt;
            _lifetime -= dt;

            if (_lifetime <= 0f) { ReturnToPool(); return; }

            // Clamp cast step to remaining range — prevents last-frame collision gap.
            float step = Mathf.Min(dist, _rangeRemaining);

            int count = Physics.SphereCastNonAlloc(
                transform.position, _radius, _direction, s_hits, step,
                _layerMask, QueryTriggerInteraction.Ignore);

            float           bestDist = float.MaxValue;
            HealthComponent bestHC   = null;
            bool            hitFound = false;

            for (int i = 0; i < count; i++)
            {
                RaycastHit      hit = s_hits[i];
                HealthComponent hc  = hit.collider.GetComponent<HealthComponent>();
                if (hc == null) hc  = hit.collider.GetComponentInParent<HealthComponent>();

                if (hc != null)
                {
                    if (hc == _ownerHealth)    continue; // pass through self
                    if (hc.Team == _ownerTeam) continue; // pass through allies
                    if (hc.IsDead)             continue;
                }
                // null hc = environment — always a valid blocker

                if (hit.distance < bestDist)
                {
                    bestDist = hit.distance;
                    bestHC   = hc;
                    hitFound = true;
                }
            }

            if (hitFound)
            {
                if (_shotType == ArcherShotType.Basic)
                {
                    if (bestHC != null)
                    {
                        float hpBefore = bestHC.CurrentHp;
                        bestHC.TakeDamage(new DamageInfo
                        {
                            BaseDamage = _damage,
                            SourceTeam = _ownerTeam,
                            SourceId   = string.Empty
                        });
                        // Gauge gain only when HP actually decreased (shield/invuln → no gain).
                        if (bestHC.CurrentHp < hpBefore)
                            _onHitGaugeGain?.Invoke(_config.archerFGaugeGainOnBasicHit);
                    }
                }
                else if (_shotType == ArcherShotType.OverdriveBasic)
                {
                    // R Overdrive Basic: explode at contact point.
                    // Direct-hit target is included in the AoE — no separate single-target damage.
                    ExplodeOverdriveAt(transform.position + _direction * bestDist);
                }
                else if (_shotType == ArcherShotType.Barrage)
                {
                    // Barrage: damage + slow treated as one attack event.
                    // TryConsumeShieldFrom absorbs both; avoids the TakeDamage-then-ApplySlow
                    // sequencing where TakeDamage consumes the shield and slow bypasses it.
                    if (bestHC != null && !bestHC.TryConsumeShieldFrom(_ownerTeam))
                    {
                        bestHC.TakeDamage(new DamageInfo
                        {
                            BaseDamage = _damage,
                            SourceTeam = _ownerTeam,
                            SourceId   = string.Empty
                        });
                        bestHC.ApplySlow(_config.archerFSlowMultiplier,
                                         _config.archerFSlowDuration, _ownerTeam);
                    }
                }
                else
                {
                    // Enhanced Q shot: explode at contact point (enemy or environment).
                    ExplodeAt(transform.position + _direction * bestDist);
                }
                ReturnToPool();
                return;
            }

            transform.position += _direction * step;
            _rangeRemaining    -= step;
            if (_rangeRemaining <= 0f) ReturnToPool();
        }

        // ── AoE explosion — R Overdrive Basic ────────────────────────────────────
        // Reuses s_explodeBuf/s_explodeHCBuf (single-threaded, single-frame safe).
        // Reward (F gauge + Z CD) fires once per explosion if any target's HP actually decreased.
        private void ExplodeOverdriveAt(Vector3 center)
        {
            int  count    = Physics.OverlapSphereNonAlloc(center, _config.archerOverdriveExplosionRadius,
                                s_explodeBuf, _layerMask, QueryTriggerInteraction.Ignore);
            int  hitCount = 0;
            bool rewardGiven = false;

            for (int i = 0; i < count; i++)
            {
                HealthComponent hc = s_explodeBuf[i].GetComponent<HealthComponent>();
                if (hc == null) hc = s_explodeBuf[i].GetComponentInParent<HealthComponent>();
                if (hc == null || hc == _ownerHealth || hc.Team == _ownerTeam
                    || hc.IsDead || !hc.IsTargetable)
                    continue;

                // Dedup: same hc across multiple colliders counts once.
                bool already = false;
                for (int j = 0; j < hitCount; j++)
                    if (s_explodeHCBuf[j] == hc) { already = true; break; }
                if (already || hitCount >= s_explodeHCBuf.Length) continue;

                s_explodeHCBuf[hitCount++] = hc;

                // Shield absorbs the entire explosion as one attack event.
                if (hc.TryConsumeShieldFrom(_ownerTeam)) continue;

                float hpBefore = hc.CurrentHp;
                hc.TakeDamage(new DamageInfo
                {
                    BaseDamage = _config.archerOverdriveExplosionDamage,
                    SourceTeam = _ownerTeam,
                    SourceId   = string.Empty
                });

                // Basic hit reward: F gauge gain + Z cooldown refund — once per explosion.
                if (!rewardGiven && hc.CurrentHp < hpBefore)
                {
                    _onHitGaugeGain?.Invoke(_config.archerFGaugeGainOnBasicHit);
                    rewardGiven = true;
                }
            }

            for (int i = 0; i < hitCount; i++) s_explodeHCBuf[i] = null;
        }

        // ── AoE explosion — Shock / Fire / Ice ────────────────────────────────────
        private void ExplodeAt(Vector3 center)
        {
            float radius, damage;
            switch (_shotType)
            {
                case ArcherShotType.Shock:
                    radius = _config.archerQShockRadius;
                    damage = _config.archerQShockDamage;
                    break;
                case ArcherShotType.Fire:
                    radius = _config.archerQFireRadius;
                    damage = _config.archerQFireDamage;
                    break;
                case ArcherShotType.Ice:
                    radius = _config.archerQIceRadius;
                    damage = _config.archerQIceDamage;
                    break;
                default:
                    return;
            }

            int count    = Physics.OverlapSphereNonAlloc(center, radius, s_explodeBuf,
                               _layerMask, QueryTriggerInteraction.Ignore);
            int hitCount = 0;
            bool gaugeGained = false;

            for (int i = 0; i < count; i++)
            {
                HealthComponent hc = s_explodeBuf[i].GetComponent<HealthComponent>();
                if (hc == null) hc = s_explodeBuf[i].GetComponentInParent<HealthComponent>();
                if (hc == null || hc == _ownerHealth || hc.Team == _ownerTeam
                    || hc.IsDead || !hc.IsTargetable)
                    continue;

                // Dedup: same hc on multiple colliders counts once per explosion.
                bool already = false;
                for (int j = 0; j < hitCount; j++)
                    if (s_explodeHCBuf[j] == hc) { already = true; break; }
                if (already || hitCount >= s_explodeHCBuf.Length) continue;

                s_explodeHCBuf[hitCount++] = hc;

                // Shield absorbs the entire attack event (damage + CC as one unit).
                // Call AFTER dedup so a multi-collider target is tracked once and
                // the shield is only tested once per explosion per target.
                if (hc.TryConsumeShieldFrom(_ownerTeam)) continue;

                float hpBefore = hc.CurrentHp;
                hc.TakeDamage(new DamageInfo
                {
                    BaseDamage = damage,
                    SourceTeam = _ownerTeam,
                    SourceId   = string.Empty
                });

                if (_shotType == ArcherShotType.Shock)
                    hc.ApplyStun(_config.archerQShockStunDuration, _ownerTeam);
                else if (_shotType == ArcherShotType.Ice)
                    hc.ApplySlow(_config.archerQIceSlowMultiplier,
                                 _config.archerQIceSlowDuration, _ownerTeam);

                // Gauge gain fires once per explosion, only when HP actually decreased
                // (invulnerable targets pass the shield check but take no damage).
                if (!gaugeGained && hc.CurrentHp < hpBefore)
                {
                    _onHitGaugeGain?.Invoke(_config.archerFGaugeGainOnEnhancedHit);
                    gaugeGained = true;
                }
            }

            for (int i = 0; i < hitCount; i++) s_explodeHCBuf[i] = null;
        }

        // ── Pool return ────────────────────────────────────────────────────────────
        private void ReturnToPool()
        {
            _onHitGaugeGain = null; // release delegate reference
            gameObject.SetActive(false);
            if (s_pool.Count < PoolCapacity)
                s_pool.Add(this);
            else
                Destroy(gameObject);
        }

        // ── Glow visual — toggled and recoloured on each Init() ───────────────────
        private void ApplyGlow(ArcherShotType shotType)
        {
            if (_glowGO == null) return;
            if (shotType == ArcherShotType.Basic    ||
                shotType == ArcherShotType.Barrage  ||
                shotType == ArcherShotType.OverdriveBasic)
            {
                _glowGO.SetActive(false);
                return;
            }
            _glowGO.SetActive(true);
            _glowRenderer.sharedMaterial = shotType switch
            {
                ArcherShotType.Shock => GetGlowShockMat(),
                ArcherShotType.Fire  => GetGlowFireMat(),
                ArcherShotType.Ice   => GetGlowIceMat(),
                _                    => GetGlowShockMat(),
            };
        }

        // ── GO allocation — called once per pool slot ──────────────────────────────
        private static ArcherBasicProjectile CreateNew(Vector3 origin, Quaternion rotation)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            go.name = "BasicShot";
            go.transform.SetPositionAndRotation(origin, rotation);
            go.transform.localScale = new Vector3(0.20f, 0.28f, 0.20f);
            Object.Destroy(go.GetComponent<Collider>());
            go.GetComponent<Renderer>().sharedMaterial = GetBodyMat();

            // Glow sphere child — compensates parent non-uniform scale for ~0.40 m world sphere.
            var glowGO = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            glowGO.name = "Glow";
            glowGO.transform.SetParent(go.transform, false);
            glowGO.transform.localPosition = Vector3.zero;
            glowGO.transform.localScale    = new Vector3(2.0f, 1.43f, 2.0f);
            Object.Destroy(glowGO.GetComponent<Collider>());
            glowGO.SetActive(false);

            var proj = go.AddComponent<ArcherBasicProjectile>();
            proj._glowGO       = glowGO;
            proj._glowRenderer = glowGO.GetComponent<Renderer>();
            return proj;
        }

        // ── Material helpers — one shared instance per type ────────────────────────
        private static Material GetBodyMat()
        {
            if (s_bodyMat != null) return s_bodyMat;
            s_bodyMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            s_bodyMat.color = new Color(0.72f, 0.40f, 0.08f);
            return s_bodyMat;
        }

        private static Material GetGlowShockMat()
        {
            if (s_glowShockMat != null) return s_glowShockMat;
            s_glowShockMat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            s_glowShockMat.color = new Color(1.0f, 0.95f, 0.10f);
            return s_glowShockMat;
        }

        private static Material GetGlowFireMat()
        {
            if (s_glowFireMat != null) return s_glowFireMat;
            s_glowFireMat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            s_glowFireMat.color = new Color(1.0f, 0.22f, 0.03f);
            return s_glowFireMat;
        }

        private static Material GetGlowIceMat()
        {
            if (s_glowIceMat != null) return s_glowIceMat;
            s_glowIceMat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            s_glowIceMat.color = new Color(0.10f, 0.72f, 1.0f);
            return s_glowIceMat;
        }
    }
}
