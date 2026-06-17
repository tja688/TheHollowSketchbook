using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

public sealed class RetroFakeLitBatchReport
{
    public int discoveredModelCount;
    public int processedSourceCount;
    public int createdPrefabCount;
    public int updatedPrefabCount;
    public int updatedSceneObjectCount;
    public int createdMaterialCount;
    public int reusedMaterialCount;
    public int convertedRendererCount;
    public int convertedMaterialSlotCount;
    public int failureCount;

    readonly List<string> lines = new List<string>();

    public IReadOnlyList<string> Lines => lines;

    public string Summary => $"发现模型 {discoveredModelCount}，处理源 {processedSourceCount}，Prefab 新建 {createdPrefabCount} / 更新 {updatedPrefabCount}，场景替换 {updatedSceneObjectCount}，材质新建 {createdMaterialCount} / 复用 {reusedMaterialCount}，Renderer {convertedRendererCount}，材质槽 {convertedMaterialSlotCount}，失败 {failureCount}";

    public void Info(string message)
    {
        lines.Add(message);
        Debug.Log(message);
    }

    public void Fail(string message)
    {
        failureCount++;
        lines.Add("失败：" + message);
        Debug.LogWarning(message);
    }
}

public static class RetroFakeLitConversionService
{
    public const string DefaultPrefabOutputFolder = "Assets/Arts/Prefabs/RetroFakeLits";

    const string ShaderName = "CardDungeon/RetroFakeLit";
    const string MaterialFolder = "Assets/_Project/Rendering/Materials/RetroFakeLitGenerated";

    static readonly string[] SupportedModelExtensions =
    {
        ".fbx",
        ".obj",
        ".gltf",
        ".glb",
        ".dae",
        ".blend"
    };

    static readonly string[] TexturePropertyNames =
    {
        "baseColorTexture",
        "_BaseMap",
        "_MainTex",
        "_Texture",
        "_BaseColorMap",
        "_Albedo"
    };

    static readonly string[] ColorPropertyNames =
    {
        "baseColorFactor",
        "_BaseColor",
        "_Color",
        "_Color_Primary"
    };

    public static List<string> GetDiscoverableModelAssetPaths(DefaultAsset folderAsset)
    {
        string folderPath = AssetDatabase.GetAssetPath(folderAsset);
        if (string.IsNullOrEmpty(folderPath) || !AssetDatabase.IsValidFolder(folderPath))
        {
            return new List<string>();
        }

        return DiscoverModelAssetPaths(folderPath);
    }

    public static RetroFakeLitBatchReport ProcessFolder(DefaultAsset folderAsset, string outputFolder = DefaultPrefabOutputFolder)
    {
        return ProcessBatch(folderAsset, Array.Empty<GameObject>(), outputFolder);
    }

    public static RetroFakeLitBatchReport ProcessSceneObjects(IEnumerable<GameObject> sceneObjects, string outputFolder = DefaultPrefabOutputFolder)
    {
        return ProcessBatch(null, sceneObjects, outputFolder);
    }

    public static RetroFakeLitBatchReport ProcessCurrentSelection(string outputFolder = DefaultPrefabOutputFolder)
    {
        return ProcessSceneObjects(Selection.gameObjects, outputFolder);
    }

