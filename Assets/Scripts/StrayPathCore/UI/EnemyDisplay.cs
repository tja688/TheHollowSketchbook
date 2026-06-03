using UnityEngine;
using UnityEngine.UI;
using TMPro;
using StrayPathCore.Combat;

namespace StrayPathCore.UI
{
    /// <summary>
    /// 单个敌人的UI显示组件。
    /// 绑定到 EnemyCombatEntity，显示HP条、Block、当前意图。
    /// 点击时通知回调（用于目标选择）。
    /// </summary>
    public class EnemyDisplay : MonoBehaviour
    {
        public System.Action<string> OnClicked;

        private EnemyCombatEntity _enemy;
        private Image _hpBar;
        private TextMeshProUGUI _hpText;
        private TextMeshProUGUI _blockText;
        private TextMeshProUGUI _intentText;
        private Image _bgImage;
        private Button _button;

        /// <summary>
        /// 绑定敌人实体并构建视觉元素。
        /// </summary>
        public void Bind(EnemyCombatEntity enemy)
        {
            _enemy = enemy;
            BuildVisuals();
            Refresh();
        }

        private void BuildVisuals()
        {
            var rt = GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(160, 200);

            // 背景
            var bgGo = new GameObject("Bg", typeof(RectTransform));
            bgGo.transform.SetParent(transform, false);
            var bgRt = bgGo.GetComponent<RectTransform>();
            bgRt.anchorMin = Vector2.zero;
            bgRt.anchorMax = Vector2.one;
            bgRt.offsetMin = Vector2.zero;
            bgRt.offsetMax = Vector2.zero;
            _bgImage = bgGo.AddComponent<Image>();
            _bgImage.color = new Color(0.4f, 0.15f, 0.15f, 1f);

            // 敌人名称
            var nameGo = new GameObject("Name", typeof(RectTransform));
            nameGo.transform.SetParent(transform, false);
            var nameRt = nameGo.GetComponent<RectTransform>();
            nameRt.anchorMin = new Vector2(0, 0.85f);
            nameRt.anchorMax = new Vector2(1, 1);
            nameRt.offsetMin = new Vector2(4, 0);
            nameRt.offsetMax = new Vector2(-4, -2);
            var nameText = nameGo.AddComponent<TextMeshProUGUI>();
            nameText.fontSize = 14;
            nameText.alignment = TextAlignmentOptions.Center;
            nameText.color = Color.white;
            nameText.text = _enemy?.Data?.EnemyName ?? "Enemy";

            // HP条背景
            var hpBgGo = new GameObject("HpBg", typeof(RectTransform));
            hpBgGo.transform.SetParent(transform, false);
            var hpBgRt = hpBgGo.GetComponent<RectTransform>();
            hpBgRt.anchorMin = new Vector2(0.1f, 0.7f);
            hpBgRt.anchorMax = new Vector2(0.9f, 0.82f);
            hpBgRt.offsetMin = Vector2.zero;
            hpBgRt.offsetMax = Vector2.zero;
            var hpBgImg = hpBgGo.AddComponent<Image>();
            hpBgImg.color = Color.black;

            // HP条填充
            var hpFillGo = new GameObject("HpFill", typeof(RectTransform));
            hpFillGo.transform.SetParent(hpBgGo.transform, false);
            var hpFillRt = hpFillGo.GetComponent<RectTransform>();
            hpFillRt.anchorMin = Vector2.zero;
            hpFillRt.anchorMax = Vector2.one;
            hpFillRt.offsetMin = Vector2.zero;
            hpFillRt.offsetMax = Vector2.zero;
            _hpBar = hpFillGo.AddComponent<Image>();
            _hpBar.color = new Color(0.8f, 0.2f, 0.2f, 1f);
            _hpBar.type = Image.Type.Filled;
            _hpBar.fillMethod = Image.FillMethod.Horizontal;
            _hpBar.fillOrigin = (int)Image.OriginHorizontal.Left;

            // HP数值
            var hpTextGo = new GameObject("HpText", typeof(RectTransform));
            hpTextGo.transform.SetParent(hpBgGo.transform, false);
            var hpTextRt = hpTextGo.GetComponent<RectTransform>();
            hpTextRt.anchorMin = Vector2.zero;
            hpTextRt.anchorMax = Vector2.one;
            hpTextRt.offsetMin = Vector2.zero;
            hpTextRt.offsetMax = Vector2.zero;
            _hpText = hpTextGo.AddComponent<TextMeshProUGUI>();
            _hpText.fontSize = 12;
            _hpText.alignment = TextAlignmentOptions.Center;
            _hpText.color = Color.white;

            // Block
            var blockGo = new GameObject("Block", typeof(RectTransform));
            blockGo.transform.SetParent(transform, false);
            var blockRt = blockGo.GetComponent<RectTransform>();
            blockRt.anchorMin = new Vector2(0.1f, 0.55f);
            blockRt.anchorMax = new Vector2(0.9f, 0.68f);
            blockRt.offsetMin = Vector2.zero;
            blockRt.offsetMax = Vector2.zero;
            _blockText = blockGo.AddComponent<TextMeshProUGUI>();
            _blockText.fontSize = 12;
            _blockText.alignment = TextAlignmentOptions.Center;
            _blockText.color = new Color(0.4f, 0.7f, 1f, 1f);

            // 意图
            var intentGo = new GameObject("Intent", typeof(RectTransform));
            intentGo.transform.SetParent(transform, false);
            var intentRt = intentGo.GetComponent<RectTransform>();
            intentRt.anchorMin = new Vector2(0.1f, 0.35f);
            intentRt.anchorMax = new Vector2(0.9f, 0.53f);
            intentRt.offsetMin = Vector2.zero;
            intentRt.offsetMax = Vector2.zero;
            _intentText = intentGo.AddComponent<TextMeshProUGUI>();
            _intentText.fontSize = 12;
            _intentText.alignment = TextAlignmentOptions.Center;
            _intentText.color = new Color(1f, 0.9f, 0.3f, 1f);

            // 点击区域
            var btnGo = new GameObject("ClickArea", typeof(RectTransform));
            btnGo.transform.SetParent(transform, false);
            var btnRt = btnGo.GetComponent<RectTransform>();
            btnRt.anchorMin = Vector2.zero;
            btnRt.anchorMax = Vector2.one;
            btnRt.offsetMin = Vector2.zero;
            btnRt.offsetMax = Vector2.zero;
            _button = btnGo.AddComponent<Button>();
            _button.targetGraphic = _bgImage;
            _button.onClick.AddListener(() => OnClicked?.Invoke(_enemy?.UniqueID));
        }

