using System;
using UnityEngine;
using Game.Combat;

namespace Game.UI
{
    // Prototype class selection screen shown at game start before any game entities exist.
    // Fires OnClassSelected exactly once, then deactivates itself.
    // Replace this component with a proper lobby/matchmaking UI when the time comes.
    // The selection boundary is a single Action<CombatClass> — easy to rewire.
    public class ClassSelectionUI : MonoBehaviour
    {
        // GameBootstrap wires this before the first frame.
        public Action<CombatClass> OnClassSelected;

        private bool      _selected;
        private GUIStyle  _titleStyle;  // lazy-initialized inside first OnGUI call

        private static readonly string[]      ClassNames = { "Warrior", "Rogue", "Archer", "Mage" };
        private static readonly CombatClass[] Classes    =
        {
            CombatClass.Warrior, CombatClass.Rogue, CombatClass.Archer, CombatClass.Mage,
        };

        void Awake()
        {
            // Cursor must be visible while the selection screen is active.
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible   = true;
        }

        void OnGUI()
        {
            if (_selected) return;

            // GUIStyle can only be constructed inside OnGUI. Initialised once, never reallocated.
            if (_titleStyle == null)
            {
                _titleStyle = new GUIStyle(GUI.skin.label)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontSize  = 24,
                };
            }

            // Title row
            GUI.Label(
                new Rect(0f, Screen.height * 0.5f - 72f, Screen.width, 36f),
                "Select Class",
                _titleStyle);

            // Button row — 4 buttons centred horizontally
            const float btnW  = 160f;
            const float btnH  =  50f;
            const float gap   =  12f;
            const int   count =   4;
            float totalW = count * btnW + (count - 1) * gap;
            float startX = (Screen.width  - totalW) * 0.5f;
            float y      =  Screen.height * 0.5f - btnH * 0.5f;

            for (int i = 0; i < count; i++)
            {
                if (GUI.Button(new Rect(startX + i * (btnW + gap), y, btnW, btnH), ClassNames[i]))
                    Select(Classes[i]);
            }
        }

        private void Select(CombatClass cls)
        {
            if (_selected) return;  // guard against double-click in same frame
            _selected = true;
            gameObject.SetActive(false); // stop rendering immediately
            OnClassSelected?.Invoke(cls);
        }
    }
}