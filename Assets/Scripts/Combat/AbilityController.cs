using UnityEngine;
using Game.Commands;
using Game.Config;

namespace Game.Combat
{
    // Routes ability inputs to per-slot cooldown checks and class-specific handlers.
    // Rogue/Warrior/Mage ability logic lives in their own handlers (AddComponent at Start).
    public class AbilityController : MonoBehaviour
    {
        public GameConfig      config;
        public HealthComponent ownerHealth;
        public Transform       cameraTransform;

        // Set by GameBootstrap after all characters are created.
        // Stores every other character's CharacterController so we can toggle IgnoreCollision.
        public CharacterController[] dashPassthroughControllers;

        private readonly float[] _cooldowns = new float[(int)AbilitySlot.Count];
        private ClassAbilityConfig _abilityConfig;
        private bool        _ready;
        private CombatClass _combatClass;

        // Shared dash state; drives DashVelocity read by LocalPlayerController -> FirstPersonMotor.
        private bool    _isDashing;
        private float   _dashTimer;
        private Vector3 _dashHorizontalVelocity;

        private CharacterController _ownerController;
        private RogueAbilityHandler    _rogueHandler;    // null when owner is not Rogue
        private WarriorAbilityHandler  _warriorHandler;  // null when owner is not Warrior
        private MageAbilityHandler     _mageHandler;     // null when owner is not Mage
        private ArcherAbilityHandler   _archerHandler;   // null when owner is not Archer

        // DashVelocity: read every frame by LocalPlayerController, passed to FirstPersonMotor.
        // Priority: RC dash > Warrior Q self-movement > zero.
        public bool    IsDashing => _isDashing;
        public Vector3 DashVelocity
        {
            get
            {
                if (_isDashing) return _dashHorizontalVelocity;
                if (_warriorHandler != null)
                {
                    Vector3 v = _warriorHandler.SelfMoveVelocity;
                    if (v.sqrMagnitude > 0.001f) return v;
                }
                return Vector3.zero;
            }
        }

        // Warrior guard state -- owned by handler; re-exposed here for AbilityDebugUI.
        public bool  IsWarriorGuardMode => _warriorHandler != null;
        public bool  WarriorIsGuarding  => _warriorHandler?.IsGuarding ?? false;
        public float WarriorGuardTimer  => _warriorHandler?.GuardTimer ?? 0f;

        // Warrior F — Slash Barrage cast state.
        public bool  WarriorIsFCasting => _warriorHandler?.IsFCasting ?? false;
        public float WarriorFCastTimer => _warriorHandler?.FCastTimer ?? 0f;

        // Warrior R — Great Sword Descent cast state.
        public bool  WarriorIsRCasting => _warriorHandler?.IsRCasting ?? false;
        public float WarriorRCastTimer  => _warriorHandler?.RCastTimer  ?? 0f;

        // Mage RC teleport state -- owned by handler; re-exposed here for AbilityDebugUI.
        public bool  IsMageRCMode         => _mageHandler != null;
        public bool  MageIsInRecastWindow => _mageHandler?.IsInRecastWindow ?? false;
        public float MageRecastTimer      => _mageHandler?.RecastTimer      ?? 0f;

        // Mage fireball ammo state -- owned by handler; re-exposed here for AbilityDebugUI.
        public int  MageFireballAmmo      => _mageHandler?.FireballAmmo      ?? 0;
        public int  MageFireballMaxAmmo   => _mageHandler?.FireballMaxAmmo   ?? 0;
        public bool MageNextFireballIsBig => _mageHandler?.NextFireballIsBig ?? false;

        // Mage E — Blackhole cast state.
        public bool  MageIsCastingBlackhole => _mageHandler?.IsCastingBlackhole ?? false;
        public float MageBlackholeCastTimer => _mageHandler?.BlackholeCastTimer  ?? 0f;

        // Mage F — Laser state.
        public bool  MageIsCastingLaser => _mageHandler?.IsCastingLaser ?? false;
        public bool  MageIsLaserActive  => _mageHandler?.IsLaserActive  ?? false;
        public float MageLaserCastTimer => _mageHandler?.LaserCastTimer ?? 0f;
        public float MageLaserTimer     => _mageHandler?.LaserTimer     ?? 0f;

        // Mage Z — Arcane Bolt state.
        public bool  MageIsZAiming    => _mageHandler?.IsZAiming    ?? false;
        public bool  MageIsZCharging  => _mageHandler?.IsZCharging  ?? false;
        public float MageZChargeTimer => _mageHandler?.ZChargeTimer ?? 0f;

        // Mage R — Meteor Judgment cast state (true for orbRiseDuration after cast).
        public bool MageIsRCasting => _mageHandler?.IsRCasting ?? false;

