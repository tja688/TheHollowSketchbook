using System.Collections.Generic;
using UnityEngine;

namespace StrayPathCore.Utils
{
    /// <summary>
    /// PRD 伪随机分布计算器 —— 20步周期伪随机，防连发/保底。
    /// 替代原 BijmProbabilityCalculator。
    /// </summary>
    public class PRDCalculator
    {
        private class PRDState
        {
            public int Step;           // 当前步数 1-20
            public int TotalTriggers;  // 本周期总触发次数
            public int LastTriggerStep;// 上次触发步数
            public List<int> BlockTriggers = new List<int>(); // 每 Block 触发次数
        }

        private readonly Dictionary<string, PRDState> _states = new Dictionary<string, PRDState>();

        /// <summary>
        /// 概率判定。probability 为 5/10/15/20/25/30 档位。
        /// </summary>
        public bool Roll(string effectName, int probability)
        {
            probability = RoundToNearest5(probability);
            if (!_states.TryGetValue(effectName, out var state))
            {
                state = new PRDState();
                _states[effectName] = state;
            }

            state.Step = state.Step % 20 + 1;
            int blockIndex = (state.Step - 1) / 5; // 0-3

            // 确保 BlockTriggers 长度
            while (state.BlockTriggers.Count <= blockIndex)
                state.BlockTriggers.Add(0);

            bool result = Evaluate(state, probability, blockIndex);

            if (result)
            {
                state.TotalTriggers++;
                state.LastTriggerStep = state.Step;
                state.BlockTriggers[blockIndex]++;
            }

            // 第20步重置
            if (state.Step == 20)
            {
                state.Step = 0;
                state.TotalTriggers = 0;
                state.LastTriggerStep = 0;
                state.BlockTriggers.Clear();
            }

            return result;
        }

        private bool Evaluate(PRDState state, int probability, int blockIndex)
        {
            // 防连发：25%/30% 禁止连续触发
            if ((probability == 25 || probability == 30) && state.LastTriggerStep == state.Step - 1)
                return false;

            switch (probability)
            {
                case 5:
                    // 每 Block 最多1次，Step19保底
                    if (state.BlockTriggers[blockIndex] >= 1) return false;
                    if (state.Step == 19 && state.TotalTriggers == 0) return true;
                    return Random.value < 0.05f;

                case 10:
                    // 每 Block 最多1次，总次数<2，Step8/18保底
                    if (state.BlockTriggers[blockIndex] >= 1) return false;
                    if (state.TotalTriggers >= 2) return false;
                    if (state.Step == 8 && state.TotalTriggers == 0) return true;
                    if (state.Step == 18 && state.TotalTriggers < 2) return true;
                    return Random.value < 0.10f;

                case 15:
                    // 每 Block 最多1次，总次数<3，Step8/13/18保底
                    if (state.BlockTriggers[blockIndex] >= 1) return false;
                    if (state.TotalTriggers >= 3) return false;
                    if (state.Step == 8 && state.TotalTriggers == 0) return true;
                    if (state.Step == 13 && state.TotalTriggers < 2) return true;
                    if (state.Step == 18 && state.TotalTriggers < 3) return true;
                    return Random.value < 0.15f;

                case 20:
                    // 每 Block 最多1次，总次数<4，Step4/9/14/19保底
                    if (state.BlockTriggers[blockIndex] >= 1) return false;
                    if (state.TotalTriggers >= 4) return false;
                    if (state.Step == 4 && state.TotalTriggers == 0) return true;
                    if (state.Step == 9 && state.TotalTriggers < 2) return true;
                    if (state.Step == 14 && state.TotalTriggers < 3) return true;
                    if (state.Step == 19 && state.TotalTriggers < 4) return true;
                    return Random.value < 0.20f;

                case 25:
                    // 最多5次，不连续，复杂保底
                    if (state.TotalTriggers >= 5) return false;
                    if (state.Step >= 16 && state.TotalTriggers < 4) return true;
                    if (state.Step % 5 == 0 && state.BlockTriggers[blockIndex] == 0) return true;
                    return Random.value < 0.25f;

                case 30:
                    // 最多6次，不连续，复杂保底
                    if (state.TotalTriggers >= 6) return false;
                    if (state.Step >= 15 && state.TotalTriggers < 5) return true;
                    if (state.Step % 4 == 0 && state.BlockTriggers[blockIndex] == 0) return true;
                    return Random.value < 0.30f;

                default:
                    return Random.value < (probability / 100f);
            }
        }

        private int RoundToNearest5(int value)
        {
            return Mathf.RoundToInt(value / 5f) * 5;
        }

        public void Reset(string effectName)
        {
            _states.Remove(effectName);
        }

        public void ResetAll()
        {
            _states.Clear();
        }
    }
}