    public static RetroFakeLitBatchReport ProcessBatch(DefaultAsset folderAsset, IEnumerable<GameObject> sceneObjects, string outputFolder = DefaultPrefabOutputFolder)
    {
        RetroFakeLitBatchReport report = new RetroFakeLitBatchReport();
        Shader shader = Shader.Find(ShaderName);
        if (shader == null)
        {
            report.Fail($"找不到 Shader：{ShaderName}");
            return report;
        }

        EnsureFolder(outputFolder);
        EnsureFolder(MaterialFolder);

        var materialCache = new Dictionary<Material, Material>();
        var reservedPrefabPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        bool sceneChanged = false;

        if (folderAsset != null)
        {
            ProcessFolderInternal(folderAsset, outputFolder, shader, materialCache, reservedPrefabPaths, report);
        }

        List<GameObject> selectionRoots = NormalizeSceneSelection(sceneObjects).ToList();
        if (selectionRoots.Count > 0)
        {
            report.Info($"场景选择共 {selectionRoots.Count} 个根对象待处理。");
            foreach (GameObject sceneObject in selectionRoots)
            {
                if (ProcessSceneObject(sceneObject, outputFolder, shader, materialCache, reservedPrefabPaths, report))
                {
                    sceneChanged = true;
                }
            }
        }

        if (folderAsset == null && selectionRoots.Count == 0)
        {
            report.Fail("没有可处理的输入。请指定源文件夹，或在场景中选择一个/多个对象。");
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        if (sceneChanged)
        {
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        }

        report.Info(report.Summary);
        return report;
    }

    static void ProcessFolderInternal(
        DefaultAsset folderAsset,
        string outputFolder,
        Shader shader,
        Dictionary<Material, Material> materialCache,
        HashSet<string> reservedPrefabPaths,
        RetroFakeLitBatchReport report)
    {
        string folderPath = AssetDatabase.GetAssetPath(folderAsset);
        if (string.IsNullOrEmpty(folderPath) || !AssetDatabase.IsValidFolder(folderPath))
        {
            report.Fail("指定的源文件夹无效。请拖入 Project 里的文件夹资源。");
            return;
        }

        List<string> modelPaths = DiscoverModelAssetPaths(folderPath);
        report.discoveredModelCount += modelPaths.Count;

        if (modelPaths.Count == 0)
        {
            report.Fail($"文件夹 `{folderPath}` 下没有找到 gltf/fbx/obj 等可导入模型。");
            return;
        }

        report.Info($"文件夹 `{folderPath}` 共发现 {modelPaths.Count} 个模型源：{string.Join(", ", modelPaths.Select(Path.GetFileName))}");

        foreach (string modelPath in modelPaths)
        {
            ProcessModelAsset(modelPath, folderPath, outputFolder, shader, materialCache, reservedPrefabPaths, report);
        }
    }

    static void ProcessModelAsset(
        string modelAssetPath,
        string sourceFolderPath,
        string outputFolder,
        Shader shader,
        Dictionary<Material, Material> materialCache,
        HashSet<string> reservedPrefabPaths,
        RetroFakeLitBatchReport report)
    {
        GameObject sourceAssetRoot = null;
        GameObject workingRoot = null;
        try
        {
            sourceAssetRoot = AssetDatabase.LoadAssetAtPath<GameObject>(modelAssetPath);
            if (sourceAssetRoot == null)
            {
                report.Fail($"无法加载模型资源：{modelAssetPath}");
                return;
            }

            workingRoot = UnityEngine.Object.Instantiate(sourceAssetRoot);
            if (workingRoot == null)
            {
                report.Fail($"无法实例化模型资源：{modelAssetPath}");
                return;
            }

            if (PrefabUtility.IsPartOfPrefabInstance(workingRoot))
            {
                PrefabUtility.UnpackPrefabInstance(workingRoot, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            }

            report.processedSourceCount++;
            report.Info($"处理模型资源：`{modelAssetPath}`（{GetModelKind(modelAssetPath)}）");

            if (!ConvertHierarchy(workingRoot, modelAssetPath, shader, materialCache, report))
            {
                report.Fail($"模型 `{modelAssetPath}` 没有找到可转换的 MeshRenderer/SkinnedMeshRenderer。");
                return;
            }

            string prefabName = BuildModelPrefabName(modelAssetPath, sourceFolderPath, workingRoot.name);
            string prefabPath = ReservePrefabPath(outputFolder, prefabName, reservedPrefabPaths);
            bool exists = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) != null;
            GameObject savedPrefab = PrefabUtility.SaveAsPrefabAsset(workingRoot, prefabPath, out bool success);
            if (!success || savedPrefab == null)
            {
                report.Fail($"写入 prefab 失败：{prefabPath}");
                return;
            }

            if (exists)
            {
                report.updatedPrefabCount++;
            }
            else
            {
                report.createdPrefabCount++;
            }

            report.Info($"输出 prefab：`{prefabPath}`");
        }
        catch (Exception exception)
        {
            report.Fail($"处理模型 `{modelAssetPath}` 时出错：{exception.Message}");
        }
        finally
        {
            if (workingRoot != null)
            {
                UnityEngine.Object.DestroyImmediate(workingRoot);
            }
        }
    }

    static bool ProcessSceneObject(
        GameObject sceneObject,
        string outputFolder,
        Shader shader,
        Dictionary<Material, Material> materialCache,
        HashSet<string> reservedPrefabPaths,
        RetroFakeLitBatchReport report)
    {
        if (sceneObject == null || !sceneObject.scene.IsValid())
        {
            report.Fail("场景对象无效，无法转换。");
            return false;
        }

        report.processedSourceCount++;
        report.Info($"处理场景对象：`{sceneObject.scene.path}/{GetHierarchyPath(sceneObject.transform)}`");

        if (!ConvertHierarchy(sceneObject, sceneObject.scene.path + ":" + GetHierarchyPath(sceneObject.transform), shader, materialCache, report))
        {
            report.Fail($"场景对象 `{GetHierarchyPath(sceneObject.transform)}` 没有找到可转换的 MeshRenderer/SkinnedMeshRenderer。");
            return false;
        }

        string prefabName = BuildScenePrefabName(sceneObject.name);
        string prefabPath = ReservePrefabPath(outputFolder, prefabName, reservedPrefabPaths);
        bool exists = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) != null;
        GameObject savedPrefab = PrefabUtility.SaveAsPrefabAssetAndConnect(sceneObject, prefabPath, InteractionMode.UserAction, out bool success);
        if (!success || savedPrefab == null)
        {
            report.Fail($"场景对象写入 prefab 失败：{prefabPath}");
            return false;
        }

        if (exists)
        {
            report.updatedPrefabCount++;
        }
        else
        {
            report.createdPrefabCount++;
        }

        report.updatedSceneObjectCount++;
        report.Info($"场景对象已替换并连接到：`{prefabPath}`");
        return true;
    }

