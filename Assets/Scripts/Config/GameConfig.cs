using UnityEngine;
using Game.Combat;

namespace Game.Config
{
    [CreateAssetMenu(fileName = "GameConfig", menuName = "Game/GameConfig")]
    public class GameConfig : ScriptableObject
    {
        [Header("Movement")]
        public float moveSpeed           = 5f;
        public float crouchSpeed         = 2.5f;
        public float jumpHeight          = 1.5f;
        public float gravity             = -20f;

        [Header("Mouse Look")]
        public float mouseSensitivity    = 2f;
        public float maxPitchAngle       = 85f;

        [Header("Crouch")]
        public float standHeight         = 2f;
        public float crouchHeight        = 1f;
        public float crouchTransitionSpeed = 10f;

        [Header("Camera Height")]
        public float standCameraLocalY   = 1.6f;
        public float crouchCameraLocalY  = 0.8f;

        [Header("Combat — Base Stats")]
        public float baseMaxHp           = 100f;
        public float baseArmor           = 0f;

        [Header("Combat — Class Attack Data")]
        public ClassAttackData warriorAttack = new ClassAttackData { Damage = 30f, Range =  2f, Cooldown = 1.0f };
        public ClassAttackData archerAttack  = new ClassAttackData { Damage =  5f, Range = 15f, Cooldown = 1.0f };
        public ClassAttackData rogueAttack   = new ClassAttackData { Damage = 15f, Range =  8f, Cooldown = 0.5f };
        public ClassAttackData mageAttack    = new ClassAttackData { Damage = 40f, Range = 25f, Cooldown = 2.0f };
        public LayerMask       attackLayerMask = -1;

        // Returns the attack data for the given class. Called once at Start(), never per-frame.
        public ClassAttackData GetAttackData(CombatClass cls) => cls switch
        {
            CombatClass.Archer  => archerAttack,
            CombatClass.Rogue   => rogueAttack,
            CombatClass.Mage    => mageAttack,
            _                   => warriorAttack,
        };

        [Header("Ability — Warrior Dash")]
        public float warriorDashDistance = 20f;
        public float warriorDashDuration = 1f;

        [Header("Warrior — Rising/Falling Slash (Q)")]
        public float warriorQHit1Delay      = 0.05f;  // seconds from Q press to rising-slash hit
        public float warriorQHit1Damage     = 15f;    // rising slash — damage only, no displacement
        public float warriorQHit2Damage     = 25f;    // falling slam (on landing) — damage + launch
        public float warriorQRange          = 3.75f;  // rising-slash hit radius (m)
        public float warriorQAngle          = 120f;   // rising-slash forward cone (degrees)
        public float warriorQHit2Range      = 6.0f;   // falling-slam hit radius — wider AoE (m)
        public float warriorQHit2Angle      = 160f;   // falling-slam cone — near-omnidirectional (degrees)
        // Air time = 2 * warriorQLaunchSpeed / |gravity| = 2 * 10 / 20 = 1.0 s
        public float warriorQLaunchSpeed    = 10f;    // upward impulse applied to slam targets (m/s)

        [Header("Warrior — Q Self-Movement")]
        public float warriorQRiseForward      = 1.5f;  // forward displacement during rising phase (m)
        public float warriorQRiseHeight       = 3.0f;  // upward displacement during rising phase (m)
        public float warriorQRiseDuration     = 0.15f; // rising movement duration (s)
        // Fall phase ends on landing (CharacterController.isGrounded), not on a fixed timer.
        public float warriorQFallSpeed        = 14f;   // downward speed during fall phase (m/s)
        public float warriorQFallForwardSpeed = 4f;    // forward speed during fall phase (m/s)
        public float warriorQMaxFallDuration  = 3.0f;  // safety timeout — cancels fall if no landing (s)

        [Header("Warrior — Iron Guard (E)")]
        public float warriorGuardDuration            = 4f;    // guard active duration (s)
        public float warriorGuardDamageMultiplier    = 0.5f;  // incoming damage multiplier (0.5 = 50% reduction)
        public float warriorGuardMoveSpeedMultiplier = 0.7f;  // movement speed multiplier during guard

