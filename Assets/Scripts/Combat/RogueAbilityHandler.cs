using UnityEngine;
using Game.Config;
using Game.Player;

namespace Game.Combat
{
    // Rogue-specific ability logic, separated from AbilityController.
    // Created and initialised by AbilityController.Start() when the owner is Rogue.
    // AbilityController owns all shared state (cooldown array, dash velocity, passthrough toggles);
    // this handler owns all Rogue-exclusive state (Q charges, E state machine, Z timer, rift schedule, R stealth).
    public class RogueAbilityHandler : MonoBehaviour
    {
        // --- References (injected via Init)
        private AbilityController     _ac;
        private GameConfig            _config;
        private HealthComponent       _ownerHealth;
        private Transform             _cameraTransform;
        private CharacterController   _ownerController; // cached for PerformTeleport
        private FirstPersonMotor      _motor;           // null when owner has no motor (bots)
        private BasicAttackController _basicAttack;     // null when owner has no BasicAttackController

        // --- Q: charge + per-cast lockout
        private int   _qCharges       = 0;
        private float _qRechargeTimer = 0f;
        private float _qLockoutTimer  = 0f;

        // --- E: two-phase state machine
        // ProjectileFlying: E1 shuriken is in the air — blocks re-fire until hit/miss resolves.
        private enum EState { Idle, ProjectileFlying, MarkedWaiting, Dashing }
        private EState          _eState      = EState.Idle;
        private HealthComponent _eTarget     = null;
        private float           _eMarkTimer  = 0f;
        private GameObject      _eMarkVisual = null;

        // --- Z: brief character passthrough after teleport
        private float _zPassthroughTimer = 0f;

        // --- RC: post-dash rift spawn schedule
        private bool    _pendingRifts;
        private Vector3 _riftPathStart;
        private Vector3 _riftPathEnd;
        private float   _riftSpawnTimer;
        private int     _riftsFired;

        // --- F: shadow slash 3-hit combo
        private enum FComboState { Idle, Dashing, Window }
        private FComboState       _fCombo           = FComboState.Idle;
        private int               _fComboStep       = 0;   // 1/2/3 while combo active, 0 otherwise
        private float             _fComboWindow     = 0f;  // seconds remaining to input next hit
        private float             _fInputLock       = 0f;  // min wait after each step before re-press
        private float             _fStep3SpeedTimer = 0f;  // countdown for post-3rd-hit speed buff
        private Vector3           _fDashDirection;          // direction locked at cast time for hit cone
        private HealthComponent[] _fHitCache        = new HealthComponent[32]; // pre-allocated; reused per step
        private int               _fHitCount        = 0;

        // --- Move speed multipliers (multiplicative; applied via RefreshMotorSpeed)
        // R stealth and F step-3 buff are mutually exclusive (F breaks stealth before buffing),
        // but tracked separately so the product is always correct if policy changes.
        private float _stealthSpeedMult = 1f;
        private float _f3SpeedMult      = 1f;

        // --- R: stealth state
        private bool       _isStealthed   = false;
        private float      _stealthTimer  = 0f;
        private GameObject _stealthVisual = null;  // faint indicator above head
        private Renderer[] _ownerRenderers;        // cached once; toggled to hide/show character

        // --- Static NonAlloc physics buffers
        private static readonly Collider[] s_zBuf = new Collider[8];
        private static readonly Collider[]        s_fBuf   = new Collider[32]; // separate from Q so both can run in the same frame

        // --- Static material cache (created once, reused every cast)
        private static Material s_eShurikenMat;
        private static Material s_eMarkMat;
        private static Material s_zBombMat;
        private static Material s_riftMat;
        private static Material s_stealthMat;
        private static Material s_fSlashMat;
        private static Material s_fSlash3Mat; // step 3: slightly brighter purple to distinguish

        // --- Public read-only state (re-exposed by AbilityController for AbilityDebugUI)
        public bool  IsE2Dashing       => _eState == EState.Dashing;
        public bool  IsEFlying         => _eState == EState.ProjectileFlying;
        public bool  IsZPassthrough    => _zPassthroughTimer > 0f;
        public bool  IsStealthed       => _isStealthed;
        public int   QCharges          => _qCharges;
        public float QRechargeTimer    => _qRechargeTimer;
        public float QLockoutTimer     => _qLockoutTimer;
        public int   FComboStep        => _fComboStep;
        public float FComboWindowTimer => _fComboWindow;
        public bool  FInComboWindow    => _fCombo == FComboState.Window;
        public float StealthTimer      => _stealthTimer;
        public float F3SpeedTimer      => _fStep3SpeedTimer;

