using UnityEngine;
using Game.Combat;

namespace Game.UI
{
    // Debug HUD: 6 large ability slots (bottom-centre).
    // All strings are rebuilt only when the displayed value (integer tenths) changes.
    // Remove or disable before shipping.
    public class AbilityDebugUI : MonoBehaviour
    {
        public AbilityController abilityController;

        private static readonly AbilitySlot[] _slots =
            { AbilitySlot.RightClick, AbilitySlot.Q, AbilitySlot.E, AbilitySlot.R, AbilitySlot.F, AbilitySlot.Z };
        private static readonly string[] _slotKeys = { "RC", "Q", "E", "R", "F", "Z" };

        // Per-slot status string and background colour (both set in Update, drawn in OnGUI).
        private readonly string[] _status   = new string[6];
        private readonly Color[]  _bgColors = new Color[6];

        // Change-detection state (integers only — no per-frame allocations).
        private readonly int[] _lastCdTenths = new int[6];   // cooldown tenths per slot

        // Q
        private int  _lastQCharges = -1;
        private int  _lastQTimerT  = -1;
        private bool _lastQLock    = false;

        // R
        private bool _lastRStealth  = false;
        private int  _lastRStealthT = -1;
        private int  _lastRCdT      = -1;

        // E (warrior guard)
        private bool _lastEGuarding = false;
        private int  _lastEGuardT   = -1;

        // R — Warrior ultimate cast tracking
        private bool _lastRCasting  = false;
        private int  _lastRCastT    = -1;

        // F — Rogue combo mode tracking
        private int  _lastFModeKey  = -1; // 0=std, 1=combo, 2=spdbuff
        private int  _lastFStep     = -1;
        private int  _lastFWinT     = -1;
        private int  _lastFSpdT     = -1;
        // F — Warrior Slash Barrage tracking
        private bool _lastFCasting  = false;
        private int  _lastFCastT    = -1;
        // _lastCdTenths[4] tracks F cooldown

        // Archer RC rapid-fire buff tracking.
        private bool _lastArcherRCActive = false;
        private int  _lastArcherRCT      = -1;

        // Archer R — Overdrive tracking.
        private bool _lastArcherOverdrive  = false;
        private int  _lastArcherOverdriveT = -1;

        // Archer Q — pending enhanced shot type and cooldown.
        private ArcherShotType _lastArcherQShot = ArcherShotType.Basic;
        private int  _lastArcherQCdT = -1;

        // Archer E shield tracking.
        private bool _lastArcherEShielded = false;
        private int  _lastArcherEShieldT  = -1;

        // Archer F — Barrage Gauge tracking.
        private int  _lastArcherFGaugePct = -1;
        private bool _lastArcherFiring    = false;

        // Mage RC teleport recast-window tracking.
        private bool _lastMageRecast  = false;
        private int  _lastMageRecastT = -1;

        // Mage fireball ammo tracking.
        private int  _lastMageAmmo    = -1;
        private int  _lastMageMaxAmmo = -1;
        private bool _lastMageNextBig = false;

        // Mage E — Blackhole cast tracking.
        private bool _lastMageECasting = false;
        private int  _lastMageECastT   = -1;

        // Mage F — Laser tracking.
        private bool _lastMageFCasting  = false;
        private bool _lastMageFActive   = false;
        private int  _lastMageFCastT    = -1;
        private int  _lastMageFActiveT  = -1;

        // Mage Z — Arcane Bolt tracking.
        private bool _lastMageZAiming   = false;
        private bool _lastMageZCharging = false;
        private int  _lastMageZChargeT  = -1;

        // Mage R — Meteor Judgment tracking.
        private bool _lastMageRCasting = false;

