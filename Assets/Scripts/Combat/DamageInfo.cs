namespace Game.Combat
{
    // Pure data passed through the damage pipeline.
    // Every damage source (player input, bot, server) constructs this struct and calls TakeDamage.
    public struct DamageInfo
    {
        public float BaseDamage;
        public Team  SourceTeam;
        public string SourceId;
    }
}