        // --- Initialisation
        public void Init(AbilityController ac, GameConfig config,
                         HealthComponent ownerHealth, Transform cameraTransform)
        {
            _ac              = ac;
            _config          = config;
            _ownerHealth     = ownerHealth;
            _cameraTransform = cameraTransform;
            _ownerController = ownerHealth.GetComponent<CharacterController>();
            _motor           = ownerHealth.GetComponent<FirstPersonMotor>(); // null on bots
            _qCharges        = config.rogueQMaxCharges;

            // Cache all renderers on the owner for stealth visibility toggling.
            _ownerRenderers = ownerHealth.GetComponentsInChildren<Renderer>(true);

            // Subscribe to BasicAttackController events if available (local player only).
            _basicAttack = GetComponent<BasicAttackController>();
            if (_basicAttack != null)
            {
                _basicAttack.OnAttackHit  += OnBasicAttackHit;
                _basicAttack.OnAttackUsed += OnBasicAttackUsed;
            }
        }

        void OnDestroy()
        {
            if (_basicAttack != null)
            {
                _basicAttack.OnAttackHit  -= OnBasicAttackHit;
                _basicAttack.OnAttackUsed -= OnBasicAttackUsed;
            }
            BreakStealth();
        }

        // --- Per-frame timer updates (called by AbilityController.Update)
        public void TickTimers(float dt)
        {
            // Q: recover one charge when recharge timer completes; lockout counts down separately.
            if (_qCharges < _config.rogueQMaxCharges)
            {
                _qRechargeTimer -= dt;
                if (_qRechargeTimer <= 0f)
                {
                    _qCharges++;
                    _qRechargeTimer = _qCharges < _config.rogueQMaxCharges
                                      ? _ac.AbilityConfig.QCooldown : 0f;
                }
            }
            if (_qLockoutTimer > 0f)
                _qLockoutTimer = Mathf.Max(0f, _qLockoutTimer - dt);

            // E: mark expiry returns state to Idle with no cooldown penalty.
            if (_eState == EState.MarkedWaiting)
            {
                _eMarkTimer -= dt;
                if (_eMarkTimer <= 0f) ClearEMark();
            }

            // Z: character passthrough clears after the configured duration.
            if (_zPassthroughTimer > 0f)
            {
                _zPassthroughTimer = Mathf.Max(0f, _zPassthroughTimer - dt);
                if (_zPassthroughTimer <= 0f)
                    _ac.TryEndZPassthrough();
            }

            // RC: rifts fire at +delay, +delay+interval, ... after the dash ends.
            if (_pendingRifts)
            {
                _riftSpawnTimer -= dt;
                if (_riftSpawnTimer <= 0f)
                {
                    SpawnRift();
                    _riftsFired++;
                    if (_riftsFired < _config.rogueDashRiftCount)
                        _riftSpawnTimer = _config.rogueDashRiftInterval;
                    else
                        _pendingRifts = false;
                }
            }

            // F: per-tick hit check while a combo step dash is active.
            if (_fCombo == FComboState.Dashing)
                FSlashTick();

            // F: minimum re-press lock between combo hits.
            if (_fInputLock > 0f)
                _fInputLock = Mathf.Max(0f, _fInputLock - dt);

            // F: combo window countdown; stun or expiry ends combo and starts full cooldown.
            if (_fCombo == FComboState.Window)
            {
                if (_ownerHealth.IsStunned)
                {
                    _ac.SetCooldown(AbilitySlot.F, _ac.AbilityConfig.FCooldown);
                    ClearFCombo();
                }
                else
                {
                    _fComboWindow -= dt;
                    if (_fComboWindow <= 0f)
                    {
                        _ac.SetCooldown(AbilitySlot.F, _ac.AbilityConfig.FCooldown);
                        ClearFCombo();
                    }
                }
            }

            // F: step-3 speed buff countdown.
            if (_fStep3SpeedTimer > 0f)
            {
                _fStep3SpeedTimer = Mathf.Max(0f, _fStep3SpeedTimer - dt);
                if (_fStep3SpeedTimer <= 0f)
                {
                    _f3SpeedMult = 1f;
                    RefreshMotorSpeed();
                }
            }

            // R: stealth duration countdown.
            if (_isStealthed)
            {
                _stealthTimer -= dt;
                if (_stealthTimer <= 0f)
                    BreakStealth();
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
                AbilitySlot.Z          => TryZ(),
                AbilitySlot.R          => TryR(),
                AbilitySlot.F          => TryF(),
                _                      => false,
            };
        }

