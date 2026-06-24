using UnityEngine;
using Game.Combat;

namespace Game.UI
{
    // Temporary debug UI: draws HP above a world-space target via OnGUI.
    // State is updated through HealthComponent.OnHealthChanged — not polled every frame.
    public class HealthDebugUI : MonoBehaviour
    {
        public HealthComponent target;

        private float  _currentHp;
        private float  _maxHp;
        private string _hpText = "";
        private UnityEngine.Camera _mainCam;

        void Start()
        {
            if (target == null) { enabled = false; return; }
            _mainCam   = UnityEngine.Camera.main;
            _currentHp = target.CurrentHp;
            _maxHp     = target.MaxHp;
            _hpText    = BuildHpText(_currentHp, _maxHp);
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
            _hpText    = BuildHpText(current, max);
        }

        void OnGUI()
        {
            if (_mainCam == null || target == null) return;

            Vector3 screen = _mainCam.WorldToScreenPoint(target.transform.position + Vector3.up * 2.5f);
            if (screen.z < 0f) return;

            GUI.Label(new Rect(screen.x - 50f, Screen.height - screen.y - 15f, 100f, 25f), _hpText);
        }

        static string BuildHpText(float current, float max) => $"HP {current:F0} / {max:F0}";
    }
}