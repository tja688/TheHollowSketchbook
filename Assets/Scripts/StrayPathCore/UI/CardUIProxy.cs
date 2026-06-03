using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using StrayPathCore.Core;
using StrayPathCore.Data;

namespace StrayPathCore.UI
{
    /// <summary>
    /// 单张卡牌的UI代理组件。
    /// 负责卡牌的视觉表现、点击与悬停交互。
    /// 不持有游戏逻辑状态，仅做表现层转发。
    /// </summary>
    public class CardUIProxy : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        public CardRuntime Card { get; private set; }
        public System.Action<CardUIProxy> OnClickCallback;
        public System.Action<CardUIProxy> OnHoverEnterCallback;
        public System.Action<CardUIProxy> OnHoverExitCallback;

        private Image _bgImage;
        private TextMeshProUGUI _nameText;
        private TextMeshProUGUI _costText;
        private Image _upgradeBorder;
        private Vector3 _baseScale;
        private bool _isInteractable = true;

        /// <summary>
        /// 绑定卡牌数据并刷新显示。
        /// </summary>
        public void Setup(CardRuntime card, CardData data)
        {
            Card = card;
            if (_nameText != null)
                _nameText.text = data?.GetName(card.IsUpgraded) ?? $"Card {card.CardID}";
            if (_costText != null)
                _costText.text = (data?.GetEnergyCost(card.IsUpgraded) ?? 0).ToString();
            if (_upgradeBorder != null)
                _upgradeBorder.gameObject.SetActive(card.IsUpgraded);
        }

        /// <summary>
        /// 程序化构建卡牌视觉元素。
        /// </summary>
        public void BuildVisuals(Vector2 size)
        {
            var rt = GetComponent<RectTransform>();
            rt.sizeDelta = size;

            // 背景
            var bgGo = new GameObject("Bg", typeof(RectTransform));
            bgGo.transform.SetParent(transform, false);
            var bgRt = bgGo.GetComponent<RectTransform>();
            bgRt.anchorMin = Vector2.zero;
            bgRt.anchorMax = Vector2.one;
            bgRt.offsetMin = Vector2.zero;
            bgRt.offsetMax = Vector2.zero;
            _bgImage = bgGo.AddComponent<Image>();
            _bgImage.color = new Color(0.2f, 0.2f, 0.3f, 1f);

            // 升级边框
            var borderGo = new GameObject("UpgradeBorder", typeof(RectTransform));
            borderGo.transform.SetParent(transform, false);
            var borderRt = borderGo.GetComponent<RectTransform>();
            borderRt.anchorMin = Vector2.zero;
            borderRt.anchorMax = Vector2.one;
            borderRt.offsetMin = new Vector2(-4, -4);
            borderRt.offsetMax = new Vector2(4, 4);
            _upgradeBorder = borderGo.AddComponent<Image>();
            _upgradeBorder.color = new Color(1f, 0.8f, 0.2f, 1f);
            _upgradeBorder.gameObject.SetActive(false);

            // 卡牌名称
            var nameGo = new GameObject("Name", typeof(RectTransform));
            nameGo.transform.SetParent(transform, false);
            var nameRt = nameGo.GetComponent<RectTransform>();
            nameRt.anchorMin = new Vector2(0, 0.7f);
            nameRt.anchorMax = new Vector2(1, 1);
            nameRt.offsetMin = new Vector2(4, 0);
            nameRt.offsetMax = new Vector2(-4, -2);
            _nameText = nameGo.AddComponent<TextMeshProUGUI>();
            _nameText.fontSize = 14;
            _nameText.alignment = TextAlignmentOptions.Center;
            _nameText.color = Color.white;

            // 费用
            var costGo = new GameObject("Cost", typeof(RectTransform));
            costGo.transform.SetParent(transform, false);
            var costRt = costGo.GetComponent<RectTransform>();
            costRt.anchorMin = new Vector2(0, 0.5f);
            costRt.anchorMax = new Vector2(0.3f, 0.7f);
            costRt.offsetMin = new Vector2(4, 0);
            costRt.offsetMax = new Vector2(-2, -2);
            _costText = costGo.AddComponent<TextMeshProUGUI>();
            _costText.fontSize = 16;
            _costText.alignment = TextAlignmentOptions.Center;
            _costText.color = new Color(0.3f, 0.7f, 1f, 1f);

            _baseScale = transform.localScale;
        }

        /// <summary>
        /// 设置是否可交互（视觉灰度反馈）。
        /// </summary>
        public void SetInteractable(bool interactable)
        {
            _isInteractable = interactable;
            if (_bgImage != null)
                _bgImage.color = interactable
                    ? new Color(0.2f, 0.2f, 0.3f, 1f)
                    : new Color(0.15f, 0.15f, 0.15f, 0.6f);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!_isInteractable) return;
            transform.localScale = _baseScale * 1.15f;
            transform.SetAsLastSibling();
            OnHoverEnterCallback?.Invoke(this);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (!_isInteractable) return;
            transform.localScale = _baseScale;
            OnHoverExitCallback?.Invoke(this);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (!_isInteractable) return;
            OnClickCallback?.Invoke(this);
        }
    }
}
