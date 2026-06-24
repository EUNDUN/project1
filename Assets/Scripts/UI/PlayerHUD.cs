using UnityEngine;
using Game.Combat;
using Game.Config;

namespace Game.UI
{
    // Shows the local player's current HP and a brief damage flash.
    // Driven entirely by HealthComponent events — no per-frame polling.
    // _hpText is rebuilt only on change, so OnGUI() has no string allocation.
    public class PlayerHUD : MonoBehaviour
    {
        public GameConfig        config;
        public HealthComponent   playerHealth;

        private float  _currentHp;
        private float  _maxHp;
        private float  _flashTimer;
        private string _hpText = "";

        void Start()
        {
            if (config == null || playerHealth == null)
            {
                Debug.LogError("[PlayerHUD] Missing required reference. Disabling.", this);
                enabled = false;
                return;
            }

            _currentHp = playerHealth.CurrentHp;
            _maxHp     = playerHealth.MaxHp;
            _hpText    = BuildHpText(_currentHp, _maxHp);

            playerHealth.OnHealthChanged += HandleHealthChanged;
        }

        void OnDestroy()
        {
            if (playerHealth != null)
                playerHealth.OnHealthChanged -= HandleHealthChanged;
        }

        void HandleHealthChanged(float current, float max)
        {
            bool tookDamage = current < _currentHp;
            _currentHp = current;
            _maxHp     = max;
            _hpText    = BuildHpText(current, max);
            if (tookDamage)
                _flashTimer = config.damageFlashDuration;
        }

        void Update()
        {
            if (_flashTimer > 0f)
                _flashTimer = Mathf.Max(0f, _flashTimer - Time.deltaTime);
        }

        void OnGUI()
        {
            // Damage flash: full-screen red tint, linear fade over damageFlashDuration.
            // Drawn first so the HP label renders on top.
            if (_flashTimer > 0f)
            {
                float alpha = (_flashTimer / config.damageFlashDuration) * 0.35f;
                GUI.color = new Color(1f, 0f, 0f, alpha);
                GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height),
                                Texture2D.whiteTexture);
                GUI.color = Color.white;
            }

            // HP label — bottom-left corner.
            GUI.Label(new Rect(20f, Screen.height - 50f, 200f, 30f), _hpText);
        }

        // Called only on change — string allocation kept out of OnGUI / Update.
        static string BuildHpText(float current, float max) =>
            $"HP  {current:F0} / {max:F0}";
    }
}