        // --- Dash lifecycle (called by AbilityController.OnDashEnded)
        public void OnDashEnded()
        {
            if (_eState == EState.Dashing)
            {
                // E2 leap complete -- ECooldown was set when E2 activated.
                _eState  = EState.Idle;
                _eTarget = null;
                return;
            }
            if (_fCombo == FComboState.Dashing)
            {
                if (_fComboStep < 3)
                {
                    if (_ownerHealth.IsStunned)
                    {
                        // Stun mid-dash: cancel instead of opening next-hit window.
                        _ac.SetCooldown(AbilitySlot.F, _ac.AbilityConfig.FCooldown);
                        ClearFCombo();
                    }
                    else
                    {
                        _fCombo       = FComboState.Window;
                        _fComboWindow = _config.rogueShadowSlashComboWindow;
                        _fInputLock   = _config.rogueShadowSlashInputLock;
                    }
                }
                else
                {
                    // Step 3 complete: apply speed buff, start full cooldown, return to idle.
                    _f3SpeedMult      = _config.rogueShadowSlashStep3MoveSpeedMultiplier;
                    _fStep3SpeedTimer = _config.rogueShadowSlashStep3MoveSpeedDuration;
                    RefreshMotorSpeed();
                    _ac.SetCooldown(AbilitySlot.F, _ac.AbilityConfig.FCooldown);
                    ClearFCombo();
                }
                return;
            }
            // RC dash complete -- arm post-dash rift spawning.
            if (_config.rogueDashRiftCount <= 0) return;
            _riftPathEnd    = _ownerHealth.transform.position;
            _pendingRifts   = true;
            _riftsFired     = 0;
            _riftSpawnTimer = _config.rogueDashRiftDelay;
        }

        // --- Death cleanup (called by AbilityController.HandleOwnerDeath)
        // Already-spawned world objects (rifts, bombs) live out their lifetime.
        // Q charges and cooldowns are preserved; they decay naturally during the respawn delay.
        public void HandleOwnerDeath()
        {
            _zPassthroughTimer = 0f;
            ClearEMark();
            _pendingRifts   = false;
            _riftSpawnTimer = 0f;
            _riftsFired     = 0;
            _fStep3SpeedTimer = 0f; _f3SpeedMult = 1f; // cancel pending speed buff
            ClearFCombo();  // drop combo state + hit cache; AC has already aborted the dash
            BreakStealth(); // stealth clears on death; R cooldown still decays during respawn delay
        }

        // --- Safety cleanup (called by AbilityController.OnDisable/OnDestroy)
        public void ForceCleanup()
        {
            _zPassthroughTimer = 0f;
            ClearEMark();
            _pendingRifts     = false;
            _fStep3SpeedTimer = 0f; _f3SpeedMult = 1f;
            ClearFCombo();
            BreakStealth();
        }

        // --- E projectile callbacks (called by RogueMarkedShurikenProjectile)
        // Hit: transition to MarkedWaiting and arm E2.
        public void OnEProjectileHit(HealthComponent target)
        {
            if (target == null || target.IsDead || _eState != EState.ProjectileFlying) return;
            ApplyEMark(target);
        }

        // Miss (range expiry or wall): return to Idle so E1 can be re-fired.
        // No ECooldown on miss — the in-flight state already prevented re-firing during flight.
        public void OnEProjectileMissed()
        {
            if (_eState != EState.ProjectileFlying) return;
            _eState = EState.Idle;
        }

        // --- Private ability implementations

        // RC dash: stealth is NOT broken on use (new policy: only backstab breaks stealth).
        private bool TryRCDash()
        {
            if (_ac.GetCooldown(AbilitySlot.RightClick) > 0f) return false;
            _ac.SetCooldown(AbilitySlot.RightClick, _ac.AbilityConfig.RightClickCooldown);
            _riftPathStart = _ownerHealth.transform.position;
            _ac.StartDash(_config.rogueDashDistance, _config.rogueDashDuration);
            return true;
        }

        // Q: charge + lockout; decrement charge, apply lockout, launch giant shuriken zone.
        private bool TryQ()
        {
            if (_qLockoutTimer > 0f || _qCharges <= 0) return false;
            // Start recharge timer only when dropping from max (timer is already running otherwise).
            if (_qCharges >= _config.rogueQMaxCharges)
                _qRechargeTimer = _ac.AbilityConfig.QCooldown;
            _qCharges--;
            _qLockoutTimer = _config.rogueQCastLockout;
            LaunchGiantShuriken();
            return true;
        }

