using UnityEngine;
using Game.Config;

namespace Game.Combat
{
    // Warrior-specific ability logic, separated from AbilityController.
    // Q skill flow: Q pressed → rising self-move + rising slash (hit1) → fall until landing
    //               → landing triggers falling-slam AoE (hit2) with launch.
    public class WarriorAbilityHandler : MonoBehaviour
    {
        private AbilityController   _ac;
        private GameConfig          _config;
        private HealthComponent     _ownerHealth;
        private Transform           _cameraTransform;
        private CharacterController _ownerCC;   // cached at Init for isGrounded polling

        // Q state machine
        private enum QState { Idle, Casting }
        private QState  _qState    = QState.Idle;
        private float   _qTimer    = 0f;    // accumulates while Casting
        private bool    _hit1Fired = false;
        private bool    _hit2Fired = false; // true once slam fires
        private Vector3 _castFwd;           // locked at Q press time

        // Self-movement: rising (timer-driven) → falling (runs until grounded or timeout).
        private Vector3 _selfMoveVelocity = Vector3.zero;
        private float   _selfMoveTimer    = 0f;
        private bool    _isFalling        = false;
        private Vector3 _fallVelocity     = Vector3.zero;
        private float   _fallTimer        = 0f;
        // Guards against slam firing the same frame the fall phase starts (character
        // may still be grounded before rising movement fully lifts them off).
        private bool    _wasAirborne      = false;

        // Static NonAlloc physics buffers (sequential Update — no threading concern).
        private static readonly Collider[]        s_buf        = new Collider[16];
        private static readonly HealthComponent[] s_hitCache   = new HealthComponent[16];
        private static readonly RaycastHit[]      s_groundHits = new RaycastHit[4];
        private static readonly Collider[]        s_rcBuf      = new Collider[8];
        private static readonly HealthComponent[] s_rcHitCache = new HealthComponent[8];

        // Guard (E) state
        private bool       _isGuarding  = false;
        private float      _guardTimer  = 0f;
        private GameObject _guardVisual = null;

        public bool  IsGuarding => _isGuarding;
        public float GuardTimer => _guardTimer;

        // Z (Parry Retreat) state
        private enum ZState { Idle, Active }
        private ZState  _zState     = ZState.Idle;
        private float   _zTimer     = 0f;
        private Vector3 _zOrigin    = Vector3.zero; // world position at cast time (wave 1 origin)
        private Vector3 _zFwd       = Vector3.zero; // travel direction locked at cast time
        private bool    _zWave2Done = false;

        // F (Slash Barrage) state
        private enum FState { Idle, Casting }
        private FState _fState       = FState.Idle;
        private float  _fTimer       = 0f;   // elapsed cast time
        private float  _fDamageTimer = 0f;   // countdown to next damage tick
        private float  _fVisualTimer = 0f;   // countdown to next visual tick
        private int    _fDamageCount = 0;    // damage ticks fired so far
        private int    _fTotalTicks  = 0;    // total ticks = round(duration / interval)
        private int    _fVisualCount = 0;    // visual ticks fired (for alternating direction)

        private static readonly HealthComponent[] s_fHitCache = new HealthComponent[8];

        public bool  IsFCasting => _fState == FState.Casting;
        public float FCastTimer => _config != null ? Mathf.Max(0f, _config.warriorFDuration - _fTimer) : 0f;

        // RC dash impact state — true while warrior is dashing and collision detection is active.
        private bool _isRCDashing = false;

        // R (Great Sword Descent) state — warrior ultimate
        private enum RState { Idle, Casting }
        private RState    _rState      = RState.Idle;
        private float     _rTimer      = 0f;
        private Vector3   _rOrigin     = Vector3.zero;   // world pos locked at cast start; judgement + visual anchor
        private Vector3   _rMoveVelocity = Vector3.zero; // forced vertical velocity during rise/drop phases
        private Transform _rSwordPivot = null;           // spawned at drop start; ref for drop animation
        private GameObject _rSwordRoot = null;           // floor marker + sword visual root

        public bool  IsRCasting => _rState == RState.Casting;
        public float RCastTimer => _config != null ? Mathf.Max(0f, _config.warriorRCastTime - _rTimer) : 0f;

        // Material cache — created once, never per-cast.
        private static Material s_hit1Mat;
        private static Material s_hit2Mat;
        private static Material s_guardMat;
        private static Material s_fMat;
        private static Material s_rBladeMat;
        private static Material s_rGuardMat;
        private static Material s_rFloorMat;

        // Read by AbilityController.TryClearPassthrough to keep unit-passthrough active during Q.
        internal bool IsQCasting => _qState == QState.Casting;

        // Read by AbilityController.DashVelocity → FirstPersonMotor every frame.
        // R vertical movement has highest priority; Q self-move is secondary.
        public Vector3 SelfMoveVelocity
        {
            get
            {
                // R rise/drop overrides Q self-move.
                if (_rState == RState.Casting && _rMoveVelocity.sqrMagnitude > 0.001f)
                    return _rMoveVelocity;
                if (_selfMoveTimer > 0f) return _selfMoveVelocity; // Q rising
                if (_isFalling)          return _fallVelocity;     // Q falling
                return Vector3.zero;
            }
        }

