using System.Collections.Generic;
using Game.Core.Domain.Cards;
using Game.Core.Domain.Events;

namespace Game.Core.Domain.Combat
{
    public sealed class DamageContext
    {
        public DamageContext(DamageInfo info, CardInstance sourceCard, CardInstance targetCard)
            : this(info, sourceCard, targetCard, null, null)
        {
        }

        public DamageContext(DamageInfo info, CardInstance sourceCard, CardInstance targetCard, DomainActionContext domain)
            : this(info, sourceCard, targetCard, domain, null)
        {
        }

        public DamageContext(DamageInfo info, CardInstance sourceCard, CardInstance targetCard, DomainActionContext domain, ICollection<DomainEvent> events)
        {
            Info = info;
            SourceCard = sourceCard;
            TargetCard = targetCard;
            Domain = domain;
            Events = events;
        }

        public DamageInfo Info { get; }
        public CardInstance SourceCard { get; }
        public CardInstance TargetCard { get; }
        public DomainActionContext Domain { get; }
        public ICollection<DomainEvent> Events { get; }
    }
}
