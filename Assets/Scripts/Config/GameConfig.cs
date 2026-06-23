using UnityEngine;

namespace Game.Config
{
    [CreateAssetMenu(fileName = "GameConfig", menuName = "Game/GameConfig")]
    public class GameConfig : ScriptableObject
    {
        [Header("Movement")]
        public float moveSpeed = 5f;
        public float crouchSpeed = 2.5f;
        public float jumpHeight = 1.5f;
        public float gravity = -20f;

        [Header("Mouse Look")]
        public float mouseSensitivity = 2f;
        public float maxPitchAngle = 85f;

        [Header("Crouch")]
        public float standHeight = 2f;
        public float crouchHeight = 1f;
        public float crouchTransitionSpeed = 10f;

        [Header("Camera Height")]
        public float standCameraLocalY = 1.6f;   // eye level: near top of 2-unit capsule
        public float crouchCameraLocalY = 0.8f;  // eye level: near top of 1-unit capsule

        [Header("Combat")]
        public float baseMaxHp         = 100f;
        public float baseArmor         = 0f;
        public float debugAttackDamage = 25f;
    }
}