        // --- Initialisation (called by AbilityController.Start)

        public void Init(AbilityController ac, GameConfig config,
                         HealthComponent ownerHealth, Transform cameraTransform)
        {
            _ac              = ac;
            _config          = config;
            _ownerHealth     = ownerHealth;
            _cameraTransform = cameraTransform;
            _ownerCC         = ownerHealth.GetComponent<CharacterController>();
        }

        // --- Per-frame timer update (called by AbilityController.Update)

        public void TickTimers(float dt)
        {
            // Guard timer: counts down independently of Q state.
            if (_isGuarding)
            {
                _guardTimer -= dt;
                if (_guardTimer <= 0f) EndGuard();
            }

            // Tick rising self-move timer.
            if (_selfMoveTimer > 0f)
            {
                _selfMoveTimer -= dt;
                if (_selfMoveTimer <= 0f) { _selfMoveTimer = 0f; _selfMoveVelocity = Vector3.zero; }
            }

            // RC dash: per-frame enemy detection — stops dash and launches enemies on contact.
            if (_isRCDashing && _ac.IsDashing)
                CheckRCDashImpact();

            // Z timer: wave 2 fires after the configured delay, then Z returns to Idle.
            if (_zState == ZState.Active)
            {
                _zTimer += dt;
                if (!_zWave2Done && _zTimer >= _config.warriorZWaveDelay)
                {
                    _zWave2Done = true;
                    FireZWave(_ownerHealth.transform.position, _zFwd);
                    ClearZState();
                }
            }

            // F timer: visual every visualInterval, damage every damageInterval.
            if (_fState == FState.Casting)
            {
                _fTimer       += dt;
                _fDamageTimer -= dt;
                _fVisualTimer -= dt;

                if (_fVisualTimer <= 0f)
                {
                    _fVisualTimer = _config.warriorFVisualInterval;
                    SpawnFVisual();
                    _fVisualCount++;
                }

                if (_fDamageTimer <= 0f)
                {
                    bool isFinal = _fDamageCount >= _fTotalTicks - 1;
                    FireFHit(isFinal);
                    _fDamageCount++;
                    if (isFinal)
                        ClearFState();
                    else
                        _fDamageTimer = _config.warriorFDamageInterval;
                }

                if (_fState == FState.Casting && _fTimer >= _config.warriorFDuration + 0.2f)
                    ClearFState();
            }

            // R timer: rise (1.9 s) → drop (0.1 s) → impact. Player invulnerable throughout.
            if (_rState == RState.Casting)
            {
                _rTimer += dt;

                if (_rTimer < _config.warriorRRiseDuration)
                {
                    // Rising phase: constant upward velocity (set once in TryR; keep stable here).
                    _rMoveVelocity = Vector3.up * (_config.warriorRRiseHeight / _config.warriorRRiseDuration);
                }
                else if (_rTimer < _config.warriorRCastTime)
                {
                    // Drop phase: fast descent.
                    _rMoveVelocity = Vector3.down * (_config.warriorRRiseHeight / _config.warriorRDropDuration);

                    // Spawn sword visual once at transition into drop.
                    if (_rSwordPivot == null) SpawnRSword();

                    // Animate sword pivot falling from above toward impact point.
                    if (_rSwordPivot != null)
                    {
                        float bladeH      = 2.0f * _config.warriorRSwordScaleMultiplier;
                        float dropElapsed  = _rTimer - _config.warriorRRiseDuration;
                        float dropProgress = Mathf.Clamp01(dropElapsed / _config.warriorRDropDuration);
                        float localY       = Mathf.Lerp(
                            _config.warriorRRiseHeight + bladeH * 0.5f,
                            bladeH * 0.35f, dropProgress);
                        Vector3 lp = _rSwordPivot.localPosition;
                        _rSwordPivot.localPosition = new Vector3(lp.x, localY, lp.z);
                    }
                }
                else
                {
                    _rMoveVelocity = Vector3.zero;
                    FireRImpact();
                    ClearRState();
                }
            }

            if (_qState != QState.Casting) return;
            _qTimer += dt;

            // Hit 1 — rising slash: fires within the rising phase, damage only.
            if (!_hit1Fired && _qTimer >= _config.warriorQHit1Delay)
            {
                _hit1Fired = true;
                FireHit1();
            }

            // Transition to fall phase once rising movement duration has elapsed.
            if (!_isFalling && _qTimer >= _config.warriorQRiseDuration)
                StartFallingPhase();

            // Fall phase: descend until CharacterController lands, then fire slam.
            if (_isFalling)
            {
                _fallTimer += dt;

                // Track whether the player has left the ground at least once.
                // This prevents an instant slam if Q is used while already standing.
                if (_ownerCC != null && !_ownerCC.isGrounded)
                    _wasAirborne = true;

                // Landing detected — fire slam and end Q.
                if (!_hit2Fired && _wasAirborne && _ownerCC != null && _ownerCC.isGrounded)
                {
                    _hit2Fired = true;
                    FireHit2();
                    ClearQCast();
                    return;
                }

                // Safety timeout: cancel Q without slam if fall takes too long.
                if (_fallTimer >= _config.warriorQMaxFallDuration)
                    ClearQCast();
            }
        }

