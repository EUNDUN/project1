using UnityEngine;
using Game.Commands;
using Game.Config;

namespace Game.Combat
{
    // Mage-specific ability logic, separated from AbilityController.
    // Implemented: Q fireball, RC double-blink teleport, E blackhole, F laser, Z Arcane Bolt, R Meteor Judgment.
    public class MageAbilityHandler : MonoBehaviour
    {
        private enum ZState { Idle, Aiming, Charging }

        private AbilityController   _ac;
        private GameConfig          _config;
        private HealthComponent     _ownerHealth;
        private Transform           _cameraTransform;
        private CharacterController _ownerCC;

        // Teleport charge state.
        private bool  _inRecastWindow   = false;  // true between 1st and 2nd teleport
        private float _recastTimer      = 0f;     // countdown: window remaining (s)
        private int   _chargesUsed      = 0;      // teleports used in current sequence

        // Post-teleport character passthrough.
        private float _passthroughTimer = 0f;

        // Fireball ammo / rate state. Survives death (ammo recharges even while dead).
        private int   _ammo;
        private float _rechargeTimer;
        private float _fireTimer;
        private int   _totalFireCount;

        // NonAlloc buffer for destination safety checks.
        private static readonly Collider[]   s_tpBuf      = new Collider[8];
        // NonAlloc buffer for ground Y detection (R spawn position correction).
        private static readonly RaycastHit[] s_groundHits = new RaycastHit[4];

        // Vertical offsets sampled when exact destination is inside geometry.
        private static readonly Vector3[] s_tpOffsets =
        {
            new Vector3(0f,  0.5f, 0f),
            new Vector3(0f, -0.5f, 0f),
            new Vector3(0f,  1.0f, 0f),
            new Vector3(0f, -1.0f, 0f),
        };

        // E — Blackhole cast state.
        private bool  _isCastingBlackhole = false;
        private float _blackholeCastTimer = 0f;

        // F — Laser state.
        private bool       _isCastingLaser  = false;
        private float      _laserCastTimer  = 0f;
        private bool       _isLaserActive   = false;
        private float      _laserTimer      = 0f;
        private float      _laserTickTimer  = 0f;
        private GameObject _laserVisualRoot = null;

        // NonAlloc buffers for laser tick — instance-level to avoid cross-instance interference.
        private static readonly Collider[]        s_laserBuf    = new Collider[8];
        private readonly        HealthComponent[] _hitThisTick  = new HealthComponent[8];
        // LoS check buffer: shared across sequential per-target checks within one tick (safe, single-threaded).
        private static readonly RaycastHit[]      s_losHits     = new RaycastHit[8];

        // Z — Arcane Bolt state.
        private ZState _zState      = ZState.Idle;
        private float  _chargeTimer = 0f;

        // R — Meteor Judgment cast state (UI only — world objects live independently).
        private bool  _isRCasting = false;
        private float _rOrbTimer  = 0f;

        // Cached materials — created once, shared across all instances.
        private static Material s_fireballMat;
        private static Material s_bigFireballMat;
        private static Material s_blackholeMat;
        private static Material s_laserMat;
        private static Material s_boltMat;

        // Bridge properties read by AbilityController for AbilityDebugUI.
        public bool  IsInRecastWindow    => _inRecastWindow;
        public float RecastTimer         => _recastTimer;
        public bool  IsPassthroughActive => _passthroughTimer > 0f;

        // Fireball state exposed to AbilityController.
        public int  FireballAmmo      => _ammo;
        public int  FireballMaxAmmo   => _config.mageFireballMaxAmmo;
        public bool NextFireballIsBig => _config.mageBigFireballEvery > 0
            && (_totalFireCount + 1) % _config.mageBigFireballEvery == 0;

        // Blackhole cast state exposed to AbilityController.
        public bool  IsCastingBlackhole => _isCastingBlackhole;
        public float BlackholeCastTimer => _blackholeCastTimer;

        // Laser state exposed to AbilityController.
        public bool  IsCastingLaser => _isCastingLaser;
        public bool  IsLaserActive  => _isLaserActive;
        public float LaserCastTimer => _laserCastTimer;
        public float LaserTimer     => _laserTimer;

