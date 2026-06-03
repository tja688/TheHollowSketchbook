// 整改: 2026-06-03 修复了 FindObjectsOfType 滥用 —— 使用 BattleStateMachine 查询接口获取目标敌人
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using StrayPathCore.Core;
using StrayPathCore.Data;

namespace StrayPathCore.Deck
{
    public class DeckManager : MonoBehaviour
    {
        public static DeckManager Instance { get; private set; }

        public List<CardRuntime> DrawPile { get; private set; } = new List<CardRuntime>();
        public List<CardRuntime> Hand { get; private set; } = new List<CardRuntime>();
        public List<CardRuntime> DiscardPile { get; private set; } = new List<CardRuntime>();
        public List<CardRuntime> BanishPile { get; private set; } = new List<CardRuntime>();
        public List<CardRuntime> HoldPile { get; private set; } = new List<CardRuntime>();

        private bool _meditateActive = false;
        private readonly Dictionary<string, int> _banishChargeCounters = new Dictionary<string, int>();
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

        private static CardData GetCardData(int cardID)
        {
            if (_cardDataCache == null)
            {
                _cardDataCache = new Dictionary<int, CardData>();
                var all = Resources.LoadAll<CardData>("");
                foreach (var cd in all)
                {
                    if (cd != null && !_cardDataCache.ContainsKey(cd.CardID))
                        _cardDataCache[cd.CardID] = cd;
                }
            }
            _cardDataCache.TryGetValue(cardID, out var data);
            return data;
        }

        public void InitializeDeck(List<CardRuntime> deckCards)
        {
            DrawPile.Clear();
            Hand.Clear();
            DiscardPile.Clear();
            BanishPile.Clear();
            HoldPile.Clear();
            _banishChargeCounters.Clear();

            if (deckCards != null)
            {
                foreach (var card in deckCards)
                {
                    if (card != null && !card.IsFake)
                        DrawPile.Add(card);
                }
            }
            Shuffle(DrawPile);
        }

        public void DrawCards(int count, bool checkRelic65 = false)
        {
            int cardsToDraw = count;
            if (checkRelic65 && GameStateManager.Instance != null && GameStateManager.Instance.HasRelic(65))
                cardsToDraw++;

            int drawn = 0;
            while (drawn < cardsToDraw)
            {
                if (DrawPile.Count == 0)
                {
                    if (DiscardPile.Count > 0)
                        RefillPlayerDeckFromDiscardPile();
                    else
                        break;
                }
                if (DrawPile.Count == 0) break;

                var card = DrawPile[0];
                DrawPile.RemoveAt(0);
                Hand.Add(card);
                drawn++;

                GameEventBus.Instance.Publish(new CardDrawnEvent
                {
                    CardID = card.CardID,
                    CopyCount = card.CopyCount,
                    SourcePile = "DrawPile"
                });

                var data = GetCardData(card.CardID);
                if (data != null && data.Type == CardType.Curse)
                {
                    if (_meditateActive)
                        DrawCards(1);
                    if (GameStateManager.Instance != null && GameStateManager.Instance.BattleState.ArcaneTrance)
                        AddFakeCardToPlayerHand(0, card.IsUpgraded);
                }
            }
        }

        public CardRuntime GetNextCardFromDeck()
        {
            if (DrawPile.Count == 0 && DiscardPile.Count > 0)
                RefillPlayerDeckFromDiscardPile();
            if (DrawPile.Count == 0) return null;
            var card = DrawPile[0];
            DrawPile.RemoveAt(0);
            return card;
        }

        public void RefillPlayerDeckFromDiscardPile()
        {
            if (DiscardPile.Count == 0) return;
            DrawPile.AddRange(DiscardPile);
            DiscardPile.Clear();
            Shuffle(DrawPile);
            GameEventBus.Instance.Publish(new DeckShuffledEvent { SourcePile = "DiscardToDraw" });
        }

