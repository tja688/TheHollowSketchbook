using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[CreateAssetMenu(menuName = "CardDungeon/Rendering/Render Preset", fileName = "RetroRenderPreset")]
public sealed class RetroRenderPreset : ScriptableObject
{
    public const string PresetFolderPath = "Assets/_Project/Rendering/Presets";
    public const string ArchivePresetPath = PresetFolderPath + "/Retro_Archive_Current.asset";

    [Header("Phase 07 / Posterize")]
    public bool phase07FeatureActive = true;
    public Texture2D lut;
    [Range(0f, 1f)] public float contribution = 0.85f;
    [Range(0f, 1f)] public float threshold = 0.50f;
    [Range(0f, 32f)] public float thresholdSharpness = 12f;
    [Range(0f, 2f)] public float lutStrength = 1f;

    [Header("Phase 08 / Retro Composite")]
    public bool phase08FeatureActive = true;
    public float virtualWidth = 960f;
    public float virtualHeight = 540f;
    [Range(0f, 1f)] public float pixelate = 1f;
    [Range(2f, 16f)] public float posterizeLevels = 8f;
    [Range(0f, 1f)] public float posterizeStrength = 0.42f;
    [Range(0f, 1f)] public float paletteStrength = 0.34f;
    [Range(0f, 1f)] public float paletteDarkThreshold = 0.46f;
    [Range(0f, 1f)] public float ditherStrength = 0.03f;
    [Range(0f, 0.5f)] public float blackCrush = 0.10f;
    [Range(0.5f, 2f)] public float contrast = 1.28f;
    [Range(0f, 2f)] public float saturation = 0.92f;
    [Range(0f, 1f)] public float vignetteStrength = 0.62f;
    [Range(0f, 1f)] public float vignetteRadius = 0.72f;
    [Range(0f, 1f)] public float scanlineStrength = 0.12f;
    [Range(0f, 4f)] public float chromaticAberration = 0.45f;
    [Range(0f, 1f)] public float noiseStrength = 0.04f;
    [Range(0f, 0.25f)] public float crtCurvature = 0.03f;
    [Range(0f, 0.25f)] public float crtEdgeSoftness = 0.02f;
    [Range(0f, 1f)] public float crtGlowBleed = 0.22f;
    [Range(0f, 4f)] public float horizontalJitter = 0.08f;
    public Color warmTint = new Color(1.08f, 0.88f, 0.62f, 1f);
    public Color coldTint = new Color(0.10f, 0.36f, 0.32f, 1f);

    [Header("Bloom (Volume Override)")]
    [Range(0f, 2f)] public float bloomThreshold = 0.52f;
    [Range(0f, 5f)] public float bloomIntensity = 1.35f;
    [Range(0f, 1f)] public float bloomScatter = 0.42f;
    public Color bloomTint = new Color(1f, 0.7f, 0.38f, 1f);

