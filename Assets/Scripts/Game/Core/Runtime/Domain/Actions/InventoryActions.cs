using System.Collections.Generic;
using System.Threading.Tasks;
using Game.Core.Actions;
using Game.Core.Domain.Cards;
using Game.Core.Domain.ContentContracts;
using Game.Core.Domain.Events;
using Game.Core.Domain.Grid;
using Game.Core.Domain.Interaction;
using Game.Core.Domain.Inventory;

namespace Game.Core.Domain.Actions
{
    public sealed class StoreItemAction : GameAction
    {
        private readonly DomainActionContext _domain;
        private readonly StoreItemIntent _intent;

        public StoreItemAction(DomainActionContext domain, StoreItemIntent intent)
        {
            _domain = domain;
            _intent = intent;
        }

        protected override async Task ExecuteActionAsync(GameActionExecutionContext ctx)
        {
            List<DomainEvent> events = new List<DomainEvent>();
            IntentValidationResult validation = new IntentValidator(_domain).Validate(_intent);
            if (!validation.IsValid)
            {
                events.Add(new DomainEvent(DomainEventType.IntentRejected) { Reason = validation.FailureCode });
                AddBatch(events);
                return;
            }

            CardInstance itemCard = _domain.Grid.GetCard(_intent.ItemCard);
            GridOperationResult transfer = _domain.Grid.MoveTopCardToZone(itemCard, CardZone.PlayerInventory, "StoreItem");
            if (!transfer.Succeeded)
            {
                events.Add(new DomainEvent(DomainEventType.IntentRejected) { Reason = transfer.FailureCode });
                AddBatch(events);
                return;
            }

            events.AddRange(transfer.Events);
            ctx.EnqueueFollowUpActions(transfer.FollowUpActions);
            InventorySlot slot = _domain.ItemInventory.Store(itemCard);
            events.Add(new DomainEvent(DomainEventType.ItemStored)
            {
                CardId = itemCard.InstanceId,
                Amount = slot.Index,
                Reason = "StoreItem"
            });

            await _domain.ProcessLifecycleAsync(events);
            _domain.AppendPlayerDefeatedIfNeeded(events);
            AddBatch(events);
        }

        private void AddBatch(IEnumerable<DomainEvent> events)
        {
            DomainEventBatch batch = new DomainEventBatch(Id, _intent);
            batch.AddRange(events);
            _domain.Batches.Add(batch);
        }
    }

    public sealed class UseItemAction : GameAction
    {
        private readonly DomainActionContext _domain;
        private readonly UseItemIntent _intent;

        public UseItemAction(DomainActionContext domain, UseItemIntent intent)
        {
            _domain = domain;
            _intent = intent;
        }

        protected override async Task ExecuteActionAsync(GameActionExecutionContext ctx)
        {
            List<DomainEvent> events = new List<DomainEvent>();
            IntentValidationResult validation = new IntentValidator(_domain).Validate(_intent);
            if (!validation.IsValid)
            {
                events.Add(new DomainEvent(DomainEventType.IntentRejected) { Reason = validation.FailureCode });
                AddBatch(events);
                return;
            }

            CardInstance itemCard = _domain.ItemInventory.Get(_intent.Slot);
            if (itemCard == null)
            {
                events.Add(new DomainEvent(DomainEventType.IntentRejected) { Reason = "ItemNotFound" });
                AddBatch(events);
                return;
            }

            if (!_domain.TryResolveCardModel(itemCard, out CardModel model) || model is not ItemCardModel itemModel)
            {
                events.Add(new DomainEvent(DomainEventType.IntentRejected) { Reason = "ItemModelNotFound" });
                AddBatch(events);
                return;
            }

            ItemUseContext useContext = new ItemUseContext(_domain, itemCard, _intent.Slot, _intent, events);
            events.Add(new DomainEvent(DomainEventType.ItemUsed)
            {
                CardId = itemCard.InstanceId,
                Reason = itemModel.Id.ToString()
            });
            await itemModel.UseAsync(useContext);
            _domain.ResolveDeadCards(events);

            int usesRemaining = itemCard.GetState("usesRemaining", itemModel.MaxUses) - 1;
            if (usesRemaining <= 0)
            {
                _domain.ItemInventory.RemoveAt(_intent.Slot);
                _domain.Grid.TrackCard(itemCard);
                itemCard.Zone = CardZone.Removed;
                itemCard.Coord = null;
                itemCard.StackIndex = -1;
                itemCard.IsFaceUp = false;
                itemCard.IsRemoved = true;
                events.Add(new DomainEvent(DomainEventType.CardZoneChanged)
                {
                    CardId = itemCard.InstanceId,
                    Reason = CardZone.Removed.ToString()
                });
                events.Add(new DomainEvent(DomainEventType.CardRemoved)
                {
                    CardId = itemCard.InstanceId,
                    Reason = RemoveReason.Consumed.ToString()
                });
            }
            else
            {
                itemCard.SetState("usesRemaining", usesRemaining);
            }

            await _domain.ProcessLifecycleAsync(events);
            _domain.AppendPlayerDefeatedIfNeeded(events);
            if (_domain.RoomClearChecker.IsRoomCleared(_domain.Grid))
            {
                events.Add(new DomainEvent(DomainEventType.RoomCleared));

                if (_domain.RoomTransition != null && _domain.Rng != null)
                {
                    List<DomainEvent> routeEvents = _domain.RoomTransition.GenerateAndPlaceRouteCards(_domain, _domain.Rng);
                    events.AddRange(routeEvents);
                }
            }

            AddBatch(events);
        }

