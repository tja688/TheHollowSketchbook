using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Game.Core.Logging;

namespace Game.Core.Actions
{
    public enum GameActionState
    {
        None,
        WaitingForExecution,
        Executing,
        Finished,
        Canceled
    }

    public abstract class GameAction
    {
        private readonly TaskCompletionSource<bool> _completionSource = new TaskCompletionSource<bool>();

        public uint Id { get; internal set; }
        public GameActionState State { get; private set; }

        public Task CompletionTask
        {
            get { return _completionSource.Task; }
        }

        public void Cancel()
        {
            if (State == GameActionState.Finished || State == GameActionState.Canceled)
            {
                return;
            }

            State = GameActionState.Canceled;
            _completionSource.TrySetCanceled();
        }

        internal void MarkQueued(uint id)
        {
            Id = id;
            State = GameActionState.WaitingForExecution;
        }

        internal async Task ExecuteAsync(GameActionExecutionContext ctx)
        {
            if (State == GameActionState.Canceled)
            {
                return;
            }

            try
            {
                State = GameActionState.Executing;
                await ExecuteActionAsync(ctx);
                State = GameActionState.Finished;
                _completionSource.TrySetResult(true);
            }
            catch (Exception exception)
            {
                State = GameActionState.Canceled;
                _completionSource.TrySetException(exception);
                throw;
            }
        }

        protected abstract Task ExecuteActionAsync(GameActionExecutionContext ctx);
    }

    public sealed class GameActionExecutionContext
    {
    }

    public sealed class ActionQueueSet
    {
        private readonly Queue<GameAction> _queue = new Queue<GameAction>();
        private uint _nextId = 1;

        public int Count
        {
            get { return _queue.Count; }
        }

        public event System.Action QueueChanged;
        public event System.Action<GameAction> ActionEnqueued;

        public void Enqueue(GameAction action)
        {
            if (action == null)
            {
                throw new GameException("Cannot enqueue a null action.");
            }

            action.MarkQueued(_nextId++);
            _queue.Enqueue(action);
            ActionEnqueued?.Invoke(action);
            QueueChanged?.Invoke();
        }

        public void Clear(bool cancelActions = true)
        {
            while (_queue.Count > 0)
            {
                GameAction action = _queue.Dequeue();
                if (cancelActions)
                {
                    action.Cancel();
                }
            }

            QueueChanged?.Invoke();
        }

        internal bool TryDequeue(out GameAction action)
        {
            if (_queue.Count > 0)
            {
                action = _queue.Dequeue();
                QueueChanged?.Invoke();
                return true;
            }

            action = null;
            return false;
        }
    }

    public sealed class ActionExecutor
    {
        private readonly ActionQueueSet _queueSet;
        private readonly GameActionExecutionContext _context = new GameActionExecutionContext();
        private readonly object _executionLock = new object();
        private Task _runningTask;

        public ActionExecutor(ActionQueueSet queueSet)
        {
            _queueSet = queueSet;
        }

        public bool IsRunning
        {
            get
            {
                lock (_executionLock)
                {
                    return _runningTask != null;
                }
            }
        }

        public Task ExecuteAllAsync()
        {
            lock (_executionLock)
            {
                if (_runningTask != null)
                {
                    return _runningTask;
                }

                _runningTask = RunAllAsync();
                return _runningTask;
            }
        }

        private async Task RunAllAsync()
        {
            try
            {
                while (_queueSet.TryDequeue(out GameAction action))
                {
                    await action.ExecuteAsync(_context);
                }
            }
            finally
            {
                lock (_executionLock)
                {
                    _runningTask = null;
                }
            }
        }
    }
}