        // True while Mage Z is aiming/charging OR Archer F is firing — blocks BasicAttackController.
        public bool ShouldBlockBasicAttack =>
            (_mageHandler   != null && _mageHandler.IsBlockingBasicAttack) ||
            (_archerHandler != null && _archerHandler.ArcherIsFiring);

        // Archer RC — Rapid Fire buff state, re-exposed for AbilityDebugUI and BasicAttackController.
        public bool  IsArcherRCMode              => _archerHandler != null;
        public bool  ArcherIsRapidFiring         => _archerHandler?.IsRapidFiring         ?? false;
        public float ArcherRapidFireTimer        => _archerHandler?.RapidFireTimer        ?? 0f;
        public float ArcherAttackSpeedMultiplier => _archerHandler?.AttackSpeedMultiplier ?? 1f;

        // Archer E — Predictive Shield state, re-exposed for AbilityDebugUI.
        public bool  ArcherIsShielded  => _archerHandler?.IsShielded  ?? false;
        public float ArcherShieldTimer => _archerHandler?.ShieldTimer ?? 0f;

        // Archer Q — Enhanced Shot pending type.
        // ConsumeArcherPendingShotType() is called by BasicAttackController on every Archer shot.
        public bool           ArcherHasPendingShot        => _archerHandler?.HasPendingShotType   ?? false;
        public ArcherShotType ArcherPendingShotType       => _archerHandler?.PendingShotType      ?? ArcherShotType.Basic;
        public ArcherShotType ConsumeArcherPendingShotType() =>
            _archerHandler?.ConsumePendingShotType() ?? ArcherShotType.Basic;

        // Archer R — Overdrive state, re-exposed for AbilityDebugUI and BasicAttackController.
        public bool  ArcherIsOverdrive    => _archerHandler?.ArcherIsOverdrive    ?? false;
        public float ArcherOverdriveTimer => _archerHandler?.ArcherOverdriveTimer ?? 0f;

        // Archer F — Barrage Gauge state, re-exposed for AbilityDebugUI and BasicAttackController.
        public float ArcherFGauge    => _archerHandler?.ArcherFGauge    ?? 0f;
        public float ArcherFMaxGauge => _archerHandler?.ArcherFMaxGauge ?? 100f;
        public bool  ArcherIsFiring  => _archerHandler?.ArcherIsFiring  ?? false;

        // Called when an Archer Basic shot actually reduces enemy HP.
        // Consolidates both F gauge gain and Z cooldown refund in one callback,
        // so BasicAttackController caches a single delegate for Basic shots.
        public void OnArcherBasicHit(float gaugeAmount)
        {
            _archerHandler?.AddBarrageGauge(gaugeAmount);
            ReduceCooldown(AbilitySlot.Z, config.archerRollCooldownRefundOnBasicHit);
        }

        // Called from Q explosion hit callbacks (cached in BasicAttackController._addFGaugeCallback).
        // Q shots earn gauge but do NOT refund Z cooldown.
        public void AddArcherFGauge(float amount) => _archerHandler?.AddBarrageGauge(amount);

        // Reduces a slot's cooldown by amount, clamped to zero.
        public void ReduceCooldown(AbilitySlot slot, float amount)
        {
            int i = (int)slot;
            _cooldowns[i] = Mathf.Max(0f, _cooldowns[i] - amount);
        }

        // Rogue Q charge state -- owned by handler; re-exposed here for AbilityDebugUI.
        public bool  IsRogueQChargeMode  => _rogueHandler != null;
        public int   RogueQCharges       => _rogueHandler?.QCharges ?? 0;
        public int   RogueQMaxCharges    => config.rogueQMaxCharges;
        public float RogueQRechargeTimer => _rogueHandler?.QRechargeTimer ?? 0f;
        public float RogueQLockoutTimer  => _rogueHandler?.QLockoutTimer ?? 0f;

        // Rogue F combo state -- re-exposed for AbilityDebugUI.
        public bool  IsRogueFComboMode      => _rogueHandler != null && _rogueHandler.FInComboWindow;
        public int   RogueFComboStep        => _rogueHandler?.FComboStep ?? 0;
        public float RogueFComboWindowTimer => _rogueHandler?.FComboWindowTimer ?? 0f;
        public float RogueF3SpeedTimer      => _rogueHandler?.F3SpeedTimer ?? 0f;

        // Rogue E state -- re-exposed for AbilityDebugUI.
        public bool  RogueIsEFlying    => _rogueHandler?.IsEFlying   ?? false;

