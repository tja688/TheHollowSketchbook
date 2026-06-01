using Game.Core.Cards;
using Game.Core.Entities;

namespace Game.Core.Combat
{
    public readonly struct DamageInfo
    {
        public DamageInfo(Creature source, Creature target, int amount, DamageType type, CardModel cardSource = null)
        {
            Source = source;
            Target = target;
            Amount = amount;
            Type = type;
            CardSource = cardSource;
        }

        public Creature Source { get; }
        public Creature Target { get; }
        public int Amount { get; }
        public DamageType Type { get; }
        public CardModel CardSource { get; }

        public bool IsAttack
        {
            get { return Type == DamageType.Attack; }
        }
    }
}
