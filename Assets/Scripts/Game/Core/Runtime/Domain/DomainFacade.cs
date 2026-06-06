using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Game.Core.Actions;
using Game.Core.Domain.Actions;
using Game.Core.Domain.Events;
using Game.Core.Domain.Interaction;

namespace Game.Core.Domain
{
    public sealed class DomainFacade
    {
        private readonly DomainActionContext _context;
        private readonly ActionQueueSet _queue = new ActionQueueSet();
        private readonly ActionExecutor _executor;
        private readonly IntentValidator _validator;
        private readonly SemaphoreSlim _submitGate = new SemaphoreSlim(1, 1);
        private readonly AsyncLocal<int> _submitDepth = new AsyncLocal<int>();

        public DomainFacade(DomainActionContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _executor = new ActionExecutor(_queue);
            _validator = new IntentValidator(context);
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
            if (_submitDepth.Value > 0)
            {
                return RejectIntent(intent, "SubmitIntentReentrant");
            }

            await _submitGate.WaitAsync().ConfigureAwait(false);
            _submitDepth.Value++;
            try
            {
                int beforeCount = _context.Batches.Count;
                IntentValidationResult validation = _validator.Validate(intent);
                if (!validation.IsValid)
                {
                    return RejectIntent(intent, validation.FailureCode);
                }

                if (intent is MovePlayerIntent moveIntent)
                {
                    _queue.Enqueue(new PlayerMoveAction(_context, moveIntent));
                }
                else if (intent is InteractWithCardIntent interactIntent)
                {
                    _queue.Enqueue(new PlayerInteractAction(_context, interactIntent));
                }
                else if (intent is StoreItemIntent storeItemIntent)
                {
                    _queue.Enqueue(new StoreItemAction(_context, storeItemIntent));
                }
                else if (intent is UseItemIntent useItemIntent)
                {
                    _queue.Enqueue(new UseItemAction(_context, useItemIntent));
                }
                else if (intent is ChooseOptionIntent chooseOptionIntent)
                {
                    if (!_context.ChoiceSessions.TryResolve(chooseOptionIntent.SessionId, chooseOptionIntent.OptionIndex, out ChoiceSession session))
                    {
                        return RejectIntent(chooseOptionIntent, "ChoiceResolveFailed");
                    }

                    DomainEventBatch batch = new DomainEventBatch(0, chooseOptionIntent);
                    List<DomainEvent> choiceEvents = new List<DomainEvent>
                    {
                        new DomainEvent(DomainEventType.ChoiceResolved)
                        {
                            Amount = chooseOptionIntent.OptionIndex,
                            SecondaryAmount = session.OptionCount,
                            Reason = chooseOptionIntent.SessionId
                        }
                    };
                    await _context.ResolveChoiceSessionAsync(session, chooseOptionIntent.OptionIndex, choiceEvents).ConfigureAwait(false);
                    _context.ResolveDeadCards(choiceEvents);
                    await _context.ProcessLifecycleAsync(choiceEvents).ConfigureAwait(false);
                    _context.AppendPlayerDefeatedIfNeeded(choiceEvents);
                    batch.AddRange(choiceEvents);
                    _context.Batches.Add(batch);
                    return batch;
                }
                else if (intent is ActivateRelicIntent activateRelicIntent)
                {
                    _queue.Enqueue(new ActivateRelicAction(_context, activateRelicIntent));
                }
                else
                {
                    return RejectIntent(intent, "UnsupportedIntent");
                }

                await _executor.ExecuteAllAsync().ConfigureAwait(false);
                while (_queue.Count > 0)
                {
                    await _executor.ExecuteAllAsync().ConfigureAwait(false);
                }
                return _context.Batches.Count > beforeCount ? _context.Batches[_context.Batches.Count - 1] : null;
            }
            finally
            {
                _submitDepth.Value--;
                _submitGate.Release();
            }
        }

        private DomainEventBatch RejectIntent(PlayerIntent intent, string reason)
        {
            DomainEventBatch rejected = new DomainEventBatch(0, intent);
            rejected.Add(new DomainEvent(DomainEventType.IntentRejected) { Reason = reason });
            _context.Batches.Add(rejected);
            return rejected;
        }
    }
}
