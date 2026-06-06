using System;
using System.Collections.Generic;
using Game.Core.Domain.Cards;

namespace Game.Core.Domain.Progression
{
    public enum PendingTriggerTiming
    {
        AfterPlayerAction
    }

    public sealed class PendingTrigger
    {
        public PendingTrigger(CardInstanceId cardId, PendingTriggerTiming timing, int dueActionIndex, string triggerKey)
        {
            CardId = cardId;
            Timing = timing;
            DueActionIndex = Math.Max(0, dueActionIndex);
            TriggerKey = triggerKey ?? string.Empty;
        }

        public CardInstanceId CardId { get; }
        public PendingTriggerTiming Timing { get; }
        public int DueActionIndex { get; }
        public string TriggerKey { get; }
    }

    public sealed class PendingTriggerQueue
    {
        private readonly List<PendingTrigger> _items = new List<PendingTrigger>();

        public int Count => _items.Count;
        public IReadOnlyList<PendingTrigger> Items => _items;

        public void Enqueue(PendingTrigger trigger)
        {
            if (trigger == null)
            {
                throw new ArgumentNullException(nameof(trigger));
            }

            _items.Add(trigger);
        }

        public PendingTrigger Peek()
        {
            return _items.Count > 0 ? _items[0] : null;
        }

        public IReadOnlyList<PendingTrigger> DequeueDue(PendingTriggerTiming timing, int actionIndex)
        {
            List<PendingTrigger> due = new List<PendingTrigger>();
            for (int i = _items.Count - 1; i >= 0; i--)
            {
                PendingTrigger trigger = _items[i];
                if (trigger.Timing == timing && trigger.DueActionIndex <= actionIndex)
                {
                    due.Insert(0, trigger);
                    _items.RemoveAt(i);
                }
            }

            return due;
        }

        public void Clear()
        {
            _items.Clear();
        }
    }
}
