using System.Collections.Generic;
using UnityEngine;

namespace Game.Combat
{
    // Stationary damage zone spawned along the Rogue's dash path.
    // Each rift hits a target at most once during its lifetime.
    // Designed for future pooling: Init() resets all mutable state.
    public class RogueDimensionalRift : MonoBehaviour
    {
        // 0.08 s interval = ~12.5 scans/s.
        // Catches any CharacterController that missed an OnTriggerEnter event,
        // without being expensive per-frame.
        private const float CheckInterval = 0.08f;

        private Team      _ownerTeam;
        private float     _damage;
        private float     _lifetime;
        private LayerMask _hitLayerMask;
        private Vector3   _halfExtents; // cached once in Init() — avoids per-scan GetComponent
        private float     _checkTimer;  // time remaining until next scan; starts at 0 to scan immediately

        // Per-rift hit tracking — cleared by Init() on reuse.
        // Small initial capacity: max targets in a 3v3 match is 5.
        private readonly List<HealthComponent> _hitTargets = new List<HealthComponent>(8);

        // Shared static buffer: allocated once, reused every scan. No per-frame heap allocation.
        private static readonly Collider[] s_overlapBuf = new Collider[16];

        public void Init(Team ownerTeam, float damage, float lifetime, LayerMask hitLayerMask)
        {
            _ownerTeam    = ownerTeam;
            _damage       = damage;
            _lifetime     = lifetime;
            _hitLayerMask = hitLayerMask;
            _hitTargets.Clear();

            // Cache box dimensions so Update() never calls GetComponent.
            BoxCollider bc = GetComponent<BoxCollider>();
            _halfExtents   = bc != null ? bc.size * 0.5f : Vector3.one * 0.5f;

            // _checkTimer = 0 causes the very first Update() to run a scan immediately,
            // covering targets already inside the zone when the rift spawns.
            _checkTimer = 0f;
        }

        void Update()
        {
            _lifetime -= Time.deltaTime;
            if (_lifetime <= 0f)
            {
                Destroy(gameObject);
                return;
            }

            _checkTimer -= Time.deltaTime;
            if (_checkTimer > 0f) return;

            _checkTimer = CheckInterval;
            ScanOverlap();
        }

        // Polls for overlapping targets using a non-allocating box query.
        // OverlapBoxNonAlloc already filters by _hitLayerMask, so TryHit needs no layer check.
        private void ScanOverlap()
        {
            int count = Physics.OverlapBoxNonAlloc(
                transform.position,
                _halfExtents,
                s_overlapBuf,
                transform.rotation,
                _hitLayerMask,
                QueryTriggerInteraction.Ignore);

            for (int i = 0; i < count; i++)
                TryHit(s_overlapBuf[i]);
        }

        private void TryHit(Collider other)
        {
            // HealthComponent lives on the same GO as the CharacterController (or a parent).
            HealthComponent hc = other.GetComponent<HealthComponent>();
            if (hc == null) hc = other.GetComponentInParent<HealthComponent>();
            if (hc == null || hc.IsDead) return;
            if (_hitTargets.Contains(hc)) return; // same rift, same target — hit only once

            _hitTargets.Add(hc);
            // HealthComponent.TakeDamage enforces no-friendly-fire via SourceTeam.
            hc.TakeDamage(new DamageInfo
            {
                BaseDamage = _damage,
                SourceTeam = _ownerTeam,
                SourceId   = string.Empty,
            });
        }
    }
}