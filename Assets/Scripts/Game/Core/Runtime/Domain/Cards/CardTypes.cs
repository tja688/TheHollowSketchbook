using System;

namespace Game.Core.Domain.Cards
{
    public readonly struct CardInstanceId : IEquatable<CardInstanceId>
    {
        public CardInstanceId(uint value)
        {
            Value = value;
        }

        public uint Value { get; }

        public bool IsEmpty
        {
            get { return Value == 0u; }
        }

        public bool Equals(CardInstanceId other)
        {
            return Value == other.Value;
        }

        public override bool Equals(object obj)
        {
            return obj is CardInstanceId other && Equals(other);
        }

        public override int GetHashCode()
        {
            return (int)Value;
        }

        public override string ToString()
        {
            return IsEmpty ? "None" : "#" + Value;
        }

        public static bool operator ==(CardInstanceId left, CardInstanceId right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(CardInstanceId left, CardInstanceId right)
        {
            return !left.Equals(right);
        }
    }

    public enum CardType
    {
        Player,
        Monster,
        Trap,
        Item,
        Gold,
        StatUpgrade,
        Chest,
        Food,
        Mentor,
        ShopProduct,
        RouteChoice,
        Special
    }

    public enum CardZone
    {
        None,
        DungeonDeck,
        Grid,
        PlayerInventory,
        RelicInventory,
        ChoicePool,
        RewardQueue,
        Removed
    }
}