        // E1: fire shuriken -- no cooldown until E2 succeeds or mark expires.
        // E2: leap to marked target's back -- ECooldown starts on success.
        private bool TryE()
        {
            if (_ac.GetCooldown(AbilitySlot.E) > 0f) return false;
            switch (_eState)
            {
                case EState.Idle:
                    FireEProjectile();
                    _eState = EState.ProjectileFlying; // blocks re-fire until hit or miss resolves
                    return true; // cooldown NOT started -- wait for E2 or mark expiry

                case EState.MarkedWaiting:
                    if (_eTarget == null || _eTarget.IsDead) { ClearEMark(); return false; }
                    StartE2Dash();
                    _ac.SetCooldown(AbilitySlot.E, _ac.AbilityConfig.ECooldown);
                    return true;

                default: // EState.Dashing -- leap in progress (blocked by Tick guard in AC)
                    return false;
            }
        }

        // Z: backwards teleport + stun bomb; cooldown consumed only on successful teleport.
        private bool TryZ()
        {
            if (_ac.GetCooldown(AbilitySlot.Z) > 0f) return false;

            Vector3 backward = -_cameraTransform.forward;
            backward.y = 0f;
            if (backward.sqrMagnitude < 0.001f) { backward = -_ownerHealth.transform.forward; backward.y = 0f; }
            if (backward.sqrMagnitude < 0.001f) backward = _ownerHealth.transform.right;
            backward.Normalize();

            if (!TryFindTeleportDestination(backward, out Vector3 dest)) return false;

            // Record bomb position before teleporting -- transform.position is about to change.
            Vector3 bombOrigin = _ownerHealth.transform.position;

            PerformTeleport(dest);

            // Brief character passthrough so landing near another unit doesn't cause push-back.
            _ac.SetDashPassthrough(true);
            _zPassthroughTimer = _config.rogueZPassthroughDuration;

            SpawnStunBomb(bombOrigin);
            _ac.SetCooldown(AbilitySlot.Z, _ac.AbilityConfig.ZCooldown);
            return true;
        }

        // F: shadow slash 3-hit combo. Each hit is a short forward dash with fan hit detection.
        // Cooldown starts only when the combo ends (all 3 steps or window expiry).
        private bool TryF()
        {
            switch (_fCombo)
            {
                case FComboState.Idle:
                    // Start a new combo — requires full cooldown to be available.
                    if (_ac.GetCooldown(AbilitySlot.F) > 0f) return false;
                    if (_ac.IsDashing) return false;              // RC or E2 dash in progress
                    if (_eState == EState.Dashing) return false;  // E2 safety guard
                    StartFStep(1);
                    return true;

                case FComboState.Window:
                    if (_fInputLock > 0f) return false;   // too soon after previous hit
                    if (_ac.IsDashing)    return false;    // RC/E2 dash in progress; window stays open
                    StartFStep(_fComboStep + 1);
                    return true;

                default: // Dashing — mid-step; ignore input
                    return false;
            }
        }

        // Executes one combo step: records direction, resets per-step hit cache, starts dash.
        private void StartFStep(int step)
        {
            _fComboStep = step;
            _fCombo     = FComboState.Dashing;
            // Reset per-step hit cache (targets hit in previous steps can be hit again).
            for (int i = 0; i < _fHitCount; i++) _fHitCache[i] = null;
            _fHitCount = 0;
            // Lock cast-time direction so rotating mid-dash does not shift the hit cone.
            Vector3 fwd = _cameraTransform.forward; fwd.y = 0f;
            if (fwd.sqrMagnitude < 0.001f) { fwd = _ownerHealth.transform.forward; fwd.y = 0f; }
            if (fwd.sqrMagnitude < 0.001f) fwd = _ownerHealth.transform.right;
            _fDashDirection = fwd.normalized;

            float dist = step == 3 ? _config.rogueShadowSlashStep3Distance
                       : step == 2 ? _config.rogueShadowSlashStep2Distance
                                   : _config.rogueShadowSlashStep1Distance;
            float dur  = step == 3 ? _config.rogueShadowSlashStep3Duration
                       : step == 2 ? _config.rogueShadowSlashStep2Duration
                                   : _config.rogueShadowSlashStep1Duration;
            _ac.StartDash(dist, dur);
            SpawnFSlashVisual(step, dist, dur);
        }