        [Header("Warrior — Parry Retreat (Z)")]
        public float warriorZRetreatDistance = 6f;    // backwards retreat distance (m)
        public float warriorZRetreatDuration = 0.35f; // retreat dash duration (s)
        public float warriorZWaveDelay       = 0.5f;  // delay from cast to second wave (s)
        public float warriorZWaveArmSpan     = 4f;    // total length of each X arm (m); extends 2m each way from centre
        public float warriorZWaveWidth       = 1.8f;  // X arm beam thickness (m)
        public float warriorZWaveLength      = 12f;   // forward travel distance per wave (m)
        public float warriorZWaveDuration    = 0.45f; // wave travel time; speed = length / duration
        public float warriorZWaveDamage      = 15f;   // damage per wave hit
        public float warriorZWaveKnockback   = 1.5f;  // knockback magnitude on hit (m/s)

        [Header("Warrior — Great Sword Descent (R)")]
        public float warriorRCastTime             = 2.0f;   // cast duration before impact (s)
        public float warriorRRiseDuration         = 1.9f;   // player rise phase duration (s)
        public float warriorRDropDuration         = 0.1f;   // player drop phase duration (s)
        public float warriorRRiseHeight           = 5.0f;   // total vertical rise (m); drop returns same distance
        public float warriorRAreaSize             = 15.0f;  // square AoE side length (m)
        public float warriorRAreaHeight           = 5.0f;   // AoE box half-height above/below centre (m)
        public float warriorRStunDuration         = 3.0f;   // stun duration on enemies hit (s)
        public float warriorRSwordScaleMultiplier = 10.0f;  // sword visual scale relative to character height

        [Header("Warrior — Slash Barrage (F)")]
        public float warriorFDuration            = 2.0f;  // total cast duration (s)
        public float warriorFDamageInterval      = 0.5f;  // seconds between damage ticks (4 ticks total)
        public float warriorFVisualInterval      = 0.1f;  // seconds between slash visuals
        public float warriorFDamage              = 5f;    // damage per normal tick
        public float warriorFFinalDamage         = 10f;   // damage on the final (4th) tick
        public float warriorFRadius              = 3.5f;  // front-hemisphere radius (m)
        public float warriorFMoveSpeedMultiplier = 0.5f;  // move speed multiplier during cast
        public float warriorFFinalKnockback      = 1.5f;  // knockback on final tick (m/s)

        [Header("Warrior — RC Dash Impact")]
        public float warriorDashImpactRadius      = 1.2f; // overlap sphere radius for enemy detection (m)
        public float warriorDashImpactLaunchSpeed = 6.0f; // upward impulse applied to hit targets (m/s)
        public float warriorDashImpactDamage      = 0f;   // bonus damage on impact (0 = no damage, launch only)

        [Header("Rogue — Dash")]
        public float rogueDashDistance = 10f;
        public float rogueDashDuration = 0.35f;

        [Header("Rogue — Dimensional Rift")]
        public float rogueDashRiftDamage   = 10f;  // damage per rift hit
        public float rogueDashRiftDuration = 1f;   // how long each rift stays active
        public float rogueDashRiftDelay    = 0.5f; // delay after dash ends before first rift
        public float rogueDashRiftInterval = 0.5f; // time between subsequent rifts
        public int   rogueDashRiftCount    = 3;    // total rifts spawned per dash
        public float rogueDashRiftWidth    = 2f;   // rift hitbox width (perpendicular to dash)
        public float rogueDashRiftHeight   = 2f;   // rift hitbox height

        [Header("Rogue — Giant Shuriken Zone (Q)")]
        public float rogueQZoneSize        = 3f;    // full width/depth of the zone square (m)
        public float rogueQTravelDistance  = 3f;    // forward travel distance (m)
        public float rogueQTravelDuration  = 0.3f;  // travel time (s)
        public float rogueQZoneDuration    = 4f;    // stationary duration after travel (s)
        public float rogueQDamagePerSecond = 5f;    // DPS while enemy is inside the zone
        public float rogueQTickInterval    = 0.5f;  // damage check interval (s)
        public float rogueQSlowMultiplier  = 0.7f;  // move speed multiplier on hit (0.7 = -30%)
        public float rogueQSlowDuration    = 1.5f;  // slow duration after last contact (s)
        public int   rogueQMaxCharges      = 2;     // maximum stored charges
        public float rogueQCastLockout     = 1f;    // seconds between consecutive casts (even with charges)

