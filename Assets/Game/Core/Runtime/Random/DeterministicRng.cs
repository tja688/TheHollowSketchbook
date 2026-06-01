using System;
using System.Collections.Generic;

namespace Game.Core
{
    public readonly struct RngState : IEquatable<RngState>
    {
        public uint Value { get; }

        public RngState(uint value)
        {
            Value = value == 0u ? 2463534242u : value;
        }

        public bool Equals(RngState other)
        {
            return Value == other.Value;
        }

        public override bool Equals(object obj)
        {
            return obj is RngState other && Equals(other);
        }

        public override int GetHashCode()
        {
            return (int)Value;
        }

        public override string ToString()
        {
            return Value.ToString();
        }
    }
}

namespace Game.Core.Random
{
    public interface IRng
    {
        int NextInt(int minInclusive, int maxExclusive);
        float NextFloat();
        T Pick<T>(IReadOnlyList<T> items);
        void Shuffle<T>(IList<T> items);
        RngState CaptureState();
    }

    public sealed class DeterministicRng : IRng
    {
        private uint _state;

        public DeterministicRng(int seed)
            : this(new RngState((uint)seed))
        {
        }

        public DeterministicRng(RngState state)
        {
            _state = state.Value == 0u ? 2463534242u : state.Value;
        }

        public RngState CaptureState()
        {
            return new RngState(_state);
        }

        public void RestoreState(RngState state)
        {
            _state = state.Value == 0u ? 2463534242u : state.Value;
        }

        public int NextInt(int minInclusive, int maxExclusive)
        {
            if (maxExclusive <= minInclusive)
            {
                throw new ArgumentOutOfRangeException(nameof(maxExclusive));
            }

            uint range = (uint)(maxExclusive - minInclusive);
            return minInclusive + (int)(NextUInt() % range);
        }

        public float NextFloat()
        {
            return (NextUInt() & 0x00FFFFFFu) / 16777216f;
        }

        public T Pick<T>(IReadOnlyList<T> items)
        {
            if (items == null || items.Count == 0)
            {
                throw new ArgumentException("Cannot pick from an empty collection.", nameof(items));
            }

            return items[NextInt(0, items.Count)];
        }

        public void Shuffle<T>(IList<T> items)
        {
            if (items == null)
            {
                throw new ArgumentNullException(nameof(items));
            }

            for (int i = items.Count - 1; i > 0; i--)
            {
                int swapIndex = NextInt(0, i + 1);
                (items[i], items[swapIndex]) = (items[swapIndex], items[i]);
            }
        }

        private uint NextUInt()
        {
            _state ^= _state << 13;
            _state ^= _state >> 17;
            _state ^= _state << 5;
            return _state;
        }
    }
}
