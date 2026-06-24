using UnityEngine;

namespace Game.Combat
{
    // Pure physics query — no state, no MonoBehaviour.
    // Callable identically from player, bot, or server authority.
    public static class AttackResolver
    {
        // Returns the first HealthComponent in the ray path that belongs to an enemy team.
        // layerMask and QueryTriggerInteraction.Ignore prevent hits on triggers, UI, and non-combat objects.
        public static bool TryHit(Vector3 origin, Vector3 direction, float range,
                                   Team attackerTeam, LayerMask layerMask,
                                   out HealthComponent target)
        {
            target = null;
            if (!Physics.Raycast(origin, direction, out RaycastHit hit, range,
                                 layerMask, QueryTriggerInteraction.Ignore))
                return false;

            HealthComponent hc = hit.collider.GetComponentInParent<HealthComponent>();
            if (hc == null) return false;
            if (hc.Team == attackerTeam && attackerTeam != Team.Neutral) return false;

            target = hc;
            return true;
        }
    }
}