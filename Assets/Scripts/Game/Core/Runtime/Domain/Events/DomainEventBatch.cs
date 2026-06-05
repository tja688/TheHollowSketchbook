using System;
using System.Collections.Generic;
using Game.Core.Domain.Interaction;

namespace Game.Core.Domain.Events
{
    public sealed class DomainEventBatch
    {
        private readonly List<DomainEvent> _events = new List<DomainEvent>();

        public DomainEventBatch(uint actionId, PlayerIntent sourceIntent)
        {
            ActionId = actionId;
            SourceIntent = sourceIntent;
        }

        public uint ActionId { get; }
        public PlayerIntent SourceIntent { get; }

        public IReadOnlyList<DomainEvent> Events
        {
            get { return _events; }
        }

        public bool RequiresPresentationGate { get; set; }

        public void Add(DomainEvent domainEvent)
        {
            if (domainEvent == null)
            {
                throw new ArgumentNullException(nameof(domainEvent));
            }

            domainEvent.ActionId = ActionId;
            domainEvent.SequenceIndex = _events.Count;
            _events.Add(domainEvent);
        }

        public void AddRange(IEnumerable<DomainEvent> events)
        {
            foreach (DomainEvent domainEvent in events)
            {
                Add(domainEvent);
            }
        }
    }
}
