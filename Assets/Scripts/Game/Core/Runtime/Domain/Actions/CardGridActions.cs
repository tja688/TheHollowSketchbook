using System.Collections.Generic;
using System.Threading.Tasks;
using Game.Core.Actions;
using Game.Core.Domain.Cards;
using Game.Core.Domain.Events;
using Game.Core.Domain.Grid;

namespace Game.Core.Domain.Actions
{
    public sealed class FlipCardAction : GameAction
    {
        private readonly DomainActionContext _domain;
        private readonly CardInstanceId _cardId;
        private readonly FlipReason _reason;

        public FlipCardAction(DomainActionContext domain, CardInstanceId cardId, FlipReason reason)
        {
            _domain = domain;
            _cardId = cardId;
            _reason = reason;
        }

        protected override Task ExecuteActionAsync(GameActionExecutionContext ctx)
        {
            CardInstance card = _domain.Grid.GetCard(_cardId);
            GridOperationResult result = _domain.Grid.FlipCard(card, _reason);
            ctx.EnqueueFollowUpActions(result.FollowUpActions);
            DomainEventBatch batch = new DomainEventBatch(Id, null);
            batch.AddRange(result.Events);
            _domain.Batches.Add(batch);
            return Task.CompletedTask;
        }
    }

    public sealed class RemoveCardAction : GameAction
    {
        private readonly DomainActionContext _domain;
        private readonly CardInstanceId _cardId;
        private readonly RemoveReason _reason;

        public RemoveCardAction(DomainActionContext domain, CardInstanceId cardId, RemoveReason reason)
        {
            _domain = domain;
            _cardId = cardId;
            _reason = reason;
        }

        protected override Task ExecuteActionAsync(GameActionExecutionContext ctx)
        {
            CardInstance card = _domain.Grid.GetCard(_cardId);
            GridOperationResult result = _domain.Grid.RemoveCard(card, _reason);
            ctx.EnqueueFollowUpActions(result.FollowUpActions);
            DomainEventBatch batch = new DomainEventBatch(Id, null);
            batch.AddRange(result.Events);
            _domain.Batches.Add(batch);
            return Task.CompletedTask;
        }
    }
}
