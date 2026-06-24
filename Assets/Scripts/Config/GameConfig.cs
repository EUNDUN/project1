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

        [Header("Combat — Base Stats")]
        public float baseMaxHp = 100f;
        public float baseArmor = 0f;

        [Header("Combat — Basic Attack")]
        public float     attackDamage     = 30f;
        public float     attackRange      = 20f;
        public float     attackCooldown   = 0.5f;
        public LayerMask attackLayerMask  = -1;   // -1 = Everything; restrict in Inspector to avoid trigger/UI hits

        [Header("Respawn")]
        public float respawnDelay             = 3f;
        public float invulnerabilityDuration  = 2f;

        [Header("Bot")]
        public float botDetectRange    = 12f;
        public float botMoveSpeed      = 2f;
        public float botAttackRange    = 1.5f;
        public float botAttackDamage   = 20f;
        public float botAttackCooldown = 1.5f;

        [Header("3v3 Spawn")]
        public int   blueBotCount  = 2;     // blue bots (player counts as the 3rd blue member)
        public int   redBotCount   = 3;     // red bots (no player on red side)
        public float blueSpawnZ    = -5f;   // blue team spawn line (z axis)
        public float redSpawnZ     =  5f;   // red team spawn line (z axis)
        public float spawnSpreadX  =  3f;   // horizontal spacing between spawn slots

        [Header("UI")]
        public float damageFlashDuration = 0.25f;

        [Header("Debug — Remove Before Shipping")]
        public bool  debugCombatLogs   = false;
        public float debugLethalDamage = 999f;
    }
}