    public void Apply(
        Material phase07Material,
        Material phase08Material,
        ScriptableRendererData rendererData,
        VolumeProfile volumeProfile,
        string posterizeFeatureName,
        string compositeFeatureName)
    {
        // Phase 07 Material
        if (phase07Material != null)
        {
            Undo.RecordObject(phase07Material, $"Apply Preset {name} Phase07");
            if (lut != null) phase07Material.SetTexture("_UserLut", lut);
            phase07Material.SetFloat("_Contribution", contribution);
            phase07Material.SetFloat("_Threshold", threshold);
            phase07Material.SetFloat("_ThresholdSharpness", thresholdSharpness);
            phase07Material.SetFloat("_LutStrength", lutStrength);
            phase07Material.SetFloat("_CompareDebug", 0f);
            phase07Material.SetFloat("_DebugMask", 0f);
            EditorUtility.SetDirty(phase07Material);
        }

        // Phase 08 Material
        if (phase08Material != null)
        {
            Undo.RecordObject(phase08Material, $"Apply Preset {name} Phase08");
            phase08Material.SetFloat("_VirtualWidth", virtualWidth);
            phase08Material.SetFloat("_VirtualHeight", virtualHeight);
            phase08Material.SetFloat("_Pixelate", pixelate);
            phase08Material.SetFloat("_PosterizeLevels", posterizeLevels);
            phase08Material.SetFloat("_PosterizeStrength", posterizeStrength);
            phase08Material.SetFloat("_PaletteStrength", paletteStrength);
            phase08Material.SetFloat("_PaletteDarkThreshold", paletteDarkThreshold);
            phase08Material.SetFloat("_DitherStrength", ditherStrength);
            phase08Material.SetFloat("_BlackCrush", blackCrush);
            phase08Material.SetFloat("_Contrast", contrast);
            phase08Material.SetFloat("_Saturation", saturation);
            phase08Material.SetFloat("_VignetteStrength", vignetteStrength);
            phase08Material.SetFloat("_VignetteRadius", vignetteRadius);
            phase08Material.SetFloat("_ScanlineStrength", scanlineStrength);
            phase08Material.SetFloat("_ChromaticAberration", chromaticAberration);
            phase08Material.SetFloat("_NoiseStrength", noiseStrength);
            phase08Material.SetFloat("_CrtCurvature", crtCurvature);
            phase08Material.SetFloat("_CrtEdgeSoftness", crtEdgeSoftness);
            phase08Material.SetFloat("_CrtGlowBleed", crtGlowBleed);
            phase08Material.SetFloat("_HorizontalJitter", horizontalJitter);
            phase08Material.SetColor("_WarmTint", warmTint);
            phase08Material.SetColor("_ColdTint", coldTint);
            EditorUtility.SetDirty(phase08Material);
        }

        // Renderer Features
        if (rendererData != null)
        {
            bool dirtyRenderer = false;
            foreach (var feature in rendererData.rendererFeatures)
            {
                if (feature == null) continue;
                if (feature.name.Equals(posterizeFeatureName, StringComparison.OrdinalIgnoreCase) ||
                    feature.name.Contains(posterizeFeatureName, StringComparison.OrdinalIgnoreCase))
                {
                    Undo.RecordObject(feature, $"Apply Preset {name} Feature");
                    using (var so = new SerializedObject(feature))
                    {
                        so.FindProperty("m_Active").boolValue = phase07FeatureActive;
                        so.ApplyModifiedPropertiesWithoutUndo();
                    }
                    EditorUtility.SetDirty(feature);
                    dirtyRenderer = true;
                }
                else if (feature.name.Equals(compositeFeatureName, StringComparison.OrdinalIgnoreCase) ||
                    feature.name.Contains(compositeFeatureName, StringComparison.OrdinalIgnoreCase))
                {
                    Undo.RecordObject(feature, $"Apply Preset {name} Feature");
                    using (var so = new SerializedObject(feature))
                    {
                        so.FindProperty("m_Active").boolValue = phase08FeatureActive;
                        so.ApplyModifiedPropertiesWithoutUndo();
                    }
                    EditorUtility.SetDirty(feature);
                    dirtyRenderer = true;
                }
            }
            if (dirtyRenderer) EditorUtility.SetDirty(rendererData);
        }

        // Bloom
        if (volumeProfile != null && volumeProfile.TryGet<Bloom>(out var bloom))
        {
            Undo.RecordObject(volumeProfile, $"Apply Preset {name} Bloom");
            bloom.threshold.value = bloomThreshold;
            bloom.threshold.overrideState = true;
            bloom.intensity.value = bloomIntensity;
            bloom.intensity.overrideState = true;
            bloom.scatter.value = bloomScatter;
            bloom.scatter.overrideState = true;
            bloom.tint.value = bloomTint;
            bloom.tint.overrideState = true;
            EditorUtility.SetDirty(volumeProfile);
        }
    }

