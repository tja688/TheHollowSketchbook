using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Game.Core;
using Game.Core.Domain.Cards;
using Game.Core.Domain.Combat;
using Game.Core.Domain.ContentContracts;
using Game.Core.Domain.Deck;
using Game.Core.Domain.Events;
using Game.Core.Domain.Grid;
using Game.Core.Domain.Interaction;
using Game.Core.Domain.Inventory;
using Game.Core.Domain.Rooms;
using Game.Core.Models;
using Game.Core.Random;

namespace Game.Core.Domain
{
    public sealed class DomainActionContext
    {
        public DomainActionContext(GridState grid, PlayerActionCounter actionCounter)
        {
            Grid = grid ?? throw new ArgumentNullException(nameof(grid));
            ActionCounter = actionCounter ?? throw new ArgumentNullException(nameof(actionCounter));
            Combat = new CombatResolution(grid);
            RoomClearChecker = new RoomClearChecker();
            ItemInventory = new PlayerInventory();
            Relics = new RelicInventory();
        }

        public GridState Grid { get; }
        public PlayerActionCounter ActionCounter { get; }
        public CombatResolution Combat { get; }
        public RoomClearChecker RoomClearChecker { get; }
        public DungeonDeck DungeonDeck { get; set; }
        public IRng Rng { get; set; }
        public PlayerInventory ItemInventory { get; }
        public RelicInventory Relics { get; }
        public int PlayerGold { get; private set; }
        public List<DomainEventBatch> Batches { get; } = new List<DomainEventBatch>();

        public CardModel ResolveCardModel(CardInstance card)
        {
            if (card == null)
            {
                throw new ArgumentNullException(nameof(card));
            }

            return ModelDb.Get<CardModel>(card.ModelId);
        }

        public bool TryResolveCardModel(CardInstance card, out CardModel model)
        {
            if (card == null)
            {
                model = null;
                return false;
            }

            return ModelDb.TryGet(card.ModelId, out model);
        }

        public bool TryResolveRelicModel(ModelId relicId, out RelicModel relic)
        {
            return ModelDb.TryGet(relicId, out relic);
        }

        public async Task ProcessLifecycleAsync(ICollection<DomainEvent> events)
        {
            if (events == null)
            {
                throw new ArgumentNullException(nameof(events));
            }

            if (events is not List<DomainEvent> eventList)
            {
                return;
            }

            await ProcessLifecycleEventsAsync(eventList, 0);
        }

        public async Task ProcessLifecycleAsync(PlayerIntent sourceIntent, ICollection<DomainEvent> events)
        {
            await NotifyAfterPlayerActionCommittedAsync(sourceIntent, events);
            await ProcessLifecycleAsync(events);
        }

        public async Task NotifyAfterPlayerActionCommittedAsync(PlayerIntent sourceIntent, ICollection<DomainEvent> events)
        {
            if (sourceIntent == null)
            {
                return;
            }

            List<CardInstanceId> observerIds = CollectActionObserverIds();
            if (events is not List<DomainEvent> eventList)
            {
                return;
            }

            await NotifyAfterPlayerActionCommittedAsync(observerIds, sourceIntent, eventList);
        }

        private List<CardInstanceId> CollectActionObserverIds()
        {
            List<CardInstanceId> observerIds = new List<CardInstanceId>();
            foreach (CardInstance card in Grid.AllGridCards)
            {
                if (card.IsFaceUp && card.CardType != CardType.Player)
                {
                    observerIds.Add(card.InstanceId);
                }
            }

            return observerIds;
        }

        private async Task NotifyAfterPlayerActionCommittedAsync(IReadOnlyList<CardInstanceId> observerIds, PlayerIntent sourceIntent, List<DomainEvent> events)
        {
            int actionIndex = ActionCounter.Value;
            for (int i = 0; i < observerIds.Count; i++)
            {
                if (!Grid.TryGetCard(observerIds[i], out CardInstance card))
                {
                    continue;
                }

                if (card.Zone != CardZone.Grid || !card.IsFaceUp || card.CardType == CardType.Player)
                {
                    continue;
                }

                if (!TryResolveCardModel(card, out CardModel model))
                {
                    continue;
                }

                await model.OnAfterPlayerActionCommittedAsync(new PlayerActionContext(this, card, sourceIntent, actionIndex, events));
            }
        }

        private async Task ProcessLifecycleEventsAsync(List<DomainEvent> events, int startIndex)
        {
            for (int i = startIndex; i < events.Count; i++)
            {
                DomainEvent domainEvent = events[i];
                if (domainEvent == null)
                {
                    continue;
                }

                if (domainEvent.EventType == DomainEventType.CardFlipped)
                {
                    if (Grid.TryGetCard(domainEvent.CardId, out CardInstance revealedCard) && TryResolveCardModel(revealedCard, out CardModel revealedModel))
                    {
                        await revealedModel.OnRevealedAsync(new CardRevealContext(this, revealedCard, domainEvent.Reason, events));
                    }
                }
                else if (domainEvent.EventType == DomainEventType.CardRemoved)
                {
                    if (Grid.TryGetCard(domainEvent.CardId, out CardInstance removedCard) && TryResolveCardModel(removedCard, out CardModel removedModel))
                    {
                        await removedModel.OnDestroyedAsync(new CardDestroyedContext(this, removedCard, domainEvent.Reason, events));
                    }
                }
            }
        }

        public void AppendPlayerDefeatedIfNeeded(ICollection<DomainEvent> events)
        {
            if (events == null)
            {
                throw new ArgumentNullException(nameof(events));
            }

            CardInstance defeatedPlayer = null;
            foreach (CardInstance card in Grid.AllKnownCards)
            {
                if (card.CardType == CardType.Player && card.HasHitPoints && !card.IsAlive)
                {
                    defeatedPlayer = card;
                    break;
                }
            }

            if (defeatedPlayer == null)
            {
                return;
            }

            foreach (DomainEvent domainEvent in events)
            {
                if (domainEvent != null && domainEvent.EventType == DomainEventType.RunEnded && domainEvent.CardId == defeatedPlayer.InstanceId)
                {
                    return;
                }
            }

            events.Add(new DomainEvent(DomainEventType.RunEnded)
            {
                CardId = defeatedPlayer.InstanceId,
                Reason = "PlayerDefeated"
            });
        }

        public void GainGold(int amount, ICollection<DomainEvent> events, string reason)
        {
            int delta = Math.Max(0, amount);
            if (delta == 0)
            {
                return;
            }

            PlayerGold += delta;
            events?.Add(new DomainEvent(DomainEventType.GoldChanged)
            {
                Amount = delta,
                SecondaryAmount = PlayerGold,
                Reason = reason
            });
        }
    }
}
