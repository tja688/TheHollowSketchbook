using UnityEngine;
using TMPro;
using StrayPathCore.Core;

namespace StrayPathCore.UI
{
    /// <summary>
    /// 能量显示组件。
    /// 读取 GameStateManager.BattleState 的当前/最大能量并显示为文本。
    /// </summary>
    public class EnergyDisplay : MonoBehaviour
    {
        private TextMeshProUGUI _text;

        private void Awake()
        {
            var rt = GetComponent<RectTransform>();
            if (rt == null)
                rt = gameObject.AddComponent<RectTransform>();

            var textGo = new GameObject("Text", typeof(RectTransform));
            textGo.transform.SetParent(transform, false);
            var textRt = textGo.GetComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = Vector2.zero;
            textRt.offsetMax = Vector2.zero;
            _text = textGo.AddComponent<TextMeshProUGUI>();
            _text.fontSize = 24;
            _text.alignment = TextAlignmentOptions.Center;
            _text.color = new Color(0.3f, 0.9f, 1f, 1f);
        }

        /// <summary>
        /// 刷新能量显示。
        /// </summary>
        public void Refresh()
        {
            int current = GameStateManager.Instance?.BattleState.CurrentEnergy ?? 0;
            int max = GameStateManager.Instance?.BattleState.CurrentMaxEnergy ?? 0;
            if (_text != null)
                _text.text = $"Energy: {current}/{max}";
        }
    }
}