    static bool ConvertHierarchy(
        GameObject root,
        string contextKey,
        Shader shader,
        Dictionary<Material, Material> materialCache,
        RetroFakeLitBatchReport report)
    {
        bool changedAnyRenderer = false;
        bool foundSupportedRenderer = false;
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        foreach (Renderer renderer in renderers)
        {
            if (renderer is not MeshRenderer && renderer is not SkinnedMeshRenderer)
            {
                continue;
            }

            foundSupportedRenderer = true;
            Material[] sharedMaterials = renderer.sharedMaterials;
            bool changedThisRenderer = false;
            for (int i = 0; i < sharedMaterials.Length; i++)
            {
                Material sourceMaterial = sharedMaterials[i];
                if (sourceMaterial == null || sourceMaterial.shader == shader)
                {
                    continue;
                }

                if (!materialCache.TryGetValue(sourceMaterial, out Material retroMaterial))
                {
                    retroMaterial = GetOrCreateRetroMaterial(sourceMaterial, shader, contextKey, report, out bool created);
                    materialCache.Add(sourceMaterial, retroMaterial);
                    if (created)
                    {
                        report.createdMaterialCount++;
                    }
                    else
                    {
                        report.reusedMaterialCount++;
                    }
                }

                sharedMaterials[i] = retroMaterial;
                report.convertedMaterialSlotCount++;
                changedThisRenderer = true;
            }

            if (!changedThisRenderer)
            {
                continue;
            }

            renderer.sharedMaterials = sharedMaterials;
            renderer.shadowCastingMode = ShadowCastingMode.On;
            renderer.receiveShadows = true;
            EditorUtility.SetDirty(renderer);
            report.convertedRendererCount++;
            changedAnyRenderer = true;
        }

        if (foundSupportedRenderer && !changedAnyRenderer)
        {
            report.Info($"`{root.name}` 已经是 RetroFakeLit，无需重复改材质。");
        }

        return foundSupportedRenderer;
    }

    static Material GetOrCreateRetroMaterial(Material source, Shader shader, string contextKey, RetroFakeLitBatchReport report, out bool created)
    {
        string materialPath = BuildMaterialPath(source);
        Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        created = material == null;

        if (material == null)
        {
            material = new Material(shader);
            AssetDatabase.CreateAsset(material, materialPath);
        }
        else
        {
            material.shader = shader;
        }

        ConfigureMaterial(material, source, contextKey + " " + source.name);
        EditorUtility.SetDirty(material);
        report.Info($"{(created ? "生成" : "复用")}材质：`{materialPath}` <- `{source.name}`");
        return material;
    }

    static string BuildMaterialPath(Material source)
    {
        string sourceAssetPath = AssetDatabase.GetAssetPath(source);
        string assetStem = string.IsNullOrEmpty(sourceAssetPath)
            ? Sanitize(source.name)
            : Sanitize(Path.GetFileNameWithoutExtension(sourceAssetPath));
        string materialStem = Sanitize(source.name);
        string guid = string.IsNullOrEmpty(sourceAssetPath) ? string.Empty : AssetDatabase.AssetPathToGUID(sourceAssetPath);
        string guidStem = string.IsNullOrEmpty(guid) ? "embedded" : guid[..8];
        return $"{MaterialFolder}/M_RetroFakeLit_{assetStem}_{materialStem}_{guidStem}.mat";
    }