        // Background colour presets (static readonly structs — zero GC).
        private static readonly Color ColReady     = new Color(0.08f, 0.40f, 0.10f, 0.88f);
        private static readonly Color ColCooldown  = new Color(0.18f, 0.18f, 0.18f, 0.88f);
        private static readonly Color ColTeleport   = new Color(0.10f, 0.45f, 0.70f, 0.88f); // teal-blue — mage recast window
        private static readonly Color ColFireball    = new Color(0.70f, 0.30f, 0.04f, 0.92f); // orange — mage ammo ready
        private static readonly Color ColBigFireball = new Color(0.65f, 0.08f, 0.04f, 0.92f); // deep red — big fireball next
        private static readonly Color ColBlackhole   = new Color(0.25f, 0.0f,  0.45f, 0.92f); // deep purple — blackhole cast
        private static readonly Color ColLaser       = new Color(0.10f, 0.0f,  0.65f, 0.92f); // bright purple — laser active
        private static readonly Color ColStealth  = new Color(0.28f, 0.04f, 0.50f, 0.88f);
        private static readonly Color ColCombo    = new Color(0.08f, 0.28f, 0.55f, 0.88f);
        private static readonly Color ColSpeedBuff = new Color(0.04f, 0.40f, 0.44f, 0.88f);
        private static readonly Color ColGuard     = new Color(0.50f, 0.40f, 0.03f, 0.88f); // gold — warrior guard
        private static readonly Color ColFCast     = new Color(0.55f, 0.08f, 0.08f, 0.88f); // dark red — warrior F cast
        private static readonly Color ColUlt        = new Color(0.38f, 0.04f, 0.48f, 0.92f); // purple — warrior R ultimate
        private static readonly Color ColAimBolt   = new Color(0.0f,  0.45f, 0.85f, 0.92f); // blue — Mage Z Aiming
        private static readonly Color ColChargeBolt = new Color(0.05f, 0.20f, 1.0f,  0.92f); // bright blue — Mage Z Charging
        private static readonly Color ColMeteor      = new Color(0.65f, 0.10f, 0.0f,  0.92f); // dark red-orange — Mage R casting
        private static readonly Color ColArcherRapid  = new Color(0.10f, 0.42f, 0.65f, 0.88f); // steel blue — Archer rapid fire active
        private static readonly Color ColArcherShield = new Color(0.05f, 0.65f, 0.90f, 0.88f); // bright cyan — Archer shield active
        private static readonly Color ColArcherShock   = new Color(1.00f, 0.92f, 0.05f, 0.88f); // bright yellow — Shock loaded
        private static readonly Color ColArcherFire   = new Color(0.88f, 0.15f, 0.02f, 0.88f); // deep red — Fire loaded
        private static readonly Color ColArcherIce    = new Color(0.05f, 0.55f, 0.92f, 0.88f); // bright blue — Ice loaded
        private static readonly Color ColArcherBarrage   = new Color(0.58f, 0.18f, 0.82f, 0.88f); // purple — F Barrage firing
        private static readonly Color ColArcherOverdrive = new Color(0.85f, 0.45f, 0.05f, 0.92f); // orange — R Overdrive active

        // Slot geometry constants.
        private const float SlotW  = 112f;
        private const float SlotH  = 60f;
        private const float KeyH   = 22f;  // top label height
        private const float Gap    = 3f;   // horizontal gap between slots

        // GUIStyles: initialised lazily on first OnGUI (requires GUI context).
        private static GUIStyle s_keyStyle;
        private static GUIStyle s_valStyle;

        void Start()
        {
            if (abilityController == null) { enabled = false; return; }
            for (int i = 0; i < 6; i++)
            {
                _lastCdTenths[i] = -1;
                _status[i]       = "READY";
                _bgColors[i]     = ColReady;
            }
        }

        void Update()
        {
            for (int i = 0; i < 6; i++)
            {
                switch (i)
                {
                    case 0: UpdateRCSlot();   break;
                    case 1: UpdateQSlot();    break;
                    case 2: UpdateESlot();    break;
                    case 3: UpdateRSlot();    break;
                    case 4: UpdateFSlot();    break;
                    case 5: UpdateZSlot();    break;
                    default: UpdateStdSlot(i); break;
                }
            }
        }

        // ── RC slot: Archer rapid fire / Mage recast window / standard ────
        void UpdateRCSlot()
        {
            if (abilityController.IsArcherRCMode) { UpdateArcherRCSlot(); return; }
            if (abilityController.IsMageRCMode)   { UpdateMageRCSlot();   return; }
            UpdateStdSlot(0);
        }

