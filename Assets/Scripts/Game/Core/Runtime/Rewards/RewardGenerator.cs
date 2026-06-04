using System;
using System.Collections.Generic;
using Game.Core.Random;
using Game.Core.Runs;

namespace Game.Core.Rewards
{
    /// <summary>
    /// Reward generator. StS CardModel dependency removed.
    /// BOUNDARY: Card rewards replaced with generic ChoiceReward placeholders.
    /// Extend this class to generate grid-specific rewards (items, relics, keywords, stat boosts).
    /// </summary>
    public static class RewardGenerator
    {
        public static IReadOnlyList<Reward> GenerateCombatRewards(RunState run, bool isElite, bool isBoss)
        {
            if (run == null)
            {
                throw new ArgumentNullException(nameof(run));
            }

            List<Reward> rewards = new List<Reward>
            {
                new GoldReward(isBoss ? 80 : isElite ? 40 : 20),
                new ChoiceReward(GenerateChoiceLabels(run.Rng, 3))
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
                new ChoiceReward(GenerateChoiceLabels(run.Rng, 3))
            };
        }

        private static IReadOnlyList<string> GenerateChoiceLabels(IRng rng, int count)
        {
            // BOUNDARY: Placeholder choice labels. Replace with actual reward pool logic.
            string[] pool = { "Option A", "Option B", "Option C", "Option D", "Option E" };
            List<string> result = new List<string>(count);
            for (int i = 0; i < count && i < pool.Length; i++)
            {
                result.Add(pool[i]);
            }
            return result;
        }
    }
}