        public void RefillPlayerDeckFromAllPiles()
        {
            List<CardRuntime> allCards = new List<CardRuntime>();
            allCards.AddRange(DrawPile);
            allCards.AddRange(Hand);
            allCards.AddRange(DiscardPile);
            allCards.AddRange(HoldPile);
            allCards.AddRange(BanishPile);

            DrawPile.Clear();
            Hand.Clear();
            DiscardPile.Clear();
            HoldPile.Clear();
            BanishPile.Clear();

            foreach (var c in allCards)
            {
                if (c != null && !c.IsFake)
                    DrawPile.Add(c);
            }

            Shuffle(DrawPile);
            GameStateManager.Instance?.SetDeckCards(new List<CardRuntime>(DrawPile));
            GameEventBus.Instance.Publish(new DeckShuffledEvent { SourcePile = "AllPiles" });
        }

        public void DiscardCard(CardRuntime card, bool specialBanish = false, bool isPlayed = false)
        {
            if (card == null) return;
            Hand.Remove(card);
            DrawPile.Remove(card);
            HoldPile.Remove(card);
            DiscardPile.Remove(card);

            int[] specialBanishIDs = { 71, 72, 173, 174 };
            if (specialBanish && specialBanishIDs.Contains(card.CardID))
            {
                BanishCard(card);
                return;
            }

            int[] infestedIDs = { 401, 402, 403, 404, 405, 408, 413 };
            if (infestedIDs.Contains(card.CardID))
            {
                BanishCard(card);
                return;
            }

            if (card.IsBanished && isPlayed)
            {
                string key = $"{card.CardID}_{card.CopyCount}";
                if (!_banishChargeCounters.TryGetValue(key, out int remaining))
                {
                    var data = GetCardData(card.CardID);
                    remaining = data != null ? data.GetBanishCharges(card.IsUpgraded) : 0;
                }
                if (remaining > 0)
                {
                    _banishChargeCounters[key] = remaining - 1;
                    DiscardPile.Add(card);
                    GameEventBus.Instance.Publish(new CardDiscardedEvent
                    {
                        CardID = card.CardID,
                        CopyCount = card.CopyCount,
                        TargetPile = "DiscardPile"
                    });
                }
                else
                {
                    BanishCard(card);
                }
                return;
            }

            DiscardPile.Add(card);
            GameEventBus.Instance.Publish(new CardDiscardedEvent
            {
                CardID = card.CardID,
                CopyCount = card.CopyCount,
                TargetPile = "DiscardPile"
            });
        }

        public void DiscardAllHand()
        {
            var cards = new List<CardRuntime>(Hand);
            foreach (var card in cards)
                DiscardCard(card);
        }

        public void BanishCard(CardRuntime card)
        {
            if (card == null) return;
            Hand.Remove(card);
            DrawPile.Remove(card);
            DiscardPile.Remove(card);
            HoldPile.Remove(card);
            BanishPile.Add(card);
            GameEventBus.Instance.Publish(new CardDiscardedEvent
            {
                CardID = card.CardID,
                CopyCount = card.CopyCount,
                TargetPile = "BanishPile"
            });
            var bs = GameStateManager.Instance?.BattleState;
            if (bs != null) bs.CardsBanishedThisTurn++;
        }

        public void HoldCard(CardRuntime card)
        {
            if (card == null) return;
            Hand.Remove(card);
            DrawPile.Remove(card);
            DiscardPile.Remove(card);
            if (!HoldPile.Contains(card))
                HoldPile.Add(card);
        }

        public void ReturnHoldPileToHand()
        {
            while (HoldPile.Count > 0)
            {
                var card = HoldPile[0];
                HoldPile.RemoveAt(0);
                Hand.Add(card);
            }
        }

