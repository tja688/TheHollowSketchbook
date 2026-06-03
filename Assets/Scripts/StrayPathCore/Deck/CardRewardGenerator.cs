using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using StrayPathCore.Core;
using StrayPathCore.Data;

namespace StrayPathCore.Deck
{
    public class CardRewardGenerator : MonoBehaviour
    {
        public static CardRewardGenerator Instance { get; private set; }

        private static List<CardData> _allCards;
        private static Dictionary<int, CardData> _cardDataCache;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private static void EnsureCache()
        {
            if (_allCards != null) return;
            _allCards = new List<CardData>(Resources.LoadAll<CardData>(""));
            _cardDataCache = new Dictionary<int, CardData>();
            foreach (var cd in _allCards)
            {
                if (cd != null && !_cardDataCache.ContainsKey(cd.CardID))
                    _cardDataCache[cd.CardID] = cd;
            }
        }

        public List<CardData> GenerateRewardOptions(int actID, bool isElite, bool isBoss, int optionCount = 3)
        {
            EnsureCache();

            if (GameStateManager.Instance != null && GameStateManager.Instance.HasRelic(94))
                optionCount = 2;

            var ownedCards = GameStateManager.Instance?.CurrentRun?.DeckCards ?? new List<CardRuntime>();
            var result = new List<CardData>();
            var usedIDs = new HashSet<int>();

            int attempts = 0;
            while (result.Count < optionCount && attempts < 100)
            {
                attempts++;
                CardRarity rarity = RollRarity(actID, isElite);
                var pool = GetCardsByRarity(rarity);
                pool = FilterExcludedCards(pool, ownedCards);
                pool = pool.Where(c => !usedIDs.Contains(c.CardID)).ToList();
                if (pool.Count == 0) continue;

                var pick = pool[UnityEngine.Random.Range(0, pool.Count)];
                usedIDs.Add(pick.CardID);
                result.Add(pick);
            }

            return result;
        }

        public CardRarity RollRarity(int actID, bool isElite)
        {
            float rareChance = 0.05f;
            float uncommonChance = 0.25f;

            if (actID == 1) rareChance = 0.05f;
            else if (actID == 2) rareChance = 0.08f;
            else if (actID >= 3) rareChance = 0.12f;

            if (isElite)
            {
                rareChance += 0.05f;
                uncommonChance += 0.10f;
            }

            if (GameStateManager.Instance != null && GameStateManager.Instance.HasRelic(47))
            {
                rareChance += 0.05f;
                uncommonChance += 0.05f;
            }

            float roll = UnityEngine.Random.value;
            if (roll < rareChance) return CardRarity.Rare;
            if (roll < rareChance + uncommonChance) return CardRarity.Uncommon;
            return CardRarity.Common;
        }

        public List<CardData> GetCardsByRarity(CardRarity rarity)
        {
            EnsureCache();
            return _allCards.Where(c => c.Rarity == rarity).ToList();
        }

        public List<CardData> FilterExcludedCards(List<CardData> pool, List<CardRuntime> ownedCards)
        {
            if (pool == null) return new List<CardData>();

            var ownedContinuousIDs = new HashSet<int>();
            if (ownedCards != null)
            {
                EnsureCache();
                foreach (var oc in ownedCards)
                {
                    if (_cardDataCache.TryGetValue(oc.CardID, out var data) && data.IsContinuous)
                        ownedContinuousIDs.Add(oc.CardID);
                }
            }

            var excluded = new HashSet<int> { 1, 2 };

            return pool.Where(c =>
                !ownedContinuousIDs.Contains(c.CardID) &&
                !excluded.Contains(c.CardID)
            ).ToList();
        }

        public void GrantCardReward(CardData cardData, bool isUpgraded = false)
        {
            if (cardData == null) return;
            int nextCopy = 1;
            var deck = GameStateManager.Instance?.CurrentRun?.DeckCards;
            if (deck != null)
            {
                foreach (var c in deck)
                {
                    if (c.CardID == cardData.CardID && c.CopyCount >= nextCopy)
                        nextCopy = c.CopyCount + 1;
                }
            }

            var runtime = new CardRuntime
            {
                CardID = cardData.CardID,
                CopyCount = nextCopy,
                IsUpgraded = isUpgraded,
                IsFake = false
            };

            GameStateManager.Instance?.AddCardToDeck(runtime);
            DeckManager.Instance?.DrawPile?.Add(runtime);
        }
    }
}