        // Z Arcane Bolt state exposed to AbilityController.
        public bool  IsBlockingBasicAttack => _zState != ZState.Idle;
        public bool  IsZAiming             => _zState == ZState.Aiming;
        public bool  IsZCharging           => _zState == ZState.Charging;
        public float ZChargeTimer          => _chargeTimer;

        // R Meteor Judgment cast state exposed to AbilityController (true during 2s orb rise).
        public bool IsRCasting => _isRCasting;

        public void Init(AbilityController ac, GameConfig config,
                         HealthComponent ownerHealth, Transform cameraTransform)
        {
            _ac              = ac;
            _config          = config;
            _ownerHealth     = ownerHealth;
            _cameraTransform = cameraTransform;
            _ownerCC         = ownerHealth.GetComponent<CharacterController>();

            _ammo          = config.mageFireballMaxAmmo;
            _rechargeTimer = config.mageFireballRechargeInterval;
            _fireTimer     = 0f;
            _totalFireCount= 0;
        }

        // Passive: any non-Q skill success restores ammo, capped at max.
        private void AddFireballAmmoFromPassive()
        {
            _ammo = Mathf.Min(_ammo + _config.magePassiveAmmoGain, _config.mageFireballMaxAmmo);
        }

        // --- Per-frame update (called by AbilityController.Update)

        public void TickTimers(float dt)
        {
            // Passthrough timer: clear character-collision-ignore shortly after teleport.
            if (_passthroughTimer > 0f)
            {
                _passthroughTimer -= dt;
                if (_passthroughTimer <= 0f)
                {
                    _passthroughTimer = 0f;
                    _ac.TryEndZPassthrough();
                }
            }

            // Recast window: if player doesn't press again in time, start cooldown.
            if (_inRecastWindow)
            {
                _recastTimer -= dt;
                if (_recastTimer <= 0f)
                {
                    _inRecastWindow = false;
                    _recastTimer    = 0f;
                    _chargesUsed    = 0;
                    _ac.SetCooldown(AbilitySlot.RightClick, _ac.AbilityConfig.RightClickCooldown);
                }
            }

            // E — Blackhole cast timer: spawn zone when complete.
            if (_isCastingBlackhole)
            {
                _blackholeCastTimer -= dt;
                if (_blackholeCastTimer <= 0f)
                {
                    _isCastingBlackhole = false;
                    _blackholeCastTimer = 0f;
                    SpawnBlackholeZone();
                }
            }

            // F — Laser cast timer: begin active phase when cast delay expires.
            if (_isCastingLaser)
            {
                _laserCastTimer -= dt;
                if (_laserCastTimer <= 0f)
                {
                    _isCastingLaser = false;
                    _laserCastTimer = 0f;
                    BeginLaserActive();
                }
            }

            // F — Laser active: update visual, fire tick pulses.
            // Movement slow is applied once in BeginLaserActive and cleared in ClearLaserState.
            if (_isLaserActive)
            {
                _laserTimer     -= dt;
                _laserTickTimer -= dt;

                UpdateLaserVisual();

                if (_laserTickTimer <= 0f)
                {
                    _laserTickTimer = _config.mageLaserTickInterval;
                    FireLaserTick();
                }

                if (_laserTimer <= 0f)
                    ClearLaserState();
            }

            // Fireball ammo recharge: 1 ammo per interval. Runs even while dead.
            if (_ammo < _config.mageFireballMaxAmmo)
            {
                _rechargeTimer -= dt;
                if (_rechargeTimer <= 0f)
                {
                    _ammo++;
                    _rechargeTimer = _config.mageFireballRechargeInterval;
                }
            }
            else
            {
                // Keep timer primed so recharge starts immediately on next consumption.
                _rechargeTimer = _config.mageFireballRechargeInterval;
            }

            // Fire rate lockout.
            if (_fireTimer > 0f) _fireTimer = Mathf.Max(0f, _fireTimer - dt);

            // Z — cancel Aiming/Charging while stunned or dead.
            // Tick(cmd) is not called in those states, so TickZ never fires to clean up naturally.
            if (_zState != ZState.Idle && (_ownerHealth.IsDead || _ownerHealth.IsStunned))
                CancelZState();

            // R — clear cast flag once orb has finished rising (world objects continue independently).
            if (_isRCasting)
            {
                _rOrbTimer -= dt;
                if (_rOrbTimer <= 0f)
                {
                    _isRCasting = false;
                    _rOrbTimer  = 0f;
                }
            }
        }