        // Called every TickTimers while a combo step dash is active.
        // Samples enemies in the forward cone at the character's current (moving) position;
        // same-step duplicates are skipped via _fHitCache (reset between steps, not across them).
        private void FSlashTick()
        {
            float damage = _fComboStep == 3 ? _config.rogueShadowSlashStep3Damage
                         : _fComboStep == 2 ? _config.rogueShadowSlashStep2Damage
                                            : _config.rogueShadowSlashStep1Damage;
            float halfAngle = _config.rogueShadowSlashAngle * 0.5f;
            Vector3 origin  = _ownerHealth.transform.position + Vector3.up * (_config.standHeight * 0.5f);

            int count = Physics.OverlapSphereNonAlloc(
                origin, _config.rogueShadowSlashRange, s_fBuf,
                _config.attackLayerMask, QueryTriggerInteraction.Ignore);

            for (int i = 0; i < count; i++)
            {
                HealthComponent hc = s_fBuf[i].GetComponent<HealthComponent>();
                if (hc == null) hc = s_fBuf[i].GetComponentInParent<HealthComponent>();
                if (hc == null || hc == _ownerHealth || hc.IsDead) continue;
                if (hc.Team == _ownerHealth.Team) continue; // no friendly fire

                // One hit per target per step (cache resets between steps, not between sub-frames).
                bool alreadyHit = false;
                for (int j = 0; j < _fHitCount; j++)
                    if (_fHitCache[j] == hc) { alreadyHit = true; break; }
                if (alreadyHit) continue;

                // Forward cone check using the direction locked at step cast-time.
                Vector3 toTarget = hc.transform.position - _ownerHealth.transform.position;
                toTarget.y = 0f;
                if (toTarget.sqrMagnitude > 0.001f &&
                    Vector3.Angle(_fDashDirection, toTarget) > halfAngle)
                    continue;

                // Record and damage together: if the cache is full we skip rather than
                // hit without tracking — prevents an un-trackable target receiving repeated hits.
                if (_fHitCount < _fHitCache.Length)
                {
                    _fHitCache[_fHitCount++] = hc;
                    hc.TakeDamage(new DamageInfo
                    {
                        BaseDamage = damage,
                        SourceTeam = _ownerHealth.Team,
                        SourceId   = string.Empty,
                    });
                }
            }
        }

        // Resets all combo state and releases hit-cache references so GC can collect targets.
        private void ClearFCombo()
        {
            _fCombo       = FComboState.Idle;
            _fComboStep   = 0;
            _fComboWindow = 0f;
            _fInputLock   = 0f;
            for (int i = 0; i < _fHitCount; i++) _fHitCache[i] = null;
            _fHitCount = 0;
        }

        // step 3 is visually larger and uses a distinct material to signal the finisher.
        private void SpawnFSlashVisual(int step, float dist, float dur)
        {
            bool isStep3 = (step == 3);
            Vector3 pos  = _ownerHealth.transform.position
                           + Vector3.up * (_config.standHeight * 0.55f)
                           + _fDashDirection * (dist * 0.5f);

            GameObject go = new GameObject("RogueFSlash");
            go.transform.SetPositionAndRotation(pos, Quaternion.LookRotation(_fDashDirection, Vector3.up));

            GameObject vis = GameObject.CreatePrimitive(PrimitiveType.Cube);
            vis.transform.SetParent(go.transform, false);
            vis.transform.localScale    = isStep3
                                          ? new Vector3(0.18f, 1.1f, dist)
                                          : new Vector3(0.12f, 0.7f, dist);
            vis.transform.localPosition = Vector3.zero;
            Destroy(vis.GetComponent<Collider>());
            vis.GetComponent<Renderer>().sharedMaterial = isStep3 ? GetFSlash3Mat() : GetFSlashMat();

            Destroy(go, dur + 0.05f);
        }

        // R: enter stealth (5 s, 2x speed). Breaks on any successful skill/attack use.
        // Death clears stealth; R cooldown continues to tick during the respawn delay.
        private bool TryR()
        {
            if (_ac.GetCooldown(AbilitySlot.R) > 0f) return false;
            if (_isStealthed) return false; // already active (cooldown prevents reuse in practice)

            _isStealthed  = true;
            _stealthTimer = _config.rogueStealthDuration;
            _ac.SetCooldown(AbilitySlot.R, _ac.AbilityConfig.RCooldown);

            _stealthSpeedMult = _config.rogueStealthMoveSpeedMultiplier;
            RefreshMotorSpeed();

            // Register stealth in HealthComponent so IsTargetable returns false for AI.
            _ownerHealth.SetStealthed(true);

            // Hide owner renderers -- prototype makes character invisible to everyone.
            for (int i = 0; i < _ownerRenderers.Length; i++)
                _ownerRenderers[i].enabled = false;

            // Small faint indicator sphere floating above head (visible to all in prototype).
            _stealthVisual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            _stealthVisual.transform.SetParent(_ownerHealth.transform, false);
            _stealthVisual.transform.localPosition = Vector3.up * 1.8f;
            _stealthVisual.transform.localScale    = Vector3.one * 0.25f;
            Destroy(_stealthVisual.GetComponent<Collider>());
            _stealthVisual.GetComponent<Renderer>().sharedMaterial = GetStealthMat();

            return true;
        }

