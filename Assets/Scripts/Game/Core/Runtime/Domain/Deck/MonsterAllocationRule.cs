using System;
using System.Collections.Generic;
using System.Linq;
using Game.Core.Random;

namespace Game.Core.Domain.Deck
{
    public sealed class MonsterAllocationRule
    {
        private readonly List<MonsterTierRange> _ranges;

        public MonsterAllocationRule(int nodeIndex, IEnumerable<MonsterTierRange> ranges)
        {
            if (nodeIndex < 1 || nodeIndex > 9)
            {
                throw new ArgumentOutOfRangeException(nameof(nodeIndex));
            }

            if (ranges == null)
            {
                throw new ArgumentNullException(nameof(ranges));
            }

            NodeIndex = nodeIndex;
            _ranges = ranges.OrderBy(range => range.Level).ToList();
            if (_ranges.Count == 0)
            {
                throw new ArgumentException("Monster allocation rule requires at least one range.", nameof(ranges));
            }
        }

        public int NodeIndex { get; }

        public IReadOnlyList<MonsterTierRange> Ranges
        {
            get { return _ranges; }
        }

        public IReadOnlyDictionary<int, int> AllocateCounts(int layerIndex, IRng rng)
        {
            if (layerIndex < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(layerIndex));
            }

            if (rng == null)
            {
                throw new ArgumentNullException(nameof(rng));
            }

            int target = 9 + layerIndex;
            Dictionary<int, int> counts = new Dictionary<int, int>();
            Dictionary<int, int> minimums = new Dictionary<int, int>();
            for (int i = 0; i < _ranges.Count; i++)
            {
                MonsterTierRange range = _ranges[i];
                counts[range.Level] = rng.NextInt(range.Min, range.Max + 1);
                minimums[range.Level] = range.Min;
            }

            while (counts.Values.Sum() < target)
            {
                for (int i = _ranges.Count - 1; i >= 0 && counts.Values.Sum() < target; i--)
                {
                    counts[_ranges[i].Level]++;
                }
            }

            while (counts.Values.Sum() > target && TryReduce(counts, minimums, ascending: true))
            {
            }

            while (counts.Values.Sum() > target && TryReduce(counts, null, ascending: false))
            {
            }

            return counts;
        }

        public static MonsterAllocationRule ForNode(int nodeIndex)
        {
            switch (nodeIndex)
            {
                case 1:
                    return new MonsterAllocationRule(1, new[] { new MonsterTierRange(1, 6, 7), new MonsterTierRange(2, 2, 3) });
                case 2:
                    return new MonsterAllocationRule(2, new[] { new MonsterTierRange(1, 5, 6), new MonsterTierRange(2, 3, 4) });
                case 3:
                    return new MonsterAllocationRule(3, new[] { new MonsterTierRange(1, 2, 3), new MonsterTierRange(2, 3, 4), new MonsterTierRange(3, 2, 3) });
                case 4:
                    return new MonsterAllocationRule(4, new[] { new MonsterTierRange(1, 1, 2), new MonsterTierRange(2, 3, 4), new MonsterTierRange(3, 3, 4) });
                case 5:
                    return new MonsterAllocationRule(5, new[] { new MonsterTierRange(1, 1, 1), new MonsterTierRange(2, 3, 4), new MonsterTierRange(3, 3, 4), new MonsterTierRange(4, 1, 1) });
                case 6:
                    return new MonsterAllocationRule(6, new[] { new MonsterTierRange(1, 0, 1), new MonsterTierRange(2, 2, 3), new MonsterTierRange(3, 3, 4), new MonsterTierRange(4, 1, 4) });
                case 7:
                case 9:
                    return new MonsterAllocationRule(nodeIndex, new[] { new MonsterTierRange(2, 1, 2), new MonsterTierRange(3, 3, 4), new MonsterTierRange(4, 3, 5) });
                default:
                    return new MonsterAllocationRule(nodeIndex, new[] { new MonsterTierRange(1, 5, 6), new MonsterTierRange(2, 3, 4) });
            }
        }

        private bool TryReduce(Dictionary<int, int> counts, Dictionary<int, int> minimums, bool ascending)
        {
            IEnumerable<MonsterTierRange> ranges = ascending ? _ranges : _ranges.OrderByDescending(range => range.Level);
            foreach (MonsterTierRange range in ranges)
            {
                int minimum = minimums != null ? minimums[range.Level] : 0;
                if (counts[range.Level] > minimum)
                {
                    counts[range.Level]--;
                    return true;
                }
            }

            return false;
        }
    }

    public sealed class MonsterTierRange
    {
        public MonsterTierRange(int level, int min, int max)
        {
            if (level < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(level));
            }

            if (min < 0 || max < min)
            {
                throw new ArgumentOutOfRangeException(nameof(min));
            }

            Level = level;
            Min = min;
            Max = max;
        }

        public int Level { get; }
        public int Min { get; }
        public int Max { get; }
    }
}