        // --- Ability activation (called by AbilityController.TryActivate)

        public bool TryActivate(AbilitySlot slot)
        {
            return slot switch
            {
                AbilitySlot.RightClick => TryRCDash(),
                AbilitySlot.Q          => TryQ(),
                AbilitySlot.E          => TryE(),
                AbilitySlot.R          => TryR(),
                AbilitySlot.F          => TryF(),
                AbilitySlot.Z          => TryZ(),
                _                      => false,
            };
        }

        // Called by AbilityController when the shared dash timer ends naturally (no impact).
        public void OnDashEnded() { _isRCDashing = false; }

        // On death: fully cancel Q so there is no stale movement, landing detection,
        // or passthrough left active. Q relies on the player's motor and isGrounded;
        // both stop on death, making continued Q execution undefined.
        // Future: when hit-resolution is server-authoritative and decoupled from the
        // local motor, reconsider allowing in-flight strikes to resolve independently.
        public void HandleOwnerDeath()
        {
            _isRCDashing = false;
            ClearQCast();
            EndGuard();
            ClearZState();
            ClearFState();
            ClearRState();
        }

        // Called by AbilityController.OnDisable / OnDestroy.
        public void ForceCleanup()
        {
            _isRCDashing = false;
            ClearQCast();
            EndGuard();
            ClearZState();
            ClearFState();
            ClearRState();
        }

        // --- Private ability implementations

        private bool TryRCDash()
        {
            if (_ac.GetCooldown(AbilitySlot.RightClick) > 0f) return false;
            if (_rState == RState.Casting) return false; // R ultimate in progress
            if (_qState == QState.Casting) return false; // Q self-move and RC dash must not overlap
            _ac.SetCooldown(AbilitySlot.RightClick, _ac.AbilityConfig.RightClickCooldown);
            _isRCDashing = true;
            _ac.StartDash(_config.warriorDashDistance, _config.warriorDashDuration);
            return true;
        }

        private bool TryQ()
        {
            if (_ac.GetCooldown(AbilitySlot.Q) > 0f) return false;
            if (_rState == RState.Casting) return false; // R ultimate in progress
            if (_qState == QState.Casting) return false; // re-press during cast is ignored
            if (_ac.IsDashing) return false;             // RC dash in progress — no overlapping moves

            _ac.SetCooldown(AbilitySlot.Q, _ac.AbilityConfig.QCooldown);

            // Lock cast direction at activation time (horizontal only).
            Vector3 fwd = _cameraTransform.forward; fwd.y = 0f;
            if (fwd.sqrMagnitude < 0.001f) { fwd = _ownerHealth.transform.forward; fwd.y = 0f; }
            if (fwd.sqrMagnitude < 0.001f) fwd = _ownerHealth.transform.right;
            _castFwd = fwd.normalized;

            _qState      = QState.Casting;
            _qTimer      = 0f;
            _hit1Fired   = false;
            _hit2Fired   = false;
            _isFalling   = false;
            _fallTimer   = 0f;
            _wasAirborne = false;

            _ac.SetDashPassthrough(true); // pass through units for the full Q duration

            // Begin rising self-movement immediately (forward + upward).
            // Velocity = displacement / duration so the exact distance is covered in the window.
            _selfMoveVelocity = _castFwd * (_config.warriorQRiseForward / _config.warriorQRiseDuration)
                              + Vector3.up * (_config.warriorQRiseHeight  / _config.warriorQRiseDuration);
            _selfMoveTimer = _config.warriorQRiseDuration;
            return true;
        }

        // Called every TickTimers frame while RC dash is active.
        // Detects enemies in an overlap sphere around the character torso; on first contact,
        // stops the dash and launches all enemies in range.
        private void CheckRCDashImpact()
        {
            Vector3 center = _ownerHealth.transform.position + Vector3.up * (_config.standHeight * 0.5f);
            int count = Physics.OverlapSphereNonAlloc(
                center, _config.warriorDashImpactRadius, s_rcBuf,
                _config.attackLayerMask, QueryTriggerInteraction.Ignore);

            int hitCount = 0;
            for (int i = 0; i < count; i++)
            {
                HealthComponent hc = s_rcBuf[i].GetComponent<HealthComponent>();
                if (hc == null) hc = s_rcBuf[i].GetComponentInParent<HealthComponent>();
                if (hc == null || hc == _ownerHealth || hc.IsDead) continue;
                if (hc.Team == _ownerHealth.Team) continue; // allies pass through

                bool dup = false;
                for (int j = 0; j < hitCount; j++)
                    if (s_rcHitCache[j] == hc) { dup = true; break; }
                if (dup || hitCount >= s_rcHitCache.Length) continue;

                s_rcHitCache[hitCount++] = hc;
            }

            if (hitCount == 0) return;

            // Enemy contact: stop dash, then apply effects to all enemies in range.
            _isRCDashing = false;
            _ac.StopDash();

            for (int i = 0; i < hitCount; i++)
            {
                HealthComponent hc = s_rcHitCache[i];
                if (_config.warriorDashImpactDamage > 0f)
                {
                    hc.TakeDamage(new DamageInfo
                    {
                        BaseDamage = _config.warriorDashImpactDamage,
                        SourceTeam = _ownerHealth.Team,
                        SourceId   = string.Empty,
                    });
                }
                hc.ApplyLaunchImpulse(_config.warriorDashImpactLaunchSpeed, _ownerHealth.Team);
                s_rcHitCache[i] = null; // release reference
            }
        }