        // Ends stealth: natural expiry, successful skill/attack use, or death.
        private void BreakStealth()
        {
            if (!_isStealthed) return;
            _isStealthed  = false;
            _stealthTimer = 0f;

            _stealthSpeedMult = 1f;
            RefreshMotorSpeed();

            _ownerHealth.SetStealthed(false); // restore IsTargetable for AI

            // Re-enable owner renderers.
            for (int i = 0; i < _ownerRenderers.Length; i++)
                _ownerRenderers[i].enabled = true;

            if (_stealthVisual != null) { Destroy(_stealthVisual); _stealthVisual = null; }
        }

        // True when the owner is within the rear cone of the target (backstab eligible).
        private bool IsBackstab(HealthComponent target)
        {
            Vector3 toAttacker = _ownerHealth.transform.position - target.transform.position;
            toAttacker.y = 0f;
            Vector3 targetFwd = target.transform.forward;
            targetFwd.y = 0f;
            if (toAttacker.sqrMagnitude < 0.001f || targetFwd.sqrMagnitude < 0.001f) return false;
            // Angle between target.forward and direction-to-attacker:
            // 180 = directly behind target; backstab succeeds within halfAngle of that.
            float angle = Vector3.Angle(targetFwd.normalized, toAttacker.normalized);
            return angle > (180f - _config.rogueBackstabAngle * 0.5f);
        }

        // --- Event callbacks

        // OnAttackHit fires BEFORE OnAttackUsed so _isStealthed is still true for backstab check.
        private void OnBasicAttackHit(HealthComponent target)
        {
            if (!_isStealthed) return;
            if (target == null || target.IsDead) return;
            if (!IsBackstab(target)) return;

            // Backstab bonus: fixed damage that bypasses armor.
            target.TakeDamage(new DamageInfo
            {
                BaseDamage  = _config.rogueBackstabBonusDamage,
                SourceTeam  = _ownerHealth.Team,
                SourceId    = string.Empty,
                IgnoreArmor = true,
            });

            // Notify local player HUD so backstab feedback can be shown.
            _basicAttack.FireBackstabLanded(_config.rogueBackstabBonusDamage);
            BreakStealth(); // stealth ends only on a successful backstab hit
        }

        // Basic attack used — stealth is NOT broken (only backstab success breaks stealth).
        private void OnBasicAttackUsed() { }

        // Damage taken — stealth is NOT broken on hit (new policy).

        // --- E helpers

        private void FireEProjectile()
        {
            // Full 3D camera direction — vertical aim (up/down) is respected.
            Vector3 fwd = _cameraTransform.forward;
            if (fwd.sqrMagnitude < 0.001f) fwd = _ownerHealth.transform.forward;
            fwd.Normalize();

            // Spawn from camera position 0.5 m ahead — matches the 1st-person aim ray and
            // avoids the owner's CharacterController capsule (radius ~0.4 m).
            Vector3 origin = _cameraTransform.position + fwd * 0.5f;

            GameObject go = new GameObject("RogueEShuriken");
            go.transform.SetPositionAndRotation(origin, Quaternion.LookRotation(fwd, Vector3.up));

            RogueMarkedShurikenProjectile proj = go.AddComponent<RogueMarkedShurikenProjectile>();
            proj.Init(_ownerHealth.Team, _config.rogueEDamage, _config.rogueEProjectileSpeed,
                      _config.rogueERange, fwd, _config.attackLayerMask, this);

            // Prototype visual: flat grey disc (face = travel direction).
            GameObject vis = GameObject.CreatePrimitive(PrimitiveType.Cube);
            vis.transform.SetParent(go.transform, false);
            vis.transform.localPosition = Vector3.zero;
            vis.transform.localScale    = new Vector3(0.4f, 0.4f, 0.05f);
            Destroy(vis.GetComponent<Collider>());
            vis.GetComponent<Renderer>().sharedMaterial = GetEShurikenMat();
        }

        private void ApplyEMark(HealthComponent target)
        {
            _eTarget    = target;
            _eState     = EState.MarkedWaiting;
            _eMarkTimer = _config.rogueEMarkDuration;

            // Prototype mark indicator: small red sphere above the target's head.
            _eMarkVisual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            _eMarkVisual.transform.SetParent(target.transform, false);
            _eMarkVisual.transform.localPosition = new Vector3(0f, _config.standHeight + 0.4f, 0f);
            _eMarkVisual.transform.localScale    = Vector3.one * 0.35f;
            Destroy(_eMarkVisual.GetComponent<Collider>());
            _eMarkVisual.GetComponent<Renderer>().sharedMaterial = GetEMarkMat();
        }

