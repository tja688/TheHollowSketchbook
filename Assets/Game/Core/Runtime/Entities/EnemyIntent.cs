namespace Game.Core.Entities
{
    public sealed class EnemyIntent
    {
        public EnemyIntentType Type { get; set; }
        public int Damage { get; set; }
        public int Hits { get; set; } = 1;
        public int Block { get; set; }
        public string Description { get; set; }
    }
}
