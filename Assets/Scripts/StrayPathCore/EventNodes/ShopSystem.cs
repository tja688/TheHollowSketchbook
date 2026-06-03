using System.Collections.Generic;
using System.Linq;
using StrayPathCore.Core;
using StrayPathCore.Data;
using StrayPathCore.Deck;
using StrayPathCore.Relic;
using UnityEngine;

namespace StrayPathCore.EventNodes
{
    /// <summary>
    /// 商店系统 —— 卡牌区、遗物区、升级服务。
    /// </summary>
    public class ShopSystem : MonoBehaviour
    {
        public static ShopSystem Instance { get; private set; }

        [Header("Current Shop")]
        public List<ShopCardOffer> CardOffers = new List<ShopCardOffer>();
        public List<ShopRelicOffer> RelicOffers = new List<ShopRelicOffer>();
        public int CurrentUpgradeCost = 75;

        [Header("Databases")]
        [SerializeField] private List<CardData> cardDatabase = new List<CardData>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        public void GenerateShop()
        {
            CardOffers.Clear();
            RelicOffers.Clear();

            int actID = GameStateManager.Instance.CurrentRun.Act;

            // === 卡牌区: 5张明牌 + 1张问号卡 ===
            for (int i = 0; i < 5; i++)
            {
                var rarity = RollCardRarity(actID);
                var pool = cardDatabase.Where(c => c.Rarity == rarity && !c.IsContinuous).ToList();
                if (pool.Count == 0) pool = cardDatabase.Where(c => c.Rarity == CardRarity.Common).ToList();
                if (pool.Count > 0)
                {
                    var card = pool[Random.Range(0, pool.Count)];
                    int price = CalculateCardPrice(card, i == 2); // 第3张半价
                    CardOffers.Add(new ShopCardOffer { Card = card, Price = price, IsSoldOut = false, IsMystery = false });
                }
            }
            // 问号卡
            int mysteryPrice = GameStateManager.Instance.CurrentGold > 150 ? 60 : 50;
            CardOffers.Add(new ShopCardOffer { Card = null, Price = mysteryPrice, IsSoldOut = false, IsMystery = true });

            // === 遗物区: 3个 ===
            var relicManager = RelicManager.Instance;
            var relics = relicManager?.Return3UniqueShopRelics(actID);
            if (relics != null)
            {
                foreach (var relic in relics)
                {
                    int price = relicManager?.GetRelicPrice(relic) ?? relic.BasePrice;
                    RelicOffers.Add(new ShopRelicOffer { Relic = relic, Price = price, IsSoldOut = false });
                }
            }

            // === 升级服务 ===
            CurrentUpgradeCost = 75;
            if (GameStateManager.Instance.HasRelic(99)) CurrentUpgradeCost = 150;
        }

        public bool BuyCard(ShopCardOffer offer)
        {
            if (offer == null || offer.IsSoldOut) return false;
            if (!GameStateManager.Instance.SpendGold(offer.Price, "shop_card")) return false;

            if (offer.IsMystery)
            {
                // 随机揭示一张卡
                var pool = cardDatabase.Where(c => c.Rarity != CardRarity.Rare).ToList();
                var card = pool[Random.Range(0, pool.Count)];
                offer.Card = card;
            }

            var runtime = new CardRuntime
            {
                CardID = offer.Card.CardID,
                CopyCount = GetNextCopyCount(offer.Card.CardID),
                IsUpgraded = false
            };
            GameStateManager.Instance.AddCardToDeck(runtime);
            offer.IsSoldOut = true;
            return true;
        }

        public bool BuyRelic(ShopRelicOffer offer)
        {
            if (offer == null || offer.IsSoldOut) return false;
            if (!GameStateManager.Instance.SpendGold(offer.Price, "shop_relic")) return false;

            RelicManager.Instance?.GiveRelicToHero(offer.Relic);
            offer.IsSoldOut = true;
            return true;
        }

        public bool UpgradeCard(CardRuntime card)
        {
            if (card == null) return false;
            int cost = CurrentUpgradeCost;
            // 遗物23: Incognito Mask 锁定价格
            if (!GameStateManager.Instance.HasRelic(23))
            {
                CurrentUpgradeCost += 25;
            }
            if (!GameStateManager.Instance.SpendGold(cost, "shop_upgrade")) return false;

            DeckManager.Instance?.UpgradeCard(card);
            return true;
        }

        private int CalculateCardPrice(CardData card, bool isHalfPrice)
        {
            int basePrice = card.BasePrice;
            switch (card.Rarity)
            {
                case CardRarity.Common: basePrice += Random.Range(-10, 6); break;
                case CardRarity.Uncommon: basePrice += Random.Range(-5, 11); break;
                case CardRarity.Rare: basePrice += Random.Range(-10, 11); break;
            }
            if (isHalfPrice) basePrice = Mathf.RoundToInt(basePrice * 0.5f);

            // 全局修正
            var curse = CurseSystem.Instance;
            if (curse != null) basePrice = Mathf.RoundToInt(basePrice * curse.GetShopPriceMultiplier());
            if (GameStateManager.Instance.HasRelic(81)) basePrice = Mathf.RoundToInt(basePrice * 0.65f);

            return Mathf.Max(1, basePrice);
        }

        private CardRarity RollCardRarity(int actID)
        {
            float roll = Random.value;
            float rareChance = actID * 0.05f;
            if (roll < rareChance) return CardRarity.Rare;
            if (roll < rareChance + 0.25f) return CardRarity.Uncommon;
            return CardRarity.Common;
        }

        private int GetNextCopyCount(int cardID)
        {
            var cards = GameStateManager.Instance.CurrentRun.DeckCards;
            int max = 0;
            foreach (var c in cards)
                if (c.CardID == cardID && c.CopyCount > max)
                    max = c.CopyCount;
            return max + 1;
        }
    }

    public class ShopCardOffer
    {
        public CardData Card;
        public int Price;
        public bool IsSoldOut;
        public bool IsMystery;
    }

    public class ShopRelicOffer
    {
        public RelicData Relic;
        public int Price;
        public bool IsSoldOut;
    }
}
