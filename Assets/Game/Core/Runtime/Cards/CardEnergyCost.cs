using System;

namespace Game.Core.Cards
{
    public readonly struct CardEnergyCost : IEquatable<CardEnergyCost>
    {
        public int BaseCost { get; }
        public bool CostsX { get; }

        private CardEnergyCost(int baseCost, bool costsX)
        {
            BaseCost = baseCost;
            CostsX = costsX;
        }

        public static CardEnergyCost Fixed(int amount)
        {
            return new CardEnergyCost(amount, false);
        }

        public static CardEnergyCost Free()
        {
            return new CardEnergyCost(0, false);
        }

        public static CardEnergyCost X()
        {
            return new CardEnergyCost(0, true);
        }

        public int GetSpendAmount(int availableEnergy)
        {
            return CostsX ? Math.Max(0, availableEnergy) : Math.Max(0, BaseCost);
        }

        public bool Equals(CardEnergyCost other)
        {
            return BaseCost == other.BaseCost && CostsX == other.CostsX;
        }

        public override bool Equals(object obj)
        {
            return obj is CardEnergyCost other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (BaseCost * 397) ^ CostsX.GetHashCode();
            }
        }

        public override string ToString()
        {
            return CostsX ? "X" : BaseCost.ToString();
        }
    }
}