        // E — Iron Guard: damage reduction + move speed penalty for warriorGuardDuration.
        private bool TryE()
        {
            if (_ac.GetCooldown(AbilitySlot.E) > 0f) return false;
            if (_isGuarding) return false; // re-press during guard is ignored

            _ac.SetCooldown(AbilitySlot.E, _ac.AbilityConfig.ECooldown);
            _isGuarding = true;
            _guardTimer = _config.warriorGuardDuration;

            _ownerHealth.SetDamageTakenMultiplier(
                _config.warriorGuardDamageMultiplier,
                _config.warriorGuardDuration);

            _ownerHealth.SetSelfMoveSpeedMultiplier(
                _config.warriorGuardMoveSpeedMultiplier,
                _config.warriorGuardDuration);

            SpawnGuardVisual();
            return true;
        }

        // Ends the guard, restoring all applied penalties. Safe to call when not guarding.
        private void EndGuard()
        {
            if (!_isGuarding) return;
            _isGuarding = false;
            _guardTimer = 0f;
            _ownerHealth.ClearDamageTakenMultiplier();
            _ownerHealth.ClearSelfMoveSpeedMultiplier();
            // If F slash barrage is still casting, re-apply its move penalty so guard expiry does not lift it.
            if (_fState == FState.Casting)
            {
                float fRemaining = Mathf.Max(0f, _config.warriorFDuration - _fTimer);
                _ownerHealth.SetSelfMoveSpeedMultiplier(_config.warriorFMoveSpeedMultiplier, fRemaining);
            }
            DestroyGuardVisual();
        }

        // Z — Parry Retreat: fire wave 1 at cast position, dash backward, fire wave 2 after delay.
        // Blocked while another dash, Q leap, or R ultimate is active to prevent overlapping movement.
        private bool TryZ()
        {
            if (_ac.GetCooldown(AbilitySlot.Z) > 0f) return false;
            if (_rState == RState.Casting) return false; // R ultimate in progress
            if (_zState == ZState.Active) return false;
            if (_ac.IsDashing) return false;             // RC dash or E2 leap in progress
            if (_qState == QState.Casting) return false; // Q leap in progress

            _ac.SetCooldown(AbilitySlot.Z, _ac.AbilityConfig.ZCooldown);

            // Lock retreat direction at cast time (opposite of camera forward, horizontal only).
            Vector3 fwd = _cameraTransform.forward; fwd.y = 0f;
            if (fwd.sqrMagnitude < 0.001f) { fwd = _ownerHealth.transform.forward; fwd.y = 0f; }
            if (fwd.sqrMagnitude < 0.001f) fwd = _ownerHealth.transform.right;
            fwd.Normalize();

            _zOrigin    = _ownerHealth.transform.position;
            _zFwd       = fwd;
            _zState     = ZState.Active;
            _zTimer     = 0f;
            _zWave2Done = false;

            // Unit passthrough via shared dash (walls still block).
            float speed = _config.warriorZRetreatDistance / _config.warriorZRetreatDuration;
            _ac.StartDash(-fwd * speed, _config.warriorZRetreatDuration);

            // Wave 1 — fired immediately at cast position, travels forward.
            FireZWave(_zOrigin, _zFwd);
            return true;
        }

        // Called when rising ends — switches to continuous downward movement.
        private void StartFallingPhase()
        {
            _isFalling   = true;
            _fallTimer   = 0f;
            _wasAirborne = false;
            _fallVelocity = _castFwd * _config.warriorQFallForwardSpeed
                          + Vector3.down * _config.warriorQFallSpeed;
        }

        // Hit 1 — rising slash: damage only, no displacement.
        private void FireHit1()
        {
            HitScan(_config.warriorQHit1Damage, _config.warriorQRange, _config.warriorQAngle, launchUp: false);
            SpawnVisual(hit2: false);
        }

        // Hit 2 — falling slam: fires on landing. Wider AoE, damage + launch targets upward.
        private void FireHit2()
        {
            HitScan(_config.warriorQHit2Damage, _config.warriorQHit2Range, _config.warriorQHit2Angle, launchUp: true);
            SpawnVisual(hit2: true);
        }

