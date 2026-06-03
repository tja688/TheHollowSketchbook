using System.Collections.Generic;
using System.Linq;
using StrayPathCore.Core;
using StrayPathCore.Data;
using StrayPathCore.Relic;
using UnityEngine;

namespace StrayPathCore.EventNodes
{
    /// <summary>
    /// Mystery 神秘事件系统 —— 四阶段流水线：初始化→描述→选择→结果。
    /// </summary>
    public class MysteryEventSystem : MonoBehaviour
    {
        public static MysteryEventSystem Instance { get; private set; }

        [Header("Event Database")]
        [SerializeField] private List<EventData> eventDatabase = new List<EventData>();

        public EventData CurrentEvent { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        public EventData SelectEvent(int actID)
        {
            var run = GameStateManager.Instance.CurrentRun;

            // 特殊覆盖: 持有TreasureMapAct1时强制触发事件22
            if (run.TreasureMapAct1 && actID == 1)
            {
                var treasureHunt = eventDatabase.Find(e => e.EventID == 22);
                if (treasureHunt != null)
                {
                    CurrentEvent = treasureHunt;
                    return treasureHunt;
                }
            }

            // 按Act分池
            int minID = actID == 1 ? 1 : (actID == 2 ? 31 : 61);
            int maxID = actID == 1 ? 22 : (actID == 2 ? 52 : 82);
            var pool = eventDatabase.Where(e => e.EventID >= minID && e.EventID <= maxID).ToList();

            // 移除已发生事件
            pool.RemoveAll(e => run.MysteryEventHistory.Contains(e.EventID));

            // 移除占位事件
            pool.RemoveAll(e => e.EventID == 2 || e.EventID == 3 || e.EventID == 4);
            if (actID == 1) pool.RemoveAll(e => e.EventID == 22);
            if (actID == 2) pool.RemoveAll(e => e.EventID == 32 || e.EventID == 33 || e.EventID == 34);
            if (actID == 3) pool.RemoveAll(e => e.EventID == 62 || e.EventID == 63 || e.EventID == 64);

            if (pool.Count == 0) return null;

            CurrentEvent = pool[Random.Range(0, pool.Count)];
            run.MysteryEventHistory.Add(CurrentEvent.EventID);
            run.MysteryEventAmount++;
            return CurrentEvent;
        }

        public void ExecuteChoice(EventChoiceData choice)
        {
            if (choice == null) return;
            var run = GameStateManager.Instance.CurrentRun;

            switch (choice.ChoiceType)
            {
                case EventChoiceType.Combat:
                    // 切换至战斗场景
                    break;
                case EventChoiceType.Heal:
                    GameStateManager.Instance.HealHP(choice.Value);
                    break;
                case EventChoiceType.Damage:
                    GameStateManager.Instance.DamageHP(choice.Value, "event");
                    break;
                case EventChoiceType.GoldChange:
                    GameStateManager.Instance.AddGold(choice.Value, "event");
                    break;
                case EventChoiceType.CardReward:
                    // 给予指定卡牌
                    break;
                case EventChoiceType.CardRemove:
                    // 打开删卡面板
                    break;
                case EventChoiceType.CardUpgrade:
                    // 打开升级面板
                    break;
                case EventChoiceType.RelicReward:
                    RelicManager.Instance?.GiveRelicToHero(choice.RelicID);
                    break;
                case EventChoiceType.MaxHPChange:
                    GameStateManager.Instance.SetMaxHP(run.MaxHP + choice.Value);
                    break;
                case EventChoiceType.MPChange:
                    GameStateManager.Instance.SetMaxMP(run.MaxMP + choice.Value);
                    break;
                case EventChoiceType.AddCurse:
                    CurseSystem.Instance?.AddCurse(choice.Value);
                    break;
                case EventChoiceType.RemoveCurse:
                    CurseSystem.Instance?.RemoveCurse(choice.Value);
                    break;
                case EventChoiceType.Shop:
                    // 切换至商店场景
                    break;
                case EventChoiceType.Leave:
                    // 离开返回地图
                    break;
            }

            if (!string.IsNullOrEmpty(choice.SetFlag))
            {
                // 设置标志
            }
        }
    }
}
