using System;
using System.Threading.Tasks;
using Game.Core.Actions;
using Game.Core.Domain.Actions;
using Game.Core.Domain.Combat;
using Game.Core.Domain.Events;
using Game.Core.Domain.Grid;
using Game.Core.Domain.Interaction;

namespace Game.Core.Domain
{
    public sealed class DomainFacade
    {
        private readonly DomainActionContext _context;
        private readonly ActionQueueSet _queue = new ActionQueueSet();
        private readonly ActionExecutor _executor;
        private readonly IntentValidator _validator;

        public DomainFacade(DomainActionContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _executor = new ActionExecutor(_queue);
            _validator = new IntentValidator(context.Grid);
        }

        public DomainActionContext Context
        {
            get { return _context; }
        }

        public IntentPreview PreviewIntent(PlayerIntent intent)
        {
            return _validator.Preview(intent);
        }

        public async Task<DomainEventBatch> SubmitIntentAsync(PlayerIntent intent)
        {
            int beforeCount = _context.Batches.Count;
            IntentValidationResult validation = _validator.Validate(intent);
            if (!validation.IsValid)
            {
                DomainEventBatch rejected = new DomainEventBatch(0, intent);
                rejected.Add(new DomainEvent(DomainEventType.IntentRejected) { Reason = validation.FailureCode });
                _context.Batches.Add(rejected);
                return rejected;
            }

            if (intent is MovePlayerIntent moveIntent)
            {
                _queue.Enqueue(new PlayerMoveAction(_context, moveIntent));
            }
            else if (intent is InteractWithCardIntent interactIntent)
            {
                _queue.Enqueue(new PlayerInteractAction(_context, interactIntent));
            }
            else
            {
                DomainEventBatch rejected = new DomainEventBatch(0, intent);
                rejected.Add(new DomainEvent(DomainEventType.IntentRejected) { Reason = "UnsupportedIntent" });
                _context.Batches.Add(rejected);
                return rejected;
            }

            await _executor.ExecuteAllAsync();
            return _context.Batches.Count > beforeCount ? _context.Batches[_context.Batches.Count - 1] : null;
        }
    }
}
