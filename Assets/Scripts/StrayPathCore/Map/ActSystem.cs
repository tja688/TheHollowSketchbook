using StrayPathCore.Core;
using UnityEngine;

namespace StrayPathCore.Map
{
    /// <summary>
    /// Act 推进系统 —— 管理3个Act的切换与跨Act状态继承。
    /// </summary>
    public class ActSystem : MonoBehaviour
    {
        public static ActSystem Instance { get; private set; }

        public int CurrentAct => GameStateManager.Instance.CurrentRun.Act;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        public void AdvanceAct()
        {
            var run = GameStateManager.Instance.CurrentRun;
            run.Act++;

            if (run.Act > 3)
            {
                // 通关
                GameEventBus.Instance.Publish(new RunCompletedEvent { Victory = true, FinalScore = CalculateScore() });
                return;
            }

            // 重置Act相关状态
            ResetForNewAct();

            // 生成新Act地图
            var map = MapGenerator.Instance?.GenerateMap(run.Act);
            if (map != null)
            {
                MapGenerator.Instance?.SaveMapToRunState(map, run.Act);
            }

            GameEventBus.Instance.Publish(new ActCompletedEvent { CompletedAct = run.Act - 1 });
        }

        public void ResetForNewAct()
        {
            var run = GameStateManager.Instance.CurrentRun;
            run.CurrentPID = 0;
            run.CurrentPlayerPathGroup = 0;
            run.MapScroll = 0;
            // 保留: HP/MP/金币/卡组/遗物/诅咒/英雄等级
            // 重置: 路径历史（可选：是否保留？设计文档说"保留路径历史"，但Act切换时重置PID/PG）
            // 实际上设计文档说"保留路径历史"，这里我们保留它用于展示
        }

        public bool IsFinalActComplete()
        {
            return CurrentAct > 3;
        }

        public string GetActTheme(int actID)
        {
            return actID switch
            {
                1 => "The Forest",
                2 => "The Castle",
                3 => "The Caverns",
                _ => "Unknown"
            };
        }

        public string GetActMusic(int actID)
        {
            return $"ACT{actID}";
        }

        public string GetActBackground(int actID, string heroID)
        {
            return $"{heroID}_Act{actID}";
        }

        public int CalculateScore()
        {
            var run = GameStateManager.Instance.CurrentRun;
            int subtotal = run.NormalBattleAmount * 10
                         + run.HardBattleAmount * 25
                         + run.BossBattleAmount * 50
                         + run.MysteryEventAmount * 15
                         + run.TauntAmount * 25
                         + Mathf.RoundToInt(run.Gold * 0.1f);

            int modifier = run.LesserCurseAmount * 10
                         + run.ModerateCurseAmount * 20
                         + run.GreaterCurseAmount * 30
                         - (run.EasyHeroHP + run.EasyEnemyHP + run.EasyGold) * 50;

            float multiplier = (modifier + 100f) / 100f;
            return Mathf.RoundToInt(subtotal * multiplier);
        }
    }
}
