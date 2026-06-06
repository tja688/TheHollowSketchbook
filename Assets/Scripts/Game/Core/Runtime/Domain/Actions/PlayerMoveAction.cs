using System.Collections.Generic;
using System.Threading.Tasks;
using Game.Core.Actions;
using Game.Core.Domain.Cards;
using Game.Core.Domain.Events;
using Game.Core.Domain.Grid;
using Game.Core.Domain.Interaction;

namespace Game.Core.Domain.Actions
{
    public sealed class PlayerMoveAction : GameAction
    {
        private readonly DomainActionContext _domain;
        private readonly MovePlayerIntent _intent;

        public PlayerMoveAction(DomainActionContext domain, MovePlayerIntent intent)
        {
            _domain = domain;
            _intent = intent;
        }

        protected override async Task ExecuteActionAsync(GameActionExecutionContext ctx)
        {
            List<DomainEvent> events = new List<DomainEvent>();
            CardInstance player = _domain.Grid.PlayerCard;
            GridOperationResult move = _domain.Grid.MoveCardToEmptyCell(player, _intent.To);
            if (!move.Succeeded)
            {
                events.Add(new DomainEvent(DomainEventType.IntentRejected) { Reason = move.FailureCode });
            }
            else
            {
                events.AddRange(move.Events);
                ctx.EnqueueFollowUpActions(move.FollowUpActions);
                events.Add(_domain.ActionCounter.Increment(_intent));
                await _domain.NotifyAfterPlayerActionCommittedAsync(_intent, events);
                GridOperationResult reveal = _domain.Grid.RevealAround(_intent.To, FlipReason.PlayerAdjacentReveal);
                events.AddRange(reveal.Events);
                ctx.EnqueueFollowUpActions(reveal.FollowUpActions);

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
                        events.AddRange(_domain.RoomTransition.GenerateAndPlaceRouteCards(_domain, _domain.Rng));
                    }
                }
            }

            DomainEventBatch batch = new DomainEventBatch(Id, _intent);
            batch.AddRange(events);
            _domain.Batches.Add(batch);
        }
    }
}