        // Sphere + cone overlap scan shared by both hits.
        // launchUp: when true, calls HealthComponent.ApplyLaunchImpulse for a one-shot vertical impulse.
        //           Air time = 2 * warriorQLaunchSpeed / |gravity|  (10 / 20 × 2 = 1.0 s).
        private void HitScan(float damage, float range, float angle, bool launchUp)
        {
            Vector3 origin    = _ownerHealth.transform.position + Vector3.up * (_config.standHeight * 0.5f);
            float   halfAngle = angle * 0.5f;

            int count = Physics.OverlapSphereNonAlloc(
                origin, range, s_buf,
                _config.attackLayerMask, QueryTriggerInteraction.Ignore);

            int hitCount = 0;
            for (int i = 0; i < count; i++)
            {
                HealthComponent hc = s_buf[i].GetComponent<HealthComponent>();
                if (hc == null) hc = s_buf[i].GetComponentInParent<HealthComponent>();
                if (hc == null || hc == _ownerHealth || hc.IsDead) continue;
                if (hc.Team == _ownerHealth.Team) continue; // no friendly fire

                // Forward cone check — only targets in front of the warrior are hit.
                Vector3 toTarget = hc.transform.position - _ownerHealth.transform.position;
                toTarget.y = 0f;
                if (toTarget.sqrMagnitude > 0.001f &&
                    Vector3.Angle(_castFwd, toTarget) > halfAngle) continue;

                // Dedup: one hit per HealthComponent per HitScan call.
                bool already = false;
                for (int j = 0; j < hitCount; j++)
                    if (s_hitCache[j] == hc) { already = true; break; }
                if (already) continue;
                if (hitCount >= s_hitCache.Length) continue; // cache full — skip

                s_hitCache[hitCount++] = hc;

                hc.TakeDamage(new DamageInfo
                {
                    BaseDamage = damage,
                    SourceTeam = _ownerHealth.Team,
                    SourceId   = string.Empty,
                });

                if (launchUp)
                    hc.ApplyLaunchImpulse(_config.warriorQLaunchSpeed, _ownerHealth.Team);
            }

            // Release hit-cache references.
            for (int i = 0; i < hitCount; i++) s_hitCache[i] = null;
        }

        private void ClearQCast()
        {
            _qState           = QState.Idle;
            _qTimer           = 0f;
            _hit1Fired        = false;
            _hit2Fired        = false;
            _isFalling        = false;
            _fallVelocity     = Vector3.zero;
            _fallTimer        = 0f;
            _wasAirborne      = false;
            _selfMoveTimer    = 0f;
            _selfMoveVelocity = Vector3.zero;
            // _qState is Idle now, so TryClearPassthrough only disables if no dash is also active.
            _ac.TryClearPassthrough();
        }

        // Spawns a WarriorZWave projectile at the given position, travelling in forward direction.
        // Hit detection and visual are managed by the wave component.
        private void FireZWave(Vector3 position, Vector3 forward)
        {
            float halfH = _config.standHeight * 0.5f;
            GameObject go = new GameObject("WarriorZWave");
            go.transform.position = position + Vector3.up * halfH;

            float travelSpeed = _config.warriorZWaveLength / _config.warriorZWaveDuration;
            WarriorZWave wave = go.AddComponent<WarriorZWave>();
            wave.Init(
                forward,
                travelSpeed,
                _config.warriorZWaveLength,
                _config.warriorZWaveDamage,
                _config.warriorZWaveKnockback,
                _ownerHealth,
                _config.attackLayerMask,
                _config.warriorZWaveArmSpan * 0.5f,
                _config.warriorZWaveWidth   * 0.5f,
                halfH);
        }

        private void ClearZState()
        {
            _zState     = ZState.Idle;
            _zTimer     = 0f;
            _zOrigin    = Vector3.zero;
            _zFwd       = Vector3.zero;
            _zWave2Done = false;
        }

        private bool TryF()
        {
            if (_ac.GetCooldown(AbilitySlot.F) > 0f) return false;
            if (_fState == FState.Casting) return false;

            _ac.SetCooldown(AbilitySlot.F, _ac.AbilityConfig.FCooldown);
            _fState       = FState.Casting;
            _fTimer       = 0f;
            _fDamageTimer = _config.warriorFDamageInterval; // first tick at damageInterval (0.5 s)
            _fVisualTimer = 0f;                              // first visual immediately
            _fDamageCount = 0;
            _fVisualCount = 0;
            _fTotalTicks  = Mathf.RoundToInt(_config.warriorFDuration / _config.warriorFDamageInterval);

            _ownerHealth.SetSelfMoveSpeedMultiplier(
                _config.warriorFMoveSpeedMultiplier,
                _config.warriorFDuration);
            return true;
        }