        void UpdateArcherRCSlot()
        {
            bool active  = abilityController.ArcherIsRapidFiring;
            int  activeT = Mathf.RoundToInt(abilityController.ArcherRapidFireTimer * 10f);
            int  cdT     = Mathf.RoundToInt(abilityController.GetCooldownRemaining(AbilitySlot.RightClick) * 10f);

            if (active == _lastArcherRCActive && activeT == _lastArcherRCT && cdT == _lastCdTenths[0]) return;
            _lastArcherRCActive = active;
            _lastArcherRCT      = activeT;
            _lastCdTenths[0]    = cdT;

            if (active)
            {
                _status[0]   = "RAPID\n" + Fmt(activeT) + "s";
                _bgColors[0] = ColArcherRapid;
            }
            else
            {
                _status[0]   = cdT <= 0 ? "READY" : Fmt(cdT) + "s";
                _bgColors[0] = cdT <= 0 ? ColReady : ColCooldown;
            }
        }

        void UpdateArcherRSlot()
        {
            bool overdrive = abilityController.ArcherIsOverdrive;
            int  overT     = Mathf.RoundToInt(abilityController.ArcherOverdriveTimer * 10f);
            int  cdT       = Mathf.RoundToInt(abilityController.GetCooldownRemaining(AbilitySlot.R) * 10f);

            if (overdrive == _lastArcherOverdrive && overT == _lastArcherOverdriveT && cdT == _lastRCdT) return;
            _lastArcherOverdrive  = overdrive;
            _lastArcherOverdriveT = overT;
            _lastRCdT             = cdT;

            if (overdrive)
            {
                _status[3]   = "OVR\n" + Fmt(overT) + "s";
                _bgColors[3] = ColArcherOverdrive;
            }
            else
            {
                _status[3]   = cdT <= 0 ? "READY" : Fmt(cdT) + "s";
                _bgColors[3] = cdT <= 0 ? ColReady : ColCooldown;
            }
        }

        void UpdateMageRCSlot()
        {
            bool recast = abilityController.MageIsInRecastWindow;
            int  rcT    = Mathf.RoundToInt(abilityController.MageRecastTimer * 10f);
            int  cdT    = Mathf.RoundToInt(abilityController.GetCooldownRemaining(AbilitySlot.RightClick) * 10f);

            if (recast == _lastMageRecast && rcT == _lastMageRecastT && cdT == _lastCdTenths[0]) return;
            _lastMageRecast  = recast;
            _lastMageRecastT = rcT;
            _lastCdTenths[0] = cdT;

            if (recast)
            {
                _status[0]   = "TP2\n" + Fmt(rcT) + "s";
                _bgColors[0] = ColTeleport;
            }
            else if (cdT > 0)
            {
                _status[0]   = Fmt(cdT) + "s";
                _bgColors[0] = ColCooldown;
            }
            else
            {
                _status[0]   = "READY";
                _bgColors[0] = ColReady;
            }
        }

        // ── Q slot: Archer enhanced shot type display + cooldown ──────────
        void UpdateArcherQSlot()
        {
            ArcherShotType shot = abilityController.ArcherPendingShotType;
            int cdT = Mathf.RoundToInt(abilityController.GetCooldownRemaining(AbilitySlot.Q) * 10f);

            if (shot == _lastArcherQShot && cdT == _lastArcherQCdT) return;
            _lastArcherQShot = shot;
            _lastArcherQCdT  = cdT;

            switch (shot)
            {
                case ArcherShotType.Shock:
                    _status[1]   = "SHOCK";
                    _bgColors[1] = ColArcherShock;
                    break;
                case ArcherShotType.Fire:
                    _status[1]   = "FIRE";
                    _bgColors[1] = ColArcherFire;
                    break;
                case ArcherShotType.Ice:
                    _status[1]   = "ICE";
                    _bgColors[1] = ColArcherIce;
                    break;
                default:
                    _status[1]   = cdT <= 0 ? "READY" : Fmt(cdT) + "s";
                    _bgColors[1] = cdT <= 0 ? ColReady : ColCooldown;
                    break;
            }
        }

        // ── Q slot: Mage fireball ammo (shown when player is Mage) ────────
        void UpdateMageAmmoSlot()
        {
            int  ammo    = abilityController.MageFireballAmmo;
            int  max     = abilityController.MageFireballMaxAmmo;
            bool nextBig = abilityController.MageNextFireballIsBig;

            if (ammo == _lastMageAmmo && max == _lastMageMaxAmmo && nextBig == _lastMageNextBig) return;
            _lastMageAmmo    = ammo;
            _lastMageMaxAmmo = max;
            _lastMageNextBig = nextBig;

            if (ammo <= 0)
            {
                _status[1]   = "0/" + max;
                _bgColors[1] = ColCooldown;
            }
            else if (nextBig)
            {
                _status[1]   = "BIG\n" + ammo + "/" + max;
                _bgColors[1] = ColBigFireball;
            }
            else
            {
                _status[1]   = "FIRE\n" + ammo + "/" + max;
                _bgColors[1] = ColFireball;
            }
        }