        [Header("Rogue — Shuriken (E1 + E2)")]
        public float rogueEDamage          = 10f;  // E1 hit damage
        public float rogueEProjectileSpeed = 18f;  // shuriken travel speed (m/s)
        public float rogueERange           = 15f;  // max shuriken travel distance (m)
        public float rogueEMarkDuration    = 3f;   // how long E1 mark lasts; expiry = E immediately ready again
        public float rogueEDashSpeed       = 20f;  // E2 forced movement speed (m/s)
        public float rogueEArrivalOffset   = 1.5f; // metres behind target for E2 landing point

        [Header("Rogue — Phase-Shift Stun Bomb (Z)")]
        public float rogueZTeleportDistance    = 6f;    // backwards teleport range (m)
        public float rogueZBombFuseTime        = 0.3f;  // seconds before explosion
        public float rogueZBlastRadius         = 2f;    // explosion radius (m)
        public float rogueZDamage              = 3f;    // test damage per hit
        public float rogueZStunDuration        = 1f;    // stun duration (s)
        public float rogueZPassthroughDuration = 0.2f;  // character passthrough after Z teleport (s)

        [Header("Rogue — Shadow Slash (F) — 3-hit combo")]
        public float rogueShadowSlashStep1Distance = 4f;
        public float rogueShadowSlashStep2Distance = 4f;
        public float rogueShadowSlashStep3Distance = 6f;
        public float rogueShadowSlashStep1Duration = 0.2f;
        public float rogueShadowSlashStep2Duration = 0.2f;
        public float rogueShadowSlashStep3Duration = 0.3f;
        public float rogueShadowSlashStep1Damage   = 25f;
        public float rogueShadowSlashStep2Damage   = 25f;
        public float rogueShadowSlashStep3Damage   = 30f;
        public float rogueShadowSlashRange         = 2f;
        public float rogueShadowSlashAngle         = 90f;
        public float rogueShadowSlashComboWindow   = 2f;    // seconds to input next hit after each step
        public float rogueShadowSlashInputLock     = 0.25f; // min wait after each step before next can trigger
        public float rogueShadowSlashStep3MoveSpeedMultiplier = 1.5f;
        public float rogueShadowSlashStep3MoveSpeedDuration   = 2f;

        [Header("Rogue — Stealth / Backstab (R)")]
        public float rogueStealthDuration            = 5f;    // stealth window (s)
        public float rogueStealthMoveSpeedMultiplier = 2f;    // movement speed while stealthed
        public float rogueBackstabBonusDamage        = 30f;   // fixed bonus damage when attacking from behind while stealthed
        public float rogueBackstabAngle              = 90f;   // total rear cone angle for backstab (45 deg each side of directly-behind)

        [Header("Archer — Basic Projectile")]
        public float archerBasicProjectileDamage   = 5f;
        public float archerBasicProjectileCooldown = 0.25f;
        public float archerBasicProjectileRange    = 30f;
        public float archerBasicProjectileSpeed    = 50f;
        public float archerBasicProjectileRadius   = 0.18f;

        [Header("Archer — Rapid Fire (RC)")]
        public float archerRapidFireDuration              = 3f;
        public float archerRapidFireMoveSpeedMultiplier   = 1.3f;
        public float archerRapidFireAttackSpeedMultiplier = 1.5f;
        public float archerRapidFireCooldown              = 10f;

        [Header("Archer — Predictive Shield (E)")]
        public float archerShieldDuration = 2.5f;
        public float archerShieldCooldown = 14f;

        [Header("Archer — Enhanced Shot (Q)")]
        public float archerQShockRadius       = 1.2f;
        public float archerQShockDamage       = 5f;
        public float archerQShockStunDuration = 0.7f;
        public float archerQFireRadius        = 2.0f;
        public float archerQFireDamage        = 10f;
        public float archerQIceRadius         = 3.0f;
        public float archerQIceDamage         = 4f;
        public float archerQIceSlowMultiplier = 0.7f;
        public float archerQIceSlowDuration   = 2.0f;

