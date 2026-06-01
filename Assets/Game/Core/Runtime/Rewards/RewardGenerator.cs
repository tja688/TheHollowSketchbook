using System;
using System.Collections.Generic;
using Game.Core.Cards;
using Game.Core.Models;
using Game.Core.Random;
using Game.Core.Runs;

namespace Game.Core.Rewards
{
    public static class RewardGenerator
    {
        private static readonly ModelId[] CardRewardPool =
        {
            new ModelId("Card", "Strike"),
            new ModelId("Card", "Defend"),
            new ModelId("Card", "Bash"),
            new ModelId("Card", "ZapDebug"),
            new ModelId("Card", "GuardDebug")
        };

        public static IReadOnlyList<Reward> GenerateCombatRewards(RunState run, bool isElite, bool isBoss)
        {
            if (run == null)
            {
                throw new ArgumentNullException(nameof(run));
            }

            List<Reward> rewards = new List<Reward>
            {
                new GoldReward(isBoss ? 80 : isElite ? 40 : 20),
                new CardRewardChoice(GenerateCardChoices(run.Rng, 3))
            };

            return rewards;
        }

        public static IReadOnlyList<Reward> GenerateTreasureRewards(RunState run)
        {
            if (run == null)
            {
                throw new ArgumentNullException(nameof(run));
            }

            return new Reward[]
            {
                new GoldReward(60),
                new CardRewardChoice(GenerateCardChoices(run.Rng, 3))
            };
        }

        private static IReadOnlyList<CardModel> GenerateCardChoices(IRng rng, int count)
        {
            List<ModelId> pool = new List<ModelId>(CardRewardPool);
            rng.Shuffle(pool);
            List<CardModel> cards = new List<CardModel>(count);
            for (int i = 0; i < count && i < pool.Count; i++)
            {
                cards.Add(ModelDb.CreateMutable<CardModel>(pool[i]));
            }

            return cards;
        }
    }
}