        // Called from AbilityController.Tick every frame to process left-click charge/release
        // input while in Z Aiming or Charging state.
        public void TickZ(PlayerCommand cmd)
        {
            switch (_zState)
            {
                case ZState.Aiming:
                    if (cmd.AttackPressed)
                    {
                        _zState      = ZState.Charging;
                        _chargeTimer = 0f;
                    }
                    break;
                case ZState.Charging:
                    if (cmd.AttackReleased)
                    {
                        FireArcaneBolt();
                        _zState      = ZState.Idle;
                        _chargeTimer = 0f;
                    }
                    else
                    {
                        _chargeTimer = Mathf.Min(
                            _chargeTimer + Time.deltaTime,
                            _config.mageArcaneBoltMaxChargeTime);
                    }
                    break;
            }
        }

        // --- Ability routing

        public bool TryActivate(AbilitySlot slot)
        {
            // Laser is a channeling skill — all other abilities are blocked while active.
            if (_isLaserActive) return false;

            // RightClick (teleport) is always routed to TryRC() — Z Aiming/Charging state is preserved
            // so the player can teleport and still fire the held bolt on release.
            if (slot == AbilitySlot.RightClick) return TryRC();

            // Z Aiming/Charging: Z repress cancels; Q/E/F/R are blocked.
            if (_zState != ZState.Idle)
            {
                if (slot == AbilitySlot.Z) { CancelZState(); return true; }
                return false;
            }

            if (slot == AbilitySlot.Q) return TryFireQ();
            if (slot == AbilitySlot.E) return TryBeginBlackhole();
            if (slot == AbilitySlot.F) return TryBeginLaser();
            if (slot == AbilitySlot.Z) return TryBeginAiming();
            if (slot == AbilitySlot.R) return TryBeginR();
            return false;
        }

        public void HandleOwnerDeath()
        {
            ClearTeleportState();
            // Cancel mid-cast blackhole. Already-spawned zones live out their lifetime independently.
            _isCastingBlackhole = false;
            _blackholeCastTimer = 0f;
            // Cancel laser (cast or active) on death.
            _isCastingLaser = false;
            _laserCastTimer = 0f;
            if (_isLaserActive) ClearLaserState();
            CancelZState();
            // R: clear UI flag on death; world objects (orb/storm) continue independently.
            _isRCasting = false;
            _rOrbTimer  = 0f;
        }

        public void ForceCleanup()
        {
            ClearTeleportState();
            _isCastingBlackhole = false;
            _blackholeCastTimer = 0f;
            _isCastingLaser = false;
            _laserCastTimer = 0f;
            if (_isLaserActive) ClearLaserState();
            CancelZState();
            _isRCasting = false;
            _rOrbTimer  = 0f;
        }

        // --- F: Laser

        private bool TryBeginLaser()
        {
            if (_ac.GetCooldown(AbilitySlot.F) > 0f) return false;
            if (_isCastingLaser) return false;
            if (_ownerHealth.IsDead || _ownerHealth.IsStunned) return false;

            // Cooldown starts at cast begin.
            _ac.SetCooldown(AbilitySlot.F, _ac.AbilityConfig.FCooldown);
            _isCastingLaser = true;
            _laserCastTimer = _config.mageLaserCastTime;
            AddFireballAmmoFromPassive(); // passive: cast start restores Q ammo
            return true;
        }

