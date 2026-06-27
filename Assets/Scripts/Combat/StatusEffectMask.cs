namespace Game.Combat
{
    // Bitmask of temporary status effects applied to characters.
    // Extend with new bit flags as more effects are added — one flag per effect type.
    [System.Flags]
    public enum StatusEffectMask
    {
        None      = 0,
        Stunned   = 1 << 0,
        Stealthed = 1 << 1, // untargetable by AI; visual cue only — does not prevent damage
        // Reserved for future effects:
        // Slowed   = 1 << 2,
        // Silenced = 1 << 3,
    }
}