        [Header("Archer — Roll (Z)")]
        public float archerRollDistance  = 4f;
        public float archerRollDuration  = 0.25f;
        public float archerRollCooldown              = 8f;
        public float archerRollGaugeGain             = 10f;
        public float archerRollCooldownRefundOnBasicHit = 0.1f;

        [Header("Archer — Overdrive (R)")]
        public float archerOverdriveDuration                 = 10f;
        public float archerOverdriveCooldown                 = 30f;
        public float archerOverdriveMoveSpeedMultiplier      = 1.5f;
        public float archerOverdriveAttackSpeedMultiplier    = 2.0f;
        public float archerOverdriveProjectileScaleMultiplier = 2.0f;
        public float archerOverdriveExplosionRadius          = 2.0f;
        public float archerOverdriveExplosionDamage          = 5f;

        [Header("Archer — Barrage Gauge (F)")]
        public float archerFMaxGauge               = 100f;
        public float archerFMinStartGauge          = 10f;
        public float archerFGaugeGainOnBasicHit    = 2f;
        public float archerFGaugeGainOnEnhancedHit = 3f;
        public float archerFDrainPerSecond         = 20f;
        public float archerFFireInterval           = 0.1f;
        public float archerFDamage                 = 3f;
        public float archerFSlowMultiplier         = 0.8f;
        public float archerFSlowDuration           = 0.4f;
        public float archerFMoveSpeedMultiplier    = 0.8f;
        public float archerFSpreadAngle            = 4f;

        [Header("Mage — Fireball (Primary Attack)")]
        public int   mageFireballMaxAmmo          = 15;    // max ammo capacity
        public float mageFireballRechargeInterval = 2.0f; // seconds per 1 ammo recharge
        public float mageFireballFireInterval     = 0.2f; // minimum seconds between shots
        public float mageFireballSpeed            = 30f;  // m/s
        public float mageFireballRange            = 20f;  // max travel distance (m)
        public float mageFireballDamage           = 6f;
        public int   mageBigFireballEvery         = 5;    // every Nth shot is a big fireball
        public float mageBigFireballDamage        = 15f;
        public float mageBigFireballScale         = 2.5f;
        public float mageFireballRadius           = 0.25f; // normal fireball sphere radius (m)
        public float mageBigFireballRadius        = 0.65f; // big fireball sphere radius (m)
        // Big fireball hit effects.
        public float mageBigFireballStunDuration      = 0.5f;   // stun applied to enemy on big fireball hit (s)
        public float mageBigFireballKnockbackSpeed    = 4.0f;   // knockback velocity magnitude (m/s)
        public float mageBigFireballKnockbackDuration = 0.25f;  // knockback timer duration (s)

        [Header("Mage — Passive")]
        public int   magePassiveAmmoGain          = 3;    // Q ammo restored when any non-Q skill succeeds

        [Header("Mage — Meteor Judgment (R)")]
        public float mageMeteorOrbRiseDuration  = 2.0f;   // orb ascent time (s)
        public float mageMeteorOrbRiseHeight    = 15.0f;  // orb ascent height (m)
        public float mageMeteorStormDelay       = 0.2f;   // delay before first meteor (s)
        public float mageMeteorStormDuration    = 3.0f;   // total meteor spawn window (s)
        public float mageMeteorStormRadius      = 15.0f;  // storm radius (m)
        public int   mageMeteorCount            = 20;     // meteors per storm
        public float mageMeteorImpactRadius     = 1.5f;   // per-meteor blast radius (m)
        public float mageMeteorDamage           = 30.0f;  // damage per meteor
        public float mageMeteorFallHeight       = 15.0f;  // height meteors fall from (m)
        public float mageMeteorFallDuration     = 0.45f;  // meteor fall time (s)
        public float mageMeteorWarningDuration  = 0.45f;  // warning indicator shown before fall (s)
        public float mageMeteorVisualRadius     = 0.35f;  // meteor sphere visual radius (m)
        public float mageMeteorAimDistance      = 10.0f;  // storm center: distance ahead of caster (m)

