using Game.Core.Domain.Cards;
using Game.Core.Domain.Grid;

namespace Game.Core.Domain.Events
{
    public sealed class DomainEvent
    {
        public DomainEvent(DomainEventType eventType)
        {
            EventType = eventType;
        }

        public ulong EventId { get; internal set; }
        public uint ActionId { get; internal set; }
        public int SequenceIndex { get; internal set; }
        public DomainEventType EventType { get; }

        public CardInstanceId CardId { get; set; }
        public CardInstanceId SourceCardId { get; set; }
        public CardInstanceId TargetCardId { get; set; }
        public GridCoord? FromCoord { get; set; }
        public GridCoord? ToCoord { get; set; }
        public int Amount { get; set; }
        public int SecondaryAmount { get; set; }
        public string Reason { get; set; }
    }
}
