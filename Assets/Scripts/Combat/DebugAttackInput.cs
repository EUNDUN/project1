using UnityEngine;
using Game.Config;

namespace Game.Combat
{
    // Temporary test tool: press F to deal damage to whatever the camera is aimed at.
    // Goes through the standard DamageInfo -> HealthComponent path — not a shortcut.
    // Replace with a proper AttackCommand flow in the attack system stage.
    public class DebugAttackInput : MonoBehaviour
    {
        public GameConfig config;
        public Transform cameraTransform;
        public HealthComponent attackerHealth;

        private const float Range = 20f;

        void Start()
        {
            if (config == null || cameraTransform == null)
            {
                Debug.LogError("[DebugAttackInput] Missing required reference. Disabling.", this);
                enabled = false;
            }
        }

        void Update()
        {
            if (!UnityEngine.Input.GetKeyDown(KeyCode.F)) return;

            if (!Physics.Raycast(cameraTransform.position, cameraTransform.forward, out RaycastHit hit, Range))
                return;

            HealthComponent target = hit.collider.GetComponentInParent<HealthComponent>();
            if (target == null || target == attackerHealth) return;

            var info = new DamageInfo
            {
                BaseDamage = config.debugAttackDamage,
                SourceTeam = attackerHealth != null ? attackerHealth.Team : Team.Neutral,
                SourceId   = string.Empty
            };
            target.TakeDamage(info);
            Debug.Log($"[DebugAttack] Hit {target.name}  {target.CurrentHp:F0}/{target.MaxHp:F0} HP");
        }
    }
}