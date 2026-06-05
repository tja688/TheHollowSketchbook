using System.Collections.Generic;
using System.Threading.Tasks;
using Game.Core.Actions;
using Game.Core.Domain.Cards;
using Game.Core.Domain.ContentContracts;
using Game.Core.Domain.Events;
using Game.Core.Domain.Grid;
using Game.Core.Domain.Interaction;

namespace Game.Core.Domain.Actions
{
    public sealed class PlayerInteractAction : GameAction
    {
        private readonly DomainActionContext _domain;
        private readonly InteractWithCardIntent _intent;

        public PlayerInteractAction(DomainActionContext domain, InteractWithCardIntent intent)
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

            CardInstance player = _domain.Grid.PlayerCard;
            CardInstance target = _domain.Grid.GetCard(_intent.Target);
            CardModel model = _domain.ResolveCardModel(target);
            CardInteractionContext interactionContext = new CardInteractionContext(_domain, player, target, _intent, events);
            if (!model.CanInteractWithPlayer(interactionContext))
            {
                events.Add(new DomainEvent(DomainEventType.IntentRejected) { Reason = "InteractionRejectedByModel" });
                AddBatch(events);
                return;
            }

            await model.OnPlayerInteractAsync(interactionContext);
            events.Add(_domain.ActionCounter.Increment(_intent));
            await _domain.NotifyAfterPlayerActionCommittedAsync(_intent, events);
            RemoveIfDead(target, events, ctx);
            RemoveIfDead(player, events, ctx);
            await _domain.ProcessLifecycleAsync(events);
            _domain.AppendPlayerDefeatedIfNeeded(events);
            if (_domain.RoomClearChecker.IsRoomCleared(_domain.Grid))
            {
                events.Add(new DomainEvent(DomainEventType.RoomCleared));
            }

            AddBatch(events);
        }

        private void RemoveIfDead(CardInstance card, List<DomainEvent> events, GameActionExecutionContext ctx)
        {
            if (card == null || !card.HasHitPoints || card.IsAlive || card.Zone != CardZone.Grid)
            {
                return;
            }

            RemoveReason reason = card.CardType == CardType.Trap ? RemoveReason.Destroyed : RemoveReason.Defeated;
            GridOperationResult remove = _domain.Grid.RemoveCard(card, reason);
            events.AddRange(remove.Events);
            ctx.EnqueueFollowUpActions(remove.FollowUpActions);
            if (card.CardType == CardType.Monster)
            {
                events.Add(new DomainEvent(DomainEventType.MonsterDefeated)
                {
                    CardId = card.InstanceId,
                    Reason = reason.ToString()
                });
                _domain.GainGold(card.GoldOnRemoved, events, "MonsterRemoved");
            }
        }

        private void AddBatch(IEnumerable<DomainEvent> events)
        {
            DomainEventBatch batch = new DomainEventBatch(Id, _intent);
            batch.AddRange(events);
            _domain.Batches.Add(batch);
        }
    }
}
