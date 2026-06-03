using System;
using System.Collections.Generic;
using System.Linq;
using StrayPathCore.Core;
using StrayPathCore.Map;
using UnityEngine;

namespace StrayPathCore.EventNodes
{
    /// <summary>
    /// 计分板系统 —— 单局结算、排行榜、XP升级、诅咒解锁。
    /// </summary>
    public class ScoreboardSystem : MonoBehaviour
    {
        public static ScoreboardSystem Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
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
            return Mathf.RoundToInt(subtotal * Mathf.Max(0.1f, multiplier));
        }

        public void ProcessRunEnd(bool victory)
        {
            var run = GameStateManager.Instance.CurrentRun;
            var account = GameStateManager.Instance.CurrentAccount;
            int score = CalculateScore();

            // XP结算
            account.RunAmount++;
            GameStateManager.Instance.AddHeroXP(run.SelectedHeroID, score);

            // 排行榜
            var entry = new ScoreEntry
            {
                Score = score,
                Time = (ulong)run.RunTime,
                CurseAmount = run.ActiveCurses.Count,
                TauntAmount = run.TauntAmount,
                Date = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                Hero = run.SelectedHeroID,
                State = victory ? 2 : 1
            };

            UpdateHighScores(entry);
            UpdateBestRuntimes(entry);

            // 诅咒解锁/征服
            if (victory)
            {
                int curseCount = run.ActiveCurses.Count;
                for (int i = 0; i <= curseCount && i < account.CurseUnlocked.Count; i++)
                {
                    if (i < account.CurseUnlocked.Count)
                        account.CurseUnlocked[i] = true;
                }
                if (curseCount > 0 && curseCount - 1 < account.CurseConquered.Count)
                    account.CurseConquered[curseCount - 1] = true;
            }

            // 存档
            GameStateManager.Instance.SaveAccountState();

            // 清除Run存档
            SaveSystem.DeleteRunSave();
            GameStateManager.Instance.ClearRunState();
        }

        private void UpdateHighScores(ScoreEntry entry)
        {
            var account = GameStateManager.Instance.CurrentAccount;
            account.HighScores.Add(entry);
            account.HighScores = account.HighScores
                .OrderByDescending(s => s.Score)
                .Take(10)
                .ToList();
        }

        private void UpdateBestRuntimes(ScoreEntry entry)
        {
            var account = GameStateManager.Instance.CurrentAccount;
            account.BestRuntimes.Add(entry);
            account.BestRuntimes = account.BestRuntimes
                .OrderBy(s => s.Time)
                .Take(10)
                .ToList();
        }

        // ==================== 成就检查 ====================

        public bool CheckAchievementHighScore() => CalculateScore() >= 1000;
        public bool CheckAchievementNormalRun()
        {
            var run = GameStateManager.Instance.CurrentRun;
            return run.ActiveCurses.Count == 0 && run.EasyHeroHP == 0 && run.EasyEnemyHP == 0 && run.EasyGold == 0;
        }
        public bool CheckAchievementCursedRun()
        {
            var run = GameStateManager.Instance.CurrentRun;
            return run.ActiveCurses.Count > 0 && run.EasyHeroHP == 0 && run.EasyEnemyHP == 0 && run.EasyGold == 0;
        }
        public bool CheckAchievementCardCollector()
        {
            return GameStateManager.Instance.CurrentRun.DeckCards.Count >= 40;
        }
        public bool CheckAchievementShopper()
        {
            // 需要外部记录购买数量
            return false;
        }
        public bool CheckAchievementHeroMastery(string heroID)
        {
            return GameStateManager.Instance.GetHeroLevel(heroID) >= 10;
        }
    }
}
