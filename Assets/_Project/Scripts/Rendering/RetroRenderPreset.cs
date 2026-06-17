using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[CreateAssetMenu(menuName = "CardDungeon/Rendering/Render Preset", fileName = "RetroRenderPreset")]
public sealed class RetroRenderPreset : ScriptableObject
{
    public const string PresetFolderPath = "Assets/_Project/Rendering/Presets";
    public const string ArchivePresetPath = PresetFolderPath + "/Retro_Archive_Current.asset";
    public const string LockedDarkRedPresetPath = PresetFolderPath + "/Retro_CurrentDarkRed_Locked.asset";

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

    [Header("Phase 05 / RetroFakeLit Shared")]
    public bool applyPhase05SharedValues = true;
    public float lightWrap = 0f;
    public Color phase05ShadowColor = new Color(0.105f, 0.075f, 0.052f, 1f);
    [Range(0f, 1f)] public float ambientStrength = 0.18f;
    public Color specColor = new Color(0.20f, 0.15f, 0.09f, 1f);
    [Range(0f, 1f)] public float specStrength = 0.025f;
    [Range(4f, 96f)] public float specPower = 18f;
    [Range(1f, 8f)] public float rampSteps = 4f;
    [Range(0f, 1f)] public float rampStrength = 0.28f;
    public Color fogColor = new Color(0.008f, 0.006f, 0.004f, 1f);
    public float fogStart = 2.2f;
    public float fogEnd = 5.5f;

    [Header("Scene Light Roles")]
    public bool applyLightRoles = true;
    public List<RetroRenderLightRolePreset> lightRoles = new List<RetroRenderLightRolePreset>();

    public void ApplyTo(RetroRenderRuntimeTargets targets)
    {
        if (targets == null)
        {
            return;
        }

        Apply(
            targets.phase07Material,
            targets.phase08Material,
            targets.volumeProfile,
            targets.ResolveRetroFakeLitMaterials(),
            targets.ResolveLightRoles());
    }

    public void Apply(
        Material phase07Material,
        Material phase08Material,
        VolumeProfile volumeProfile,
        IEnumerable<Material> retroFakeLitMaterials,
        IEnumerable<RetroRenderResolvedLightRole> resolvedLightRoles = null)
    {
        ApplyPhase07(phase07Material);
        ApplyPhase08(phase08Material);
        ApplyBloom(volumeProfile);
        ApplyPhase05(retroFakeLitMaterials);
        ApplyLights(resolvedLightRoles);
    }