        // ── Standard cooldown slot ──────────────────────────────────────────
        void UpdateStdSlot(int i)
        {
            float cd     = abilityController.GetCooldownRemaining(_slots[i]);
            int   tenths = Mathf.RoundToInt(cd * 10f);
            if (tenths == _lastCdTenths[i]) return;
            _lastCdTenths[i] = tenths;
            if (tenths <= 0) { _status[i] = "READY";  _bgColors[i] = ColReady;    }
            else             { _status[i] = Fmt(tenths) + "s"; _bgColors[i] = ColCooldown; }
        }

        // ── Q slot: Archer enhanced shot / Mage ammo / Rogue charge / standard ─
        void UpdateQSlot()
        {
            if (abilityController.IsArcherRCMode)      { UpdateArcherQSlot();   return; }
            if (abilityController.IsMageRCMode)        { UpdateMageAmmoSlot();  return; }
            if (!abilityController.IsRogueQChargeMode) { UpdateStdSlot(1); return; }

            int   charges = abilityController.RogueQCharges;
            int   max     = abilityController.RogueQMaxCharges;
            bool  lockout = abilityController.RogueQLockoutTimer > 0f;
            float timer   = lockout ? abilityController.RogueQLockoutTimer : abilityController.RogueQRechargeTimer;
            int   timerT  = Mathf.RoundToInt(timer * 10f);

            if (charges == _lastQCharges && timerT == _lastQTimerT && lockout == _lastQLock) return;
            _lastQCharges = charges; _lastQTimerT = timerT; _lastQLock = lockout;

            if (charges >= max)
            {
                _status[1]   = charges + "/" + max;
                _bgColors[1] = ColReady;
            }
            else if (lockout)
            {
                _status[1]   = charges + "/" + max + "\nL " + Fmt(timerT) + "s";
                _bgColors[1] = ColCooldown;
            }
            else
            {
                _status[1]   = charges + "/" + max + "\n" + Fmt(timerT) + "s";
                _bgColors[1] = charges > 0 ? ColReady : ColCooldown;
            }
        }

        // ── E slot: Archer shield / Mage blackhole cast / warrior guard / Rogue E1 / cooldown / ready ──
        void UpdateESlot()
        {
            if (abilityController.IsArcherRCMode)      { UpdateArcherESlot(); return; }
            if (abilityController.IsMageRCMode)        { UpdateMageESlot();   return; }
            if (!abilityController.IsWarriorGuardMode)
            {
                // Rogue: show E1 in-flight indicator when projectile is airborne.
                if (abilityController.IsRogueQChargeMode && abilityController.RogueIsEFlying)
                {
                    if (_lastCdTenths[2] != -2) // -2 = sentinel for "E1 in flight"
                    {
                        _lastCdTenths[2] = -2;
                        _status[2]   = "E1";
                        _bgColors[2] = ColCombo;
                    }
                    return;
                }
                UpdateStdSlot(2);
                return;
            }

            bool guard  = abilityController.WarriorIsGuarding;
            int  guardT = Mathf.RoundToInt(abilityController.WarriorGuardTimer * 10f);
            int  cdT    = Mathf.RoundToInt(abilityController.GetCooldownRemaining(AbilitySlot.E) * 10f);

            if (guard == _lastEGuarding && guardT == _lastEGuardT && cdT == _lastCdTenths[2]) return;
            _lastEGuarding = guard; _lastEGuardT = guardT; _lastCdTenths[2] = cdT;

            if (guard)
            {
                _status[2]   = "GUARD\n" + Fmt(guardT) + "s";
                _bgColors[2] = ColGuard;
            }
            else
            {
                _status[2]   = cdT <= 0 ? "READY" : Fmt(cdT) + "s";
                _bgColors[2] = cdT <= 0 ? ColReady : ColCooldown;
            }
        }