        [Header("Mage — Arcane Bolt (Z)")]
        public float mageArcaneBoltMaxChargeTime = 2.0f;  // full charge duration (s)
        public float mageArcaneBoltMinSpeed      = 8.0f;  // speed at zero charge (m/s)
        public float mageArcaneBoltMaxSpeed      = 30.0f; // speed at full charge (m/s)
        public float mageArcaneBoltRange         = 18.0f; // max travel distance (m)
        public float mageArcaneBoltRadius        = 0.35f; // projectile sphere radius (m)
        public float mageArcaneBoltMinStun       = 0.2f;  // stun at zero charge (s)
        public float mageArcaneBoltMaxStun       = 1.5f;  // stun at full charge (s)
        public float mageArcaneBoltMinDamage     = 5.0f;  // damage at zero charge
        public float mageArcaneBoltMaxDamage     = 20.0f; // damage at full charge

        [Header("Mage — Laser (F)")]
        public float mageLaserCastTime       = 0.5f;  // cast delay before beam starts (s)
        public float mageLaserDuration       = 5.0f;  // beam lifetime (s)
        public float mageLaserTickInterval   = 0.5f;  // seconds between damage/knockback pulses
        public float mageLaserRange          = 15.0f; // beam length (m)
        public float mageLaserRadius         = 2.0f;  // beam width radius for hit detection (m)
        public float mageLaserDamage         = 5.0f;  // damage per tick
        public float mageLaserKnockbackSpeed      = 5.0f;  // knockback magnitude per tick (m/s)
        public float mageLaserVisualRadius        = 1.0f;  // visual beam width radius (m)
        public float mageLaserMoveSpeedMultiplier = 0.3f;  // movement speed during active laser (30% of normal)

        [Header("Mage — Blackhole (E)")]
        public float mageBlackholeCastTime            = 1.0f;   // cast delay before zone spawns (s)
        public float mageBlackholeDuration            = 5.0f;   // zone lifetime (s)
        public float mageBlackholeRange               = 7.0f;   // horizontal distance from caster (m)
        public float mageBlackholeHeightOffset        = 5.0f;   // height above caster feet (m)
        public float mageBlackholeRadius              = 8.0f;   // horizontal (XZ) effect radius (m)
        public float mageBlackholeCylinderHalfHeight  = 20.0f;  // vertical half-extent for candidate search (m)
        public float mageBlackholeVisualRadius        = 2.5f;   // visual sphere radius shown in-game (m)
        public float mageBlackholeSlowMultiplier      = 0.7f;   // move speed multiplier inside zone
        public float mageBlackholeSlowRefreshDuration = 0.25f;  // slow/pull timer refresh per tick (s)
        public float mageBlackholeTickInterval        = 0.1f;   // seconds between zone effect pulses
        public float mageBlackholePullSpeed           = 2.0f;   // horizontal pull velocity (m/s)

        [Header("Mage — Teleport (RC)")]
        public float mageTeleportDistance           = 6.0f;   // blink range (m)
        public float mageTeleportRecastWindow       = 0.5f;   // window to trigger 2nd teleport (s)
        public int   mageTeleportMaxCharges         = 2;      // consecutive teleports per cooldown cycle
        public float mageTeleportPassthroughDuration = 0.15f; // post-teleport character passthrough (s)

        [Header("Visual - First Person Weapon / Warrior")]
        public Vector3 warriorWeaponViewLocalPosition = new Vector3(0.45f, -0.35f, 0.75f);
        public Vector3 warriorWeaponViewLocalEuler    = new Vector3(10f, 180f, 0f);
        public float warriorWeaponViewScale = 0.015f;

        [Header("Visual - First Person Weapon / Archer")]
        public Vector3 archerWeaponViewLocalPosition = new Vector3(0.45f, -0.35f, 0.75f);
        public Vector3 archerWeaponViewLocalEuler    = new Vector3(10f, 180f, 0f);
        public float archerWeaponViewScale  = 0.0025f;

