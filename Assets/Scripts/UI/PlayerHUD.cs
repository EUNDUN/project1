using UnityEngine;
using Game.Combat;
using Game.Config;

namespace Game.UI
{
    // Displays player HP bar (colour-coded), damage flash, and backstab feedback.
    // All string building is event-driven; OnGUI draws pre-built strings only.
    public class PlayerHUD : MonoBehaviour
    {
        public GameConfig            config;
        public HealthComponent       playerHealth;
        public BasicAttackController attackController;

        private float  _currentHp;
        private float  _maxHp;
        private float  _flashTimer;
        private bool   _isDead;
        private string _hpLabel = "HP  100 / 100";
        private Color  _hpColor = new Color(0.25f, 0.90f, 0.30f);

        private string _backstabLabel = "";
        private float  _backstabTimer = 0f;
        private const float BackstabDuration = 1.5f;

        private static GUIStyle s_hpStyle;
        private static GUIStyle s_deadStyle;
        private static GUIStyle s_backstabStyle;

        void Start()
        {
            if (config == null || playerHealth == null)
            {
                Debug.LogError("[PlayerHUD] Missing required reference.", this);
                enabled = false;
                return;
            }
            _currentHp = playerHealth.CurrentHp;
            _maxHp     = playerHealth.MaxHp;
            RefreshHpLabel(_currentHp, _maxHp);

            playerHealth.OnHealthChanged += HandleHealthChanged;
            playerHealth.OnDeath         += HandleDeath;

            if (attackController != null)
                attackController.OnBackstabLanded += HandleBackstabLanded;
        }

        void OnDestroy()
        {
            if (playerHealth != null)
            {
                playerHealth.OnHealthChanged -= HandleHealthChanged;
                playerHealth.OnDeath         -= HandleDeath;
            }
            if (attackController != null)
                attackController.OnBackstabLanded -= HandleBackstabLanded;
        }

        void HandleHealthChanged(float current, float max)
        {
            bool tookDamage = !_isDead && current < _currentHp;
            if (_isDead && current > 0f) _isDead = false; // respawned
            _currentHp = current;
            _maxHp     = max;
            if (!_isDead) RefreshHpLabel(current, max);
            if (tookDamage) _flashTimer = config.damageFlashDuration;
        }

        void HandleDeath(HealthComponent _)
        {
            _isDead  = true;
            _hpLabel = "DEAD";
            _hpColor = new Color(1f, 0.15f, 0.15f);
        }

        void HandleBackstabLanded(float bonus)
        {
            _backstabLabel = "BACKSTAB +" + Mathf.RoundToInt(bonus);
            _backstabTimer = BackstabDuration;
        }

        void Update()
        {
            if (_flashTimer    > 0f) _flashTimer    = Mathf.Max(0f, _flashTimer    - Time.deltaTime);
            if (_backstabTimer > 0f) _backstabTimer = Mathf.Max(0f, _backstabTimer - Time.deltaTime);
        }

        void OnGUI()
        {
            EnsureStyles();

            // Full-screen damage flash.
            if (_flashTimer > 0f)
            {
                float a = (_flashTimer / config.damageFlashDuration) * 0.35f;
                GUI.color = new Color(1f, 0f, 0f, a);
                GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
                GUI.color = Color.white;
            }

            // HP bar — centered at bottom.
            const float barW = 340f;
            const float barH = 32f;
            float barX = (Screen.width  - barW) * 0.5f;
            float barY =  Screen.height - 54f;

            if (_isDead)
            {
                GUI.color = new Color(0.28f, 0f, 0f, 0.88f);
                GUI.DrawTexture(new Rect(barX, barY, barW, barH), Texture2D.whiteTexture);
                GUI.color = Color.white;
                GUI.Label(new Rect(barX, barY, barW, barH), _hpLabel, s_deadStyle);
            }
            else
            {
                float ratio = _maxHp > 0f ? Mathf.Clamp01(_currentHp / _maxHp) : 0f;

                // Dark background track.
                GUI.color = new Color(0.10f, 0.10f, 0.10f, 0.82f);
                GUI.DrawTexture(new Rect(barX, barY, barW, barH), Texture2D.whiteTexture);
                // Coloured HP fill.
                GUI.color = new Color(_hpColor.r, _hpColor.g, _hpColor.b, 0.88f);
                if (ratio > 0f)
                    GUI.DrawTexture(new Rect(barX, barY, barW * ratio, barH), Texture2D.whiteTexture);
                // Text overlay.
                GUI.color = Color.white;
                GUI.Label(new Rect(barX, barY, barW, barH), _hpLabel, s_hpStyle);
            }

            // Backstab notification — screen centre, fades in final 0.5 s.
            if (_backstabTimer > 0f)
            {
                float a = Mathf.Clamp01(_backstabTimer / 0.5f);
                GUI.color = new Color(1f, 0.85f, 0f, a);
                GUI.Label(new Rect(Screen.width * 0.5f - 150f, Screen.height * 0.5f - 80f, 300f, 40f),
                          _backstabLabel, s_backstabStyle);
                GUI.color = Color.white;
            }
        }

        // Called from event; string built once and reused until next change.
        void RefreshHpLabel(float current, float max)
        {
            int cur = Mathf.RoundToInt(current);
            int mx  = Mathf.RoundToInt(max);
            _hpLabel = "  HP   " + cur + "  /  " + mx;
            _hpColor = HpBarColor(current, max);
        }

        static Color HpBarColor(float current, float max)
        {
            if (max <= 0f) return Color.red;
            float r = current / max;
            if (r <= 0.25f) return new Color(1.00f, 0.18f, 0.12f); // red
            if (r <= 0.50f) return new Color(1.00f, 0.75f, 0.08f); // yellow
            return                   new Color(0.25f, 0.90f, 0.30f); // green
        }

        static void EnsureStyles()
        {
            if (s_hpStyle != null) return;
            s_hpStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize  = 17,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft,
            };
            s_deadStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize  = 18,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal    = { textColor = new Color(1f, 0.30f, 0.30f) },
            };
            s_backstabStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize  = 22,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
            };
        }
    }
}