        /// <summary>
        /// 刷新HP、Block与意图显示。
        /// </summary>
        public void Refresh()
        {
            if (_enemy == null) return;
            float hpRatio = _enemy.MaxHP > 0 ? (float)_enemy.CurrentHP / _enemy.MaxHP : 0f;
            _hpBar.fillAmount = hpRatio;
            _hpText.text = $"{_enemy.CurrentHP}/{_enemy.MaxHP}";
            _blockText.text = _enemy.CurrentBlock > 0 ? $"Block: {_enemy.CurrentBlock}" : "";
            RefreshIntent();
        }

        /// <summary>
        /// 仅刷新意图显示。
        /// </summary>
        public void RefreshIntent()
        {
            if (_enemy == null || _intentText == null) return;
            var intent = _enemy.CurrentIntent;
            if (intent == null)
            {
                _intentText.text = "";
                return;
            }
            string text = intent.AbilityName ?? "Attack";
            if (_enemy.PreviewDamage > 0)
                text += $"\nDmg: {_enemy.PreviewDamage}";
            if (intent.BlockValue > 0)
                text += $"\nBlk: {intent.BlockValue}";
            _intentText.text = text;
        }

        /// <summary>
        /// 设置高亮状态（目标选择时）。
        /// </summary>
        public void SetHighlight(bool highlight)
        {
            if (_bgImage != null)
                _bgImage.color = highlight
                    ? new Color(0.8f, 0.6f, 0.2f, 1f)
                    : new Color(0.4f, 0.15f, 0.15f, 1f);
        }
    }
}
