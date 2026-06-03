using System.Collections.Generic;
using StrayPathCore.Core;
using UnityEngine;

namespace StrayPathCore.Relic
{
    /// <summary>
    /// 诅咒系统 —— 管理运行时诅咒与腐化诅咒。
    /// </summary>
    public class CurseSystem : MonoBehaviour
    {
        public static CurseSystem Instance { get; private set; }

        public List<int> ActiveCurses => GameStateManager.Instance.CurrentRun.ActiveCurses;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        public void AddCurse(int curseID)
        {
            if (!ActiveCurses.Contains(curseID))
            {
                ActiveCurses.Add(curseID);
                if (curseID <= 5) GameStateManager.Instance.CurrentRun.LesserCurseAmount++;
                else if (curseID <= 10) GameStateManager.Instance.CurrentRun.ModerateCurseAmount++;
                else GameStateManager.Instance.CurrentRun.GreaterCurseAmount++;
            }
        }

        public void RemoveCurse(int curseID)
        {
            if (ActiveCurses.Remove(curseID))
            {
                if (curseID <= 5) GameStateManager.Instance.CurrentRun.LesserCurseAmount--;
                else if (curseID <= 10) GameStateManager.Instance.CurrentRun.ModerateCurseAmount--;
                else GameStateManager.Instance.CurrentRun.GreaterCurseAmount--;
            }
        }

        public bool HasCurse(int curseID)
        {
            return ActiveCurses.Contains(curseID);
        }

        // ==================== 诅咒效果查询 ====================

        public float GetGoldMultiplier()
        {
            float mult = 1.0f;
            if (HasCurse(1)) mult *= 0.5f;  // 金币-50%
            if (HasCurse(9)) mult *= 0.8f;  // 金币×0.8
            return mult;
        }

        public float GetShopPriceMultiplier()
        {
            float mult = 1.0f;
            if (HasCurse(3)) mult *= 1.1f;  // 商店价格+10%
            return mult;
        }

        public float GetCampfireHealMultiplier()
        {
            float mult = 1.0f;
            if (HasCurse(5)) mult = 0.35f / 0.4f; // 从40%降至35%
            return mult;
        }

        public int GetCardRewardCount(int baseCount)
        {
            if (HasCurse(3)) return Mathf.Max(1, baseCount - 1);
            return baseCount;
        }

        public int GetMPCostIncrease()
        {
            if (HasCurse(2)) return 1;
            return 0;
        }

        public bool CanGetRelicFromTreasure()
        {
            return !HasCurse(7); // 诅咒7: 宝箱不再包含遗物
        }

        public int GetUpgradeCost(int baseCost)
        {
            if (HasCurse(8)) return baseCost * 2;
            return baseCost;
        }

        // ==================== 腐化诅咒 ====================

        public void ApplyCorruptionCurse()
        {
            if (!HasCurse(6)) return;
            var options = GenerateCorruptionOptions();
            // 三选一负面效果（由UI层处理选择）
            Debug.Log($"[Curse] 腐化诅咒触发，选项: {string.Join(", ", options)}");
        }

        public List<int> GenerateCorruptionOptions()
        {
            var pool = new List<int> { 1, 2, 3, 4, 5, 7, 8, 9 };
            pool.RemoveAll(c => ActiveCurses.Contains(c));
            var result = new List<int>();
            while (result.Count < 3 && pool.Count > 0)
            {
                int idx = Random.Range(0, pool.Count);
                result.Add(pool[idx]);
                pool.RemoveAt(idx);
            }
            return result;
        }
    }
}