        // ── R slot: Archer overdrive / Warrior ultimate / Mage meteor / Rogue stealth ──
        void UpdateRSlot()
        {
            // Warrior R — Great Sword Descent: cast timer or cooldown.
            if (abilityController.IsWarriorGuardMode) { UpdateWarriorRSlot(); return; }
            // Mage R — Meteor Judgment: cast state or cooldown.
            if (abilityController.IsMageRCMode)        { UpdateMageRSlot();    return; }
            // Archer R — Overdrive: active timer or cooldown.
            if (abilityController.IsArcherRCMode)      { UpdateArcherRSlot();  return; }
            // Rogue R — stealth system.
            if (!abilityController.IsRogueQChargeMode) { UpdateStdSlot(3);     return; }

            bool stealth = abilityController.RogueIsStealthed;
            int  stT     = Mathf.RoundToInt(abilityController.RogueStealthTimer * 10f);
            int  cdT     = Mathf.RoundToInt(abilityController.GetCooldownRemaining(AbilitySlot.R) * 10f);

            if (stealth == _lastRStealth && stT == _lastRStealthT && cdT == _lastRCdT) return;

            _lastRStealth  = stealth;
            _lastRStealthT = stT; _lastRCdT = cdT;

            if (stealth)
            {
                _status[3]   = "STEALTH\n" + Fmt(stT) + "s";
                _bgColors[3] = ColStealth;
            }
            else
            {
                _status[3]   = cdT <= 0 ? "READY" : Fmt(cdT) + "s";
                _bgColors[3] = cdT <= 0 ? ColReady : ColCooldown;
            }
        }

        // ── F slot: Archer Barrage gauge / Mage laser / Warrior Slash Barrage / Rogue combo ──
        void UpdateFSlot()
        {
            // Archer F — Barrage Gauge.
            if (abilityController.IsArcherRCMode) { UpdateArcherFSlot(); return; }
            // Mage F — Laser: show cast / active / cooldown.
            if (abilityController.IsMageRCMode) { UpdateMageFSlot(); return; }
            // Warrior F — Slash Barrage: show cast timer or cooldown.
            if (abilityController.IsWarriorGuardMode)
            {
                UpdateWarriorFSlot();
                return;
            }
            // Rogue F — 3-hit combo system.
            if (!abilityController.IsRogueQChargeMode) { UpdateStdSlot(4); return; }

            bool  combo   = abilityController.IsRogueFComboMode;
            int   fStep   = abilityController.RogueFComboStep;
            int   winT    = Mathf.RoundToInt(abilityController.RogueFComboWindowTimer * 10f);
            int   spdT    = Mathf.RoundToInt(abilityController.RogueF3SpeedTimer      * 10f);
            int   cdT     = Mathf.RoundToInt(abilityController.GetCooldownRemaining(AbilitySlot.F) * 10f);
            int   modeKey = combo ? 1 : (spdT > 0 ? 2 : 0);

            if (modeKey == _lastFModeKey && fStep == _lastFStep
                && winT == _lastFWinT && spdT == _lastFSpdT && cdT == _lastCdTenths[4]) return;

            _lastFModeKey = modeKey; _lastFStep = fStep;
            _lastFWinT = winT; _lastFSpdT = spdT; _lastCdTenths[4] = cdT;

            if (combo)
            {
                _status[4]   = "F" + (fStep + 1) + "\n" + Fmt(winT) + "s";
                _bgColors[4] = ColCombo;
            }
            else if (spdT > 0)
            {
                string cd = cdT > 0 ? Fmt(cdT) + "s" : "READY";
                _status[4]   = "SPD " + Fmt(spdT) + "s\n" + cd;
                _bgColors[4] = ColSpeedBuff;
            }
            else
            {
                _status[4]   = cdT <= 0 ? "READY" : Fmt(cdT) + "s";
                _bgColors[4] = cdT <= 0 ? ColReady : ColCooldown;
            }
        }

        void UpdateArcherFSlot()
        {
            bool  firing  = abilityController.ArcherIsFiring;
            float maxG    = abilityController.ArcherFMaxGauge;
            int   pct     = maxG > 0f
                ? Mathf.Clamp(Mathf.RoundToInt(abilityController.ArcherFGauge / maxG * 100f), 0, 100)
                : 0;

            if (firing == _lastArcherFiring && pct == _lastArcherFGaugePct) return;
            _lastArcherFiring    = firing;
            _lastArcherFGaugePct = pct;

            if (firing)
            {
                _status[4]   = "FIRE\n" + pct + "%";
                _bgColors[4] = ColArcherBarrage;
            }
            else
            {
                _status[4]   = pct + "%";
                _bgColors[4] = pct > 0 ? ColReady : ColCooldown;
            }
        }