        private void BeginLaserActive()
        {
            _isLaserActive  = true;
            _laserTimer     = _config.mageLaserDuration;
            _laserTickTimer = 0f; // fire damage tick immediately on first frame
            // Apply movement slow for the full laser duration + a small buffer.
            // Cleared explicitly in ClearLaserState — no per-frame refresh needed.
            _ownerHealth.SetSelfMoveSpeedMultiplier(
                _config.mageLaserMoveSpeedMultiplier, _config.mageLaserDuration + 0.2f);
            SpawnLaserVisual();
        }

        private void FireLaserTick()
        {
            if (_ownerHealth.IsDead) { ClearLaserState(); return; }

            Vector3 origin    = _cameraTransform.position;
            Vector3 direction = _cameraTransform.forward;
            float   range     = _config.mageLaserRange;
            float   radius    = _config.mageLaserRadius;

            // Capsule from camera to laser tip covers the entire beam volume.
            Vector3 tip   = origin + direction * range;
            int     count = Physics.OverlapCapsuleNonAlloc(
                origin, tip, radius, s_laserBuf, _config.attackLayerMask,
                QueryTriggerInteraction.Ignore);

            int hitCount = 0; // number of unique targets hit this tick

            for (int i = 0; i < count; i++)
            {
                Collider col = s_laserBuf[i];

                HealthComponent hc = col.GetComponent<HealthComponent>();
                if (hc == null) hc = col.GetComponentInParent<HealthComponent>();
                if (hc == null) continue;

                if (hc == _ownerHealth) continue;
                if (hc.Team == _ownerHealth.Team) continue;
                if (!hc.IsTargetable) continue;

                // Skip duplicates — one character may have multiple colliders.
                bool dup = false;
                for (int j = 0; j < hitCount; j++)
                    if (_hitThisTick[j] == hc) { dup = true; break; }
                if (dup) continue;

                // Line-of-sight check: only environment/wall colliders block the laser.
                // Characters between the caster and the target are NOT blockers — the laser
                // is wide-area and should pierce through other characters to hit all targets.
                // Strategy: RaycastNonAlloc → find the closest hit with no HealthComponent.
                // If that wall-distance < target-distance, the target is behind a wall.
                Vector3 torso    = hc.transform.position + Vector3.up * (_config.standHeight * 0.5f);
                Vector3 toTarget = torso - origin;
                float   dist     = toTarget.magnitude;
                if (dist > 0.01f)
                {
                    int losCount = Physics.RaycastNonAlloc(
                        origin, toTarget / dist, s_losHits, dist,
                        _config.attackLayerMask, QueryTriggerInteraction.Ignore);
                    float closestWallDist = float.MaxValue;
                    for (int k = 0; k < losCount; k++)
                    {
                        RaycastHit lh  = s_losHits[k];
                        HealthComponent lhc = lh.collider.GetComponent<HealthComponent>();
                        if (lhc == null) lhc = lh.collider.GetComponentInParent<HealthComponent>();
                        if (lhc != null) continue; // character collider — not a wall
                        if (lh.distance < closestWallDist) closestWallDist = lh.distance;
                    }
                    if (closestWallDist < dist) continue; // wall is closer than target → blocked
                }

                if (hitCount < _hitThisTick.Length)
                    _hitThisTick[hitCount++] = hc;

                hc.TakeDamage(new DamageInfo
                {
                    BaseDamage = _config.mageLaserDamage,
                    SourceTeam = _ownerHealth.Team,
                    SourceId   = string.Empty
                });

                // Horizontal knockback along laser forward direction.
                Vector3 kbDir = direction;
                kbDir.y = 0f;
                if (kbDir.sqrMagnitude > 0.001f)
                    hc.ApplyKnockback(kbDir.normalized * _config.mageLaserKnockbackSpeed,
                                      _config.mageLaserTickInterval, _ownerHealth.Team);
            }
        }

