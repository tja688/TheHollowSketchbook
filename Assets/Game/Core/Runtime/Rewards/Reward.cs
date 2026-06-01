using System;
using Game.Core.Entities;
using Game.Core.Runs;

namespace Game.Core.Rewards
{
    public abstract class Reward
    {
        public bool IsResolved { get; private set; }

        public abstract RewardType Type { get; }
        public abstract string Label { get; }

        public void Resolve(RunState run, Player player)
        {
            if (IsResolved)
            {
                return;
            }

            Apply(run, player ?? throw new ArgumentNullException(nameof(player)));
            IsResolved = true;
        }

        public void SetResolvedState(bool value)
        {
            IsResolved = value;
        }

        protected abstract void Apply(RunState run, Player player);
    }
}