        private void StartE2Dash()
        {
            Vector3 dest     = _eTarget.transform.position
                               - _eTarget.transform.forward * _config.rogueEArrivalOffset;
            Vector3 toTarget = dest - _ownerHealth.transform.position;
            toTarget.y       = 0f;
            float dist       = toTarget.magnitude;

            if (_eMarkVisual != null) { Destroy(_eMarkVisual); _eMarkVisual = null; }

            if (dist < 0.1f) { _eState = EState.Idle; _eTarget = null; return; }

            _eState = EState.Dashing;
            _ac.StartDash(toTarget.normalized * _config.rogueEDashSpeed, dist / _config.rogueEDashSpeed);
        }

        private void ClearEMark()
        {
            _eState     = EState.Idle;
            _eMarkTimer = 0f;
            _eTarget    = null;
            if (_eMarkVisual != null) { Destroy(_eMarkVisual); _eMarkVisual = null; }
        }

        // --- Z helpers

        // Tries distances longest-first (1 m steps from rogueZTeleportDistance down to 1 m).
        private bool TryFindTeleportDestination(Vector3 backward, out Vector3 dest)
        {
            if (_ownerController == null) { dest = Vector3.zero; return false; }

            int maxDist = Mathf.FloorToInt(_config.rogueZTeleportDistance);
            if (maxDist <= 0) { dest = Vector3.zero; return false; }

            float r     = _ownerController.radius;
            float halfH = _ownerController.height * 0.5f - r;
            float cy    = _ownerController.center.y;
            Vector3 localBottom = Vector3.up * (cy - halfH); // (0, ~0.4)
            Vector3 localTop    = Vector3.up * (cy + halfH); // (0, ~1.6)
            Vector3 origin      = _ownerHealth.transform.position;

            for (int d = maxDist; d >= 1; d--)
            {
                Vector3 candidate = origin + backward * d;
                if (IsSafeDestination(candidate + localBottom, candidate + localTop, r))
                { dest = candidate; return true; }
            }
            dest = Vector3.zero;
            return false;
        }