        private void SpawnLaserVisual()
        {
            if (s_laserMat == null)
            {
                s_laserMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                s_laserMat.color = new Color(0.20f, 0.0f, 0.70f);
            }

            _laserVisualRoot = new GameObject("LaserVisual");

            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.transform.SetParent(_laserVisualRoot.transform, false);
            float d = _config.mageLaserVisualRadius * 2f;
            // Cube local Z is the beam axis; scale along Z = beam length.
            cube.transform.localScale    = new Vector3(d, d, _config.mageLaserRange);
            cube.transform.localPosition = Vector3.zero;
            Object.Destroy(cube.GetComponent<Collider>());
            cube.GetComponent<Renderer>().sharedMaterial = s_laserMat;

            UpdateLaserVisual();
        }

        private void UpdateLaserVisual()
        {
            if (_laserVisualRoot == null) return;
            Vector3 origin = _cameraTransform.position;
            Vector3 fwd    = _cameraTransform.forward;
            // Root at midpoint of beam so the cube (centered at local origin) spans origin→tip.
            _laserVisualRoot.transform.SetPositionAndRotation(
                origin + fwd * (_config.mageLaserRange * 0.5f),
                Quaternion.LookRotation(fwd));
        }

        private void ClearLaserVisual()
        {
            if (_laserVisualRoot == null) return;
            Object.Destroy(_laserVisualRoot);
            _laserVisualRoot = null;
        }

        private void ClearLaserState()
        {
            _isLaserActive  = false;
            _laserTimer     = 0f;
            _laserTickTimer = 0f;
            _ownerHealth.ClearSelfMoveSpeedMultiplier();
            ClearLaserVisual();
        }

        // --- E: Blackhole

        private bool TryBeginBlackhole()
        {
            if (_ac.GetCooldown(AbilitySlot.E) > 0f) return false;
            if (_isCastingBlackhole) return false;
            if (_ownerHealth.IsDead || _ownerHealth.IsStunned) return false;

            // Cooldown starts immediately at cast begin (not after zone spawns).
            _ac.SetCooldown(AbilitySlot.E, _ac.AbilityConfig.ECooldown);
            _isCastingBlackhole = true;
            _blackholeCastTimer = _config.mageBlackholeCastTime;
            AddFireballAmmoFromPassive(); // passive: cast start restores Q ammo
            return true;
        }

        private void SpawnBlackholeZone()
        {
            // Horizontal direction from camera forward, height is a fixed offset from player feet.
            // Separating horizontal and vertical prevents absurd positions when looking straight up/down.
            Vector3 hfwd = _cameraTransform.forward;
            hfwd.y = 0f;
            if (hfwd.sqrMagnitude < 0.01f)
            {
                hfwd = _ownerHealth.transform.forward;
                hfwd.y = 0f;
            }
            hfwd.Normalize();

            Vector3 spawnPos = _ownerHealth.transform.position
                             + hfwd * _config.mageBlackholeRange
                             + Vector3.up * _config.mageBlackholeHeightOffset;

            var go = new GameObject("BlackholeZone");
            go.transform.position = spawnPos;

            var zone = go.AddComponent<MageBlackholeZone>();
            zone.Init(_ownerHealth.Team, _ownerHealth, _config);

            SpawnBlackholeVisual(go);
        }

        private void SpawnBlackholeVisual(GameObject parent)
        {
            if (s_blackholeMat == null)
            {
                s_blackholeMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                s_blackholeMat.color = new Color(0.06f, 0.0f, 0.18f);
            }

            var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.transform.SetParent(parent.transform, false);
            // diameter = 2 * visualRadius; kept separate from effect radius so both are tunable.
            sphere.transform.localScale = Vector3.one * (_config.mageBlackholeVisualRadius * 2f);
            Object.Destroy(sphere.GetComponent<Collider>());
            sphere.GetComponent<Renderer>().sharedMaterial = s_blackholeMat;
        }

        // --- Q: fireball (one shot per key press; rate-limited by _fireTimer and ammo)

        private bool TryFireQ()
        {
            if (_fireTimer > 0f) return false;
            if (_ownerHealth.IsDead || _ownerHealth.IsStunned) return false;
            if (_ammo <= 0) return false;

            _totalFireCount++;
            bool isBig = _config.mageBigFireballEvery > 0
                && _totalFireCount % _config.mageBigFireballEvery == 0;
            SpawnFireball(isBig);

            _ammo--;
            _fireTimer = _config.mageFireballFireInterval;
            return true;
        }

