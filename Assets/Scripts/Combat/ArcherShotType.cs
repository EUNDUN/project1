namespace Game.Combat
{
    // Identifies which enhanced shot type is pending for the Archer's next basic attack.
    // Basic   — standard single-target projectile (default, no Q loaded).
    // Shock   — small AoE + stun.
    // Fire    — medium AoE + bonus damage.
    // Ice     — large AoE + slow.
    // Barrage       — F skill rapid-fire shot: damage + slow, no AoE, no Q pending consume.
    // OverdriveBasic — R Overdrive-upgraded Basic: larger projectile + AoE explosion on contact.
    public enum ArcherShotType { Basic, Shock, Fire, Ice, Barrage, OverdriveBasic }
}