    public void CaptureFrom(
        Material phase07Material,
        Material phase08Material,
        VolumeProfile volumeProfile,
        Material phase05SampleMaterial)
    {
        if (phase07Material != null)
        {
            lut = phase07Material.GetTexture("_UserLut") as Texture2D;
            contribution = GetMaterialFloat(phase07Material, "_Contribution", contribution);
            threshold = GetMaterialFloat(phase07Material, "_Threshold", threshold);
            thresholdSharpness = GetMaterialFloat(phase07Material, "_ThresholdSharpness", thresholdSharpness);
            lutStrength = GetMaterialFloat(phase07Material, "_LutStrength", lutStrength);
        }

        if (phase08Material != null)
        {
            virtualWidth = GetMaterialFloat(phase08Material, "_VirtualWidth", virtualWidth);
            virtualHeight = GetMaterialFloat(phase08Material, "_VirtualHeight", virtualHeight);
            pixelate = GetMaterialFloat(phase08Material, "_Pixelate", pixelate);
            posterizeLevels = GetMaterialFloat(phase08Material, "_PosterizeLevels", posterizeLevels);
            posterizeStrength = GetMaterialFloat(phase08Material, "_PosterizeStrength", posterizeStrength);
            paletteStrength = GetMaterialFloat(phase08Material, "_PaletteStrength", paletteStrength);
            paletteDarkThreshold = GetMaterialFloat(phase08Material, "_PaletteDarkThreshold", paletteDarkThreshold);
            ditherStrength = GetMaterialFloat(phase08Material, "_DitherStrength", ditherStrength);
            blackCrush = GetMaterialFloat(phase08Material, "_BlackCrush", blackCrush);
            contrast = GetMaterialFloat(phase08Material, "_Contrast", contrast);
            saturation = GetMaterialFloat(phase08Material, "_Saturation", saturation);
            vignetteStrength = GetMaterialFloat(phase08Material, "_VignetteStrength", vignetteStrength);
            vignetteRadius = GetMaterialFloat(phase08Material, "_VignetteRadius", vignetteRadius);
            scanlineStrength = GetMaterialFloat(phase08Material, "_ScanlineStrength", scanlineStrength);
            chromaticAberration = GetMaterialFloat(phase08Material, "_ChromaticAberration", chromaticAberration);
            noiseStrength = GetMaterialFloat(phase08Material, "_NoiseStrength", noiseStrength);
            crtCurvature = GetMaterialFloat(phase08Material, "_CrtCurvature", crtCurvature);
            crtEdgeSoftness = GetMaterialFloat(phase08Material, "_CrtEdgeSoftness", crtEdgeSoftness);
            crtGlowBleed = GetMaterialFloat(phase08Material, "_CrtGlowBleed", crtGlowBleed);
            horizontalJitter = GetMaterialFloat(phase08Material, "_HorizontalJitter", horizontalJitter);
            warmTint = GetMaterialColor(phase08Material, "_WarmTint", warmTint);
            coldTint = GetMaterialColor(phase08Material, "_ColdTint", coldTint);
        }

        if (volumeProfile != null && volumeProfile.TryGet<Bloom>(out var bloom))
        {
            bloomThreshold = bloom.threshold.value;
            bloomIntensity = bloom.intensity.value;
            bloomScatter = bloom.scatter.value;
            bloomTint = bloom.tint.value;
        }

        if (phase05SampleMaterial != null)
        {
            lightWrap = GetMaterialFloat(phase05SampleMaterial, "_LightWrap", lightWrap);
            phase05ShadowColor = GetMaterialColor(phase05SampleMaterial, "_ShadowColor", phase05ShadowColor);
            ambientStrength = GetMaterialFloat(phase05SampleMaterial, "_AmbientStrength", ambientStrength);
            specColor = GetMaterialColor(phase05SampleMaterial, "_SpecColor", specColor);
            specStrength = GetMaterialFloat(phase05SampleMaterial, "_SpecStrength", specStrength);
            specPower = GetMaterialFloat(phase05SampleMaterial, "_SpecPower", specPower);
            rampSteps = GetMaterialFloat(phase05SampleMaterial, "_RampSteps", rampSteps);
            rampStrength = GetMaterialFloat(phase05SampleMaterial, "_RampStrength", rampStrength);
            fogColor = GetMaterialColor(phase05SampleMaterial, "_FogColor", fogColor);
            fogStart = GetMaterialFloat(phase05SampleMaterial, "_FogStart", fogStart);
            fogEnd = GetMaterialFloat(phase05SampleMaterial, "_FogEnd", fogEnd);
        }
    }

    private void ApplyPhase07(Material material)
    {
        if (material == null)
        {
            return;
        }

        if (lut != null && material.HasProperty("_UserLut"))
        {
            material.SetTexture("_UserLut", lut);
        }

        SetMaterialFloat(material, "_Contribution", contribution);
        SetMaterialFloat(material, "_Threshold", threshold);
        SetMaterialFloat(material, "_ThresholdSharpness", thresholdSharpness);
        SetMaterialFloat(material, "_LutStrength", lutStrength);
        SetMaterialFloat(material, "_CompareDebug", 0f);
        SetMaterialFloat(material, "_DebugMask", 0f);
    }

    private void ApplyPhase08(Material material)
    {
        if (material == null)
        {
            return;
        }

        SetMaterialFloat(material, "_VirtualWidth", virtualWidth);
        SetMaterialFloat(material, "_VirtualHeight", virtualHeight);
        SetMaterialFloat(material, "_Pixelate", pixelate);
        SetMaterialFloat(material, "_PosterizeLevels", posterizeLevels);
        SetMaterialFloat(material, "_PosterizeStrength", posterizeStrength);
        SetMaterialFloat(material, "_PaletteStrength", paletteStrength);
        SetMaterialFloat(material, "_PaletteDarkThreshold", paletteDarkThreshold);
        SetMaterialFloat(material, "_DitherStrength", ditherStrength);
        SetMaterialFloat(material, "_BlackCrush", blackCrush);
        SetMaterialFloat(material, "_Contrast", contrast);
        SetMaterialFloat(material, "_Saturation", saturation);
        SetMaterialFloat(material, "_VignetteStrength", vignetteStrength);
        SetMaterialFloat(material, "_VignetteRadius", vignetteRadius);
        SetMaterialFloat(material, "_ScanlineStrength", scanlineStrength);
        SetMaterialFloat(material, "_ChromaticAberration", chromaticAberration);
        SetMaterialFloat(material, "_NoiseStrength", noiseStrength);
        SetMaterialFloat(material, "_CrtCurvature", crtCurvature);
        SetMaterialFloat(material, "_CrtEdgeSoftness", crtEdgeSoftness);
        SetMaterialFloat(material, "_CrtGlowBleed", crtGlowBleed);
        SetMaterialFloat(material, "_HorizontalJitter", horizontalJitter);
        SetMaterialColor(material, "_WarmTint", warmTint);
        SetMaterialColor(material, "_ColdTint", coldTint);
    }

