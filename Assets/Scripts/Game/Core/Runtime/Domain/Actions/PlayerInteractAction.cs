using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Game.Core.Actions;
using Game.Core.Domain.Cards;
using Game.Core.Domain.ContentContracts;
using Game.Core.Domain.Events;
using Game.Core.Domain.Grid;
using Game.Core.Domain.Interaction;
using Game.Core.Domain.Rooms;

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

            // Route choice card: handle room transition
            if (model is RouteChoiceCardModel routeModel)
            {
                await HandleRouteChoiceAsync(routeModel, player, target, events);
                AddBatch(events);
                return;
            }

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
            _domain.ResolveDeadCards(events);

            bool roomCleared = _domain.RoomClearChecker.IsRoomCleared(_domain.Grid);
            if (roomCleared)
            {
                events.Add(new DomainEvent(DomainEventType.RoomCleared));
            }

            await _domain.ProcessLifecycleAsync(events);
            _domain.AppendPlayerDefeatedIfNeeded(events);
            if (roomCleared)
            {
                if (_domain.RoomTransition != null && _domain.Rng != null)
                {
                    List<DomainEvent> routeEvents = _domain.RoomTransition.GenerateAndPlaceRouteCards(_domain, _domain.Rng);
                    events.AddRange(routeEvents);
                }
            }

            AddBatch(events);
        }

        private async Task HandleRouteChoiceAsync(
            RouteChoiceCardModel routeModel,
            CardInstance player,
            CardInstance target,
            List<DomainEvent> events)
        {
            CardInteractionContext interactionContext = new CardInteractionContext(_domain, player, target, _intent, events);

            // Delegate to the route choice model which calls RoomTransitionService.EnterRoom()
            await routeModel.OnPlayerInteractAsync(interactionContext);

            // Increment action counter for the route selection
            events.Add(_domain.ActionCounter.Increment(_intent));

            events.Add(new DomainEvent(DomainEventType.RouteChoiceSelected)
            {
                CardId = target.InstanceId,
                Reason = routeModel.TargetRoomType.ToString()
            });
        }

        private void AddBatch(IEnumerable<DomainEvent> events)
        {
            DomainEventBatch batch = new DomainEventBatch(Id, _intent);
            batch.AddRange(events);
            _domain.Batches.Add(batch);
        }
    }
}
