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
    }
}