        // Rogue R stealth/reveal state -- re-exposed for AbilityDebugUI.
        public bool  RogueIsStealthed  => _rogueHandler?.IsStealthed ?? false;
        public float RogueStealthTimer => _rogueHandler?.StealthTimer ?? 0f;
        public bool  RogueIsRevealed   => _rogueHandler?.IsRevealed  ?? false;
        public float RogueRevealTimer  => _rogueHandler?.RevealTimer  ?? 0f;

        void Start()
        {
            if (config == null || ownerHealth == null || cameraTransform == null)
            {
                Debug.LogError("[AbilityController] Missing required reference. Disabling.", this);
                enabled = false;
                return;
            }
            _combatClass     = ownerHealth.CombatClass;
            _abilityConfig   = config.GetAbilityConfig(_combatClass);
            _ownerController = ownerHealth.GetComponent<CharacterController>();

            if (_combatClass == CombatClass.Rogue)
            {
                _rogueHandler = gameObject.AddComponent<RogueAbilityHandler>();
                _rogueHandler.Init(this, config, ownerHealth, cameraTransform);
            }
            if (_combatClass == CombatClass.Warrior)
            {
                _warriorHandler = gameObject.AddComponent<WarriorAbilityHandler>();
                _warriorHandler.Init(this, config, ownerHealth, cameraTransform);
            }
            if (_combatClass == CombatClass.Mage)
            {
                _mageHandler = gameObject.AddComponent<MageAbilityHandler>();
                _mageHandler.Init(this, config, ownerHealth, cameraTransform);
            }
            if (_combatClass == CombatClass.Archer)
            {
                _archerHandler = gameObject.AddComponent<ArcherAbilityHandler>();
                _archerHandler.Init(this, config, ownerHealth, cameraTransform);
            }

            ownerHealth.OnDeath += HandleOwnerDeath;
            _ready = true;
        }

        void Update()
        {
            if (!_ready) return;
            float dt = Time.deltaTime;

            // Shared cooldown array -- ticks down for all slots, all classes.
            for (int i = 0; i < _cooldowns.Length; i++)
                if (_cooldowns[i] > 0f)
                    _cooldowns[i] = Mathf.Max(0f, _cooldowns[i] - dt);

            // Class-specific ability timers.
            _rogueHandler?.TickTimers(dt);
            _warriorHandler?.TickTimers(dt);
            _mageHandler?.TickTimers(dt);
            _archerHandler?.TickTimers(dt);

            // Shared dash timer -- used by Warrior RC, Rogue RC, and Rogue E2.
            if (_isDashing)
            {
                _dashTimer -= dt;
                if (_dashTimer <= 0f)
                {
                    _isDashing = false;
                    _dashTimer = 0f;
                    TryClearPassthrough();
                    OnDashEnded();
                }
            }
        }

        // Called by LocalPlayerController each frame to translate input into ability activations.
        public void Tick(PlayerCommand cmd)
        {
            if (!_ready) return;
            // E2 forced movement blocks all ability input until the leap finishes.
            if (_rogueHandler != null && _rogueHandler.IsE2Dashing) return;
            // Per-frame held-input handlers run FIRST so their input caches are current
            // when the TryActivate calls below read them (e.g. Archer Z reads _cachedMoveInput
            // that TickHeldInputs just wrote from this frame's cmd.MoveInput).
            _mageHandler?.TickZ(cmd);
            _archerHandler?.TickHeldInputs(cmd);
            if (cmd.RightClickPressed) TryActivate(AbilitySlot.RightClick);
            if (cmd.SkillQPressed)     TryActivate(AbilitySlot.Q);
            if (cmd.SkillEPressed)     TryActivate(AbilitySlot.E);
            if (cmd.SkillRPressed)     TryActivate(AbilitySlot.R);
            if (cmd.SkillFPressed)     TryActivate(AbilitySlot.F);
            if (cmd.SkillZPressed)     TryActivate(AbilitySlot.Z);
        }

        // Public so bot AI can trigger abilities without going through PlayerCommand.
        public bool TryActivate(AbilitySlot slot)
        {
            // Class handlers own all slots for their class.
            if (_rogueHandler   != null) return _rogueHandler.TryActivate(slot);
            if (_warriorHandler != null) return _warriorHandler.TryActivate(slot);
            if (_mageHandler    != null) return _mageHandler.TryActivate(slot);
            if (_archerHandler  != null) return _archerHandler.TryActivate(slot);

            // Other classes: shared cooldown array path.
            int i = (int)slot;
            if (_cooldowns[i] > 0f) return false;
            _cooldowns[i] = GetBaseCooldown(slot);
            ExecuteEffect(slot);
            return true;
        }

        public float GetCooldownRemaining(AbilitySlot slot) => _cooldowns[(int)slot];

        // --- Internal API for RogueAbilityHandler

        internal ClassAbilityConfig AbilityConfig => _abilityConfig;

