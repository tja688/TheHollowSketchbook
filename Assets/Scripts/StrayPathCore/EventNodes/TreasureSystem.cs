using StrayPathCore.Core;
using StrayPathCore.Data;
using StrayPathCore.Relic;
using UnityEngine;

namespace StrayPathCore.EventNodes
{
    /// <summary>
    /// 宝藏系统 —— 固定收益：遗物 + 金币。
    /// </summary>
    public class TreasureSystem : MonoBehaviour
    {
        public static TreasureSystem Instance { get; private set; }

        public int LastGoldReward { get; private set; }
        public RelicData LastRelicReward { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        public void OpenTreasure()
        {
            var run = GameStateManager.Instance.CurrentRun;

            // 遗物
            var relicManager = RelicManager.Instance;
            bool canGetRelic = true;

            // 诅咒7: 宝箱不再包含遗物
            var curse = CurseSystem.Instance;
            if (curse != null && !curse.CanGetRelicFromTreasure())
                canGetRelic = false;

            // 遗物98: Cloak of the Collector 跳过遗物
            if (GameStateManager.Instance.HasRelic(98))
                canGetRelic = false;

            if (canGetRelic)
            {
                LastRelicReward = relicManager?.ReturnTreasureRelic();
                if (LastRelicReward != null)
                {
                    relicManager?.GiveRelicToHero(LastRelicReward);
                }
            }

            // 金币
            int baseGold = Random.Range(90, 111);

            // 遗物51: Key of Prosperity 金币×2
            if (GameStateManager.Instance.HasRelic(51)) baseGold *= 2;

            // 遗物52: Crown of Wealth 金币×1.2
            if (GameStateManager.Instance.HasRelic(52)) baseGold = Mathf.RoundToInt(baseGold * 1.2f);

            // 诅咒9: 金币×0.8
            if (curse != null) baseGold = Mathf.RoundToInt(baseGold * curse.GetGoldMultiplier());

            // 遗物92: Tainted Gift 金币×0.5
            if (GameStateManager.Instance.HasRelic(92)) baseGold = Mathf.RoundToInt(baseGold * 0.5f);

            LastGoldReward = baseGold;
            GameStateManager.Instance.AddGold(LastGoldReward, "treasure");
        }
    }
}
