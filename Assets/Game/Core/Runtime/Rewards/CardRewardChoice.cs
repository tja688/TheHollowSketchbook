using System;
using System.Collections.Generic;
using Game.Core.Cards;
using Game.Core.Entities;
using Game.Core.Runs;

namespace Game.Core.Rewards
{
    public sealed class CardRewardChoice : Reward
    {
        private readonly List<CardModel> _choices = new List<CardModel>();

        public CardRewardChoice(IEnumerable<CardModel> choices)
        {
            if (choices == null)
            {
                throw new ArgumentNullException(nameof(choices));
            }

            _choices.AddRange(choices);
        }

        public IReadOnlyList<CardModel> Choices
        {
            get { return _choices; }
        }

        public int SelectedIndex { get; private set; } = -1;
        public bool WasSkipped { get; private set; }
        public override RewardType Type => RewardType.CardChoice;
        public override string Label => "Card Reward";

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
            if (WasSkipped || SelectedIndex < 0 || SelectedIndex >= _choices.Count)
            {
                return;
            }

            player.AddCardToDeck(_choices[SelectedIndex].CloneMutable<CardModel>());
        }
    }
}