        private void AddBatch(IEnumerable<DomainEvent> events)
        {
            DomainEventBatch batch = new DomainEventBatch(Id, _intent);
            batch.AddRange(events);
            _domain.Batches.Add(batch);
        }
    }

    public sealed class ActivateRelicAction : GameAction
    {
        private readonly DomainActionContext _domain;
        private readonly ActivateRelicIntent _intent;

        public ActivateRelicAction(DomainActionContext domain, ActivateRelicIntent intent)
        {
            _domain = domain;
            _intent = intent;
        }

        protected override async Task ExecuteActionAsync(GameActionExecutionContext ctx)
        {
            List<DomainEvent> events = new List<DomainEvent>();
            IntentValidationResult validation = new IntentValidator(_domain).Validate(_intent);
            if (!validation.IsValid)
            {
                events.Add(new DomainEvent(DomainEventType.IntentRejected) { Reason = validation.FailureCode });
                AddBatch(events);
                return;
            }

            if (!_domain.TryResolveRelicModel(_intent.RelicId, out RelicModel relic))
            {
                events.Add(new DomainEvent(DomainEventType.IntentRejected) { Reason = "RelicModelNotFound" });
                AddBatch(events);
                return;
            }

            ActiveRelicContext relicContext = new ActiveRelicContext(_domain, relic, _domain.Relics.ActiveSlot, _intent, events);
            if (!relic.CanActivate(relicContext))
            {
                events.Add(new DomainEvent(DomainEventType.IntentRejected) { Reason = "RelicCannotActivate" });
                AddBatch(events);
                return;
            }

            events.Add(new DomainEvent(DomainEventType.RelicActivated)
            {
                Reason = relic.Id.ToString()
            });
            await relic.ActivateAsync(relicContext);
            _domain.ResolveDeadCards(events);
            _domain.Relics.ActiveSlot.MarkActivated(relic.Id);
            await _domain.ProcessLifecycleAsync(events);
            _domain.AppendPlayerDefeatedIfNeeded(events);
            if (_domain.RoomClearChecker.IsRoomCleared(_domain.Grid))
            {
                events.Add(new DomainEvent(DomainEventType.RoomCleared));

                if (_domain.RoomTransition != null && _domain.Rng != null)
                {
                    List<DomainEvent> routeEvents = _domain.RoomTransition.GenerateAndPlaceRouteCards(_domain, _domain.Rng);
                    events.AddRange(routeEvents);
                }
            }

            AddBatch(events);
        }

        private void AddBatch(IEnumerable<DomainEvent> events)
        {
            DomainEventBatch batch = new DomainEventBatch(Id, _intent);
            batch.AddRange(events);
            _domain.Batches.Add(batch);
        }
    }
}