        internal float GetCooldown(AbilitySlot slot)              => _cooldowns[(int)slot];
        internal void  SetCooldown(AbilitySlot slot, float value) => _cooldowns[(int)slot] = value;

        // Forward-direction dash: velocity = distance / duration, direction = horizontal camera fwd.
        internal void StartDash(float distance, float duration)
        {
            Vector3 fwd = cameraTransform.forward;
            fwd.y = 0f;
            if (fwd.sqrMagnitude < 0.001f)
            {
                fwd   = ownerHealth.transform.forward;
                fwd.y = 0f;
                if (fwd.sqrMagnitude < 0.001f) fwd = ownerHealth.transform.right;
                fwd.Normalize();
            }
            else fwd.Normalize();

            _dashHorizontalVelocity = fwd * (distance / duration);
            _dashTimer              = duration;
            _isDashing              = true;
            SetDashPassthrough(true);
        }

        // Direction-specified dash -- used by Rogue E2 to leap toward a fixed world target.
        internal void StartDash(Vector3 velocity, float duration)
        {
            _dashHorizontalVelocity = velocity;
            _dashTimer              = duration;
            _isDashing              = true;
            SetDashPassthrough(true);
        }

        // Toggles Physics.IgnoreCollision between the owner and all pre-registered characters.
        // Called by dash, Rogue Z, and Warrior Q to share the same collision-ignore layer.
        internal void SetDashPassthrough(bool ignore)
        {
            if (_ownerController == null || dashPassthroughControllers == null) return;
            for (int i = 0; i < dashPassthroughControllers.Length; i++)
            {
                CharacterController other = dashPassthroughControllers[i];
                if (other != null && other != _ownerController)
                    Physics.IgnoreCollision(_ownerController, other, ignore);
            }
        }

        // Called by RogueAbilityHandler when the Z passthrough timer expires.
        // Only clears passthrough if no dash is also in progress.
        internal void TryEndZPassthrough()
        {
            if (!_isDashing)
                SetDashPassthrough(false);
        }

        internal float GetBaseCooldown(AbilitySlot slot) => slot switch
        {
            AbilitySlot.RightClick => _abilityConfig.RightClickCooldown,
            AbilitySlot.Q          => _abilityConfig.QCooldown,
            AbilitySlot.E          => _abilityConfig.ECooldown,
            AbilitySlot.R          => _abilityConfig.RCooldown,
            AbilitySlot.F          => _abilityConfig.FCooldown,
            AbilitySlot.Z          => _abilityConfig.ZCooldown,
            _                      => 0f,
        };

        // --- Private

        private void ExecuteEffect(AbilitySlot slot)
        {
            // Warrior and Rogue are fully handled by their own handlers.
            // Add cases here for Archer/Mage as their abilities are implemented.
        }

        private void OnDashEnded()
        {
            _rogueHandler?.OnDashEnded();
            _warriorHandler?.OnDashEnded();
        }

        // Clears passthrough only when no ability that needs it is still active.
        internal void TryClearPassthrough()
        {
            bool zActive    = _rogueHandler   != null && _rogueHandler.IsZPassthrough;
            bool qActive    = _warriorHandler != null && _warriorHandler.IsQCasting;
            bool mageActive = _mageHandler    != null && _mageHandler.IsPassthroughActive;
            if (!_isDashing && !zActive && !qActive && !mageActive)
                SetDashPassthrough(false);
        }

        // On death: abort dash, clear passthrough, delegate Rogue state cleanup to handler.
        // Policy: already-spawned world objects (rifts, bombs) live out their lifetime.
        //         Scheduled future rifts are cancelled. Q charges/cooldowns are preserved.
        private void HandleOwnerDeath(HealthComponent _)
        {
            _isDashing              = false;
            _dashTimer              = 0f;
            _dashHorizontalVelocity = Vector3.zero;
            SetDashPassthrough(false);
            _rogueHandler?.HandleOwnerDeath();
            _warriorHandler?.HandleOwnerDeath();
            _mageHandler?.HandleOwnerDeath();
            _archerHandler?.HandleOwnerDeath();
        }

        void OnDisable()
        {
            SetDashPassthrough(false);
            _rogueHandler?.ForceCleanup();
            _warriorHandler?.ForceCleanup();
            _mageHandler?.ForceCleanup();
            _archerHandler?.ForceCleanup();
        }

        void OnDestroy()
        {
            if (ownerHealth != null) ownerHealth.OnDeath -= HandleOwnerDeath;
            SetDashPassthrough(false);
            _rogueHandler?.ForceCleanup();
            _warriorHandler?.ForceCleanup();
            _mageHandler?.ForceCleanup();
            _archerHandler?.ForceCleanup();
        }
    }
}
