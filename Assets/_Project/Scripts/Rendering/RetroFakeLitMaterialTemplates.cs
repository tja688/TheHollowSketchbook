using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public enum RetroFakeLitMaterialTemplateKind
{
    Wood,
    Iron,
    Silver,
    Gold
}

public readonly struct RetroFakeLitMaterialTemplate
{
    public readonly Color baseTint;
    public readonly Color specColor;
    public readonly float specStrength;
    public readonly float specPower;
    public readonly float rampStrength;

    public RetroFakeLitMaterialTemplate(Color baseTint, Color specColor, float specStrength, float specPower, float rampStrength)
    {
        this.baseTint = baseTint;
        this.specColor = specColor;
        this.specStrength = specStrength;
        this.specPower = specPower;
        this.rampStrength = rampStrength;
    }
}

public static class RetroFakeLitMaterialTemplates
{
    private const string RetroFakeLitShaderName = "CardDungeon/RetroFakeLit";

    public static RetroFakeLitMaterialTemplateKind GuessKind(string key)
    {
        string lowerKey = (key ?? string.Empty).ToLowerInvariant();
        if (ContainsAny(lowerKey, "silver", "chrome"))
        {
            return RetroFakeLitMaterialTemplateKind.Silver;
        }

        if (ContainsAny(lowerKey, "gold", "brass", "bronze", "candle"))
        {
            return RetroFakeLitMaterialTemplateKind.Gold;
        }

        if (ContainsAny(lowerKey, "iron", "steel", "metal"))
        {
            return RetroFakeLitMaterialTemplateKind.Iron;
        }

        return RetroFakeLitMaterialTemplateKind.Wood;
    }

    public static RetroFakeLitMaterialTemplate GetTemplate(RetroFakeLitMaterialTemplateKind kind)
    {
        return kind switch
        {
            RetroFakeLitMaterialTemplateKind.Iron => new RetroFakeLitMaterialTemplate(
                new Color(0.45f, 0.43f, 0.38f, 1f),
                new Color(0.18f, 0.19f, 0.18f, 1f),
                0.095f,
                32f,
                0.24f),
            RetroFakeLitMaterialTemplateKind.Silver => new RetroFakeLitMaterialTemplate(
                new Color(0.75f, 0.72f, 0.65f, 1f),
                new Color(0.32f, 0.33f, 0.30f, 1f),
                0.14f,
                44f,
                0.22f),
            RetroFakeLitMaterialTemplateKind.Gold => new RetroFakeLitMaterialTemplate(
                new Color(0.86f, 0.64f, 0.30f, 1f),
                new Color(0.38f, 0.27f, 0.12f, 1f),
                0.12f,
                36f,
                0.24f),
            _ => new RetroFakeLitMaterialTemplate(
                new Color(0.78f, 0.62f, 0.42f, 1f),
                new Color(0.20f, 0.15f, 0.09f, 1f),
                0.025f,
                18f,
                0.28f)
        };
    }

    public static void ConfigureConvertedMaterial(Material material, Color sourceColor, string key)
    {
        RetroFakeLitMaterialTemplateKind kind = GuessKind(key);
        RetroFakeLitMaterialTemplate template = GetTemplate(kind);
        ApplySharedProperties(material, template);
        SetColor(material, "_BaseColor", Multiply(sourceColor, template.baseTint));
    }

#if UNITY_EDITOR
    public static int ApplyToSelection(RetroFakeLitMaterialTemplateKind kind)
    {
        HashSet<Material> materials = new HashSet<Material>();
        foreach (UnityEngine.Object selectedObject in Selection.objects)
        {
            if (selectedObject is Material material)
            {
                TryAddRetroFakeLitMaterial(materials, material);
            }
            else if (selectedObject is GameObject gameObject)
            {
                foreach (Renderer renderer in gameObject.GetComponentsInChildren<Renderer>(true))
                {
                    foreach (Material rendererMaterial in renderer.sharedMaterials)
                    {
                        TryAddRetroFakeLitMaterial(materials, rendererMaterial);
                    }
                }
            }
        }

        if (materials.Count == 0)
        {
            return 0;
        }

        Undo.RecordObjects(materials.Cast<UnityEngine.Object>().ToArray(), $"Apply RetroFakeLit {kind} Template");
        RetroFakeLitMaterialTemplate template = GetTemplate(kind);
        foreach (Material material in materials)
        {
            ApplySharedProperties(material, template);
            SetColor(material, "_BaseColor", template.baseTint);
            EditorUtility.SetDirty(material);
        }

        return materials.Count;
    }
#endif

    private static void ApplySharedProperties(Material material, RetroFakeLitMaterialTemplate template)
    {
        if (material == null)
        {
            return;
        }

        SetFloat(material, "_LightWrap", 0f);
        SetColor(material, "_ShadowColor", new Color(0.105f, 0.075f, 0.052f, 1f));
        SetFloat(material, "_AmbientStrength", 0.18f);
        SetColor(material, "_SpecColor", template.specColor);
        SetFloat(material, "_SpecStrength", template.specStrength);
        SetFloat(material, "_SpecPower", template.specPower);
        SetFloat(material, "_RampSteps", 4f);
        SetFloat(material, "_RampStrength", template.rampStrength);
        SetColor(material, "_FogColor", new Color(0.008f, 0.006f, 0.004f, 1f));
        SetFloat(material, "_FogStart", 2.2f);
        SetFloat(material, "_FogEnd", 5.5f);
        SetFloat(material, "_EmissionStrength", 0f);
    }

    private static void TryAddRetroFakeLitMaterial(HashSet<Material> materials, Material material)
    {
        if (material != null && material.shader != null && material.shader.name == RetroFakeLitShaderName)
        {
            materials.Add(material);
        }
    }

    private static bool ContainsAny(string value, params string[] needles)
    {
        return needles.Any(needle => value.Contains(needle, StringComparison.OrdinalIgnoreCase));
    }

    private static Color Multiply(Color a, Color b)
    {
        return new Color(a.r * b.r, a.g * b.g, a.b * b.b, a.a);
    }

    private static void SetFloat(Material material, string propertyName, float value)
    {
        if (material.HasProperty(propertyName))
        {
            material.SetFloat(propertyName, value);
        }
    }

    private static void SetColor(Material material, string propertyName, Color value)
    {
        if (material.HasProperty(propertyName))
        {
            material.SetColor(propertyName, value);
        }
    }
}