        // --- Right-click: double-blink teleport

        private bool TryRC()
        {
            if (_ac.GetCooldown(AbilitySlot.RightClick) > 0f) return false;

            if (!TryPerformTeleport()) return false;

            AddFireballAmmoFromPassive(); // passive: teleport success restores Q ammo
            _chargesUsed++;

            if (_chargesUsed >= _config.mageTeleportMaxCharges)
            {
                _inRecastWindow = false;
                _recastTimer    = 0f;
                _chargesUsed    = 0;
                _ac.SetCooldown(AbilitySlot.RightClick, _ac.AbilityConfig.RightClickCooldown);
            }
            else
            {
                _inRecastWindow = true;
                _recastTimer    = _config.mageTeleportRecastWindow;
            }
            return true;
        }

        // --- Teleport internals

        private bool TryPerformTeleport()
        {
            Vector3 dir  = GetTeleportDirection();
            Vector3 dest = _ownerHealth.transform.position + dir * _config.mageTeleportDistance;

            if (!TryFindSafeDestination(dest, out Vector3 safePos)) return false;

            ExecuteTeleport(safePos);
            return true;
        }

        // CC disable → position → enable is Unity's standard one-frame teleport.
        // cc.Move is NOT called here; FirstPersonMotor.Tick remains the only cc.Move caller.
        private void ExecuteTeleport(Vector3 dest)
        {
            _ownerCC.enabled = false;
            _ownerHealth.transform.position = dest;
            _ownerCC.enabled = true;

            _passthroughTimer = _config.mageTeleportPassthroughDuration;
            _ac.SetDashPassthrough(true);
        }

        private bool TryFindSafeDestination(Vector3 desired, out Vector3 safePos)
        {
            if (IsSafeDestination(desired)) { safePos = desired; return true; }

            for (int i = 0; i < s_tpOffsets.Length; i++)
            {
                Vector3 candidate = desired + s_tpOffsets[i];
                if (IsSafeDestination(candidate)) { safePos = candidate; return true; }
            }
            safePos = Vector3.zero;
            return false;
        }

        // Returns true if the capsule at dest overlaps no non-character solid geometry.
        private bool IsSafeDestination(Vector3 dest)
        {
            float   r      = _ownerCC.radius;
            float   h      = _ownerCC.height;
            Vector3 bottom = dest + Vector3.up * r;
            Vector3 top    = dest + Vector3.up * (h - r);
            int count = Physics.OverlapCapsuleNonAlloc(
                bottom, top, r, s_tpBuf, -1, QueryTriggerInteraction.Ignore);

            for (int i = 0; i < count; i++)
            {
                Collider col = s_tpBuf[i];
                if (col == _ownerCC) continue;
                HealthComponent hc = col.GetComponent<HealthComponent>();
                if (hc == null) hc = col.GetComponentInParent<HealthComponent>();
                if (hc != null) continue;
                return false;
            }
            return true;
        }

        // Teleport direction: horizontal camera forward, with fallback for straight up/down.
        private Vector3 GetTeleportDirection()
        {
            Vector3 fwd = _cameraTransform.forward;
            fwd.y = 0f;
            if (fwd.sqrMagnitude > 0.01f) return fwd.normalized;
            fwd = _ownerHealth.transform.forward;
            fwd.y = 0f;
            if (fwd.sqrMagnitude > 0.01f) return fwd.normalized;
            return Vector3.forward;
        }

        // --- Fireball internals

