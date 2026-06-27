namespace Game.Combat
{
    // Identifies the source of a self-applied move speed buff in HealthComponent.
    // Each source occupies its own slot in the buff array — buffs from different
    // sources multiply together independently.
    // Count must equal HealthComponent.BuffSourceCount (3).
    public enum SelfMoveSpeedSource
    {
        Generic          = 0, // SetSelfMoveSpeedMultiplier() legacy path; Warrior/Mage abilities
        ArcherRapidFire  = 1, // Archer RC buff
        ArcherOverdrive  = 2, // Archer R Overdrive buff
    }
}
