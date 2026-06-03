// 整改: 2026-06-03 配合 BattleStateMachine 消除 FindObjectOfType —— 添加单例模式
using System.Collections.Generic;
using StrayPathCore.Core;
using StrayPathCore.Data;
using UnityEngine;

namespace StrayPathCore.Combat
{
    /// <summary>
    /// 战斗奖励结算系统 —— 处理金币、卡牌选择与 Boss 法术奖励。
    /// </summary>
    public class CombatRewardSystem : MonoBehaviour
    {
        public static CombatRewardSystem Instance { get; private set; }

        [Header("Card Database Path")]
        [SerializeField] private string cardResourcesPath = "StrayPath/Data/Cards";

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        public void GenerateBattleRewards(int actID, bool isElite, bool isBoss)
        {
            int gold = CalculateGoldReward(actID, isElite, isBoss);
            var cards = GenerateCardRewardOptions(actID, isElite);

            Debug.Log($"[CombatRewardSystem] Generated rewards: Gold={gold}, CardOptions={cards.Count}");
        }

        public List<CardData> GenerateCardRewardOptions(int actID, bool isElite)
        {
            var allCards = LoadAllCards();
            var result = new List<CardData>();
            if (allCards.Count == 0) return result;

            var exclusions = new HashSet<int>();
            var ownedContinuous = GetOwnedContinuousCardIDs(allCards);

            foreach (var card in allCards)
            {
                bool isBasicAttack = card.Type == CardType.Attack && card.Rarity == CardRarity.Common && !card.IsRemovable;
                bool isBasicDefense = card.Type == CardType.Defense && card.Rarity == CardRarity.Common && !card.IsRemovable;
                if (isBasicAttack || isBasicDefense)
                    exclusions.Add(card.CardID);

                if (card.IsContinuous && ownedContinuous.Contains(card.CardID))
                    exclusions.Add(card.CardID);
            }

            float rareMult = 1.0f;
            if (isElite) rareMult += 0.5f;
            if (actID >= 2) rareMult += 0.2f;
            if (GameStateManager.Instance?.HasRelic(47) ?? false) rareMult += 0.3f;
            if (GameStateManager.Instance?.HasRelic(94) ?? false) rareMult += 0.3f;

            var pool = new List<CardData>();
            foreach (var card in allCards)
            {
                if (exclusions.Contains(card.CardID)) continue;

                int weight = 1;
                switch (card.Rarity)
                {
                    case CardRarity.Common:
                        weight = Mathf.RoundToInt(10f / rareMult);
                        break;
                    case CardRarity.Uncommon:
                        weight = Mathf.RoundToInt(5f * rareMult);
                        break;
                    case CardRarity.Rare:
                        weight = Mathf.RoundToInt(2f * rareMult);
                        break;
                }
                weight = Mathf.Max(1, weight);
                for (int i = 0; i < weight; i++)
                    pool.Add(card);
            }

            int targetCount = Mathf.Min(3, pool.Count);
            while (result.Count < targetCount && pool.Count > 0)
            {
                int idx = Random.Range(0, pool.Count);
                var pick = pool[idx];
                if (!result.Contains(pick))
                    result.Add(pick);
                pool.RemoveAll(c => c == pick);
            }

            return result;
        }

        public int CalculateGoldReward(int actID, bool isElite, bool isBoss)
        {
            int baseGold = 10 + actID * 5;
            if (isElite) baseGold += 25;
            if (isBoss) baseGold += 60;

            if (GameStateManager.Instance?.HasRelic(47) ?? false) baseGold += 15;
            if (GameStateManager.Instance?.HasRelic(94) ?? false) baseGold = Mathf.RoundToInt(baseGold * 1.25f);

            return baseGold;
        }

        public List<int> GenerateBossSpellRewards(int bossEnemyID)
        {
            var spells = new List<int>();
            switch (bossEnemyID)
            {
                case 38: spells.AddRange(new[] { 101, 102 }); break;
                case 50: spells.Add(103); break;
                case 66: spells.AddRange(new[] { 104, 105 }); break;
                default: spells.Add(100); break;
            }
            return spells;
        }

        public void GrantRewards()
        {
            // 由 UI/流程层在玩家完成选择后调用，执行实际发放
        }

        private List<CardData> LoadAllCards()
        {
            var cards = new List<CardData>();
            if (!string.IsNullOrEmpty(cardResourcesPath))
            {
                var loaded = Resources.LoadAll<CardData>(cardResourcesPath);
                if (loaded != null)
                    cards.AddRange(loaded);
            }
            return cards;
        }

        private HashSet<int> GetOwnedContinuousCardIDs(List<CardData> allCards)
        {
            var set = new HashSet<int>();
            var deck = GameStateManager.Instance?.CurrentRun.DeckCards;
            if (deck == null || allCards == null) return set;

            foreach (var runtime in deck)
            {
                var data = allCards.Find(c => c.CardID == runtime.CardID);
                if (data != null && data.IsContinuous)
                    set.Add(runtime.CardID);
            }
            return set;
        }
    }
}
