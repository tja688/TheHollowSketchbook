using System;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class CardPrintBakeUtility
{
    public struct Request
    {
        public string title;
        public string targetObjectName;
        public string baseTexturePath;
        public string illustrationPath;
        public string iconPath;
        public string fontAssetPath;
        public string outputTexturePath;
        public string outputMaterialPath;
    }

    private const string MenuPath = "Tools/CardDungeon Rendering/Bake Same Target Card Print";
    private const int OutputSize = 1024;
    private static readonly Color InkColor = new Color(0.115f, 0.065f, 0.035f, 1f);

    [MenuItem(MenuPath)]
    public static void BakeSameTargetCardPrint()
    {
        BakeAndApply(new Request
        {
            title = "Same Target",
            targetObjectName = "卡牌-原版材质3",
            baseTexturePath = "Assets/Arts/Models/gezinkte_spielkarte_km-o.811/textures/card-colour_baseColor.png",
            illustrationPath = "Assets/Arts/Pictures/卡面测试素材.png",
            iconPath = "Assets/Arts/Pictures/people.png",
            fontAssetPath = "Assets/Arts/Fronts/Rock_Salt/RockSalt-Regular SDF.asset",
            outputTexturePath = "Assets/_Project/Rendering/CardPrints/T_CardPrint_SameTarget.png",
            outputMaterialPath = "Assets/_Project/Rendering/Materials/CardPrints/M_CardPrint_SameTarget.mat"
        });
    }

    public static void BakeAndApply(Request request)
    {
        EnsureFolder(Path.GetDirectoryName(request.outputTexturePath));
        EnsureFolder(Path.GetDirectoryName(request.outputMaterialPath));

        Texture2D baked = BakeTexture(request);
        File.WriteAllBytes(request.outputTexturePath, baked.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(baked);
        AssetDatabase.ImportAsset(request.outputTexturePath, ImportAssetOptions.ForceUpdate);
        ConfigureTexture(request.outputTexturePath);

        Texture2D bakedAsset = AssetDatabase.LoadAssetAtPath<Texture2D>(request.outputTexturePath);
        Material material = LoadOrCreateMaterial(request.outputMaterialPath);
        material.SetTexture("_BaseMap", bakedAsset);
        material.SetColor("_BaseColor", Color.white);
        material.SetColor("_ShadowColor", new Color(0.105f, 0.075f, 0.052f, 1f));
        material.SetFloat("_AmbientStrength", 0.18f);
        material.SetFloat("_RampSteps", 4f);
        material.SetFloat("_RampStrength", 0.28f);
        material.SetFloat("_SpecStrength", 0.025f);
        material.SetFloat("_SpecPower", 18f);
        material.SetColor("_FogColor", new Color(0.008f, 0.006f, 0.004f, 1f));
        material.SetFloat("_FogStart", 2.2f);
        material.SetFloat("_FogEnd", 5.5f);
        EditorUtility.SetDirty(material);
        AssetDatabase.SaveAssets();

        GameObject target = GameObject.Find(request.targetObjectName);
        if (target == null)
        {
            throw new InvalidOperationException($"Cannot find scene object: {request.targetObjectName}");
        }

        MeshRenderer renderer = target.GetComponent<MeshRenderer>();
        if (renderer == null)
        {
            throw new InvalidOperationException($"Target has no MeshRenderer: {request.targetObjectName}");
        }

        Undo.RecordObject(renderer, "Apply card print material");
        renderer.sharedMaterial = material;
        EditorUtility.SetDirty(renderer);
        EditorSceneManager.MarkSceneDirty(target.scene);
        EditorSceneManager.SaveScene(target.scene);
    }

    private static Texture2D BakeTexture(Request request)
    {
        Texture2D baseTexture = ReadableCopy(AssetDatabase.LoadAssetAtPath<Texture2D>(request.baseTexturePath), OutputSize, OutputSize);
        Texture2D illustration = ReadableCopy(AssetDatabase.LoadAssetAtPath<Texture2D>(request.illustrationPath));
        Texture2D icon = ReadableCopy(AssetDatabase.LoadAssetAtPath<Texture2D>(request.iconPath));
        TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(request.fontAssetPath);
        if (baseTexture == null || illustration == null || icon == null || font == null)
        {
            throw new InvalidOperationException("Card print bake source asset missing.");
        }

        Color[] pixels = baseTexture.GetPixels();
        RectInt front = new RectInt(512, 22, 512, 738);

        LightlyDirtyExistingCard(pixels, OutputSize, front, 0.018f, 7);

        RectInt artRect = new RectInt(front.x + 6, front.y - 10, front.width - 12, front.height + 20);
        DrawDoodleImage(pixels, OutputSize, illustration, artRect, front, 0.96f, 0.035f, 0.16f, 19);

        Texture2D titleMask = RenderTitleMask(request.title, font, 704, 176);
        RectInt titleRect = new RectInt(front.x - 24, front.y + 560, front.width + 48, 156);
        DrawInkMask(pixels, OutputSize, titleMask, titleRect, front, 0.95f, 0.055f, 23);

        RectInt iconRect = new RectInt(front.x + 176, front.y + 72, 160, 160);
        DrawDoodleImage(pixels, OutputSize, icon, iconRect, front, 1.0f, 0.02f, 0.04f, 31);

        LightlyDirtyExistingCard(pixels, OutputSize, front, 0.012f, 41);
        baseTexture.SetPixels(pixels);
        baseTexture.Apply(false, false);
        UnityEngine.Object.DestroyImmediate(illustration);
        UnityEngine.Object.DestroyImmediate(icon);
        UnityEngine.Object.DestroyImmediate(titleMask);
        return baseTexture;
    }

    private static void DrawDoodleImage(Color[] pixels, int width, Texture2D source, RectInt rect, RectInt clip, float opacity, float erosion, float sourceInfluence, int seed)
    {
        RectInt draw = Intersect(rect, clip);
        float targetAspect = rect.width / (float)rect.height;
        float sourceAspect = source.width / (float)source.height;
        for (int y = draw.yMin; y < draw.yMax; y++)
        {
            for (int x = draw.xMin; x < draw.xMax; x++)
            {
                float u = (x - rect.xMin + 0.5f) / rect.width;
                float v = (y - rect.yMin + 0.5f) / rect.height;
                if (!MapContainUv(ref u, ref v, sourceAspect, targetAspect)) continue;

                Color src = source.GetPixelBilinear(u, v);
                float luma = src.r * 0.299f + src.g * 0.587f + src.b * 0.114f;
                float mark = src.a * Mathf.SmoothStep(0.02f, 0.78f, 1f - luma);
                if (mark <= 0.001f) continue;

                float noise = ValueNoise(x, y, seed) * 0.55f + ValueNoise(x * 3, y * 3, seed + 5) * 0.45f;
                float a = mark * opacity * Mathf.SmoothStep(erosion, 1f, noise * 0.35f + mark * 0.92f);
                Color sourceInk = new Color(src.r * 0.38f, src.g * 0.31f, src.b * 0.24f, 1f);
                BlendInk(pixels, width, x, y, a, Color.Lerp(InkColor, sourceInk, sourceInfluence));
            }
        }
    }

    private static bool MapContainUv(ref float u, ref float v, float sourceAspect, float targetAspect)
    {
        if (sourceAspect > targetAspect)
        {
            float h = targetAspect / sourceAspect;
            float yMin = 0.5f - h * 0.5f;
            if (v < yMin || v > yMin + h) return false;
            v = (v - yMin) / h;
        }
        else
        {
            float w = sourceAspect / targetAspect;
            float xMin = 0.5f - w * 0.5f;
            if (u < xMin || u > xMin + w) return false;
            u = (u - xMin) / w;
        }
        return true;
    }

    private static RectInt Intersect(RectInt a, RectInt b)
    {
        int xMin = Mathf.Max(a.xMin, b.xMin);
        int yMin = Mathf.Max(a.yMin, b.yMin);
        int xMax = Mathf.Min(a.xMax, b.xMax);
        int yMax = Mathf.Min(a.yMax, b.yMax);
        return new RectInt(xMin, yMin, Mathf.Max(0, xMax - xMin), Mathf.Max(0, yMax - yMin));
    }

    private static void LightlyDirtyExistingCard(Color[] pixels, int width, RectInt rect, float amount, int seed)
    {
        for (int y = rect.yMin; y < rect.yMax; y++)
        {
            for (int x = rect.xMin; x < rect.xMax; x++)
            {
                float n = ValueNoise(x / 5, y / 5, seed) * 0.65f + ValueNoise(x / 29, y / 29, seed + 9) * 0.35f;
                int index = y * width + x;
                pixels[index] = Color.Lerp(pixels[index], pixels[index] * 0.72f, Mathf.Max(0f, n - 0.42f) * amount);
            }
        }
    }

    private static void DrawInkMask(Color[] pixels, int width, Texture2D mask, RectInt rect, RectInt clip, float opacity, float erosion, int seed)
    {
        RectInt draw = Intersect(rect, clip);
        for (int y = draw.yMin; y < draw.yMax; y++)
        {
            for (int x = draw.xMin; x < draw.xMax; x++)
            {
                float u = (x - rect.xMin + 0.5f) / rect.width;
                float v = (y - rect.yMin + 0.5f) / rect.height;
                Color src = mask.GetPixelBilinear(u, v);
                float m = src.a * Mathf.Max(src.r, Mathf.Max(src.g, src.b));
                float noise = ValueNoise(x, y, seed) * 0.75f + ValueNoise(x * 3, y * 3, seed + 5) * 0.25f;
                float a = m * opacity * Mathf.SmoothStep(erosion, 1f, noise * 0.28f + m * 0.86f);
                BlendInk(pixels, width, x, y, a);
            }
        }
    }

    private static void BlendInk(Color[] pixels, int width, int x, int y, float alpha)
    {
        BlendInk(pixels, width, x, y, alpha, InkColor);
    }

    private static void BlendInk(Color[] pixels, int width, int x, int y, float alpha, Color inkColor)
    {
        if (alpha <= 0.001f) return;
        int index = y * width + x;
        Color current = pixels[index];
        Color ink = Color.Lerp(inkColor, current * 0.33f, 0.14f);
        pixels[index] = Color.Lerp(current, ink, Mathf.Clamp01(alpha));
    }

    private static Texture2D RenderTitleMask(string text, TMP_FontAsset font, int width, int height)
    {
        RenderTexture rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
        Camera camera = null;
        GameObject root = null;
        try
        {
            root = new GameObject("CardPrintBake_TitleRoot") { hideFlags = HideFlags.HideAndDontSave };
            GameObject camGo = new GameObject("CardPrintBake_TitleCamera") { hideFlags = HideFlags.HideAndDontSave };
            camGo.transform.SetParent(root.transform, false);
            camera = camGo.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.clear;
            camera.orthographic = true;
            camera.orthographicSize = height * 0.5f;
            camera.transform.position = new Vector3(width * 0.5f, height * 0.5f, -10f);
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 50f;
            camera.targetTexture = rt;

            GameObject canvasGo = new GameObject("CardPrintBake_TitleCanvas") { hideFlags = HideFlags.HideAndDontSave };
            canvasGo.transform.SetParent(root.transform, false);
            Canvas canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = camera;
            RectTransform canvasRect = canvas.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(width, height);
            canvasRect.position = new Vector3(width * 0.5f, height * 0.5f, 0f);

            GameObject textGo = new GameObject("CardPrintBake_TitleText") { hideFlags = HideFlags.HideAndDontSave };
            textGo.transform.SetParent(canvasGo.transform, false);
            TextMeshProUGUI tmp = textGo.AddComponent<TextMeshProUGUI>();
            tmp.font = font;
            tmp.text = text;
            tmp.fontSize = 92f;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
            tmp.enableWordWrapping = false;
            tmp.raycastTarget = false;
            RectTransform textRect = tmp.rectTransform;
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(0f, 0f);
            textRect.offsetMax = new Vector2(0f, 0f);
            Canvas.ForceUpdateCanvases();

            RenderTexture previous = RenderTexture.active;
            camera.Render();
            RenderTexture.active = rt;
            Texture2D result = new Texture2D(width, height, TextureFormat.RGBA32, false, true);
            result.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            result.Apply(false, false);
            RenderTexture.active = previous;
            return result;
        }
        finally
        {
            if (camera != null) camera.targetTexture = null;
            rt.Release();
            UnityEngine.Object.DestroyImmediate(rt);
            if (root != null) UnityEngine.Object.DestroyImmediate(root);
        }
    }

    private static Texture2D ReadableCopy(Texture2D source, int width = 0, int height = 0)
    {
        if (source == null) return null;
        int w = width > 0 ? width : source.width;
        int h = height > 0 ? height : source.height;
        RenderTexture rt = RenderTexture.GetTemporary(w, h, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
        Graphics.Blit(source, rt);
        RenderTexture previous = RenderTexture.active;
        RenderTexture.active = rt;
        Texture2D copy = new Texture2D(w, h, TextureFormat.RGBA32, false, false);
        copy.ReadPixels(new Rect(0, 0, w, h), 0, 0);
        copy.Apply(false, false);
        RenderTexture.active = previous;
        RenderTexture.ReleaseTemporary(rt);
        return copy;
    }

    private static float ValueNoise(int x, int y, int seed)
    {
        unchecked
        {
            uint h = (uint)(x * 374761393 + y * 668265263 + seed * 1442695041);
            h = (h ^ (h >> 13)) * 1274126177u;
            return ((h ^ (h >> 16)) & 0x00FFFFFF) / 16777215f;
        }
    }

    private static Material LoadOrCreateMaterial(string path)
    {
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material != null) return material;
        Shader shader = Shader.Find("CardDungeon/RetroFakeLit");
        if (shader == null) throw new InvalidOperationException("Missing shader: CardDungeon/RetroFakeLit");
        material = new Material(shader) { name = Path.GetFileNameWithoutExtension(path) };
        AssetDatabase.CreateAsset(material, path);
        return material;
    }

    private static void ConfigureTexture(string path)
    {
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null) return;
        importer.sRGBTexture = true;
        importer.mipmapEnabled = true;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.filterMode = FilterMode.Bilinear;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.alphaSource = TextureImporterAlphaSource.None;
        importer.SaveAndReimport();
    }

    private static void EnsureFolder(string folder)
    {
        if (string.IsNullOrEmpty(folder) || AssetDatabase.IsValidFolder(folder)) return;
        string parent = Path.GetDirectoryName(folder)?.Replace('\\', '/');
        string name = Path.GetFileName(folder);
        EnsureFolder(parent);
        if (!AssetDatabase.IsValidFolder(folder)) AssetDatabase.CreateFolder(parent, name);
    }
}
