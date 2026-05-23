using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class RetroFakeLitMaterialConverter
{
    const string ShaderName = "CardDungeon/RetroFakeLit";
    const string MaterialFolder = "Assets/_Project/Rendering/Materials";

    static readonly HashSet<string> ExcludedObjectNames = new HashSet<string>
    {
        "card", "card (1)", "card (2)",
        "MonsterEye_L", "MonsterEye_R",
        "TableWarmLight", "CandleLight_1", "CandleLight_2", "CandleLight_3", "SoulBottle_GlowHint"
    };

    [MenuItem("Tools/CardDungeon Rendering/Convert Scene Ordinary Objects To RetroFakeLit")]
    public static void ConvertActiveSceneOrdinaryObjects()
    {
        ConvertRenderers(Object.FindObjectsOfType<MeshRenderer>(true)
            .Where(renderer => renderer.gameObject.scene == EditorSceneManager.GetActiveScene())
            .Where(IsOrdinarySceneRenderer));
    }

    [MenuItem("Tools/CardDungeon Rendering/Convert Selection To RetroFakeLit")]
    public static void ConvertSelection()
    {
        ConvertRenderers(Selection.gameObjects.SelectMany(go => go.GetComponentsInChildren<MeshRenderer>(true))
            .Where(renderer => !ExcludedObjectNames.Contains(renderer.gameObject.name)));
    }

    static bool IsOrdinarySceneRenderer(MeshRenderer renderer)
    {
        if (ExcludedObjectNames.Contains(renderer.gameObject.name))
            return false;

        string rootName = renderer.transform.root.name;
        if (rootName.StartsWith("card"))
            return false;

        if (rootName == "Lights")
            return false;

        return renderer.sharedMaterials.Any(material => material != null && material.shader != null && material.shader.name != ShaderName);
    }

    static void ConvertRenderers(IEnumerable<MeshRenderer> sourceRenderers)
    {
        Shader shader = Shader.Find(ShaderName);
        if (shader == null)
        {
            Debug.LogError($"Shader not found: {ShaderName}");
            return;
        }

        EnsureFolder(MaterialFolder);

        var materialCache = new Dictionary<Material, Material>();
        int rendererCount = 0;
        int slotCount = 0;
        int createdCount = 0;

        foreach (MeshRenderer renderer in sourceRenderers.Distinct())
        {
            Material[] materials = renderer.sharedMaterials;
            bool changed = false;

            for (int i = 0; i < materials.Length; i++)
            {
                Material source = materials[i];
                if (source == null || source.shader == shader)
                    continue;

                if (!materialCache.TryGetValue(source, out Material retroMaterial))
                {
                    retroMaterial = GetOrCreateConvertedMaterial(source, shader, renderer.transform.root.name, out bool created);
                    if (created)
                        createdCount++;
                    materialCache.Add(source, retroMaterial);
                }

                materials[i] = retroMaterial;
                slotCount++;
                changed = true;
            }

            if (!changed)
                continue;

            renderer.sharedMaterials = materials;
            EditorUtility.SetDirty(renderer);
            rendererCount++;
        }

        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log($"RetroFakeLit conversion complete: {rendererCount} renderers, {slotCount} material slots, {createdCount} new materials.");
    }

    static Material GetOrCreateConvertedMaterial(Material source, Shader shader, string contextName, out bool created)
    {
        string path = AssetDatabase.GenerateUniqueAssetPath($"{MaterialFolder}/M_RetroFakeLit_{Sanitize(source.name)}.mat");
        string existingPath = $"{MaterialFolder}/M_RetroFakeLit_{Sanitize(source.name)}.mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(existingPath);
        created = material == null;

        if (material == null)
        {
            material = new Material(shader);
            AssetDatabase.CreateAsset(material, path);
        }
        else
        {
            material.shader = shader;
        }

        ConfigureMaterial(material, source, contextName + " " + source.name);
        EditorUtility.SetDirty(material);
        return material;
    }

    static void ConfigureMaterial(Material material, Material source, string key)
    {
        material.SetTexture("_BaseMap", FindTexture(source));
        Color sourceColor = FindColor(source);
        Color tint = PresetTint(key);
        material.SetColor("_BaseColor", new Color(sourceColor.r * tint.r, sourceColor.g * tint.g, sourceColor.b * tint.b, sourceColor.a));
        material.SetFloat("_LightWrap", 0f);
        material.SetColor("_ShadowColor", new Color(0.105f, 0.075f, 0.052f, 1f));
        material.SetFloat("_AmbientStrength", 0.18f);
        material.SetColor("_SpecColor", new Color(0.20f, 0.15f, 0.09f, 1f));
        material.SetFloat("_SpecStrength", key.ToLowerInvariant().Contains("brass") ? 0.05f : 0.025f);
        material.SetFloat("_SpecPower", 18f);
        material.SetFloat("_RampSteps", 4f);
        material.SetFloat("_RampStrength", 0.28f);
        material.SetColor("_FogColor", new Color(0.008f, 0.006f, 0.004f, 1f));
        material.SetFloat("_FogStart", 2.2f);
        material.SetFloat("_FogEnd", 5.5f);
        material.SetFloat("_EmissionStrength", 0f);
    }

    static Texture FindTexture(Material source)
    {
        foreach (string name in new[] { "baseColorTexture", "_BaseMap", "_MainTex", "_Texture", "_BaseColorMap", "_Albedo" })
        {
            if (!source.HasProperty(name))
                continue;

            Texture texture = source.GetTexture(name);
            if (texture != null)
                return texture;
        }

        return null;
    }

    static Color FindColor(Material source)
    {
        foreach (string name in new[] { "baseColorFactor", "_BaseColor", "_Color", "_Color_Primary" })
        {
            if (source.HasProperty(name))
                return source.GetColor(name);
        }

        return Color.white;
    }

    static Color PresetTint(string key)
    {
        key = key.ToLowerInvariant();
        if (key.Contains("wood") || key.Contains("table") || key.Contains("bird") || key.Contains("chest") || key.Contains("elephant") || key.Contains("farmer"))
            return new Color(0.78f, 0.62f, 0.42f, 1f);
        if (key.Contains("brass") || key.Contains("candle"))
            return new Color(0.86f, 0.68f, 0.36f, 1f);
        if (key.Contains("cat") || key.Contains("concrete") || key.Contains("stone"))
            return new Color(0.86f, 0.80f, 0.70f, 1f);
        if (key.Contains("panda") || key.Contains("girl"))
            return new Color(0.72f, 0.62f, 0.52f, 1f);
        return new Color(0.80f, 0.70f, 0.58f, 1f);
    }

    static string Sanitize(string value)
    {
        string result = new string(value.Select(character => char.IsLetterOrDigit(character) ? character : '_').ToArray());
        while (result.Contains("__"))
            result = result.Replace("__", "_");
        return result.Trim('_');
    }

    static void EnsureFolder(string folder)
    {
        string[] parts = folder.Split('/');
        string current = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(current, parts[i]);
            current = next;
        }
    }
}
