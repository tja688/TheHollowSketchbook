using System;
using Game.Core.Domain.Events;
using Game.Core.Domain.Interaction;

namespace Game.Core.Domain.Combat
{
    public sealed class PlayerActionCounter
    {
        public int Value { get; private set; }

        public DomainEvent Increment(PlayerIntent sourceIntent)
        {
            Value++;
            return new DomainEvent(DomainEventType.PlayerActionCommitted)
            {
                Amount = Value,
                Reason = sourceIntent != null ? sourceIntent.Kind.ToString() : string.Empty
            };
        }

        public void RestoreValue(int value)
        {
            Value = Math.Max(0, value);
        }
    }
}