        void UpdateArcherESlot()
        {
            bool shielded = abilityController.ArcherIsShielded;
            int  shieldT  = Mathf.RoundToInt(abilityController.ArcherShieldTimer * 10f);
            int  cdT      = Mathf.RoundToInt(abilityController.GetCooldownRemaining(AbilitySlot.E) * 10f);

            if (shielded == _lastArcherEShielded && shieldT == _lastArcherEShieldT && cdT == _lastCdTenths[2]) return;
            _lastArcherEShielded = shielded;
            _lastArcherEShieldT  = shieldT;
            _lastCdTenths[2]     = cdT;

            if (shielded)
            {
                _status[2]   = "SHLD\n" + Fmt(shieldT) + "s";
                _bgColors[2] = ColArcherShield;
            }
            else
            {
                _status[2]   = cdT <= 0 ? "READY" : Fmt(cdT) + "s";
                _bgColors[2] = cdT <= 0 ? ColReady : ColCooldown;
            }
        }

        void UpdateMageESlot()
        {
            bool casting = abilityController.MageIsCastingBlackhole;
            int  castT   = Mathf.RoundToInt(abilityController.MageBlackholeCastTimer * 10f);
            int  cdT     = Mathf.RoundToInt(abilityController.GetCooldownRemaining(AbilitySlot.E) * 10f);

            if (casting == _lastMageECasting && castT == _lastMageECastT && cdT == _lastCdTenths[2]) return;
            _lastMageECasting  = casting;
            _lastMageECastT    = castT;
            _lastCdTenths[2]   = cdT;

            if (casting)
            {
                _status[2]   = "CAST\n" + Fmt(castT) + "s";
                _bgColors[2] = ColBlackhole;
            }
            else
            {
                _status[2]   = cdT <= 0 ? "READY" : Fmt(cdT) + "s";
                _bgColors[2] = cdT <= 0 ? ColReady : ColCooldown;
            }
        }

        void UpdateMageFSlot()
        {
            bool casting = abilityController.MageIsCastingLaser;
            bool active  = abilityController.MageIsLaserActive;
            int  castT   = Mathf.RoundToInt(abilityController.MageLaserCastTimer * 10f);
            int  activeT = Mathf.RoundToInt(abilityController.MageLaserTimer     * 10f);
            int  cdT     = Mathf.RoundToInt(abilityController.GetCooldownRemaining(AbilitySlot.F) * 10f);

            if (casting == _lastMageFCasting && active == _lastMageFActive
                && castT == _lastMageFCastT && activeT == _lastMageFActiveT
                && cdT == _lastCdTenths[4]) return;

            _lastMageFCasting = casting;
            _lastMageFActive  = active;
            _lastMageFCastT   = castT;
            _lastMageFActiveT = activeT;
            _lastCdTenths[4]  = cdT;

            if (casting)
            {
                _status[4]   = "CAST\n" + Fmt(castT) + "s";
                _bgColors[4] = ColBlackhole;
            }
            else if (active)
            {
                _status[4]   = "LASER\n" + Fmt(activeT) + "s";
                _bgColors[4] = ColLaser;
            }
            else
            {
                _status[4]   = cdT <= 0 ? "READY" : Fmt(cdT) + "s";
                _bgColors[4] = cdT <= 0 ? ColReady : ColCooldown;
            }
        }

        void UpdateMageRSlot()
        {
            bool casting = abilityController.MageIsRCasting;
            int  cdT     = Mathf.RoundToInt(abilityController.GetCooldownRemaining(AbilitySlot.R) * 10f);

            if (casting == _lastMageRCasting && cdT == _lastCdTenths[3]) return;
            _lastMageRCasting = casting;
            _lastCdTenths[3]  = cdT;

            if (casting)
            {
                _status[3]   = "CAST";
                _bgColors[3] = ColMeteor;
            }
            else
            {
                _status[3]   = cdT <= 0 ? "READY" : Fmt(cdT) + "s";
                _bgColors[3] = cdT <= 0 ? ColReady : ColCooldown;
            }
        }

