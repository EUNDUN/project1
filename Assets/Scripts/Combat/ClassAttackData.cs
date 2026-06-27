using System;

namespace Game.Combat
{
    // Base attack parameters for one class.
    // Cached once at Start() by controllers — never read per-frame from config.
    [Serializable]
    public struct ClassAttackData
    {
        public float Damage;
        public float Range;
        public float Cooldown;
    }
}