        // Returns true when the capsule at [bottom..top] overlaps only characters (no geometry).
        private static bool IsSafeDestination(Vector3 bottom, Vector3 top, float radius)
        {
            int count = Physics.OverlapCapsuleNonAlloc(bottom, top, radius, s_zBuf,
                                                       ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
            {
                if (s_zBuf[i].GetComponent<HealthComponent>() == null &&
                    s_zBuf[i].GetComponentInParent<HealthComponent>() == null)
                    return false;
            }
            return true;
        }

        // CC must be disabled to reposition transform.position.
        // cc.Move invariant preserved: Move is still called only inside FirstPersonMotor.Tick.
        private void PerformTeleport(Vector3 dest)
        {
            _ownerController.enabled        = false;
            _ownerHealth.transform.position = dest;
            _ownerController.enabled        = true;
        }

        private void SpawnStunBomb(Vector3 origin)
        {
            Vector3 bombPos = origin + Vector3.up * (_config.standHeight * 0.5f);

            GameObject go = new GameObject("RogueZStunBomb");
            go.transform.position = bombPos;

            // Prototype visual: small dark-purple sphere.
            GameObject vis = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            vis.transform.SetParent(go.transform, false);
            vis.transform.localScale    = Vector3.one * 0.3f;
            vis.transform.localPosition = Vector3.zero;
            Destroy(vis.GetComponent<Collider>());
            vis.GetComponent<Renderer>().sharedMaterial = GetZBombMat();

            RogueStunBomb bomb = go.AddComponent<RogueStunBomb>();
            bomb.Init(_ownerHealth.Team, _config.rogueZDamage, _config.rogueZBlastRadius,
                      _config.rogueZStunDuration, _config.rogueZBombFuseTime, _config.attackLayerMask);
        }

        // --- Q helpers

        private void LaunchGiantShuriken()
        {
            Vector3 fwd = _cameraTransform.forward;
            fwd.y = 0f;
            if (fwd.sqrMagnitude < 0.001f) { fwd = _ownerHealth.transform.forward; fwd.y = 0f; }
            if (fwd.sqrMagnitude < 0.001f) fwd = _ownerHealth.transform.right;
            fwd.Normalize();

            // Spawn just above ground, slightly forward to clear the player's CharacterController.
            Vector3 spawnPos   = _ownerHealth.transform.position;
            spawnPos.y        += 0.05f;
            spawnPos          += fwd * 0.6f;

            GameObject go = new GameObject("RogueQGiantShuriken");
            go.transform.SetPositionAndRotation(spawnPos, Quaternion.LookRotation(fwd, Vector3.up));

            RogueGiantShurikenZone zone = go.AddComponent<RogueGiantShurikenZone>();
            float damagePerTick = _config.rogueQDamagePerSecond * _config.rogueQTickInterval;
            zone.Init(
                _ownerHealth.Team,
                damagePerTick,
                _config.rogueQTickInterval,
                _config.rogueQTravelDuration,
                _config.rogueQTravelDistance,
                _config.rogueQZoneDuration,
                _config.rogueQZoneSize,
                _config.rogueQSlowMultiplier,
                _config.rogueQSlowDuration,
                fwd,
                _config.attackLayerMask
            );
        }

        // --- RC rift helper

        private void SpawnRift()
        {
            Vector3 dir = _riftPathEnd - _riftPathStart;
            dir.y = 0f;
            float length = dir.magnitude;
            if (length < 0.1f) return; // dash was blocked immediately -- skip

            Vector3    center = (_riftPathStart + _riftPathEnd) * 0.5f;
            center.y          = _riftPathStart.y;
            Quaternion rot    = Quaternion.LookRotation(dir / length, Vector3.up);

            GameObject riftGo = new GameObject("RogueDrift");
            riftGo.transform.SetPositionAndRotation(center, rot);

            BoxCollider col = riftGo.AddComponent<BoxCollider>();
            col.isTrigger   = true;
            col.size        = new Vector3(_config.rogueDashRiftWidth, _config.rogueDashRiftHeight, length);

            RogueDimensionalRift rift = riftGo.AddComponent<RogueDimensionalRift>();
            rift.Init(_ownerHealth.Team, _config.rogueDashRiftDamage,
                      _config.rogueDashRiftDuration, _config.attackLayerMask);

            // Prototype visual: flat purple slab on the ground along the dash direction.
            GameObject vis = GameObject.CreatePrimitive(PrimitiveType.Cube);
            vis.transform.SetParent(riftGo.transform, false);
            vis.transform.localScale    = new Vector3(_config.rogueDashRiftWidth, 0.05f, length);
            vis.transform.localPosition = Vector3.zero;
            Destroy(vis.GetComponent<Collider>());
            vis.GetComponent<Renderer>().sharedMaterial = GetRiftMat();
        }

        // --- Material cache getters

        private static Material GetEShurikenMat()
        {
            if (s_eShurikenMat == null) { s_eShurikenMat = new Material(Shader.Find("Standard")); s_eShurikenMat.color = new Color(0.85f, 0.85f, 0.85f); }
            return s_eShurikenMat;
        }

        private static Material GetEMarkMat()
        {
            if (s_eMarkMat == null) { s_eMarkMat = new Material(Shader.Find("Standard")); s_eMarkMat.color = new Color(1f, 0.15f, 0.15f); }
            return s_eMarkMat;
        }

        private static Material GetZBombMat()
        {
            if (s_zBombMat == null) { s_zBombMat = new Material(Shader.Find("Standard")); s_zBombMat.color = new Color(0.15f, 0.05f, 0.35f); }
            return s_zBombMat;
        }

        private static Material GetRiftMat()
        {
            if (s_riftMat == null) { s_riftMat = new Material(Shader.Find("Standard")); s_riftMat.color = new Color(0.55f, 0f, 1f); }
            return s_riftMat;
        }

        private static Material GetStealthMat()
        {
            if (s_stealthMat == null) { s_stealthMat = new Material(Shader.Find("Standard")); s_stealthMat.color = new Color(0.4f, 0f, 0.8f); }
            return s_stealthMat;
        }

        private static Material GetFSlashMat()
        {
            if (s_fSlashMat == null) { s_fSlashMat = new Material(Shader.Find("Standard")); s_fSlashMat.color = new Color(0.9f, 0.85f, 1f); }
            return s_fSlashMat;
        }

        private static Material GetFSlash3Mat()
        {
            if (s_fSlash3Mat == null) { s_fSlash3Mat = new Material(Shader.Find("Standard")); s_fSlash3Mat.color = new Color(0.65f, 0.4f, 1f); }
            return s_fSlash3Mat;
        }

        // Applies the product of all active speed multipliers to the motor.
        // Always call this instead of setting MoveSpeedMultiplier directly.
        private void RefreshMotorSpeed()
        {
            if (_motor != null)
                _motor.MoveSpeedMultiplier = _stealthSpeedMult * _f3SpeedMult;
        }
    }
}
