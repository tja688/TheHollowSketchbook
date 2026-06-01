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

        public event System.Action<int, int> EnergyChanged;

        public void ResetEnergy(int maxEnergy)
        {
            int oldValue = Energy;
            MaxEnergy = maxEnergy;
            Energy = maxEnergy;
            if (oldValue != Energy)
            {
                EnergyChanged?.Invoke(oldValue, Energy);
            }
        }

        public void SpendEnergy(int amount)
        {
            int oldValue = Energy;
            Energy -= amount;
            if (Energy < 0)
            {
                Energy = 0;
            }
            if (oldValue != Energy)
            {
                EnergyChanged?.Invoke(oldValue, Energy);
            }
        }

        public void GainEnergy(int amount)
        {
            int oldValue = Energy;
            Energy += amount;
            if (oldValue != Energy)
            {
                EnergyChanged?.Invoke(oldValue, Energy);
            }
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