        private void FireFHit(bool isFinal)
        {
            Vector3 center  = _ownerHealth.transform.position + Vector3.up * (_config.standHeight * 0.5f);
            Vector3 forward = _cameraTransform.forward;
            // Fall back to character horizontal forward when looking nearly straight up or down.
            Vector3 fwdH = new Vector3(forward.x, 0f, forward.z);
            if (fwdH.sqrMagnitude < 0.1f) forward = _ownerHealth.transform.forward;

            float damage    = isFinal ? _config.warriorFFinalDamage    : _config.warriorFDamage;
            float knockback = isFinal ? _config.warriorFFinalKnockback  : 0f;

            int count = Physics.OverlapSphereNonAlloc(
                center, _config.warriorFRadius, s_buf,
                _config.attackLayerMask, QueryTriggerInteraction.Ignore);

            int hitCount = 0;
            for (int i = 0; i < count; i++)
            {
                HealthComponent hc = s_buf[i].GetComponent<HealthComponent>();
                if (hc == null) hc = s_buf[i].GetComponentInParent<HealthComponent>();
                if (hc == null || hc == _ownerHealth || hc.IsDead) continue;
                if (hc.Team == _ownerHealth.Team) continue;

                // 3D front hemisphere: closest point avoids false positives for thick bodies.
                Vector3 closest = s_buf[i].ClosestPoint(center);
                Vector3 dir     = closest - center;
                if (Vector3.Dot(forward, dir) < 0f) continue; // behind — skip

                bool already = false;
                for (int j = 0; j < hitCount; j++)
                    if (s_fHitCache[j] == hc) { already = true; break; }
                if (already || hitCount >= s_fHitCache.Length) continue;

                s_fHitCache[hitCount++] = hc;

                hc.TakeDamage(new DamageInfo
                {
                    BaseDamage = damage,
                    SourceTeam = _ownerHealth.Team,
                    SourceId   = string.Empty,
                });

                if (knockback > 0f)
                {
                    Vector3 kbDir = dir; kbDir.y = 0f;
                    if (kbDir.sqrMagnitude < 0.001f) kbDir = forward;
                    hc.ApplyKnockback(kbDir.normalized * knockback, 0.3f, _ownerHealth.Team);
                }
            }

            for (int i = 0; i < hitCount; i++) s_fHitCache[i] = null;
        }

        private void SpawnFVisual()
        {
            Vector3 fwd = _cameraTransform.forward; fwd.y = 0f;
            if (fwd.sqrMagnitude < 0.01f) fwd = _ownerHealth.transform.forward;
            fwd.Normalize();

            float   side  = (_fVisualCount % 2 == 0) ? 1f : -1f;
            Vector3 right = Vector3.Cross(Vector3.up, fwd).normalized;
            Vector3 pos   = _ownerHealth.transform.position
                          + Vector3.up  * (_config.standHeight * 0.5f)
                          + fwd         * (_config.warriorFRadius * 0.5f)
                          + right       * (side * 0.5f);

            GameObject root = new GameObject("WarriorFSlash");
            root.transform.SetPositionAndRotation(pos, Quaternion.LookRotation(fwd, Vector3.up));

            GameObject vis = GameObject.CreatePrimitive(PrimitiveType.Cube);
            vis.transform.SetParent(root.transform, false);
            vis.transform.localRotation = Quaternion.Euler(0f, 0f, side * 50f); // diagonal slash
            vis.transform.localPosition = Vector3.zero;
            vis.transform.localScale    = new Vector3(0.12f, 2.0f, 0.12f);
            Destroy(vis.GetComponent<Collider>());
            vis.GetComponent<Renderer>().sharedMaterial = GetFMat();

            Destroy(root, _config.warriorFVisualInterval + 0.02f);
        }

        private static Material GetFMat()
        {
            if (s_fMat == null)
            {
                s_fMat = new Material(Shader.Find("Standard"));
                s_fMat.color = new Color(0.9f, 0.9f, 1.0f); // silver-white slash
            }
            return s_fMat;
        }

        private void ClearFState()
        {
            _fState       = FState.Idle;
            _fTimer       = 0f;
            _fDamageTimer = 0f;
            _fVisualTimer = 0f;
            _fDamageCount = 0;
            _fVisualCount = 0;

            // SetSelfMoveSpeedMultiplier only reduces (never raises); must clear first, then re-apply
            // guard penalty if guard is still active so E+F coexistence is handled correctly.
            _ownerHealth.ClearSelfMoveSpeedMultiplier();
            if (_isGuarding && _guardTimer > 0f)
                _ownerHealth.SetSelfMoveSpeedMultiplier(
                    _config.warriorGuardMoveSpeedMultiplier, _guardTimer);
        }

        private bool TryR()
        {
            if (_ac.GetCooldown(AbilitySlot.R) > 0f) return false;
            if (_rState == RState.Casting) return false;   // already casting — no re-entry
            if (_ac.IsDashing) return false;               // RC dash / Z dash in progress — R rise would be ignored
            if (_qState == QState.Casting) return false;   // Q leap in progress — vertical states would conflict
            if (_zState == ZState.Active) return false;    // Z retreat in progress

            _ac.SetCooldown(AbilitySlot.R, _ac.AbilityConfig.RCooldown);
            _rState = RState.Casting;
            _rTimer = 0f;

            // Snap character to ground so rise/drop spans exactly [ground → ground+riseHeight → ground],
            // regardless of whether the caster is airborne at cast time.
            if (TryFindGroundBelow(out Vector3 groundPos))
            {
                _rOrigin = groundPos;
                if (_ownerCC != null) _ownerCC.enabled = false;
                _ownerHealth.transform.position = _rOrigin;
                if (_ownerCC != null) _ownerCC.enabled = true;
            }
            else
            {
                _rOrigin = _ownerHealth.transform.position; // fallback: current position
            }

            // Start rise: velocity = height / duration, applied via SelfMoveVelocity → FirstPersonMotor.
            _rMoveVelocity = Vector3.up * (_config.warriorRRiseHeight / _config.warriorRRiseDuration);
            _rSwordPivot   = null; // sword spawns later at drop start

            _ownerHealth.SetAbilityInvulnerable(true);
            _ac.SetDashPassthrough(true); // pass through units for the full R duration
            SpawnRVisual();
            return true;
        }

