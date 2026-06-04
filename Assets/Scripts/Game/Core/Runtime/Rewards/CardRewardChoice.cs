using System;
using System.Collections.Generic;
using Game.Core.Entities;
using Game.Core.Runs;

namespace Game.Core.Rewards
{
    /// <summary>
    /// Generic choice reward. StS CardModel dependency removed.
    /// BOUNDARY: Replaced CardModel choices with string labels.
    /// Extend this class to add typed choices (ItemModel, RelicModel, KeywordModel) for the grid-based system.
    /// </summary>
    public sealed class ChoiceReward : Reward
    {
        private readonly List<string> _choices = new List<string>();

        public ChoiceReward(IEnumerable<string> choices)
        {
            if (choices == null)
            {
                throw new ArgumentNullException(nameof(choices));
            }

            _choices.AddRange(choices);
        }

        public IReadOnlyList<string> Choices
        {
            get { return _choices; }
        }

        public int SelectedIndex { get; private set; } = -1;
        public bool WasSkipped { get; private set; }
        public override RewardType Type => RewardType.Choice;
        public override string Label => "Choice Reward";

        public bool CanSelect(int index)
        {
            return index >= 0 && index < _choices.Count;
        }

        public void Select(int index)
        {
            if (!CanSelect(index))
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            SelectedIndex = index;
            WasSkipped = false;
        }

        public void Skip()
        {
            SelectedIndex = -1;
            WasSkipped = true;
        }

        protected override void Apply(RunState run, Player player)
        {
            // BOUNDARY: Override or extend this method to apply the chosen reward.
            // Currently a no-op placeholder until the grid-based reward system is built.
        }
    }
}
