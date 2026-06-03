using UnityEngine;
using UnityEngine.UI;
using TMPro;
using StrayPathCore.Core;
using StrayPathCore.Combat;

namespace StrayPathCore.UI
{
    /// <summary>
    /// Boost 显示与激活按钮。
    /// 读取 GameStateManager.CurrentRun 的 BoostBarValue 与 BoostEnergy，点击后尝试激活 Boost。
    /// </summary>
    public class BoostDisplay : MonoBehaviour
    {
        private Image _barFill;
        private TextMeshProUGUI _text;
        private Button _button;

        private void Awake()
        {
            var rt = GetComponent<RectTransform>();
            if (rt == null)
                rt = gameObject.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(160, 60);

            var bgGo = new GameObject("Bg", typeof(RectTransform));
            bgGo.transform.SetParent(transform, false);
            var bgRt = bgGo.GetComponent<RectTransform>();
            bgRt.anchorMin = Vector2.zero;
            bgRt.anchorMax = Vector2.one;
            bgRt.offsetMin = Vector2.zero;
            bgRt.offsetMax = Vector2.zero;
            var bgImg = bgGo.AddComponent<Image>();
            bgImg.color = new Color(0.2f, 0.1f, 0.3f, 1f);

            // 进度条背景
            var barBgGo = new GameObject("BarBg", typeof(RectTransform));
            barBgGo.transform.SetParent(transform, false);
            var barBgRt = barBgGo.GetComponent<RectTransform>();
            barBgRt.anchorMin = new Vector2(0.1f, 0.55f);
            barBgRt.anchorMax = new Vector2(0.9f, 0.8f);
            barBgRt.offsetMin = Vector2.zero;
            barBgRt.offsetMax = Vector2.zero;
            var barBgImg = barBgGo.AddComponent<Image>();
            barBgImg.color = Color.black;

            // 进度条填充
            var barFillGo = new GameObject("BarFill", typeof(RectTransform));
            barFillGo.transform.SetParent(barBgGo.transform, false);
            var barFillRt = barFillGo.GetComponent<RectTransform>();
            barFillRt.anchorMin = Vector2.zero;
            barFillRt.anchorMax = Vector2.one;
            barFillRt.offsetMin = Vector2.zero;
            barFillRt.offsetMax = Vector2.zero;
            _barFill = barFillGo.AddComponent<Image>();
            _barFill.color = new Color(0.8f, 0.3f, 0.9f, 1f);
            _barFill.type = Image.Type.Filled;
            _barFill.fillMethod = Image.FillMethod.Horizontal;
            _barFill.fillOrigin = (int)Image.OriginHorizontal.Left;

            // 文本
            var textGo = new GameObject("Text", typeof(RectTransform));
            textGo.transform.SetParent(transform, false);
            var textRt = textGo.GetComponent<RectTransform>();
            textRt.anchorMin = new Vector2(0.1f, 0.1f);
            textRt.anchorMax = new Vector2(0.9f, 0.5f);
            textRt.offsetMin = Vector2.zero;
            textRt.offsetMax = Vector2.zero;
            _text = textGo.AddComponent<TextMeshProUGUI>();
            _text.fontSize = 14;
            _text.alignment = TextAlignmentOptions.Center;
            _text.color = Color.white;

            // 按钮
            _button = gameObject.AddComponent<Button>();
            _button.targetGraphic = bgImg;
            _button.onClick.AddListener(OnClickBoost);
        }

        /// <summary>
        /// 刷新 Boost 进度与能量显示。
        /// </summary>
        public void Refresh()
        {
            int bar = GameStateManager.Instance?.BoostBar ?? 0;
            int energy = GameStateManager.Instance?.BoostEnergy ?? 0;
            if (_barFill != null)
                _barFill.fillAmount = bar / 20f;
            if (_text != null)
                _text.text = $"Boost: {bar}/20  [{energy}]";
        }

        private void OnClickBoost()
        {
            if (GameStateManager.Instance?.BoostEnergy > 0)
            {
                var boost = BoostSystem.Instance;
                if (boost != null && !boost.IsBoostActive)
                {
                    boost.PreSetBoostOn();
                }
            }
        }

        private void OnDestroy()
        {
            if (_button != null)
                _button.onClick.RemoveListener(OnClickBoost);
        }
    }
}
