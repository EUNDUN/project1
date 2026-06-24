using UnityEngine;
using Game.Combat;
using Game.Config;

// DEBUG ONLY — tests the death/respawn pipeline. Remove before shipping.
namespace Game
{
    public class DebugDamageInput : MonoBehaviour
    {
        public GameConfig config;
        public HealthComponent targetHealth;

        void Update()
        {
            if (!UnityEngine.Input.GetKeyDown(KeyCode.K)) return;

            // Team.Neutral bypasses the friendly-fire check, so the damage always lands.
            var info = new DamageInfo
            {
                BaseDamage = config.debugLethalDamage,
                SourceTeam = Team.Neutral,
                SourceId   = string.Empty
            };
            targetHealth.TakeDamage(info);
            UnityEngine.Debug.Log("[DebugDamage] K pressed — lethal damage applied to player.");
        }
    }
}