        public void PlayCard(CardRuntime card, string targetEnemyUID = null)
        {
            if (card == null) return;
            if (!Hand.Contains(card)) return;

            int energyCost = EnergyManager.Instance?.GetCardEnergyCost(card,
                GameStateManager.Instance?.BattleState.ComboActive ?? false,
                GameStateManager.Instance?.BattleState.OnslaughtActive ?? false,
                GameStateManager.Instance?.BattleState.CurrentHeroSlow ?? 0,
                GameStateManager.Instance?.BattleState.CurrentHeroHaste ?? 0) ?? 0;

            if (EnergyManager.Instance != null && !EnergyManager.Instance.ConsumeEnergy(energyCost))
                return;

            StrayPathCore.Combat.EnemyCombatEntity target = null;
            if (!string.IsNullOrEmpty(targetEnemyUID))
            {
                target = StrayPathCore.Combat.BattleStateMachine.Instance?.GetEnemyByUID(targetEnemyUID);
            }

            CardEffectDispatcher.Instance?.ExecuteCardEffect(card.CardID, card, target);

            var bs = GameStateManager.Instance?.BattleState;
            if (bs != null)
            {
                bs.CardsPlayedThisTurn++;
                var data = GetCardData(card.CardID);
                if (data != null)
                {
                    if (data.Type == CardType.Attack) bs.AttackCardsPlayedThisTurn++;
                    if (data.Type == CardType.Defense) bs.DefenseCardsPlayedThisTurn++;
                    if (energyCost == 0) bs.ZeroCostCardsPlayedThisTurn++;
                    if (data.IsCombo) bs.ComboActive = true;
                    if (data.IsFinisher) bs.FinisherActive = true;
                }
            }

            Hand.Remove(card);
            GameEventBus.Instance.Publish(new CardPlayedEvent
            {
                CardID = card.CardID,
                CopyCount = card.CopyCount,
                EnergyCost = energyCost,
                IsUpgraded = card.IsUpgraded,
                TargetEnemyUID = targetEnemyUID
            });
            DiscardCard(card, specialBanish: false, isPlayed: true);
        }

        public void Shuffle(List<CardRuntime> pile)
        {
            if (pile == null || pile.Count <= 1) return;
            int n = pile.Count;
            for (int i = n - 1; i > 0; i--)
            {
                int j = UnityEngine.Random.Range(0, i + 1);
                var temp = pile[i];
                pile[i] = pile[j];
                pile[j] = temp;
            }
        }

        public CardRuntime CreateFakeCard(int cardID, bool isUpgraded = false)
        {
            int maxCopy = 0;
            var piles = new[] { DrawPile, Hand, DiscardPile, BanishPile, HoldPile };
            foreach (var pile in piles)
            {
                foreach (var c in pile)
                {
                    if (c.CardID == cardID && c.CopyCount > maxCopy)
                        maxCopy = c.CopyCount;
                }
            }
            var persistent = GameStateManager.Instance?.CurrentRun?.DeckCards;
            if (persistent != null)
            {
                foreach (var c in persistent)
                {
                    if (c.CardID == cardID && c.CopyCount > maxCopy)
                        maxCopy = c.CopyCount;
                }
            }
            return new CardRuntime
            {
                CardID = cardID,
                CopyCount = maxCopy + 1,
                IsUpgraded = isUpgraded,
                IsFake = true
            };
        }

        public void AddFakeCardToPlayerHand(int cardID, bool isUpgraded = false)
        {
            var fake = CreateFakeCard(cardID, isUpgraded);
            Hand.Add(fake);
        }

        public void AddFakeCardToPlayerDeck(int cardID)
        {
            var fake = CreateFakeCard(cardID, false);
            DrawPile.Add(fake);
        }

        public void AddFakeCardToDiscardPile(int cardID)
        {
            var fake = CreateFakeCard(cardID, false);
            DiscardPile.Add(fake);
        }

        public void AddMultipleFakeCardsToPlayerHand(List<(int cardID, bool upgraded, int count)> cards)
        {
            if (cards == null) return;
            foreach (var entry in cards)
            {
                for (int i = 0; i < entry.count; i++)
                    AddFakeCardToPlayerHand(entry.cardID, entry.upgraded);
            }
        }