    public void CaptureFrom(
        Material phase07Material,
        Material phase08Material,
        ScriptableRendererData rendererData,
        VolumeProfile volumeProfile,
        string posterizeFeatureName,
        string compositeFeatureName)
    {
        // Phase 07
        if (phase07Material != null)
        {
            lut = phase07Material.GetTexture("_UserLut") as Texture2D;
            contribution = phase07Material.GetFloat("_Contribution");
            threshold = phase07Material.GetFloat("_Threshold");
            thresholdSharpness = phase07Material.GetFloat("_ThresholdSharpness");
            lutStrength = phase07Material.GetFloat("_LutStrength");
        }

        // Phase 08
        if (phase08Material != null)
        {
            virtualWidth = phase08Material.GetFloat("_VirtualWidth");
            virtualHeight = phase08Material.GetFloat("_VirtualHeight");
            pixelate = phase08Material.GetFloat("_Pixelate");
            posterizeLevels = phase08Material.GetFloat("_PosterizeLevels");
            posterizeStrength = phase08Material.GetFloat("_PosterizeStrength");
            paletteStrength = phase08Material.GetFloat("_PaletteStrength");
            paletteDarkThreshold = phase08Material.GetFloat("_PaletteDarkThreshold");
            ditherStrength = phase08Material.GetFloat("_DitherStrength");
            blackCrush = phase08Material.GetFloat("_BlackCrush");
            contrast = phase08Material.GetFloat("_Contrast");
            saturation = phase08Material.GetFloat("_Saturation");
            vignetteStrength = phase08Material.GetFloat("_VignetteStrength");
            vignetteRadius = phase08Material.GetFloat("_VignetteRadius");
            scanlineStrength = phase08Material.GetFloat("_ScanlineStrength");
            chromaticAberration = phase08Material.GetFloat("_ChromaticAberration");
            noiseStrength = phase08Material.GetFloat("_NoiseStrength");
            crtCurvature = phase08Material.GetFloat("_CrtCurvature");
            crtEdgeSoftness = phase08Material.GetFloat("_CrtEdgeSoftness");
            crtGlowBleed = phase08Material.GetFloat("_CrtGlowBleed");
            horizontalJitter = phase08Material.GetFloat("_HorizontalJitter");
            warmTint = phase08Material.GetColor("_WarmTint");
            coldTint = phase08Material.GetColor("_ColdTint");
        }

        // Features
        if (rendererData != null)
        {
            foreach (var feature in rendererData.rendererFeatures)
            {
                if (feature == null) continue;
                if (feature.name.Equals(posterizeFeatureName, StringComparison.OrdinalIgnoreCase) ||
                    feature.name.Contains(posterizeFeatureName, StringComparison.OrdinalIgnoreCase))
                {
                    using (var so = new SerializedObject(feature))
                    {
                        phase07FeatureActive = so.FindProperty("m_Active").boolValue;
                    }
                }
                else if (feature.name.Equals(compositeFeatureName, StringComparison.OrdinalIgnoreCase) ||
                    feature.name.Contains(compositeFeatureName, StringComparison.OrdinalIgnoreCase))
                {
                    using (var so = new SerializedObject(feature))
                    {
                        phase08FeatureActive = so.FindProperty("m_Active").boolValue;
                    }
                }
            }
        }

        // Bloom
        if (volumeProfile != null && volumeProfile.TryGet<Bloom>(out var bloom))
        {
            bloomThreshold = bloom.threshold.value;
            bloomIntensity = bloom.intensity.value;
            bloomScatter = bloom.scatter.value;
            bloomTint = bloom.tint.value;
        }

        EditorUtility.SetDirty(this);
    }

    public static bool SyncPresetList(CardDungeonRenderPipelineConsoleConfig config)
    {
        if (config == null)
        {
            return false;
        }

        if (config.presets == null)
        {
            config.presets = new List<RetroRenderPreset>();
        }

        if (!AssetDatabase.IsValidFolder(PresetFolderPath))
        {
            bool hadEntries = config.presets.Count > 0;
            if (hadEntries)
            {
                config.presets.Clear();
            }
            return hadEntries;
        }

        List<RetroRenderPreset> discovered = AssetDatabase.FindAssets("t:RetroRenderPreset", new[] { PresetFolderPath })
            .Select(guid => AssetDatabase.LoadAssetAtPath<RetroRenderPreset>(AssetDatabase.GUIDToAssetPath(guid)))
            .Where(preset => preset != null)
            .OrderBy(preset => preset.name, StringComparer.Ordinal)
            .ToList();

        if (config.presets.SequenceEqual(discovered))
        {
            return false;
        }

        config.presets.Clear();
        config.presets.AddRange(discovered);
        return true;
    }

