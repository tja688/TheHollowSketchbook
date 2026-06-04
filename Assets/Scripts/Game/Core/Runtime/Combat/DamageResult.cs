namespace Game.Core.Combat
{
    public sealed class DamageResult
    {
        public int OriginalAmount { get; set; }
        public int ModifiedAmount { get; set; }
        public int BlockedAmount { get; set; }
        public int HpLoss { get; set; }
        public bool Killed { get; set; }
    }
}