        void UpdateWarriorRSlot()
        {
            bool casting = abilityController.WarriorIsRCasting;
            int  castT   = Mathf.RoundToInt(abilityController.WarriorRCastTimer * 10f);
            int  cdT     = Mathf.RoundToInt(abilityController.GetCooldownRemaining(AbilitySlot.R) * 10f);

            if (casting == _lastRCasting && castT == _lastRCastT && cdT == _lastCdTenths[3]) return;
            _lastRCasting = casting; _lastRCastT = castT; _lastCdTenths[3] = cdT;

            if (casting)
            {
                _status[3]   = "ULT\n" + Fmt(castT) + "s";
                _bgColors[3] = ColUlt;
            }
            else
            {
                _status[3]   = cdT <= 0 ? "READY" : Fmt(cdT) + "s";
                _bgColors[3] = cdT <= 0 ? ColReady : ColCooldown;
            }
        }

        void UpdateWarriorFSlot()
        {
            bool casting = abilityController.WarriorIsFCasting;
            int  castT   = Mathf.RoundToInt(abilityController.WarriorFCastTimer * 10f);
            int  cdT     = Mathf.RoundToInt(abilityController.GetCooldownRemaining(AbilitySlot.F) * 10f);

            if (casting == _lastFCasting && castT == _lastFCastT && cdT == _lastCdTenths[4]) return;
            _lastFCasting = casting; _lastFCastT = castT; _lastCdTenths[4] = cdT;

            if (casting)
            {
                _status[4]   = "SLSH\n" + Fmt(castT) + "s";
                _bgColors[4] = ColFCast;
            }
            else
            {
                _status[4]   = cdT <= 0 ? "READY" : Fmt(cdT) + "s";
                _bgColors[4] = cdT <= 0 ? ColReady : ColCooldown;
            }
        }

        // ── Z slot: Mage Arcane Bolt Aiming/Charging or standard cooldown ──
        void UpdateZSlot()
        {
            if (abilityController.IsMageRCMode) { UpdateMageZSlot(); return; }
            UpdateStdSlot(5);
        }

        void UpdateMageZSlot()
        {
            bool aiming   = abilityController.MageIsZAiming;
            bool charging = abilityController.MageIsZCharging;
            int  chargeT  = Mathf.RoundToInt(abilityController.MageZChargeTimer * 10f);
            int  cdT      = Mathf.RoundToInt(abilityController.GetCooldownRemaining(AbilitySlot.Z) * 10f);

            if (aiming   == _lastMageZAiming
                && charging == _lastMageZCharging
                && chargeT  == _lastMageZChargeT
                && cdT      == _lastCdTenths[5]) return;

            _lastMageZAiming   = aiming;
            _lastMageZCharging = charging;
            _lastMageZChargeT  = chargeT;
            _lastCdTenths[5]   = cdT;

            if (aiming)
            {
                _status[5]   = "AIM";
                _bgColors[5] = ColAimBolt;
            }
            else if (charging)
            {
                _status[5]   = "CHG\n" + Fmt(chargeT) + "s";
                _bgColors[5] = ColChargeBolt;
            }
            else
            {
                _status[5]   = cdT <= 0 ? "READY" : Fmt(cdT) + "s";
                _bgColors[5] = cdT <= 0 ? ColReady : ColCooldown;
            }
        }

        void OnGUI()
        {
            if (s_keyStyle == null)
            {
                s_keyStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize  = 15,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter,
                    normal    = { textColor = Color.white },
                };
                s_valStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize  = 13,
                    alignment = TextAnchor.UpperCenter,
                    wordWrap  = false,
                    normal    = { textColor = Color.white },
                };
            }

            float totalW = _slots.Length * (SlotW + Gap) - Gap;
            float startX = Screen.width - totalW - 16f;
            float slotY  = Screen.height - SlotH - 16f;

            for (int i = 0; i < _slots.Length; i++)
            {
                float x = startX + i * (SlotW + Gap);

                // Background.
                GUI.color = _bgColors[i];
                GUI.DrawTexture(new Rect(x, slotY, SlotW, SlotH), Texture2D.whiteTexture);
                GUI.color = Color.white;

                // Key name (top row).
                GUI.Label(new Rect(x, slotY + 2f, SlotW, KeyH), _slotKeys[i], s_keyStyle);

                // Status (bottom area).
                GUI.Label(new Rect(x, slotY + KeyH, SlotW, SlotH - KeyH - 2f), _status[i], s_valStyle);
            }
        }

        // Formats integer tenths as "X.Y" — no heap allocation.
        static string Fmt(int tenths) => (tenths / 10) + "." + (tenths % 10);
    }
}