        [Header("Visual - First Person Weapon / Rogue")]
        public Vector3 rogueWeaponViewLocalPosition = new Vector3(0.45f, -0.35f, 0.75f);
        public Vector3 rogueWeaponViewLocalEuler    = new Vector3(10f, 180f, 0f);
        public float rogueWeaponViewScale   = 0.02f;

        [Header("Visual - First Person Weapon / Mage")]
        public Vector3 mageWeaponViewLocalPosition = new Vector3(0.45f, -0.35f, 0.75f);
        public Vector3 mageWeaponViewLocalEuler    = new Vector3(10f, 180f, 0f);
        public float mageWeaponViewScale    = 0.01f;

        public bool debugLiveUpdateWeaponView = true;

        [Header("Visual - First Person Weapon Motion")]
        public bool  weaponViewMotionEnabled = true;
        public float weaponViewIdleBobSpeed = 1.6f;
        public float weaponViewIdleBobAmount = 0.012f;
        public float weaponViewMoveBobSpeed = 8.0f;
        public float weaponViewMoveBobXAmount = 0.035f;
        public float weaponViewMoveBobYAmount = 0.045f;
        public float weaponViewLookSwayAmount = 0.012f;
        public float weaponViewLookSwayRotation = 2.5f;
        public float weaponViewSwingDuration = 0.28f;
        public float weaponViewSwingPositionAmount = 0.16f;
        public float weaponViewSwingRotationAmount = 55f;
        public float weaponViewGunRecoilDuration = 0.12f;
        public float weaponViewGunRecoilPositionAmount = 0.10f;
        public float weaponViewGunRecoilRotationAmount = 9f;
        public bool  cameraViewBobEnabled = true;
        public float cameraViewBobAmount = 0.035f;
        public float cameraViewBobSideAmount = 0.018f;
        public float cameraViewBobRollAmount = 0.8f;

        [Header("Ability — Class Cooldowns")]
        public ClassAbilityConfig warriorAbility = new ClassAbilityConfig { RightClickCooldown = 15f, QCooldown = 8f, ECooldown = 14f, ZCooldown = 18f, FCooldown = 12f, RCooldown = 30f };
        public ClassAbilityConfig archerAbility = new ClassAbilityConfig { RightClickCooldown = 10f, QCooldown = 6f };
        public ClassAbilityConfig rogueAbility   = new ClassAbilityConfig { RightClickCooldown = 12f, ZCooldown = 10f, RCooldown = 10f, FCooldown = 9f };
        public ClassAbilityConfig mageAbility    = new ClassAbilityConfig { RightClickCooldown = 8f, ECooldown = 12f, FCooldown = 18f, ZCooldown = 8f, RCooldown = 30f };

        // Returns the ability cooldown config for the given class. Called once at Start(), never per-frame.
        public ClassAbilityConfig GetAbilityConfig(CombatClass cls) => cls switch
        {
            CombatClass.Archer  => archerAbility,
            CombatClass.Rogue   => rogueAbility,
            CombatClass.Mage    => mageAbility,
            _                   => warriorAbility,
        };

        [Header("Player")]
        public CombatClass playerStartingClass = CombatClass.Warrior;

        [Header("Respawn")]
        public float respawnDelay            = 3f;
        public float invulnerabilityDuration = 2f;

        [Header("Bot")]
        public float botDetectRange = 20f;
        public float botMoveSpeed   = 2f;

        [Header("3v3 Spawn")]
        public int   blueBotCount  = 2;
        public int   redBotCount   = 3;
        public float blueSpawnZ    = -5f;
        public float redSpawnZ     =  5f;
        public float spawnSpreadX  =  3f;

        [Header("3v3 Class Composition")]
        public CombatClass[] blueBotClasses = { CombatClass.Warrior, CombatClass.Archer };
        public CombatClass[] redBotClasses  = { CombatClass.Warrior, CombatClass.Warrior, CombatClass.Archer };

        [Header("UI")]
        public float damageFlashDuration = 0.25f;

        [Header("Debug — Remove Before Shipping")]
        public bool  debugCombatLogs   = false;
        public float debugLethalDamage = 999f;
    }
}
