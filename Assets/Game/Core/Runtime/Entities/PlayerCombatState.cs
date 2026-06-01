using Game.Core.Cards;

namespace Game.Core.Entities
{
    public sealed class PlayerCombatState
    {
        public PlayerCombatState(Player player, int maxEnergy)
        {
            Player = player;
            MaxEnergy = maxEnergy;
            Energy = maxEnergy;
            Hand = new CardPile(PileType.Hand);
            DrawPile = new CardPile(PileType.Draw);
            DiscardPile = new CardPile(PileType.Discard);
            ExhaustPile = new CardPile(PileType.Exhaust);
            PlayPile = new CardPile(PileType.Play);
        }

        public Player Player { get; }
        public int Energy { get; private set; }
        public int MaxEnergy { get; private set; }

        public CardPile Hand { get; }
        public CardPile DrawPile { get; }
        public CardPile DiscardPile { get; }
        public CardPile ExhaustPile { get; }
        public CardPile PlayPile { get; }

        public void ResetEnergy(int maxEnergy)
        {
            MaxEnergy = maxEnergy;
            Energy = maxEnergy;
        }

        public void SpendEnergy(int amount)
        {
            Energy -= amount;
            if (Energy < 0)
            {
                Energy = 0;
            }
        }

        public void GainEnergy(int amount)
        {
            Energy += amount;
        }

        public void ClearPiles()
        {
            Hand.Clear();
            DrawPile.Clear();
            DiscardPile.Clear();
            ExhaustPile.Clear();
            PlayPile.Clear();
        }
    }
}