    static List<string> DiscoverModelAssetPaths(string folderPath)
    {
        return Directory.GetFiles(folderPath, "*.*", SearchOption.AllDirectories)
            .Where(IsSupportedModelFile)
            .Select(ToAssetPath)
            .Where(path => AssetDatabase.LoadAssetAtPath<GameObject>(path) != null)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    static bool IsSupportedModelFile(string fullPath)
    {
        string extension = Path.GetExtension(fullPath);
        return SupportedModelExtensions.Any(value => value.Equals(extension, StringComparison.OrdinalIgnoreCase));
    }

    static string ToAssetPath(string fullPath)
    {
        string normalizedFullPath = Path.GetFullPath(fullPath).Replace('\\', '/');
        string assetsRoot = Path.GetFullPath(Application.dataPath).Replace('\\', '/');
        if (!normalizedFullPath.StartsWith(assetsRoot, StringComparison.OrdinalIgnoreCase))
        {
            return fullPath.Replace('\\', '/');
        }

        return "Assets" + normalizedFullPath[assetsRoot.Length..];
    }

    static string GetModelKind(string assetPath)
    {
        string extension = Path.GetExtension(assetPath).TrimStart('.');
        return string.IsNullOrEmpty(extension) ? "unknown" : extension.ToUpperInvariant();
    }

    static IEnumerable<GameObject> NormalizeSceneSelection(IEnumerable<GameObject> sceneObjects)
    {
        var candidates = sceneObjects
            .Where(gameObject => gameObject != null && gameObject.scene.IsValid())
            .Distinct()
            .OrderBy(gameObject => GetDepth(gameObject.transform))
            .ToList();

        var selectedTransforms = new HashSet<Transform>(candidates.Select(gameObject => gameObject.transform));
        foreach (GameObject candidate in candidates)
        {
            Transform current = candidate.transform.parent;
            bool nestedUnderSelection = false;
            while (current != null)
            {
                if (selectedTransforms.Contains(current))
                {
                    nestedUnderSelection = true;
                    break;
                }
                current = current.parent;
            }

            if (!nestedUnderSelection)
            {
                yield return candidate;
            }
        }
    }

    static int GetDepth(Transform transform)
    {
        int depth = 0;
        while (transform.parent != null)
        {
            depth++;
            transform = transform.parent;
        }
        return depth;
    }

    static string BuildModelPrefabName(string modelAssetPath, string sourceFolderPath, string rootName)
    {
        string fileStem = Path.GetFileNameWithoutExtension(modelAssetPath);
        string folderStem = Path.GetFileName(sourceFolderPath.TrimEnd('/', '\\'));
        string preferred = IsGenericModelName(fileStem) ? folderStem : fileStem;
        if (string.IsNullOrWhiteSpace(preferred))
        {
            preferred = rootName;
        }

        return preferred + "_RetroFakeLit";
    }

    static string BuildScenePrefabName(string sceneObjectName)
    {
        return sceneObjectName + "_RetroFakeLit";
    }

    static bool IsGenericModelName(string value)
    {
        return value.Equals("scene", StringComparison.OrdinalIgnoreCase)
            || value.Equals("model", StringComparison.OrdinalIgnoreCase)
            || value.Equals("default", StringComparison.OrdinalIgnoreCase);
    }

    static string ReservePrefabPath(string outputFolder, string prefabName, HashSet<string> reservedPrefabPaths)
    {
        string safeName = Sanitize(prefabName);
        string basePath = $"{outputFolder}/{safeName}.prefab";
        string finalPath = reservedPrefabPaths.Contains(basePath)
            ? AssetDatabase.GenerateUniqueAssetPath(basePath)
            : basePath;
        reservedPrefabPaths.Add(finalPath);
        return finalPath;
    }

    static string GetHierarchyPath(Transform transform)
    {
        Stack<string> parts = new Stack<string>();
        while (transform != null)
        {
            parts.Push(transform.name);
            transform = transform.parent;
        }

        return string.Join("/", parts);
    }

    static void ConfigureMaterial(Material material, Material source, string key)
    {
        material.SetTexture("_BaseMap", FindTexture(source));
        Color sourceColor = FindColor(source);
        RetroFakeLitMaterialTemplates.ConfigureConvertedMaterial(material, sourceColor, key);
    }

    static Texture FindTexture(Material source)
    {
        foreach (string propertyName in TexturePropertyNames)
        {
            if (!source.HasProperty(propertyName))
            {
                continue;
            }

            Texture texture = source.GetTexture(propertyName);
            if (texture != null)
            {
                return texture;
            }
        }

        return null;
    }

    static Color FindColor(Material source)
    {
        foreach (string propertyName in ColorPropertyNames)
        {
            if (source.HasProperty(propertyName))
            {
                return source.GetColor(propertyName);
            }
        }

        return Color.white;
    }

    static string Sanitize(string value)
    {
        char[] chars = value.Select(character => char.IsLetterOrDigit(character) ? character : '_').ToArray();
        string result = new string(chars);
        while (result.Contains("__", StringComparison.Ordinal))
        {
            result = result.Replace("__", "_");
        }

        result = result.Trim('_');
        return string.IsNullOrEmpty(result) ? "RetroFakeLit" : result;
    }

    static void EnsureFolder(string folderPath)
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
