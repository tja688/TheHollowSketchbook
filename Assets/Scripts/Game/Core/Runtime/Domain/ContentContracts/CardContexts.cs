using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Game.Core.Domain.Cards;
using Game.Core.Domain.Combat;
using Game.Core.Domain.Events;
using Game.Core.Domain.Grid;
using Game.Core.Domain.Interaction;
using Game.Core.Domain.Inventory;

namespace Game.Core.Domain.ContentContracts
{
    public abstract class DomainCallbackContext
    {
        protected DomainCallbackContext(DomainActionContext domain, CardInstance card, ICollection<DomainEvent> events)
        {
            Domain = domain ?? throw new ArgumentNullException(nameof(domain));
            Card = card ?? throw new ArgumentNullException(nameof(card));
            Events = events ?? throw new ArgumentNullException(nameof(events));
        }

        public DomainActionContext Domain { get; }
        public CardInstance Card { get; }
        public ICollection<DomainEvent> Events { get; }

        public CardModel ResolveCardModel(CardInstance card)
        {
            return Domain.ResolveCardModel(card);
        }

        public void AddEvent(DomainEvent domainEvent)
        {
            if (domainEvent != null)
            {
                Events.Add(domainEvent);
            }
        }

        public void AddEvents(IEnumerable<DomainEvent> events)
        {
            if (events == null)
            {
                return;
            }

            foreach (DomainEvent domainEvent in events)
            {
                AddEvent(domainEvent);
            }
        }

        public void AddResult(GridOperationResult result)
        {
            if (result != null)
            {
                AddEvents(result.Events);
            }
        }

        public async Task<DamageResult> ApplyDamageAsync(DamageInfo info)
        {
            return await Domain.Combat.ApplyDamageAsync(info, Events).ConfigureAwait(false);
        }
    }

    public sealed class CardInteractionContext : DomainCallbackContext
    {
        public CardInteractionContext(DomainActionContext domain, CardInstance playerCard, CardInstance targetCard, PlayerIntent sourceIntent, ICollection<DomainEvent> events)
            : base(domain, targetCard, events)
        {
            PlayerCard = playerCard ?? throw new ArgumentNullException(nameof(playerCard));
            SourceIntent = sourceIntent;
        }

        public CardInstance PlayerCard { get; }
        public CardInstance TargetCard
        {
            get { return Card; }
        }

        public PlayerIntent SourceIntent { get; }
    }

    public class CardRevealContext : DomainCallbackContext
    {
        public CardRevealContext(DomainActionContext domain, CardInstance card, string reason, ICollection<DomainEvent> events)
            : base(domain, card, events)
        {
            Reason = reason ?? string.Empty;
        }

        public string Reason { get; }
    }

    public class CardDestroyedContext : DomainCallbackContext
    {
        public CardDestroyedContext(DomainActionContext domain, CardInstance card, string reason, ICollection<DomainEvent> events)
            : base(domain, card, events)
        {
            Reason = reason ?? string.Empty;
        }

        public string Reason { get; }
    }

    public class PlayerActionContext : DomainCallbackContext
    {
        public PlayerActionContext(DomainActionContext domain, CardInstance observedCard, PlayerIntent sourceIntent, int actionIndex, ICollection<DomainEvent> events)
            : base(domain, observedCard, events)
        {
            SourceIntent = sourceIntent;
            ActionIndex = actionIndex;
        }

        public CardInstance ObservedCard
        {
            get { return Card; }
        }

        public CardInstance PlayerCard
        {
            get { return Domain.Grid.PlayerCard; }
        }

        public PlayerIntent SourceIntent { get; }
        public int ActionIndex { get; }
    }

    public sealed class TrapContext : DomainCallbackContext
    {
        public TrapContext(DomainActionContext domain, CardInstance trapCard, string reason, PlayerIntent sourceIntent, int actionIndex, ICollection<DomainEvent> events)
            : base(domain, trapCard, events)
        {
            Reason = reason ?? string.Empty;
            SourceIntent = sourceIntent;
            ActionIndex = actionIndex;
        }

        public CardInstance TrapCard
        {
            get { return Card; }
        }

        public CardInstance PlayerCard
        {
            get { return Domain.Grid.PlayerCard; }
        }

        public string Reason { get; }
        public PlayerIntent SourceIntent { get; }
        public int ActionIndex { get; }
    }

    public sealed class ItemUseContext : DomainCallbackContext
    {
        public ItemUseContext(DomainActionContext domain, CardInstance itemCard, InventorySlot slot, PlayerIntent sourceIntent, ICollection<DomainEvent> events)
            : base(domain, itemCard, events)
        {
            Slot = slot;
            SourceIntent = sourceIntent;
        }

        public CardInstance ItemCard
        {
            get { return Card; }
        }

        public CardInstance PlayerCard
        {
            get { return Domain.Grid.PlayerCard; }
        }

        public InventorySlot Slot { get; }
        public PlayerIntent SourceIntent { get; }
    }

    public sealed class ActiveRelicContext
    {
        public ActiveRelicContext(DomainActionContext domain, RelicModel relic, ActiveRelicSlot slot, PlayerIntent sourceIntent, ICollection<DomainEvent> events)
        {
            Domain = domain ?? throw new ArgumentNullException(nameof(domain));
            Relic = relic ?? throw new ArgumentNullException(nameof(relic));
            Slot = slot ?? throw new ArgumentNullException(nameof(slot));
            SourceIntent = sourceIntent;
            Events = events ?? throw new ArgumentNullException(nameof(events));
        }

        public DomainActionContext Domain { get; }
        public RelicModel Relic { get; }
        public ActiveRelicSlot Slot { get; }
        public PlayerIntent SourceIntent { get; }
        public ICollection<DomainEvent> Events { get; }

        public CardInstance PlayerCard
        {
            get { return Domain.Grid.PlayerCard; }
        }

        public void AddEvent(DomainEvent domainEvent)
        {
            if (domainEvent != null)
            {
                Events.Add(domainEvent);
            }
        }

        public void AddEvents(IEnumerable<DomainEvent> events)
        {
            if (events == null)
            {
                return;
            }

            foreach (DomainEvent domainEvent in events)
            {
                AddEvent(domainEvent);
            }
        }

        public void AddResult(GridOperationResult result)
        {
            if (result != null)
            {
                AddEvents(result.Events);
            }
        }

        public async Task<DamageResult> ApplyDamageAsync(DamageInfo info)
        {
            return await Domain.Combat.ApplyDamageAsync(info, Events).ConfigureAwait(false);
        }
    }
}