    public static void GenerateOrUpdateDefaultPresets(CardDungeonRenderPipelineConsoleConfig config)
    {
        if (config == null)
        {
            return;
        }

        EnsureFolder(PresetFolderPath);

        RetroRenderPreset clean = LoadOrCreatePreset($"{PresetFolderPath}/Retro_CleanDebug.asset", "Retro_CleanDebug");
        clean.phase07FeatureActive = true;
        clean.lut = config.dirtyBrownLut;
        clean.contribution = 0.35f;
        clean.threshold = 0.35f;
        clean.thresholdSharpness = 8f;
        clean.lutStrength = 0.5f;
        clean.phase08FeatureActive = true;
        clean.virtualWidth = 960f; clean.virtualHeight = 540f; clean.pixelate = 0.5f;
        clean.posterizeLevels = 12f; clean.posterizeStrength = 0.15f;
        clean.paletteStrength = 0.10f; clean.paletteDarkThreshold = 0.40f;
        clean.ditherStrength = 0.01f; clean.blackCrush = 0.02f;
        clean.contrast = 1.0f; clean.saturation = 1.0f;
        clean.vignetteStrength = 0.20f; clean.vignetteRadius = 0.85f;
        clean.scanlineStrength = 0f; clean.chromaticAberration = 0.10f;
        clean.noiseStrength = 0.01f; clean.crtCurvature = 0.01f;
        clean.crtEdgeSoftness = 0.01f; clean.crtGlowBleed = 0.10f;
        clean.horizontalJitter = 0.02f;
        clean.warmTint = new Color(1.02f, 0.95f, 0.85f, 1f);
        clean.coldTint = new Color(0.12f, 0.34f, 0.30f, 1f);
        clean.bloomThreshold = 1.2f; clean.bloomIntensity = 0.15f;
        clean.bloomScatter = 0.30f; clean.bloomTint = new Color(1f, 0.9f, 0.7f, 1f);
        EditorUtility.SetDirty(clean);

        RetroRenderPreset def = LoadOrCreatePreset($"{PresetFolderPath}/Retro_Default.asset", "Retro_Default");
        def.phase07FeatureActive = true;
        def.lut = config.dirtyBrownLut;
        def.contribution = 0.85f; def.threshold = 0.50f;
        def.thresholdSharpness = 12f; def.lutStrength = 1f;
        def.phase08FeatureActive = true;
        def.virtualWidth = 960f; def.virtualHeight = 540f; def.pixelate = 1f;
        def.posterizeLevels = 8f; def.posterizeStrength = 0.42f;
        def.paletteStrength = 0.34f; def.paletteDarkThreshold = 0.46f;
        def.ditherStrength = 0.03f; def.blackCrush = 0.10f;
        def.contrast = 1.28f; def.saturation = 0.92f;
        def.vignetteStrength = 0.62f; def.vignetteRadius = 0.72f;
        def.scanlineStrength = 0.12f; def.chromaticAberration = 0.45f;
        def.noiseStrength = 0.04f; def.crtCurvature = 0.03f;
        def.crtEdgeSoftness = 0.02f; def.crtGlowBleed = 0.22f;
        def.horizontalJitter = 0.08f;
        def.warmTint = new Color(1.08f, 0.88f, 0.62f, 1f);
        def.coldTint = new Color(0.10f, 0.36f, 0.32f, 1f);
        def.bloomThreshold = 0.52f; def.bloomIntensity = 1.35f;
        def.bloomScatter = 0.42f; def.bloomTint = new Color(1f, 0.7f, 0.38f, 1f);
        EditorUtility.SetDirty(def);

        RetroRenderPreset dark = LoadOrCreatePreset($"{PresetFolderPath}/Retro_DarkHorror.asset", "Retro_DarkHorror");
        dark.phase07FeatureActive = true;
        dark.lut = config.darkGreenLut;
        dark.contribution = 1.0f; dark.threshold = 0.55f;
        dark.thresholdSharpness = 16f; dark.lutStrength = 1.2f;
        dark.phase08FeatureActive = true;
        dark.virtualWidth = 960f; dark.virtualHeight = 540f; dark.pixelate = 1f;
        dark.posterizeLevels = 6f; dark.posterizeStrength = 0.65f;
        dark.paletteStrength = 0.65f; dark.paletteDarkThreshold = 0.55f;
        dark.ditherStrength = 0.06f; dark.blackCrush = 0.18f;
        dark.contrast = 1.5f; dark.saturation = 0.65f;
        dark.vignetteStrength = 0.85f; dark.vignetteRadius = 0.65f;
        dark.scanlineStrength = 0.15f; dark.chromaticAberration = 0.60f;
        dark.noiseStrength = 0.06f; dark.crtCurvature = 0.06f;
        dark.crtEdgeSoftness = 0.04f; dark.crtGlowBleed = 0.35f;
        dark.horizontalJitter = 0.15f;
        dark.warmTint = new Color(0.90f, 0.80f, 0.70f, 1f);
        dark.coldTint = new Color(0.05f, 0.30f, 0.35f, 1f);
        dark.bloomThreshold = 0.8f; dark.bloomIntensity = 0.30f;
        dark.bloomScatter = 0.50f; dark.bloomTint = new Color(0.8f, 0.9f, 1f, 1f);
        EditorUtility.SetDirty(dark);

        RetroRenderPreset combat = LoadOrCreatePreset($"{PresetFolderPath}/Retro_CombatReadability.asset", "Retro_CombatReadability");
        combat.phase07FeatureActive = true;
        combat.lut = config.dirtyBrownLut;
        combat.contribution = 0.65f; combat.threshold = 0.48f;
        combat.thresholdSharpness = 10f; combat.lutStrength = 0.9f;
        combat.phase08FeatureActive = true;
        combat.virtualWidth = 960f; combat.virtualHeight = 540f; combat.pixelate = 0.8f;
        combat.posterizeLevels = 10f; combat.posterizeStrength = 0.30f;
        combat.paletteStrength = 0.25f; combat.paletteDarkThreshold = 0.42f;
        combat.ditherStrength = 0.02f; combat.blackCrush = 0.06f;
        combat.contrast = 1.15f; combat.saturation = 1.10f;
        combat.vignetteStrength = 0.40f; combat.vignetteRadius = 0.80f;
        combat.scanlineStrength = 0.02f; combat.chromaticAberration = 0.20f;
        combat.noiseStrength = 0.02f; combat.crtCurvature = 0.02f;
        combat.crtEdgeSoftness = 0.02f; combat.crtGlowBleed = 0.18f;
        combat.horizontalJitter = 0.04f;
        combat.warmTint = new Color(1.05f, 0.92f, 0.72f, 1f);
        combat.coldTint = new Color(0.10f, 0.36f, 0.32f, 1f);
        combat.bloomThreshold = 0.9f; combat.bloomIntensity = 0.35f;
        combat.bloomScatter = 0.35f; combat.bloomTint = new Color(1f, 0.8f, 0.5f, 1f);
        EditorUtility.SetDirty(combat);

        SyncPresetList(config);
        EditorUtility.SetDirty(config);
        AssetDatabase.SaveAssets();
    }

