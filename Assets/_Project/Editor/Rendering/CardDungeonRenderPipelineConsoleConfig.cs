using Sirenix.OdinInspector;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[CreateAssetMenu(menuName = "CardDungeon/Rendering/Render Pipeline Console Config", fileName = "CardDungeonRenderPipelineConsoleConfig")]
public sealed class CardDungeonRenderPipelineConsoleConfig : ScriptableObject
{
    public const string AssetPath = "Assets/_Project/Editor/Rendering/CardDungeonRenderPipelineConsoleConfig.asset";

    [TitleGroup("说明")]
    [InfoBox("这个配置资产只保存控制台引用。真正的渲染数值会直接写回 URP 资产、Renderer 资产和材质资产。")]
    [ReadOnly]
    public string purpose = "项目渲染管线综合控制台引用表";

    [TitleGroup("核心资产")]
    [Required, AssetsOnly]
    public UniversalRenderPipelineAsset highFidelityPipeline;

    [TitleGroup("核心资产")]
    [Required, AssetsOnly]
    public ScriptableRendererData highFidelityRenderer;

    [TitleGroup("核心资产")]
    [Required, AssetsOnly]
    public Material retroPosterizeThresholdMaterial;

    [TitleGroup("核心资产")]
    [Required, AssetsOnly]
    public Material retroCompositeMaterial;

    [TitleGroup("核心资产")]
    [AssetsOnly]
    public DefaultAsset retroFakeLitMaterialFolder;

    [TitleGroup("核心资产")]
    [AssetsOnly]
    public Shader retroFakeLitShader;

    [TitleGroup("Phase 07 LUT")]
    [PreviewField(70, ObjectFieldAlignment.Left), AssetsOnly]
    public Texture2D dirtyBrownLut;

    [TitleGroup("Phase 07 LUT")]
    [PreviewField(70, ObjectFieldAlignment.Left), AssetsOnly]
    public Texture2D darkGreenLut;

    [TitleGroup("Phase 07 LUT")]
    [PreviewField(70, ObjectFieldAlignment.Left), AssetsOnly]
    public Texture2D candleRedLut;

    [TitleGroup("核心资产")]
    [Required, AssetsOnly]
    public VolumeProfile globalVolumeProfile;

    [TitleGroup("预设")]
    [AssetsOnly]
    public List<RetroRenderPreset> presets = new List<RetroRenderPreset>();

    [TitleGroup("操作")]
    [Button("按当前项目默认路径自动填充")]
    public void AutoPopulate()
    {
        highFidelityPipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>("Assets/Settings/URP-HighFidelity.asset");
        highFidelityRenderer = AssetDatabase.LoadAssetAtPath<ScriptableRendererData>("Assets/Settings/URP-HighFidelity-Renderer.asset");
        retroPosterizeThresholdMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Rendering/Materials/M_RetroPosterizeThreshold_Phase07.mat");
        retroCompositeMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/VisualPrototypes/InscryptionRetro/Materials/M_RetroComposite_Inscryption.mat");
        retroFakeLitShader = Shader.Find("CardDungeon/RetroFakeLit");
        retroFakeLitMaterialFolder = AssetDatabase.LoadAssetAtPath<DefaultAsset>("Assets/_Project/Rendering/Materials");
        dirtyBrownLut = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/_Project/Rendering/Textures/PosterizeLUT/T_LUT_DirtyBrown.asset");
        darkGreenLut = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/_Project/Rendering/Textures/PosterizeLUT/T_LUT_DarkGreen.asset");
        candleRedLut = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/_Project/Rendering/Textures/PosterizeLUT/T_LUT_CandleRed.asset");
        globalVolumeProfile = AssetDatabase.LoadAssetAtPath<VolumeProfile>("Assets/Settings/SampleSceneProfile.asset");

        if (presets == null) presets = new List<RetroRenderPreset>();
        RetroRenderPreset.SyncPresetList(this);

        EditorUtility.SetDirty(this);
    }

    private static void EnsureFolder(string folderPath)
    {
        string[] parts = folderPath.Split('/');
        string current = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next))
            {
                AssetDatabase.CreateFolder(current, parts[i]);
            }
            current = next;
        }
    }
}
