using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public static class RetroRenderPresetEditorUtility
{
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

        if (!AssetDatabase.IsValidFolder(RetroRenderPreset.PresetFolderPath))
        {
            bool hadEntries = config.presets.Count > 0;
            if (hadEntries)
            {
                config.presets.Clear();
            }
            return hadEntries;
        }

        List<RetroRenderPreset> discovered = AssetDatabase.FindAssets("t:RetroRenderPreset", new[] { RetroRenderPreset.PresetFolderPath })
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

    public static void ApplyPreset(
        RetroRenderPreset preset,
        CardDungeonRenderPipelineConsoleConfig config,
        string posterizeFeatureName,
        string compositeFeatureName,
        IReadOnlyList<Material> retroFakeLitMaterials)
    {
        if (preset == null || config == null)
        {
            return;
        }

        List<UnityEngine.Object> undoTargets = new List<UnityEngine.Object>();
        AddIfNotNull(undoTargets, config.retroPosterizeThresholdMaterial);
        AddIfNotNull(undoTargets, config.retroCompositeMaterial);
        AddIfNotNull(undoTargets, config.globalVolumeProfile);
        if (retroFakeLitMaterials != null)
        {
            undoTargets.AddRange(retroFakeLitMaterials.Where(material => material != null));
        }

        if (undoTargets.Count > 0)
        {
            Undo.RecordObjects(undoTargets.ToArray(), $"Apply Render Preset {preset.name}");
        }

        preset.Apply(
            config.retroPosterizeThresholdMaterial,
            config.retroCompositeMaterial,
            config.globalVolumeProfile,
            retroFakeLitMaterials);

        SetRendererFeatureActive(config.highFidelityRenderer, posterizeFeatureName, preset.phase07FeatureActive, preset.name);
        SetRendererFeatureActive(config.highFidelityRenderer, compositeFeatureName, preset.phase08FeatureActive, preset.name);

        MarkDirty(config.retroPosterizeThresholdMaterial);
        MarkDirty(config.retroCompositeMaterial);
        MarkDirty(config.globalVolumeProfile);
        if (retroFakeLitMaterials != null)
        {
            foreach (Material material in retroFakeLitMaterials)
            {
                MarkDirty(material);
            }
        }
    }

    public static RetroRenderPreset CaptureCurrentToAsset(
        string assetPath,
        string assetName,
        CardDungeonRenderPipelineConsoleConfig config,
        string posterizeFeatureName,
        string compositeFeatureName,
        IReadOnlyList<Material> retroFakeLitMaterials)
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
        preset.CaptureFrom(
            config.retroPosterizeThresholdMaterial,
            config.retroCompositeMaterial,
            config.globalVolumeProfile,
            retroFakeLitMaterials != null ? retroFakeLitMaterials.FirstOrDefault() : null);

        preset.phase07FeatureActive = IsRendererFeatureActive(config.highFidelityRenderer, posterizeFeatureName);
        preset.phase08FeatureActive = IsRendererFeatureActive(config.highFidelityRenderer, compositeFeatureName);

        EditorUtility.SetDirty(preset);
        SyncPresetList(config);
        EditorUtility.SetDirty(config);
        AssetDatabase.SaveAssets();
        return preset;
    }

    public static void GenerateOrUpdateDefaultPresets(CardDungeonRenderPipelineConsoleConfig config)
    {
        if (config == null)
        {
            return;
        }

        EnsureFolder(RetroRenderPreset.PresetFolderPath);

        ConfigureCleanDebug(LoadOrCreatePreset($"{RetroRenderPreset.PresetFolderPath}/Retro_CleanDebug.asset", "Retro_CleanDebug"), config);
        ConfigureDefault(LoadOrCreatePreset($"{RetroRenderPreset.PresetFolderPath}/Retro_Default.asset", "Retro_Default"), config);
        ConfigureDarkHorror(LoadOrCreatePreset($"{RetroRenderPreset.PresetFolderPath}/Retro_DarkHorror.asset", "Retro_DarkHorror"), config);
        ConfigureCombatReadability(LoadOrCreatePreset($"{RetroRenderPreset.PresetFolderPath}/Retro_CombatReadability.asset", "Retro_CombatReadability"), config);
        ConfigureCurrentDarkRed(LoadOrCreatePreset(RetroRenderPreset.LockedDarkRedPresetPath, "Retro_CurrentDarkRed_Locked"), config);
        ConfigureShadeGreen(LoadOrCreatePreset($"{RetroRenderPreset.PresetFolderPath}/Retro_ShadeGreen.asset", "Retro_ShadeGreen"), config);
        ConfigureGhostBlue(LoadOrCreatePreset($"{RetroRenderPreset.PresetFolderPath}/Retro_GhostBlue.asset", "Retro_GhostBlue"), config);
        ConfigureMysticPurple(LoadOrCreatePreset($"{RetroRenderPreset.PresetFolderPath}/Retro_MysticPurple.asset", "Retro_MysticPurple"), config);

        SyncPresetList(config);
        EditorUtility.SetDirty(config);
        AssetDatabase.SaveAssets();
    }

    private static void ConfigureCleanDebug(RetroRenderPreset preset, CardDungeonRenderPipelineConsoleConfig config)
    {
        SetCommonPreset(preset, config.dirtyBrownLut, 0.35f, 0.35f, 8f, 0.5f);
        SetComposite(preset, 960f, 540f, 0.5f, 12f, 0.15f, 0.10f, 0.40f, 0.01f, 0.02f, 1.0f, 1.0f, 0.20f, 0.85f, 0f, 0.10f, 0.01f, 0.01f, 0.01f, 0.10f, 0.02f, new Color(1.02f, 0.95f, 0.85f, 1f), new Color(0.12f, 0.34f, 0.30f, 1f));
        SetPhase05(preset, new Color(0.105f, 0.075f, 0.052f, 1f), 0.18f, new Color(0.20f, 0.15f, 0.09f, 1f), 0.025f, 18f, new Color(0.008f, 0.006f, 0.004f, 1f));
        SetBloom(preset, 1.2f, 0.15f, 0.30f, new Color(1f, 0.9f, 0.7f, 1f));
        SetWarmLightRoles(preset, new Color(1f, 0.70f, 0.42f, 1f), new Color(1f, 0.70f, 0.42f, 1f), new Color(1f, 0.70f, 0.42f, 1f));
        EditorUtility.SetDirty(preset);
    }

    private static void ConfigureDefault(RetroRenderPreset preset, CardDungeonRenderPipelineConsoleConfig config)
    {
        SetCommonPreset(preset, config.dirtyBrownLut, 0.85f, 0.50f, 12f, 1f);
        SetComposite(preset, 960f, 540f, 1f, 8f, 0.42f, 0.34f, 0.46f, 0.03f, 0.10f, 1.28f, 0.92f, 0.62f, 0.72f, 0.12f, 0.45f, 0.04f, 0.03f, 0.02f, 0.22f, 0.08f, new Color(1.08f, 0.88f, 0.62f, 1f), new Color(0.10f, 0.36f, 0.32f, 1f));
        SetPhase05(preset, new Color(0.105f, 0.075f, 0.052f, 1f), 0.18f, new Color(0.20f, 0.15f, 0.09f, 1f), 0.025f, 18f, new Color(0.008f, 0.006f, 0.004f, 1f));
        SetBloom(preset, 0.52f, 1.35f, 0.42f, new Color(1f, 0.7f, 0.38f, 1f));
        SetWarmLightRoles(preset, new Color(1f, 0.70f, 0.42f, 1f), new Color(1f, 0.70f, 0.42f, 1f), new Color(1f, 0.70f, 0.42f, 1f));
        EditorUtility.SetDirty(preset);
    }

    private static void ConfigureDarkHorror(RetroRenderPreset preset, CardDungeonRenderPipelineConsoleConfig config)
    {
        SetCommonPreset(preset, config.darkGreenLut, 1.0f, 0.55f, 16f, 1.2f);
        SetComposite(preset, 960f, 540f, 1f, 6f, 0.65f, 0.65f, 0.55f, 0.06f, 0.18f, 1.5f, 0.65f, 0.85f, 0.65f, 0.15f, 0.60f, 0.06f, 0.06f, 0.04f, 0.35f, 0.15f, new Color(0.90f, 0.80f, 0.70f, 1f), new Color(0.05f, 0.30f, 0.35f, 1f));
        SetPhase05(preset, new Color(0.065f, 0.085f, 0.065f, 1f), 0.15f, new Color(0.12f, 0.18f, 0.14f, 1f), 0.02f, 20f, new Color(0.004f, 0.010f, 0.008f, 1f));
        SetBloom(preset, 0.8f, 0.30f, 0.50f, new Color(0.8f, 0.9f, 1f, 1f));
        SetWarmLightRoles(preset, new Color(0.65f, 0.85f, 0.64f, 1f), new Color(0.55f, 0.75f, 0.56f, 1f), new Color(0.45f, 0.70f, 0.75f, 1f));
        EditorUtility.SetDirty(preset);
    }

    private static void ConfigureCombatReadability(RetroRenderPreset preset, CardDungeonRenderPipelineConsoleConfig config)
    {
        SetCommonPreset(preset, config.dirtyBrownLut, 0.65f, 0.48f, 10f, 0.9f);
        SetComposite(preset, 960f, 540f, 0.8f, 10f, 0.30f, 0.25f, 0.42f, 0.02f, 0.06f, 1.15f, 1.10f, 0.40f, 0.80f, 0.02f, 0.20f, 0.02f, 0.02f, 0.02f, 0.18f, 0.04f, new Color(1.05f, 0.92f, 0.72f, 1f), new Color(0.10f, 0.36f, 0.32f, 1f));
        SetPhase05(preset, new Color(0.105f, 0.075f, 0.052f, 1f), 0.18f, new Color(0.20f, 0.15f, 0.09f, 1f), 0.025f, 18f, new Color(0.008f, 0.006f, 0.004f, 1f));
        SetBloom(preset, 0.9f, 0.35f, 0.35f, new Color(1f, 0.8f, 0.5f, 1f));
        SetWarmLightRoles(preset, new Color(1f, 0.72f, 0.45f, 1f), new Color(1f, 0.72f, 0.45f, 1f), new Color(1f, 0.72f, 0.45f, 1f));
        EditorUtility.SetDirty(preset);
    }

    private static void ConfigureCurrentDarkRed(RetroRenderPreset preset, CardDungeonRenderPipelineConsoleConfig config)
    {
        SetCommonPreset(preset, config.dirtyBrownLut, 0.359f, 0.412f, 8f, 0.8f);
        SetComposite(preset, 1920f, 1080f, 0.818f, 10.28f, 0.32f, 0.20f, 0.38f, 0f, 0.06f, 1.22f, 0.86f, 0.58f, 0.74f, 0f, 0.311f, 0f, 0f, 0.0227f, 0.053f, 0f, new Color(1.06f, 0.84f, 0.58f, 1f), new Color(0.12f, 0.26f, 0.23f, 1f));
        SetPhase05(preset, new Color(0.105f, 0.075f, 0.052f, 1f), 0.18f, new Color(0.20f, 0.15f, 0.09f, 1f), 0.025f, 18f, new Color(0.008f, 0.006f, 0.004f, 1f));
        SetBloom(preset, 1.05f, 0.28f, 0.32f, new Color(1f, 0.78f, 0.48f, 1f));
        SetWarmLightRoles(preset, new Color(1f, 0.70f, 0.42f, 1f), new Color(1f, 0.70f, 0.42f, 1f), new Color(1f, 0.70f, 0.42f, 1f));
        EditorUtility.SetDirty(preset);
    }

    private static void ConfigureShadeGreen(RetroRenderPreset preset, CardDungeonRenderPipelineConsoleConfig config)
    {
        SetCommonPreset(preset, config.darkGreenLut, 0.42f, 0.43f, 8f, 0.85f);
        SetComposite(preset, 1920f, 1080f, 0.818f, 10.28f, 0.32f, 0.24f, 0.38f, 0f, 0.06f, 1.22f, 0.86f, 0.58f, 0.74f, 0f, 0.311f, 0f, 0f, 0.0227f, 0.053f, 0f, new Color(0.90f, 0.98f, 0.76f, 1f), new Color(0.06f, 0.38f, 0.30f, 1f));
        SetPhase05(preset, new Color(0.045f, 0.085f, 0.065f, 1f), 0.18f, new Color(0.10f, 0.18f, 0.14f, 1f), 0.025f, 18f, new Color(0.004f, 0.010f, 0.008f, 1f));
        SetBloom(preset, 1.05f, 0.22f, 0.32f, new Color(0.55f, 0.95f, 0.68f, 1f));
        SetWarmLightRoles(preset, new Color(0.58f, 0.90f, 0.62f, 1f), new Color(0.50f, 0.80f, 0.56f, 1f), new Color(0.48f, 0.82f, 0.68f, 1f));
        EditorUtility.SetDirty(preset);
    }

    private static void ConfigureGhostBlue(RetroRenderPreset preset, CardDungeonRenderPipelineConsoleConfig config)
    {
        SetCommonPreset(preset, config.ghostBlueLut, 0.40f, 0.44f, 8f, 0.85f);
        SetComposite(preset, 1920f, 1080f, 0.818f, 10.28f, 0.32f, 0.22f, 0.38f, 0f, 0.06f, 1.22f, 0.86f, 0.58f, 0.74f, 0f, 0.311f, 0f, 0f, 0.0227f, 0.053f, 0f, new Color(0.82f, 0.90f, 1.05f, 1f), new Color(0.06f, 0.16f, 0.42f, 1f));
        SetPhase05(preset, new Color(0.045f, 0.055f, 0.095f, 1f), 0.18f, new Color(0.12f, 0.16f, 0.24f, 1f), 0.025f, 18f, new Color(0.003f, 0.004f, 0.012f, 1f));
        SetBloom(preset, 1.05f, 0.25f, 0.32f, new Color(0.45f, 0.75f, 1f, 1f));
        SetWarmLightRoles(preset, new Color(0.46f, 0.66f, 1f, 1f), new Color(0.38f, 0.58f, 0.95f, 1f), new Color(0.50f, 0.70f, 1f, 1f));
        EditorUtility.SetDirty(preset);
    }

    private static void ConfigureMysticPurple(RetroRenderPreset preset, CardDungeonRenderPipelineConsoleConfig config)
    {
        SetCommonPreset(preset, config.mysticPurpleLut, 0.44f, 0.44f, 8f, 0.85f);
        SetComposite(preset, 1920f, 1080f, 0.818f, 10.28f, 0.32f, 0.24f, 0.38f, 0f, 0.06f, 1.22f, 0.86f, 0.58f, 0.74f, 0f, 0.311f, 0f, 0f, 0.0227f, 0.053f, 0f, new Color(1.05f, 0.78f, 1.05f, 1f), new Color(0.22f, 0.08f, 0.36f, 1f));
        SetPhase05(preset, new Color(0.075f, 0.045f, 0.10f, 1f), 0.18f, new Color(0.20f, 0.12f, 0.26f, 1f), 0.025f, 18f, new Color(0.008f, 0.004f, 0.012f, 1f));
        SetBloom(preset, 1.05f, 0.24f, 0.32f, new Color(0.85f, 0.55f, 1f, 1f));
        SetWarmLightRoles(preset, new Color(0.86f, 0.48f, 1f, 1f), new Color(0.70f, 0.40f, 0.92f, 1f), new Color(0.80f, 0.55f, 1f, 1f));
        EditorUtility.SetDirty(preset);
    }

    private static void SetCommonPreset(RetroRenderPreset preset, Texture2D lut, float contribution, float threshold, float sharpness, float lutStrength)
    {
        preset.phase07FeatureActive = true;
        preset.phase08FeatureActive = true;
        preset.lut = lut;
        preset.contribution = contribution;
        preset.threshold = threshold;
        preset.thresholdSharpness = sharpness;
        preset.lutStrength = lutStrength;
    }

    private static void SetComposite(
        RetroRenderPreset preset,
        float virtualWidth,
        float virtualHeight,
        float pixelate,
        float posterizeLevels,
        float posterizeStrength,
        float paletteStrength,
        float paletteDarkThreshold,
        float ditherStrength,
        float blackCrush,
        float contrast,
        float saturation,
        float vignetteStrength,
        float vignetteRadius,
        float scanlineStrength,
        float chromaticAberration,
        float noiseStrength,
        float crtCurvature,
        float crtEdgeSoftness,
        float crtGlowBleed,
        float horizontalJitter,
        Color warmTint,
        Color coldTint)
    {
        preset.virtualWidth = virtualWidth;
        preset.virtualHeight = virtualHeight;
        preset.pixelate = pixelate;
        preset.posterizeLevels = posterizeLevels;
        preset.posterizeStrength = posterizeStrength;
        preset.paletteStrength = paletteStrength;
        preset.paletteDarkThreshold = paletteDarkThreshold;
        preset.ditherStrength = ditherStrength;
        preset.blackCrush = blackCrush;
        preset.contrast = contrast;
        preset.saturation = saturation;
        preset.vignetteStrength = vignetteStrength;
        preset.vignetteRadius = vignetteRadius;
        preset.scanlineStrength = scanlineStrength;
        preset.chromaticAberration = chromaticAberration;
        preset.noiseStrength = noiseStrength;
        preset.crtCurvature = crtCurvature;
        preset.crtEdgeSoftness = crtEdgeSoftness;
        preset.crtGlowBleed = crtGlowBleed;
        preset.horizontalJitter = horizontalJitter;
        preset.warmTint = warmTint;
        preset.coldTint = coldTint;
    }

    private static void SetPhase05(RetroRenderPreset preset, Color shadow, float ambient, Color spec, float specStrength, float specPower, Color fog)
    {
        preset.applyPhase05SharedValues = true;
        preset.lightWrap = 0f;
        preset.phase05ShadowColor = shadow;
        preset.ambientStrength = ambient;
        preset.specColor = spec;
        preset.specStrength = specStrength;
        preset.specPower = specPower;
        preset.rampSteps = 4f;
        preset.rampStrength = 0.28f;
        preset.fogColor = fog;
        preset.fogStart = 2.2f;
        preset.fogEnd = 5.5f;
    }

    private static void SetBloom(RetroRenderPreset preset, float threshold, float intensity, float scatter, Color tint)
    {
        preset.bloomThreshold = threshold;
        preset.bloomIntensity = intensity;
        preset.bloomScatter = scatter;
        preset.bloomTint = tint;
    }

    private static void SetWarmLightRoles(RetroRenderPreset preset, Color tableMain, Color tableFill, Color player)
    {
        preset.applyLightRoles = true;
        preset.lightRoles = new List<RetroRenderLightRolePreset>
        {
            CreateLightRole("TableWarmLight", tableMain, 30.1f, 4.08f, 60.77f, 0.35f),
            CreateLightRole("TableWarmLight (1)", tableFill, 4.2f, 2.08f, 60.77f, 0.25f),
            CreateLightRole("playerlight", player, 10f, 4.08f, 60.77f, 0.20f)
        };
    }

    private static RetroRenderLightRolePreset CreateLightRole(string name, Color color, float intensity, float range, float spotAngle, float shadowStrength)
    {
        return new RetroRenderLightRolePreset
        {
            roleName = name,
            setGameObjectActive = true,
            active = true,
            color = color,
            intensity = intensity,
            range = range,
            spotAngle = spotAngle,
            shadows = LightShadows.Soft,
            shadowStrength = shadowStrength
        };
    }

    private static RetroRenderPreset LoadOrCreatePreset(string assetPath, string assetName)
    {
        RetroRenderPreset preset = AssetDatabase.LoadAssetAtPath<RetroRenderPreset>(assetPath);
        if (preset == null)
        {
            preset = ScriptableObject.CreateInstance<RetroRenderPreset>();
            preset.name = assetName;
            AssetDatabase.CreateAsset(preset, assetPath);
        }
        else if (preset.name != assetName)
        {
            preset.name = assetName;
        }

        return preset;
    }

    private static bool IsRendererFeatureActive(ScriptableRendererData rendererData, string featureName)
    {
        ScriptableRendererFeature feature = FindRendererFeature(rendererData, featureName);
        if (feature == null)
        {
            return false;
        }

        SerializedObject so = new SerializedObject(feature);
        SerializedProperty property = so.FindProperty("m_Active");
        return property != null && property.boolValue;
    }

    private static void SetRendererFeatureActive(ScriptableRendererData rendererData, string featureName, bool active, string presetName)
    {
        ScriptableRendererFeature feature = FindRendererFeature(rendererData, featureName);
        if (feature == null)
        {
            return;
        }

        SerializedObject so = new SerializedObject(feature);
        SerializedProperty property = so.FindProperty("m_Active");
        if (property == null)
        {
            return;
        }

        Undo.RecordObject(feature, $"Apply Render Preset {presetName} Feature");
        property.boolValue = active;
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(feature);
        MarkDirty(rendererData);
    }

    private static ScriptableRendererFeature FindRendererFeature(ScriptableRendererData rendererData, string featureName)
    {
        if (rendererData == null)
        {
            return null;
        }

        string rendererPath = AssetDatabase.GetAssetPath(rendererData);
        UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath(rendererPath);
        return assets.OfType<ScriptableRendererFeature>()
            .FirstOrDefault(feature => feature != null &&
                (feature.name.Equals(featureName, StringComparison.OrdinalIgnoreCase) ||
                 feature.name.Contains(featureName, StringComparison.OrdinalIgnoreCase)));
    }

    private static void AddIfNotNull(List<UnityEngine.Object> targets, UnityEngine.Object target)
    {
        if (target != null)
        {
            targets.Add(target);
        }
    }

    private static void MarkDirty(UnityEngine.Object target)
    {
        if (target != null)
        {
            EditorUtility.SetDirty(target);
        }
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
