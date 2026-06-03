using UnityEngine;
using UnityEngine.UI;
using TMPro;
using StrayPathCore.Combat;

namespace StrayPathCore.UI
{
    /// <summary>
    /// 结束回合按钮。
    /// 点击后调用 BattleStateMachine.Instance.EndPlayerTurn()，不做任何状态修改。
    /// </summary>
    public class EndTurnButton : MonoBehaviour
    {
        private Button _button;
        private TextMeshProUGUI _text;

        private void Awake()
        {
            var rt = GetComponent<RectTransform>();
            if (rt == null)
                rt = gameObject.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(120, 50);

            var bgGo = new GameObject("Bg", typeof(RectTransform));
            bgGo.transform.SetParent(transform, false);
            var bgRt = bgGo.GetComponent<RectTransform>();
            bgRt.anchorMin = Vector2.zero;
            bgRt.anchorMax = Vector2.one;
            bgRt.offsetMin = Vector2.zero;
            bgRt.offsetMax = Vector2.zero;
            var bgImg = bgGo.AddComponent<Image>();
            bgImg.color = new Color(0.3f, 0.3f, 0.5f, 1f);

            _button = gameObject.AddComponent<Button>();
            _button.targetGraphic = bgImg;
            _button.onClick.AddListener(OnClickEndTurn);

            var textGo = new GameObject("Text", typeof(RectTransform));
            textGo.transform.SetParent(transform, false);
            var textRt = textGo.GetComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = Vector2.zero;
            textRt.offsetMax = Vector2.zero;
            _text = textGo.AddComponent<TextMeshProUGUI>();
            _text.fontSize = 18;
            _text.alignment = TextAlignmentOptions.Center;
            _text.color = Color.white;
            _text.text = "End Turn";
        }

        private void OnClickEndTurn()
        {
            BattleStateMachine.Instance?.EndPlayerTurn();
        }

        private void OnDestroy()
        {
            if (_button != null)
                _button.onClick.RemoveListener(OnClickEndTurn);
        }
    }
}
