using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Tests
{
    /// <summary>
    /// Phase 03: 低清画布原型 — 运行时快速切换 Render Scale 进行效果对比
    /// 按键说明：
    ///   1 = Render Scale 1.0 (原始)
    ///   2 = Render Scale 0.75 (略低)
    ///   3 = Render Scale 0.5  (目标，约 960×540 @ 1920×1080)
    ///   4 = Render Scale 0.333 (过低，参考)
    ///   0 = 切换 Upscaling Filter (Auto / Point 循环)
    /// </summary>
    public class RenderScaleTester : MonoBehaviour
    {
        [Header("当前状态")]
        [SerializeField, Tooltip("当前 Render Scale")] private float currentRenderScale = 0.5f;
        [SerializeField, Tooltip("当前 Upscaling Filter")] private int currentUpscaleFilter = 3; // 3 = Point

        [Header("参考值")]
        [SerializeField] private float scaleOriginal = 1.0f;
        [SerializeField] private float scaleClean = 0.75f;
        [SerializeField] private float scaleTarget = 0.5f;
        [SerializeField] private float scaleDirty = 0.333f;

        private UniversalRenderPipelineAsset urpAsset;

        void Start()
        {
            urpAsset = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            if (urpAsset == null)
            {
                Debug.LogWarning("[RenderScaleTester] 未找到 URP Asset，请确认当前渲染管线为 URP。");
                enabled = false;
                return;
            }

            // 同步当前值
            currentRenderScale = urpAsset.renderScale;
            currentUpscaleFilter = (int)urpAsset.upscalingFilter;

            Debug.Log($"[RenderScaleTester] 已初始化。当前 RenderScale={currentRenderScale}, UpscalingFilter={(UpscalingFilterSelection)currentUpscaleFilter}");
            Debug.Log($"[RenderScaleTester] 按键说明: 1=1.0 2=0.75 3=0.5 4=0.333 0=切换Filter");
        }

        void Update()
        {
            if (urpAsset == null) return;

            bool changed = false;

            if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                currentRenderScale = scaleOriginal;
                changed = true;
                Debug.Log("[RenderScaleTester] 切换到 Render Scale 1.0 (原始高清)");
            }
            else if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                currentRenderScale = scaleClean;
                changed = true;
                Debug.Log("[RenderScaleTester] 切换到 Render Scale 0.75 (略低，偏干净)");
            }
            else if (Input.GetKeyDown(KeyCode.Alpha3))
            {
                currentRenderScale = scaleTarget;
                changed = true;
                Debug.Log("[RenderScaleTester] 切换到 Render Scale 0.5 (目标，约 960×540)");
            }
            else if (Input.GetKeyDown(KeyCode.Alpha4))
            {
                currentRenderScale = scaleDirty;
                changed = true;
                Debug.Log("[RenderScaleTester] 切换到 Render Scale 0.333 (过低，参考)");
            }
            else if (Input.GetKeyDown(KeyCode.Alpha0))
            {
                currentUpscaleFilter = (currentUpscaleFilter + 1) % 4; // 0~3 循环
                changed = true;
                Debug.Log($"[RenderScaleTester] 切换 Upscaling Filter 到: {(UpscalingFilterSelection)currentUpscaleFilter}");
            }

            if (changed)
            {
                ApplySettings();
            }
        }

        void ApplySettings()
        {
            urpAsset.renderScale = currentRenderScale;
            urpAsset.upscalingFilter = (UpscalingFilterSelection)currentUpscaleFilter;
        }

        void OnGUI()
        {
            if (urpAsset == null) return;

            var style = new GUIStyle(GUI.skin.label);
            style.fontSize = 14;
            style.normal.textColor = Color.yellow;

            GUILayout.BeginArea(new Rect(10, 10, 350, 120));
            GUILayout.Label($"=== Phase 03 低清画布测试 ===", style);
            GUILayout.Label($"Render Scale: {urpAsset.renderScale:F3}", style);
            GUILayout.Label($"Upscaling Filter: {urpAsset.upscalingFilter}", style);
            GUILayout.Label($"内部分辨率 ~{(int)(Screen.width * urpAsset.renderScale)}×{(int)(Screen.height * urpAsset.renderScale)}", style);
            GUILayout.Label($"按键: 1=1.0 2=0.75 3=0.5 4=0.333 0=Filter", style);
            GUILayout.EndArea();
        }
    }
}
