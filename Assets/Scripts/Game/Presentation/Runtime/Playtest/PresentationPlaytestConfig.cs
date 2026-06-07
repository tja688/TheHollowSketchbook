using UnityEngine;

namespace Game.Presentation.Runtime.Playtest
{
    [CreateAssetMenu(menuName = "CardDungeon/Presentation/Playtest Config", fileName = "PresentationPlaytestConfig")]
    public sealed class PresentationPlaytestConfig : ScriptableObject
    {
        public const string ResourcesPath = "Presentation/PresentationPlaytestConfig";
        public const string DefaultAssetPath = "Assets/Resources/Presentation/PresentationPlaytestConfig.asset";

        [Header("启动")]
        public bool autoStartOnEnable = true;
        public int seed = 12345;

        [Header("动效时长")]
        [Range(0.05f, 1f)] public float moveDuration = 0.38f;
        [Range(0.05f, 1f)] public float flipDuration = 0.34f;
        [Range(0.05f, 1f)] public float fadeDuration = 0.30f;
        [Range(0.05f, 1f)] public float hitPunchDuration = 0.22f;
        [Range(0f, 0.6f)] public float hitPunchStrength = 0.18f;
        [Range(1f, 1.2f)] public float hoverScale = 1.05f;

        [Header("卡牌配色")]
        public Color faceDownColor = new Color(0.22f, 0.18f, 0.16f, 0.92f);
        public Color playerColor = new Color(0.90f, 0.95f, 0.78f, 0.95f);
        public Color monsterColor = new Color(0.76f, 0.34f, 0.30f, 0.95f);
        public Color trapColor = new Color(0.52f, 0.32f, 0.22f, 0.95f);
        public Color itemColor = new Color(0.42f, 0.60f, 0.86f, 0.95f);
        public Color goldColor = new Color(0.92f, 0.78f, 0.26f, 0.95f);
        public Color chestColor = new Color(0.82f, 0.54f, 0.22f, 0.95f);
        public Color statColor = new Color(0.54f, 0.86f, 0.70f, 0.95f);
        public Color foodColor = new Color(0.64f, 0.88f, 0.60f, 0.95f);
        public Color mentorColor = new Color(0.72f, 0.64f, 0.88f, 0.95f);
        public Color shopColor = new Color(0.52f, 0.80f, 0.92f, 0.95f);
        public Color routeColor = new Color(0.48f, 0.76f, 0.58f, 0.95f);
        public Color specialColor = new Color(0.74f, 0.58f, 0.88f, 0.95f);
        public Color relicColor = new Color(0.90f, 0.68f, 0.36f, 0.95f);

        [Header("交互提示")]
        public Color cellIdleColor = new Color(0f, 0f, 0f, 0.05f);
        public Color outlineIdleColor = new Color(0f, 0f, 0f, 0.35f);
        public Color previewValidColor = new Color(0.58f, 0.88f, 0.58f, 1f);
        public Color previewInvalidColor = new Color(0.95f, 0.34f, 0.34f, 1f);

        [ContextMenu("Reset To Default")]
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
            chestColor = new Color(0.82f, 0.54f, 0.22f, 0.95f);
            statColor = new Color(0.54f, 0.86f, 0.70f, 0.95f);
            foodColor = new Color(0.64f, 0.88f, 0.60f, 0.95f);
            mentorColor = new Color(0.72f, 0.64f, 0.88f, 0.95f);
            shopColor = new Color(0.52f, 0.80f, 0.92f, 0.95f);
            routeColor = new Color(0.48f, 0.76f, 0.58f, 0.95f);
            specialColor = new Color(0.74f, 0.58f, 0.88f, 0.95f);
            relicColor = new Color(0.90f, 0.68f, 0.36f, 0.95f);

            cellIdleColor = new Color(0f, 0f, 0f, 0.05f);
            outlineIdleColor = new Color(0f, 0f, 0f, 0.35f);
            previewValidColor = new Color(0.58f, 0.88f, 0.58f, 1f);
            previewInvalidColor = new Color(0.95f, 0.34f, 0.34f, 1f);
        }
    }
}
