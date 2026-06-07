using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Game.Presentation.Runtime.Playtest
{
    /// <summary>
    /// Odin-enhanced playtest and effect tuning panel for the Presentation layer.
    /// Attach to a ScriptableObject asset and assign to DomainPresentationController.
    /// Use the Inspector to tune animation timing, card colors, and preview playtest state.
    /// </summary>
    [CreateAssetMenu(menuName = "CardDungeon/Presentation/Effect Panel", fileName = "PresentationEffectPanel")]
    public sealed class PresentationEffectPanel : ScriptableObject
    {
        public const string ResourcesPath = "Presentation/PresentationEffectPanel";
        public const string DefaultAssetPath = "Assets/Resources/Presentation/PresentationEffectPanel.asset";

        // ──────────────── Playtest Controls ────────────────

        [TabGroup("操控")]
        [TitleGroup("操控/启动")]
        [InfoBox("控制 DomainPresentationController 的启动行为。Auto Start 会在 OnEnable 时自动开始演出。")]
        public bool autoStartOnEnable = true;

        [TabGroup("操控")]
        [TitleGroup("操控/启动")]
        [InfoBox("随机种子。相同种子 = 相同地牢布局，方便复现与调试。")]
        public int seed = 12345;

        [TabGroup("操控")]
        [TitleGroup("操控/效果关联")]
        [InfoBox("当前面板所关联的 DomainPresentationController（运行时自动回填）。")]
        [ShowInInspector, ReadOnly]
        public string LinkedControllerStatus => _linkedController != null
            ? $"已关联：{_linkedController.name}（状态：{(_linkedController.IsRunning ? "运行中" : "已停止")}）"
            : "未关联。将面板拖入场景对象的 Effect Panel 字段即可。";

        [NonSerialized] private Runtime.DomainPresentationController _linkedController;

        public void LinkController(Runtime.DomainPresentationController controller)
        {
            _linkedController = controller;
        }

        [TabGroup("操控")]
        [TitleGroup("操控/快捷操作")]
        [Button("开始演出", ButtonHeight = 36)]
        private void StartPresentationFromPanel()
        {
            if (_linkedController != null)
            {
                _linkedController.StartPresentation();
                Debug.Log("[PresentationEffectPanel] 已触发 StartPresentation。");
            }
            else
            {
                Debug.LogWarning("[PresentationEffectPanel] 未关联 Controller，无法触发演出。请在场景中设置关联。");
            }
        }

        [TabGroup("操控")]
        [TitleGroup("操控/快捷操作")]
        [Button("重置为默认配置")]
        public void ResetToDefault()
        {
            autoStartOnEnable = true;
            seed = 12345;
            moveDuration = 0.38f;
            flipDuration = 0.34f;
            fadeDuration = 0.30f;
            hitPunchDuration = 0.22f;
            hitPunchStrength = 0.18f;
            hoverScale = 1.05f;

            faceDownColor = new Color(0.22f, 0.18f, 0.16f, 0.92f);
            playerColor = new Color(0.90f, 0.95f, 0.78f, 0.95f);
            monsterColor = new Color(0.76f, 0.34f, 0.30f, 0.95f);
            trapColor = new Color(0.52f, 0.32f, 0.22f, 0.95f);
            itemColor = new Color(0.42f, 0.60f, 0.86f, 0.95f);
            goldColor = new Color(0.92f, 0.78f, 0.26f, 0.95f);
            routeColor = new Color(0.48f, 0.76f, 0.58f, 0.95f);
            specialColor = new Color(0.74f, 0.58f, 0.88f, 0.95f);
            relicColor = new Color(0.90f, 0.68f, 0.36f, 0.95f);

            previewValidColor = new Color(0.58f, 0.88f, 0.58f, 1f);
            previewInvalidColor = new Color(0.95f, 0.34f, 0.34f, 1f);
            outlineIdleColor = new Color(0f, 0f, 0f, 0.35f);
        }

        // ──────────────── Animation Timing ────────────────

        [TabGroup("动效")]
        [TitleGroup("动效/时长")]
        [Range(0.05f, 1f)] public float moveDuration = 0.38f;

        [TabGroup("动效")]
        [TitleGroup("动效/时长")]
        [Range(0.05f, 1f)] public float flipDuration = 0.34f;

        [TabGroup("动效")]
        [TitleGroup("动效/时长")]
        [Range(0.05f, 1f)] public float fadeDuration = 0.30f;

        [TabGroup("动效")]
        [TitleGroup("动效/时长")]
        [Range(0.05f, 1f)] public float hitPunchDuration = 0.22f;

        [TabGroup("动效")]
        [TitleGroup("动效/力度")]
        [Range(0f, 0.6f)] public float hitPunchStrength = 0.18f;

        [TabGroup("动效")]
        [TitleGroup("动效/力度")]
        [Range(1f, 1.2f)] public float hoverScale = 1.05f;

        // ──────────────── Card Colors ────────────────

        [TabGroup("配色")]
        [TitleGroup("配色/基础")]
        [PreviewField(45)] public Color faceDownColor = new Color(0.22f, 0.18f, 0.16f, 0.92f);

        [TabGroup("配色")]
        [TitleGroup("配色/基础")]
        [PreviewField(45)] public Color playerColor = new Color(0.90f, 0.95f, 0.78f, 0.95f);

        [TabGroup("配色")]
        [TitleGroup("配色/基础")]
        [PreviewField(45)] public Color monsterColor = new Color(0.76f, 0.34f, 0.30f, 0.95f);

        [TabGroup("配色")]
        [TitleGroup("配色/基础")]
        [PreviewField(45)] public Color trapColor = new Color(0.52f, 0.32f, 0.22f, 0.95f);

        [TabGroup("配色")]
        [TitleGroup("配色/基础")]
        [PreviewField(45)] public Color itemColor = new Color(0.42f, 0.60f, 0.86f, 0.95f);

        [TabGroup("配色")]
        [TitleGroup("配色/基础")]
        [PreviewField(45)] public Color goldColor = new Color(0.92f, 0.78f, 0.26f, 0.95f);

        [TabGroup("配色")]
        [TitleGroup("配色/扩展")]
        [PreviewField(45)] public Color routeColor = new Color(0.48f, 0.76f, 0.58f, 0.95f);

        [TabGroup("配色")]
        [TitleGroup("配色/扩展")]
        [PreviewField(45)] public Color specialColor = new Color(0.74f, 0.58f, 0.88f, 0.95f);

        [TabGroup("配色")]
        [TitleGroup("配色/扩展")]
        [PreviewField(45)] public Color relicColor = new Color(0.90f, 0.68f, 0.36f, 0.95f);

        // ──────────────── Interaction Hints ────────────────

        [TabGroup("交互")]
        [TitleGroup("交互/预览色")]
        [PreviewField(45)] public Color previewValidColor = new Color(0.58f, 0.88f, 0.58f, 1f);

        [TabGroup("交互")]
        [TitleGroup("交互/预览色")]
        [PreviewField(45)] public Color previewInvalidColor = new Color(0.95f, 0.34f, 0.34f, 1f);

        [TabGroup("交互")]
        [TitleGroup("交互/描边")]
        [PreviewField(45)] public Color outlineIdleColor = new Color(0f, 0f, 0f, 0.35f);
    }
}