        // Finds the nearest ground surface directly below the character.
        // Skips HealthComponent colliders so other characters do not count as ground.
        // Searches up to [warriorRRiseHeight + 5 m] below the current position.
        private bool TryFindGroundBelow(out Vector3 groundPos)
        {
            Vector3 origin   = _ownerHealth.transform.position + Vector3.up * 0.1f;
            float   maxDist  = _config.warriorRRiseHeight + 5f;
            int     count    = Physics.RaycastNonAlloc(origin, Vector3.down, s_groundHits,
                                   maxDist, ~0, QueryTriggerInteraction.Ignore);

            float closestDist = float.MaxValue;
            groundPos         = _ownerHealth.transform.position;
            bool  found       = false;

            for (int i = 0; i < count; i++)
            {
                RaycastHit h = s_groundHits[i];
                HealthComponent hc = h.collider.GetComponent<HealthComponent>();
                if (hc == null) hc = h.collider.GetComponentInParent<HealthComponent>();
                if (hc != null) continue; // skip character capsules

                if (h.distance < closestDist)
                {
                    closestDist = h.distance;
                    groundPos   = h.point;
                    found       = true;
                }
            }
            return found;
        }

        private void FireRImpact()
        {
            Vector3 center = _rOrigin
                           + Vector3.up * (_config.standHeight * 0.5f);
            Vector3 half   = new Vector3(
                _config.warriorRAreaSize   * 0.5f,
                _config.warriorRAreaHeight * 0.5f,
                _config.warriorRAreaSize   * 0.5f);

            int count = Physics.OverlapBoxNonAlloc(
                center, half, s_buf, Quaternion.identity,
                _config.attackLayerMask, QueryTriggerInteraction.Ignore);

            int hitCount = 0;
            for (int i = 0; i < count; i++)
            {
                HealthComponent hc = s_buf[i].GetComponent<HealthComponent>();
                if (hc == null) hc = s_buf[i].GetComponentInParent<HealthComponent>();
                if (hc == null || hc == _ownerHealth || hc.IsDead) continue;
                if (hc.Team == _ownerHealth.Team) continue;

                bool already = false;
                for (int j = 0; j < hitCount; j++)
                    if (s_hitCache[j] == hc) { already = true; break; }
                if (already || hitCount >= s_hitCache.Length) continue;

                s_hitCache[hitCount++] = hc;
                hc.ApplyStun(_config.warriorRStunDuration, _ownerHealth.Team);
            }

            for (int i = 0; i < hitCount; i++) s_hitCache[i] = null;
        }

        // Spawns floor area marker at cast origin. Sword is added separately in SpawnRSword at drop start.
        private void SpawnRVisual()
        {
            _rSwordRoot = new GameObject("WarriorRUlt");
            _rSwordRoot.transform.SetPositionAndRotation(_rOrigin, Quaternion.identity);

            float area  = _config.warriorRAreaSize;
            var   floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.transform.SetParent(_rSwordRoot.transform, false);
            floor.transform.localPosition = new Vector3(0f, 0.05f, 0f);
            floor.transform.localScale    = new Vector3(area, 0.1f, area);
            Destroy(floor.GetComponent<Collider>());
            floor.GetComponent<Renderer>().sharedMaterial = GetRFloorMat();
        }

        // Spawns the giant sword starting high above and animated down during the 0.1 s drop phase.
        private void SpawnRSword()
        {
            float m      = _config.warriorRSwordScaleMultiplier;
            float bladeH = 2.0f * m;
            float bladeW = 0.15f * m;
            float bladeD = 0.04f * m;

            var pivot = new GameObject("SwordPivot");
            pivot.transform.SetParent(_rSwordRoot.transform, false);
            // Start at the top of rise height so the sword appears to fall from above.
            pivot.transform.localPosition = new Vector3(0f, _config.warriorRRiseHeight + bladeH * 0.5f, 0f);
            _rSwordPivot = pivot.transform;

            // Blade
            var blade = GameObject.CreatePrimitive(PrimitiveType.Cube);
            blade.transform.SetParent(pivot.transform, false);
            blade.transform.localPosition = Vector3.zero;
            blade.transform.localScale    = new Vector3(bladeW, bladeH, bladeD);
            Destroy(blade.GetComponent<Collider>());
            blade.GetComponent<Renderer>().sharedMaterial = GetRBladeMat();

            // Crossguard — perpendicular bar near the upper third of the blade.
            float guardW = 0.6f * m; float guardH = 0.08f * m; float guardD = 0.08f * m;
            var guard = GameObject.CreatePrimitive(PrimitiveType.Cube);
            guard.transform.SetParent(pivot.transform, false);
            guard.transform.localPosition = new Vector3(0f, bladeH * 0.35f, 0f);
            guard.transform.localScale    = new Vector3(guardW, guardH, guardD);
            Destroy(guard.GetComponent<Collider>());
            guard.GetComponent<Renderer>().sharedMaterial = GetRGuardMat();

            // Handle — above the crossguard.
            float handleW = 0.06f * m; float handleH = 0.25f * m; float handleD = 0.06f * m;
            var handle = GameObject.CreatePrimitive(PrimitiveType.Cube);
            handle.transform.SetParent(pivot.transform, false);
            handle.transform.localPosition = new Vector3(0f, bladeH * 0.35f + guardH * 0.5f + handleH * 0.5f, 0f);
            handle.transform.localScale    = new Vector3(handleW, handleH, handleD);
            Destroy(handle.GetComponent<Collider>());
            handle.GetComponent<Renderer>().sharedMaterial = GetRGuardMat();
        }

