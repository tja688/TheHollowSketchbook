using Game.Core.Cards;

namespace Game.Core.Combat
{
    public sealed class CardPlay
    {
        public CardModel Card { get; set; }
        public PlayTarget Target { get; set; }
        public PileType ResultPile { get; set; } = PileType.Discard;
        public ResourceInfo Resources { get; set; }
        public bool IsAutoPlay { get; set; }
        public int PlayIndex { get; set; }
        public int PlayCount { get; set; }
    }
}
