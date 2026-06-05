using Game.Core.Domain.Cards;

namespace Game.Core.Domain.Combat
{
    public sealed class DamageContext
    {
        public DamageContext(DamageInfo info, CardInstance sourceCard, CardInstance targetCard)
        {
            Info = info;
            SourceCard = sourceCard;
            TargetCard = targetCard;
        }

        public DamageInfo Info { get; }
        public CardInstance SourceCard { get; }
        public CardInstance TargetCard { get; }
    }
}