    private void ApplyBloom(VolumeProfile volumeProfile)
    {
        if (volumeProfile == null || !volumeProfile.TryGet<Bloom>(out var bloom))
        {
            return;
        }

        bloom.threshold.value = bloomThreshold;
        bloom.threshold.overrideState = true;
        bloom.intensity.value = bloomIntensity;
        bloom.intensity.overrideState = true;
        bloom.scatter.value = bloomScatter;
        bloom.scatter.overrideState = true;
        bloom.tint.value = bloomTint;
        bloom.tint.overrideState = true;
    }

    private void ApplyPhase05(IEnumerable<Material> materials)
    {
        if (!applyPhase05SharedValues || materials == null)
        {
            return;
        }

        foreach (Material material in materials)
        {
            if (material == null)
            {
                continue;
            }

            SetMaterialFloat(material, "_LightWrap", lightWrap);
            SetMaterialColor(material, "_ShadowColor", phase05ShadowColor);
            SetMaterialFloat(material, "_AmbientStrength", ambientStrength);
            SetMaterialColor(material, "_SpecColor", specColor);
            SetMaterialFloat(material, "_SpecStrength", specStrength);
            SetMaterialFloat(material, "_SpecPower", specPower);
            SetMaterialFloat(material, "_RampSteps", rampSteps);
            SetMaterialFloat(material, "_RampStrength", rampStrength);
            SetMaterialColor(material, "_FogColor", fogColor);
            SetMaterialFloat(material, "_FogStart", fogStart);
            SetMaterialFloat(material, "_FogEnd", fogEnd);
        }
    }

    private void ApplyLights(IEnumerable<RetroRenderResolvedLightRole> resolvedRoles)
    {
        if (!applyLightRoles || resolvedRoles == null || lightRoles == null)
        {
            return;
        }

        foreach (RetroRenderResolvedLightRole resolvedRole in resolvedRoles)
        {
            if (resolvedRole.Light == null)
            {
                continue;
            }

            RetroRenderLightRolePreset rolePreset = lightRoles.Find(role =>
                role != null && string.Equals(role.roleName, resolvedRole.RoleName, StringComparison.OrdinalIgnoreCase));

            if (rolePreset == null)
            {
                continue;
            }

            rolePreset.ApplyTo(resolvedRole.Light);
        }
    }

    private static void SetMaterialFloat(Material material, string propertyName, float value)
    {
        if (material != null && material.HasProperty(propertyName))
        {
            material.SetFloat(propertyName, value);
        }
    }

    private static void SetMaterialColor(Material material, string propertyName, Color value)
    {
        if (material != null && material.HasProperty(propertyName))
        {
            material.SetColor(propertyName, value);
        }
    }

    private static float GetMaterialFloat(Material material, string propertyName, float fallback)
    {
        return material != null && material.HasProperty(propertyName) ? material.GetFloat(propertyName) : fallback;
    }

    private static Color GetMaterialColor(Material material, string propertyName, Color fallback)
    {
        return material != null && material.HasProperty(propertyName) ? material.GetColor(propertyName) : fallback;
    }
}

[Serializable]
public sealed class RetroRenderLightRolePreset
{
    public string roleName = "TableWarmLight";
    public bool setGameObjectActive = true;
    public bool active = true;
    public Color color = new Color(1f, 0.70f, 0.42f, 1f);
    public float intensity = 10f;
    public float range = 4f;
    public float spotAngle = 60f;
    public LightShadows shadows = LightShadows.Soft;
    [Range(0f, 1f)] public float shadowStrength = 0.25f;

    public void ApplyTo(Light light)
    {
        if (light == null)
        {
            return;
        }

        if (setGameObjectActive)
        {
            light.gameObject.SetActive(active);
        }

        light.enabled = active;
        light.color = color;
        light.intensity = intensity;
        light.range = range;
        if (light.type == LightType.Spot)
        {
            light.spotAngle = spotAngle;
        }

        light.shadows = shadows;
        light.shadowStrength = shadowStrength;
    }
}
