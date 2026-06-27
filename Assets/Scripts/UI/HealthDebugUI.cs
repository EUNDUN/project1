using UnityEngine;
using Game.Combat;

namespace Game.UI
{
    // World-space health bar drawn above each character via OnGUI.
    // HP ratio is updated through HealthComponent.OnHealthChanged — not polled every frame.
    // Bar color is cached once from the character's team; camera reference is cached at Start.
    public class HealthDebugUI : MonoBehaviour
    {
        public HealthComponent target;

        private float  _currentHp;
        private float  _maxHp;
        private Color  _fillColor;
        private Color  _emptyColor;
        private UnityEngine.Camera _mainCam;

        private static readonly Color s_blueHp  = new Color(0.25f, 0.50f, 1.00f);
        private static readonly Color s_redHp   = new Color(1.00f, 0.25f, 0.25f);
        private static readonly Color s_empty   = new Color(0.15f, 0.15f, 0.15f);
        private static readonly Color s_outline  = new Color(0.00f, 0.00f, 0.00f, 0.85f);

        void Start()
        {
            if (target == null) { enabled = false; return; }
            _mainCam   = UnityEngine.Camera.main;
            _currentHp = target.CurrentHp;
            _maxHp     = target.MaxHp;
            _fillColor  = target.Team == Team.Blue ? s_blueHp : s_redHp;
            _emptyColor = s_empty;
            target.OnHealthChanged += HandleHealthChanged;
        }

        void OnDestroy()
        {
            if (target != null)
                target.OnHealthChanged -= HandleHealthChanged;
        }

        void HandleHealthChanged(float current, float max)
        {
            _currentHp = current;
            _maxHp     = max;
        }

        void OnGUI()
        {
            if (_mainCam == null || target == null) return;

            // Project world position to screen; skip if behind camera.
            Vector3 screen = _mainCam.WorldToScreenPoint(target.transform.position + Vector3.up * 2.8f);
            if (screen.z < 0f) return;

            const float barW = 90f;
            const float barH = 12f;
            const float pad  = 1f;

            float x = screen.x - barW * 0.5f;
            float y = Screen.height - screen.y - barH * 0.5f;

            float ratio = (_maxHp > 0f) ? Mathf.Clamp01(_currentHp / _maxHp) : 0f;

            // Black outline
            GUI.color = s_outline;
            GUI.DrawTexture(new Rect(x - pad, y - pad, barW + pad * 2f, barH + pad * 2f),
                            Texture2D.whiteTexture);

            // HP fill (team color)
            GUI.color = _fillColor;
            if (ratio > 0f)
                GUI.DrawTexture(new Rect(x, y, barW * ratio, barH), Texture2D.whiteTexture);

            // Empty portion
            GUI.color = _emptyColor;
            if (ratio < 1f)
                GUI.DrawTexture(new Rect(x + barW * ratio, y, barW * (1f - ratio), barH),
                                Texture2D.whiteTexture);

            GUI.color = Color.white;
        }
    }
}
