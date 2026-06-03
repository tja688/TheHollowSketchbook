using System.Collections.Generic;
using UnityEngine;
using StrayPathCore.Core;
using StrayPathCore.Data;
using StrayPathCore.Deck;

namespace StrayPathCore.UI
{
    /// <summary>
    /// 玩家手牌显示区域。
    /// 从 DeckManager 读取手牌数据，动态创建/销毁 CardUIProxy。
    /// 纯表现层，点击后转发给 BattleUIManager 处理目标选择或出牌。
    /// </summary>
    public class PlayerHandDisplay : MonoBehaviour
    {
        [Header("布局参数")]
        [SerializeField] private Vector2 cardSize = new Vector2(100, 140);
        [SerializeField] private float cardSpacing = 10f;
        [SerializeField] private float maxWidth = 900f;

        private List<CardUIProxy> _cardProxies = new List<CardUIProxy>();
        private RectTransform _container;

        private void Awake()
        {
            _container = GetComponent<RectTransform>();
            if (_container == null)
                _container = gameObject.AddComponent<RectTransform>();
        }

        /// <summary>
        /// 完全刷新手牌显示（从 DeckManager.Hand 重新构建）。
        /// </summary>
        public void RefreshHand()
        {
            ClearHand();
            var hand = DeckManager.Instance?.Hand;
            if (hand == null) return;

            foreach (var card in hand)
            {
                if (card != null)
                    CreateCardProxy(card);
            }
            LayoutCards();
        }

        /// <summary>
        /// 根据卡牌ID与CopyCount移除对应UI代理。
        /// </summary>
        public void RemoveCard(int cardID, int copyCount)
        {
            var proxy = _cardProxies.Find(p => p.Card != null && p.Card.CardID == cardID && p.Card.CopyCount == copyCount);
            if (proxy != null)
            {
                _cardProxies.Remove(proxy);
                if (proxy.gameObject != null)
                    Destroy(proxy.gameObject);
            }
            LayoutCards();
        }

        /// <summary>
        /// 清空所有卡牌UI代理。
        /// </summary>
        public void ClearHand()
        {
            foreach (var proxy in _cardProxies)
            {
                if (proxy != null && proxy.gameObject != null)
                    Destroy(proxy.gameObject);
            }
            _cardProxies.Clear();
        }

        private void CreateCardProxy(CardRuntime card)
        {
            var go = new GameObject($"Card_{card.CardID}_{card.CopyCount}", typeof(RectTransform));
            go.transform.SetParent(_container, false);
            var proxy = go.AddComponent<CardUIProxy>();
            proxy.BuildVisuals(cardSize);
            var data = GetCardData(card.CardID);
            proxy.Setup(card, data);
            proxy.OnClickCallback = OnCardClicked;
            _cardProxies.Add(proxy);
        }

        private void OnCardClicked(CardUIProxy proxy)
        {
            if (proxy?.Card == null) return;
            BattleUIManager.Instance?.OnCardClicked(proxy.Card);
        }

        private void LayoutCards()
        {
            int count = _cardProxies.Count;
            if (count == 0) return;

            float totalWidth = count * cardSize.x + (count - 1) * cardSpacing;
            float scale = 1f;
            if (totalWidth > maxWidth && maxWidth > 0)
                scale = maxWidth / totalWidth;

            float startX = -(totalWidth * scale) / 2f + (cardSize.x * scale) / 2f;

            for (int i = 0; i < count; i++)
            {
                var proxy = _cardProxies[i];
                if (proxy == null) continue;
                var rt = proxy.GetComponent<RectTransform>();
                rt.anchoredPosition = new Vector2(startX + i * (cardSize.x + cardSpacing) * scale, 0);
                rt.localScale = Vector3.one * scale;
            }
        }

        private static Dictionary<int, CardData> _cardDataCache;

        private static CardData GetCardData(int cardID)
        {
            if (_cardDataCache == null)
            {
                _cardDataCache = new Dictionary<int, CardData>();
                var all = Resources.LoadAll<CardData>("");
                if (all != null)
                {
                    foreach (var cd in all)
                    {
                        if (cd != null && !_cardDataCache.ContainsKey(cd.CardID))
                            _cardDataCache[cd.CardID] = cd;
                    }
                }
            }
            _cardDataCache.TryGetValue(cardID, out var data);
            return data;
        }
    }
}