    public static RetroRenderPreset CaptureCurrentToAsset(
        string assetPath,
        string assetName,
        CardDungeonRenderPipelineConsoleConfig config,
        string posterizeFeatureName,
        string compositeFeatureName)
    {
        if (config == null || string.IsNullOrEmpty(assetPath))
        {
            return null;
        }

        string folderPath = Path.GetDirectoryName(assetPath)?.Replace('\\', '/');
        if (!string.IsNullOrEmpty(folderPath))
        {
            EnsureFolder(folderPath);
        }

        RetroRenderPreset preset = LoadOrCreatePreset(assetPath, assetName);
        preset.CaptureFrom(config.retroPosterizeThresholdMaterial, config.retroCompositeMaterial,
            config.highFidelityRenderer, config.globalVolumeProfile,
            posterizeFeatureName, compositeFeatureName);
        SyncPresetList(config);
        EditorUtility.SetDirty(config);
        AssetDatabase.SaveAssets();
        return preset;
    }

    private static RetroRenderPreset LoadOrCreatePreset(string assetPath, string assetName)
    {
        RetroRenderPreset preset = AssetDatabase.LoadAssetAtPath<RetroRenderPreset>(assetPath);
        if (preset == null)
        {
            preset = CreateInstance<RetroRenderPreset>();
            preset.name = assetName;
            AssetDatabase.CreateAsset(preset, assetPath);
        }
        else if (preset.name != assetName)
        {
            preset.name = assetName;
        }

        return preset;
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
