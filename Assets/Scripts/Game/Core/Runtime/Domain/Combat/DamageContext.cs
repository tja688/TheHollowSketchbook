using Game.Core.Domain.Cards;

namespace Game.Core.Domain.Combat
{
    public sealed class DamageContext
    {
        public DamageContext(DamageInfo info, CardInstance sourceCard, CardInstance targetCard)
            : this(info, sourceCard, targetCard, null)
        {
        }

        public DamageContext(DamageInfo info, CardInstance sourceCard, CardInstance targetCard, DomainActionContext domain)
        {
            Info = info;
            SourceCard = sourceCard;
            TargetCard = targetCard;
            Domain = domain;
        }

        public DamageInfo Info { get; }
        public CardInstance SourceCard { get; }
        public CardInstance TargetCard { get; }
        public DomainActionContext Domain { get; }
    }
}
