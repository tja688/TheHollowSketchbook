using Game.Core.Entities;
using Game.Core.Runs;

namespace Game.Core.Rewards
{
    public sealed class GoldReward : Reward
    {
        public GoldReward(int amount)
        {
            Amount = amount;
        }

        public int Amount { get; }
        public override RewardType Type => RewardType.Gold;
        public override string Label => Amount + " Gold";

        protected override void Apply(RunState run, Player player)
        {
            player.GainGold(Amount);
        }
    }
}
