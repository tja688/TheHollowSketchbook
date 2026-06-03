using UnityEngine;
using UnityEngine.UI;
using TMPro;
using StrayPathCore.Combat;

namespace StrayPathCore.UI
{
    /// <summary>
    /// 英雄状态显示组件。
    /// 绑定 HeroCombatEntity，显示HP条、Block、关键Buff/Debuff。
    /// </summary>
    public class HeroDisplay : MonoBehaviour
    {
        private HeroCombatEntity _hero;
        private Image _hpBar;
        private TextMeshProUGUI _hpText;
        private TextMeshProUGUI _blockText;
        private TextMeshProUGUI _statusText;

        /// <summary>
        /// 绑定英雄实体并构建视觉元素。
        /// </summary>
        public void Bind(HeroCombatEntity hero)
        {
            _hero = hero;
            BuildVisuals();
            Refresh();
        }

        private void BuildVisuals()
        {
            var rt = GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(200, 120);

            // 背景
            var bgGo = new GameObject("Bg", typeof(RectTransform));
            bgGo.transform.SetParent(transform, false);
            var bgRt = bgGo.GetComponent<RectTransform>();
            bgRt.anchorMin = Vector2.zero;
            bgRt.anchorMax = Vector2.one;
            bgRt.offsetMin = Vector2.zero;
            bgRt.offsetMax = Vector2.zero;
            var bgImg = bgGo.AddComponent<Image>();
            bgImg.color = new Color(0.15f, 0.25f, 0.4f, 1f);

            // 名称
            var nameGo = new GameObject("Name", typeof(RectTransform));
            nameGo.transform.SetParent(transform, false);
            var nameRt = nameGo.GetComponent<RectTransform>();
            nameRt.anchorMin = new Vector2(0, 0.8f);
            nameRt.anchorMax = new Vector2(1, 1);
            nameRt.offsetMin = new Vector2(4, 0);
            nameRt.offsetMax = new Vector2(-4, -2);
            var nameText = nameGo.AddComponent<TextMeshProUGUI>();
            nameText.fontSize = 16;
            nameText.alignment = TextAlignmentOptions.Center;
            nameText.color = Color.white;
            nameText.text = "Hero";

            // HP条背景
            var hpBgGo = new GameObject("HpBg", typeof(RectTransform));
            hpBgGo.transform.SetParent(transform, false);
            var hpBgRt = hpBgGo.GetComponent<RectTransform>();
            hpBgRt.anchorMin = new Vector2(0.1f, 0.55f);
            hpBgRt.anchorMax = new Vector2(0.9f, 0.75f);
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
            _hpBar.color = new Color(0.2f, 0.7f, 0.3f, 1f);
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
            blockRt.anchorMin = new Vector2(0.1f, 0.35f);
            blockRt.anchorMax = new Vector2(0.5f, 0.52f);
            blockRt.offsetMin = Vector2.zero;
            blockRt.offsetMax = Vector2.zero;
            _blockText = blockGo.AddComponent<TextMeshProUGUI>();
            _blockText.fontSize = 12;
            _blockText.alignment = TextAlignmentOptions.Center;
            _blockText.color = new Color(0.4f, 0.7f, 1f, 1f);

            // 状态效果
            var statusGo = new GameObject("Status", typeof(RectTransform));
            statusGo.transform.SetParent(transform, false);
            var statusRt = statusGo.GetComponent<RectTransform>();
            statusRt.anchorMin = new Vector2(0.1f, 0.05f);
            statusRt.anchorMax = new Vector2(0.9f, 0.32f);
            statusRt.offsetMin = Vector2.zero;
            statusRt.offsetMax = Vector2.zero;
            _statusText = statusGo.AddComponent<TextMeshProUGUI>();
            _statusText.fontSize = 11;
            _statusText.alignment = TextAlignmentOptions.Center;
            _statusText.color = new Color(1f, 0.7f, 0.7f, 1f);
        }

        /// <summary>
        /// 刷新英雄状态显示。
        /// </summary>
        public void Refresh()
        {
            if (_hero == null) return;
            float hpRatio = _hero.MaxHP > 0 ? (float)_hero.CurrentHP / _hero.MaxHP : 0f;
            _hpBar.fillAmount = hpRatio;
            _hpText.text = $"{_hero.CurrentHP}/{_hero.MaxHP}";
            _blockText.text = _hero.CurrentBlock > 0 ? $"Block: {_hero.CurrentBlock}" : "";
            _statusText.text = BuildStatusText();
        }

        private string BuildStatusText()
        {
            if (_hero == null) return "";
            var parts = new System.Collections.Generic.List<string>();
            if (_hero.WeakStacks > 0) parts.Add($"Weak({_hero.WeakStacks})");
            if (_hero.FragileStacks > 0) parts.Add($"Fragile({_hero.FragileStacks})");
            if (_hero.BleedStacks > 0) parts.Add($"Bleed({_hero.BleedStacks})");
            if (_hero.Power > 0) parts.Add($"Power({_hero.Power})");
            if (_hero.Toughness > 0) parts.Add($"Tough({_hero.Toughness})");
            if (_hero.Armor > 0) parts.Add($"Armor({_hero.Armor})");
            return string.Join(" ", parts);
        }
    }
}
