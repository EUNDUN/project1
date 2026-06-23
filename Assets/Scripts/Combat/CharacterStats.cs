using UnityEngine;
using Game.Config;

namespace Game.Combat
{
    // Resolved stat snapshot for one character.
    // Future: base values from GameConfig + modifiers from abilities / buffs.
    public struct CharacterStats
    {
        public float MaxHp;
        public float Armor;  // flat damage reduction per hit

        public float CalculateFinalDamage(float baseDamage)
            => Mathf.Max(0f, baseDamage - Armor);

        public static CharacterStats FromConfig(GameConfig cfg)
            => new CharacterStats { MaxHp = cfg.baseMaxHp, Armor = cfg.baseArmor };
    }
}