        public List<CardRuntime> ListAllCardsInPlayerHand() => new List<CardRuntime>(Hand);
        public List<CardRuntime> ListAllCardsInDeck() => new List<CardRuntime>(GameStateManager.Instance?.CurrentRun?.DeckCards ?? new List<CardRuntime>());
        public List<CardRuntime> ListAllCardsInDiscardPile() => new List<CardRuntime>(DiscardPile);
        public List<CardRuntime> ListAllCardsInBanishPile() => new List<CardRuntime>(BanishPile);
        public List<CardRuntime> ListAllCardsInHoldPile() => new List<CardRuntime>(HoldPile);

        public int GetHandCount() => Hand.Count;
        public bool HasCardInHand(int cardID) => Hand.Exists(c => c.CardID == cardID);
        public CardRuntime FindCardInHand(int cardID, int copyCount) => Hand.Find(c => c.CardID == cardID && c.CopyCount == copyCount);
        public void RemoveCardFromHand(CardRuntime card) => Hand.Remove(card);

        public void UpgradeCard(CardRuntime card)
        {
            if (card == null) return;
            card.IsUpgraded = true;
            card.ExtraUpgrades++;
        }

        public void RemoveCardFromDeck(CardRuntime card)
        {
            if (card == null) return;
            DrawPile.Remove(card);
            Hand.Remove(card);
            DiscardPile.Remove(card);
            BanishPile.Remove(card);
            HoldPile.Remove(card);
            GameStateManager.Instance?.RemoveCardFromDeck(card.CardID, card.CopyCount);
        }

        public void BurnCard(CardRuntime card)
        {
            if (card == null) return;
            RemoveCardFromDeck(card);
        }

        public void DuplicateCard(CardRuntime card)
        {
            if (card == null) return;
            var copy = new CardRuntime
            {
                CardID = card.CardID,
                CopyCount = GetNextCopyCount(card.CardID),
                IsUpgraded = card.IsUpgraded,
                ExtraUpgrades = card.ExtraUpgrades,
                IsBanished = card.IsBanished
            };
            GameStateManager.Instance?.AddCardToDeck(copy);
            DrawPile.Add(copy);
        }

        public void SwapCard(CardRuntime oldCard, int newCardID)
        {
            if (oldCard == null) return;
            RemoveCardFromDeck(oldCard);
            AddCardByIdExternal(newCardID, 1);
        }

        public void AddCardByIdExternal(int cardID, int copyCount = 1)
        {
            for (int i = 0; i < copyCount; i++)
            {
                var newCard = new CardRuntime
                {
                    CardID = cardID,
                    CopyCount = GetNextCopyCount(cardID),
                    IsUpgraded = false,
                    IsFake = false
                };
                GameStateManager.Instance?.AddCardToDeck(newCard);
                DrawPile.Add(newCard);
            }
        }

        public void SetCounterMeasures(int value)
        {
            if (GameStateManager.Instance != null)
                GameStateManager.Instance.BattleState.CounterMeasures = value;
        }

        public bool GetArcaneTrance() => GameStateManager.Instance?.BattleState.ArcaneTrance ?? false;
        public void SetArcaneTrance(bool value)
        {
            if (GameStateManager.Instance != null)
                GameStateManager.Instance.BattleState.ArcaneTrance = value;
        }

        private int GetNextCopyCount(int cardID)
        {
            int max = 0;
            var persistent = GameStateManager.Instance?.CurrentRun?.DeckCards;
            if (persistent != null)
            {
                foreach (var c in persistent)
                    if (c.CardID == cardID && c.CopyCount > max)
                        max = c.CopyCount;
            }
            foreach (var pile in new[] { DrawPile, Hand, DiscardPile, BanishPile, HoldPile })
            {
                foreach (var c in pile)
                    if (c.CardID == cardID && c.CopyCount > max)
                        max = c.CopyCount;
            }
            return max + 1;
        }
    }
}
