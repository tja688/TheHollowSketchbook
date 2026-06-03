using System.Collections.Generic;
using UnityEngine;
using StrayPathCore.Core;
using StrayPathCore.Data;

namespace StrayPathCore.Deck
{
    public class EnergyManager : MonoBehaviour
    {
        public static EnergyManager Instance { get; private set; }

        public int CurrentEnergy { get; private set; }
        public int MaxEnergy { get; private set; } = 3;

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

        public void ResetEnergy()
        {
            int old = CurrentEnergy;
            int max = 3;
            if (GameStateManager.Instance != null && GameStateManager.Instance.HasRelic(73))
                max = 4;
            MaxEnergy = max;
            CurrentEnergy = max;
            GameEventBus.Instance.Publish(new EnergyChangedEvent
            {
                OldValue = old,
                NewValue = CurrentEnergy,
                Reason = "TurnStart"
            });
        }

        public bool ConsumeEnergy(int amount)
        {
            if (amount < 0) amount = 0;
            if (CurrentEnergy < amount) return false;
            int old = CurrentEnergy;
            CurrentEnergy -= amount;
            GameEventBus.Instance.Publish(new EnergyChangedEvent
            {
                OldValue = old,
                NewValue = CurrentEnergy,
                Reason = "Consume"
            });
            return true;
        }

        public void GainEnergy(int amount)
        {
            if (amount <= 0) return;
            int old = CurrentEnergy;
            CurrentEnergy += amount;
            GameEventBus.Instance.Publish(new EnergyChangedEvent
            {
                OldValue = old,
                NewValue = CurrentEnergy,
                Reason = "Gain"
            });
        }

        public int GetCardEnergyCost(CardRuntime card, bool comboActive, bool onslaughtActive, int slowDuration, int hasteDuration)
        {
            if (card == null) return 0;
            var data = GetCardData(card.CardID);
            if (data == null) return 0;

            int cost = data.GetEnergyCost(card.IsUpgraded);

            if (comboActive && data.IsCombo)
                cost = Mathf.Max(0, cost - 1);

            if (onslaughtActive && data.Type == CardType.Attack)
                cost = 0;

            if (slowDuration > 0)
                cost += 1;

            if (hasteDuration > 0)
                cost = Mathf.Max(0, cost - 1);

            return cost;
        }
    }
}