        private void SpawnFireball(bool isBig)
        {
            float damage = isBig ? _config.mageBigFireballDamage : _config.mageFireballDamage;
            float radius = isBig ? _config.mageBigFireballRadius : _config.mageFireballRadius;

            // Big fireball applies stun + knockback on hit; normal fireball passes zeroes (no effect).
            float stunDur = isBig ? _config.mageBigFireballStunDuration      : 0f;
            float kbSpeed = isBig ? _config.mageBigFireballKnockbackSpeed    : 0f;
            float kbDur   = isBig ? _config.mageBigFireballKnockbackDuration : 0f;

            Vector3 spawnPos = _cameraTransform.position + _cameraTransform.forward * 0.3f;
            Vector3 dir      = _cameraTransform.forward;

            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = isBig ? "BigFireball" : "Fireball";
            go.transform.SetPositionAndRotation(spawnPos, Quaternion.identity);
            go.transform.localScale = Vector3.one * (radius * 2f);
            Object.Destroy(go.GetComponent<Collider>());
            go.GetComponent<Renderer>().sharedMaterial = isBig ? GetBigFireballMat() : GetFireballMat();

            var proj = go.AddComponent<MageFireballProjectile>();
            proj.Init(_ownerHealth.Team, _ownerHealth, damage,
                      _config.mageFireballSpeed, _config.mageFireballRange,
                      radius, dir, _config.attackLayerMask,
                      stunDur, kbSpeed, kbDur);
        }

        private static Material GetFireballMat()
        {
            if (s_fireballMat != null) return s_fireballMat;
            s_fireballMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            s_fireballMat.color = new Color(1.0f, 0.45f, 0.0f);
            return s_fireballMat;
        }

        private static Material GetBigFireballMat()
        {
            if (s_bigFireballMat != null) return s_bigFireballMat;
            s_bigFireballMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            s_bigFireballMat.color = new Color(1.0f, 0.10f, 0.0f);
            return s_bigFireballMat;
        }

        // --- Z: Arcane Bolt

        private bool TryBeginAiming()
        {
            if (_ac.GetCooldown(AbilitySlot.Z) > 0f) return false;
            if (_ownerHealth.IsDead || _ownerHealth.IsStunned) return false;
            _zState = ZState.Aiming;
            return true;
        }

        private void CancelZState()
        {
            _zState      = ZState.Idle;
            _chargeTimer = 0f;
            // No cooldown consumed on cancel.
        }

        private void FireArcaneBolt()
        {
            if (_ownerHealth.IsDead) { CancelZState(); return; }

            float chargeRatio = Mathf.Clamp01(_chargeTimer / _config.mageArcaneBoltMaxChargeTime);
            float damage = Mathf.Lerp(_config.mageArcaneBoltMinDamage, _config.mageArcaneBoltMaxDamage, chargeRatio);
            float speed  = Mathf.Lerp(_config.mageArcaneBoltMinSpeed,  _config.mageArcaneBoltMaxSpeed,  chargeRatio);
            float stun   = Mathf.Lerp(_config.mageArcaneBoltMinStun,   _config.mageArcaneBoltMaxStun,   chargeRatio);

            Vector3 spawnPos = _cameraTransform.position + _cameraTransform.forward * 0.3f;
            Vector3 dir      = _cameraTransform.forward;

            _ac.SetCooldown(AbilitySlot.Z, _ac.AbilityConfig.ZCooldown);
            AddFireballAmmoFromPassive(); // passive: bolt fired restores Q ammo (not on aim/cancel)

            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "ArcaneBolt";
            go.transform.SetPositionAndRotation(spawnPos, Quaternion.identity);
            go.transform.localScale = Vector3.one * (_config.mageArcaneBoltRadius * 2f);
            Object.Destroy(go.GetComponent<Collider>());
            go.GetComponent<Renderer>().sharedMaterial = GetBoltMat();

            var proj = go.AddComponent<MageArcaneBoltProjectile>();
            proj.Init(_ownerHealth.Team, _ownerHealth, damage, stun,
                      speed, _config.mageArcaneBoltRange,
                      _config.mageArcaneBoltRadius, dir, _config.attackLayerMask);
        }

        private static Material GetBoltMat()
        {
            if (s_boltMat != null) return s_boltMat;
            s_boltMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            s_boltMat.color = new Color(0.10f, 0.35f, 1.0f);  // bright blue
            return s_boltMat;
        }

        // --- R: Meteor Judgment