        private void DestroyRVisual()
        {
            if (_rSwordRoot == null) return;
            Destroy(_rSwordRoot);
            _rSwordRoot = null;
        }

        private static Material GetRBladeMat()
        {
            if (s_rBladeMat == null)
            {
                s_rBladeMat = new Material(Shader.Find("Standard"));
                s_rBladeMat.color = new Color(0.65f, 0.68f, 0.80f); // steel-grey blade
            }
            return s_rBladeMat;
        }

        private static Material GetRGuardMat()
        {
            if (s_rGuardMat == null)
            {
                s_rGuardMat = new Material(Shader.Find("Standard"));
                s_rGuardMat.color = new Color(0.80f, 0.60f, 0.08f); // gold crossguard / handle
            }
            return s_rGuardMat;
        }

        private static Material GetRFloorMat()
        {
            if (s_rFloorMat == null)
            {
                s_rFloorMat = new Material(Shader.Find("Standard"));
                s_rFloorMat.color = new Color(0.90f, 0.75f, 0.0f); // bright yellow area marker
            }
            return s_rFloorMat;
        }

        private void ClearRState()
        {
            _rMoveVelocity = Vector3.zero;
            _rSwordPivot   = null; // reference only; actual destroy happens via _rSwordRoot
            _rState        = RState.Idle; // set Idle before TryClearPassthrough so IsRCasting = false
            _rTimer        = 0f;
            _ownerHealth.SetAbilityInvulnerable(false);
            _ac.TryClearPassthrough(); // release unit-passthrough unless Q/dash also active
            DestroyRVisual();
        }

        // --- Visuals

        private void SpawnVisual(bool hit2)
        {
            // Use each hit's own range so the visual footprint matches the AoE.
            float   range = hit2 ? _config.warriorQHit2Range : _config.warriorQRange;
            Vector3 pos   = hit2
                ? _ownerHealth.transform.position + Vector3.up * 0.15f
                : _ownerHealth.transform.position + Vector3.up * (_config.standHeight * 0.5f);

            GameObject go = new GameObject(hit2 ? "WarriorQHit2" : "WarriorQHit1");
            go.transform.SetPositionAndRotation(pos, Quaternion.LookRotation(_castFwd, Vector3.up));

            GameObject vis = GameObject.CreatePrimitive(PrimitiveType.Cube);
            vis.transform.SetParent(go.transform, false);
            vis.transform.localPosition = Vector3.zero;
            vis.transform.localScale    = hit2
                ? new Vector3(range * 2f, 0.20f, range * 2f)         // wide flat shockwave
                : new Vector3(range * 2f, range * 1.5f, range * 2f); // tall rising slash
            Destroy(vis.GetComponent<Collider>());
            vis.GetComponent<Renderer>().sharedMaterial = hit2 ? GetHit2Mat() : GetHit1Mat();

            Destroy(go, 0.25f);
        }

        // Guard visual: a gold sphere attached to the player, spawned once per guard activation.
        private void SpawnGuardVisual()
        {
            if (_guardVisual != null) return;
            _guardVisual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            _guardVisual.transform.SetParent(_ownerHealth.transform, false);
            _guardVisual.transform.localPosition = Vector3.up * (_config.standHeight * 0.5f);
            _guardVisual.transform.localScale    = Vector3.one * 1.8f;
            Destroy(_guardVisual.GetComponent<Collider>());
            _guardVisual.GetComponent<Renderer>().sharedMaterial = GetGuardMat();
        }

        private void DestroyGuardVisual()
        {
            if (_guardVisual == null) return;
            Destroy(_guardVisual);
            _guardVisual = null;
        }

        private static Material GetHit1Mat()
        {
            if (s_hit1Mat == null)
            {
                s_hit1Mat = new Material(Shader.Find("Standard"));
                s_hit1Mat.color = new Color(1f, 0.88f, 0.20f); // bright yellow — rising slash
            }
            return s_hit1Mat;
        }

        private static Material GetHit2Mat()
        {
            if (s_hit2Mat == null)
            {
                s_hit2Mat = new Material(Shader.Find("Standard"));
                s_hit2Mat.color = new Color(1f, 0.28f, 0.05f); // orange-red — falling slam
            }
            return s_hit2Mat;
        }

        private static Material GetGuardMat()
        {
            if (s_guardMat == null)
            {
                s_guardMat = new Material(Shader.Find("Standard"));
                s_guardMat.color = new Color(0.85f, 0.72f, 0.08f); // gold — iron guard
            }
            return s_guardMat;
        }

    }
}
