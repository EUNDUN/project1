using UnityEngine;

namespace Game.Commands
{
    // Snapshot of one frame's player intent.
    // Pure data — no MonoBehaviour, no allocation.
    public struct PlayerCommand
    {
        public Vector2 MoveInput;
        public Vector2 LookInput;
        public bool JumpPressed;
        public bool CrouchHeld;
        public bool AttackPressed;
        public bool AttackHeld;
        public bool AttackReleased;
        public bool RightClickPressed;
        public bool SkillQPressed;
        public bool SkillEPressed;
        public bool SkillRPressed;
        public bool SkillFPressed;
        public bool SkillFHeld;
        public bool SkillFReleased;
        public bool SkillZPressed;
    }
}