        private bool TryBeginR()
        {
            if (_ac.GetCooldown(AbilitySlot.R) > 0f) return false;
            if (_ownerHealth.IsDead || _ownerHealth.IsStunned) return false;

            // Cooldown starts at cast time; IsRCasting stays true for orbRiseDuration (UI only).
            _ac.SetCooldown(AbilitySlot.R, _ac.AbilityConfig.RCooldown);
            _isRCasting = true;
            _rOrbTimer  = _config.mageMeteorOrbRiseDuration;
            AddFireballAmmoFromPassive(); // passive: R activation restores Q ammo

            SpawnMeteorOrbSequence();
            return true;
        }

        private void SpawnMeteorOrbSequence()
        {
            // Horizontal forward from camera; fallback to character forward if looking straight up/down.
            Vector3 camFwd = _cameraTransform.forward;
            camFwd.y = 0f;
            if (camFwd.sqrMagnitude < 0.01f)
            {
                camFwd = _ownerHealth.transform.forward;
                camFwd.y = 0f;
                if (camFwd.sqrMagnitude < 0.01f) camFwd = _ownerHealth.transform.right;
            }
            camFwd.Normalize();

            Vector3 ownerPos = _ownerHealth.transform.position;

            // Storm center XZ: mageMeteorAimDistance ahead of caster.
            // Y is snapped to ground below that XZ point so meteors always land at floor level.
            Vector3 stormXZ = ownerPos + camFwd * _config.mageMeteorAimDistance;
            float   stormY;
            if (!TryFindGroundY(stormXZ, out stormY)) stormY = ownerPos.y;
            Vector3 stormCenter = new Vector3(stormXZ.x, stormY, stormXZ.z);

            // Orb spawns slightly in front of the caster, y snapped to caster's ground level.
            float   orbY;
            if (!TryFindGroundY(ownerPos, out orbY)) orbY = ownerPos.y;
            Vector3 orbXZ    = ownerPos + camFwd * 1.5f;
            Vector3 spawnPos = new Vector3(orbXZ.x, orbY, orbXZ.z);

            var go = new GameObject("MageMeteorOrbSeq");
            go.transform.position = spawnPos;
            var seq = go.AddComponent<MageMeteorOrbSequence>();
            seq.Init(_ownerHealth.Team, _ownerHealth, _config, stormCenter);
        }

        // Finds ground Y below the given XZ world position.
        // Casts from 20 m above; skips character colliders.
        private static bool TryFindGroundY(Vector3 xzPos, out float groundY)
        {
            Vector3 origin  = xzPos + Vector3.up * 20f;
            int     count   = Physics.RaycastNonAlloc(origin, Vector3.down, s_groundHits,
                                  40f, ~0, QueryTriggerInteraction.Ignore);
            groundY         = xzPos.y;
            float closest   = float.MaxValue;
            bool  found     = false;

            for (int i = 0; i < count; i++)
            {
                RaycastHit h = s_groundHits[i];
                HealthComponent hc = h.collider.GetComponent<HealthComponent>();
                if (hc == null) hc = h.collider.GetComponentInParent<HealthComponent>();
                if (hc != null) continue; // skip character capsules

                if (h.distance < closest)
                {
                    closest = h.distance;
                    groundY = h.point.y;
                    found   = true;
                }
            }
            return found;
        }

        // --- Teleport state cleanup

        private void ClearTeleportState()
        {
            // If charges were consumed but cooldown not yet started (recast window open),
            // apply cooldown now so the ability isn't free after respawn.
            if (_inRecastWindow && _chargesUsed > 0 &&
                _ac.GetCooldown(AbilitySlot.RightClick) <= 0f)
            {
                _ac.SetCooldown(AbilitySlot.RightClick, _ac.AbilityConfig.RightClickCooldown);
            }

            _inRecastWindow   = false;
            _recastTimer      = 0f;
            _chargesUsed      = 0;
            if (_passthroughTimer > 0f)
            {
                _passthroughTimer = 0f;
                _ac.SetDashPassthrough(false);
            }
            // Fireball state intentionally NOT reset here — ammo/count persist through death.
        }
    }
}
