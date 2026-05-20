using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

public static class RatPaperExperimentBuilder
{
    private const string TexturePath = "Assets/tests/Pictures/Monsters/Prototype/老鼠_PaperProcessed.png";
    private const string MaterialDir = "Assets/tests/Pictures/Monsters/Materials";
    private const string PrototypeDir = "Assets/tests/Pictures/Monsters/Prototype";

    [MenuItem("Tools/Prototype/Build Rat Paper Experiments")]
    public static void Build()
    {
        if (!AssetDatabase.IsValidFolder(PrototypeDir))
        {
            AssetDatabase.CreateFolder("Assets/tests/Pictures/Monsters", "Prototype");
        }

        Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(TexturePath);
        Material sourceMaterial = AssetDatabase.LoadAssetAtPath<Material>($"{MaterialDir}/老鼠_PaperCutout.mat");
        Material backMaterial = AssetDatabase.LoadAssetAtPath<Material>($"{MaterialDir}/老鼠_PaperBack_Edge.mat");
        Material schemeAMaterial = CopyPaperMaterial($"{MaterialDir}/老鼠_SchemeA_VertexPaper.mat", sourceMaterial, texture, new Color(0.82f, 0.76f, 0.62f, 1f));
        Material schemeBMaterial = CopyPaperMaterial($"{MaterialDir}/老鼠_SchemeB_ControlPaper.mat", sourceMaterial, texture, new Color(0.78f, 0.73f, 0.58f, 1f));

        Mesh gridMesh = CreateGridMesh($"{PrototypeDir}/Rat_SchemeA_12x16.asset", 12, 16);
        Mesh bodyMesh = CreateSegmentMesh($"{PrototypeDir}/Rat_SchemeB_Body.asset", 0.22f, 0.78f, 0.14f, 0.76f, 8, 10);
        Mesh headMesh = CreateSegmentMesh($"{PrototypeDir}/Rat_SchemeB_Head.asset", 0.25f, 0.78f, 0.55f, 0.96f, 8, 6);
        Mesh leftMesh = CreateSegmentMesh($"{PrototypeDir}/Rat_SchemeB_LeftSide.asset", 0.00f, 0.36f, 0.16f, 0.72f, 5, 8);
        Mesh rightMesh = CreateSegmentMesh($"{PrototypeDir}/Rat_SchemeB_RightSide.asset", 0.64f, 1.00f, 0.16f, 0.72f, 5, 8);
        Mesh tailMesh = CreateSegmentMesh($"{PrototypeDir}/Rat_SchemeB_Tail.asset", 0.02f, 0.36f, 0.00f, 0.28f, 5, 4);

        DeleteRoot("Rat_Paper_SchemeA_VertexDeform");
        DeleteRoot("Rat_Paper_SchemeB_ControlPoints");

        GameObject schemeA = new GameObject("Rat_Paper_SchemeA_VertexDeform");
        schemeA.transform.position = new Vector3(-2.2f, -1.02f, 0f);
        schemeA.transform.rotation = Quaternion.Euler(-6f, 0f, -2f);
        CreateBackPlate("A dark paper thickness silhouette", schemeA.transform, new Vector3(0.045f, -0.02f, 0.045f), new Vector3(2.42f, 2.42f, 1f), backMaterial);
        GameObject aFront = CreateMeshObject("A subdivided paper mesh vertex wobble", schemeA.transform, gridMesh, schemeAMaterial, Vector3.zero, new Vector3(2.35f, 2.35f, 1f));
        PaperVertexSpringDeformer deformerA = aFront.AddComponent<PaperVertexSpringDeformer>();
        SetVertexDeformer(deformerA, 0.085f, 4.1f, 2.7f, 0.045f, false);
        CreateBackPlate("A bottom exposed paper edge", schemeA.transform, new Vector3(0.03f, -1.17f, 0.025f), new Vector3(1.55f, 0.10f, 1f), backMaterial);

        GameObject schemeB = new GameObject("Rat_Paper_SchemeB_ControlPoints");
        schemeB.transform.position = new Vector3(2.2f, -1.02f, 0f);
        schemeB.transform.rotation = Quaternion.Euler(-6f, 0f, -2f);
        CreateBackPlate("B shared dark paper backing", schemeB.transform, new Vector3(0.045f, -0.02f, 0.045f), new Vector3(2.42f, 2.42f, 1f), backMaterial);
        AddSegment(schemeB.transform, "B body root paper panel", bodyMesh, schemeBMaterial, new Vector3(0f, -0.07f, 0f), Vector3.right, 0.018f, 0.2f, 0.000f);
        AddSegment(schemeB.transform, "B head spring panel", headMesh, schemeBMaterial, new Vector3(0.02f, 0.30f, 0.01f), new Vector3(1f, 0.35f, 0f), 0.055f, 1.4f, 0.006f);
        AddSegment(schemeB.transform, "B left side spring panel", leftMesh, schemeBMaterial, new Vector3(-0.22f, 0.02f, 0.018f), new Vector3(-1f, 0.2f, 0f), 0.050f, 2.4f, 0.012f);
        AddSegment(schemeB.transform, "B right side spring panel", rightMesh, schemeBMaterial, new Vector3(0.22f, 0.00f, 0.020f), new Vector3(1f, 0.15f, 0f), 0.050f, 3.2f, 0.018f);
        AddSegment(schemeB.transform, "B tail spring panel", tailMesh, schemeBMaterial, new Vector3(-0.28f, -0.34f, 0.025f), new Vector3(-1f, -0.25f, 0f), 0.070f, 4.0f, 0.024f);
        CreateBackPlate("B bottom exposed paper edge", schemeB.transform, new Vector3(0.03f, -1.17f, 0.025f), new Vector3(1.55f, 0.10f, 1f), backMaterial);

        GameObject original = GameObject.Find("PaperCutout_Rat_Prototype");
        if (original != null)
        {
            original.transform.position = new Vector3(0f, -1.02f, 0f);
        }

        Camera camera = Camera.main;
        if (camera != null)
        {
            camera.transform.position = new Vector3(0f, 0.0f, -7.1f);
            camera.transform.rotation = Quaternion.Euler(5f, 0f, 0f);
            camera.fieldOfView = 41f;
        }

        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
    }

    private static Material CopyPaperMaterial(string path, Material sourceMaterial, Texture texture, Color tint)
    {
        Shader lit = Shader.Find("Universal Render Pipeline/Lit");
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            material = sourceMaterial != null ? new Material(sourceMaterial) : new Material(lit);
            AssetDatabase.CreateAsset(material, path);
        }
        else if (sourceMaterial != null)
        {
            material.CopyPropertiesFromMaterial(sourceMaterial);
        }

        material.shader = lit;
        material.SetTexture("_BaseMap", texture);
        material.SetTexture("_MainTex", texture);
        material.SetColor("_BaseColor", tint);
        material.SetColor("_Color", tint);
        material.SetFloat("_AlphaClip", 1f);
        material.SetFloat("_AlphaToMask", 1f);
        material.SetFloat("_Cutoff", 0.38f);
        material.SetFloat("_Cull", 0f);
        material.SetFloat("_Smoothness", 0.05f);
        material.EnableKeyword("_ALPHATEST_ON");
        material.SetOverrideTag("RenderType", "TransparentCutout");
        material.renderQueue = (int)RenderQueue.AlphaTest;
        EditorUtility.SetDirty(material);
        return material;
    }

    private static Mesh CreateGridMesh(string path, int columns, int rows)
    {
        Mesh mesh = LoadOrCreateMesh(path);
        Vector3[] vertices = new Vector3[(columns + 1) * (rows + 1)];
        Vector2[] uvs = new Vector2[vertices.Length];
        int[] triangles = new int[columns * rows * 6];

        for (int y = 0; y <= rows; y++)
        {
            for (int x = 0; x <= columns; x++)
            {
                int index = y * (columns + 1) + x;
                float u = x / (float)columns;
                float v = y / (float)rows;
                vertices[index] = new Vector3(u - 0.5f, v - 0.5f, 0f);
                uvs[index] = new Vector2(u, v);
            }
        }

        FillTriangles(triangles, columns, rows);
        ApplyMesh(mesh, vertices, uvs, triangles);
        return mesh;
    }

    private static Mesh CreateSegmentMesh(string path, float uMin, float uMax, float vMin, float vMax, int columns, int rows)
    {
        Mesh mesh = LoadOrCreateMesh(path);
        Vector3[] vertices = new Vector3[(columns + 1) * (rows + 1)];
        Vector2[] uvs = new Vector2[vertices.Length];
        int[] triangles = new int[columns * rows * 6];

        for (int y = 0; y <= rows; y++)
        {
            for (int x = 0; x <= columns; x++)
            {
                int index = y * (columns + 1) + x;
                float u = Mathf.Lerp(uMin, uMax, x / (float)columns);
                float v = Mathf.Lerp(vMin, vMax, y / (float)rows);
                vertices[index] = new Vector3(u - 0.5f, v - 0.5f, 0f);
                uvs[index] = new Vector2(u, v);
            }
        }

        FillTriangles(triangles, columns, rows);
        ApplyMesh(mesh, vertices, uvs, triangles);
        return mesh;
    }

    private static Mesh LoadOrCreateMesh(string path)
    {
        Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (mesh == null)
        {
            mesh = new Mesh();
            mesh.name = Path.GetFileNameWithoutExtension(path);
            AssetDatabase.CreateAsset(mesh, path);
        }
        return mesh;
    }

    private static void FillTriangles(int[] triangles, int columns, int rows)
    {
        int triangleIndex = 0;
        for (int y = 0; y < rows; y++)
        {
            for (int x = 0; x < columns; x++)
            {
                int index = y * (columns + 1) + x;
                triangles[triangleIndex++] = index;
                triangles[triangleIndex++] = index + columns + 1;
                triangles[triangleIndex++] = index + 1;
                triangles[triangleIndex++] = index + 1;
                triangles[triangleIndex++] = index + columns + 1;
                triangles[triangleIndex++] = index + columns + 2;
            }
        }
    }

    private static void ApplyMesh(Mesh mesh, Vector3[] vertices, Vector2[] uvs, int[] triangles)
    {
        mesh.Clear();
        mesh.vertices = vertices;
        mesh.uv = uvs;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        EditorUtility.SetDirty(mesh);
    }

    private static GameObject CreateMeshObject(string name, Transform parent, Mesh mesh, Material material, Vector3 localPosition, Vector3 localScale)
    {
        GameObject gameObject = new GameObject(name);
        gameObject.transform.SetParent(parent, false);
        gameObject.transform.localPosition = localPosition;
        gameObject.transform.localScale = localScale;
        MeshFilter meshFilter = gameObject.AddComponent<MeshFilter>();
        meshFilter.sharedMesh = mesh;
        MeshRenderer meshRenderer = gameObject.AddComponent<MeshRenderer>();
        meshRenderer.sharedMaterial = material;
        meshRenderer.shadowCastingMode = ShadowCastingMode.On;
        meshRenderer.receiveShadows = true;
        return gameObject;
    }

    private static GameObject CreateBackPlate(string name, Transform parent, Vector3 localPosition, Vector3 localScale, Material material)
    {
        GameObject gameObject = GameObject.CreatePrimitive(PrimitiveType.Quad);
        gameObject.name = name;
        gameObject.transform.SetParent(parent, false);
        gameObject.transform.localPosition = localPosition;
        gameObject.transform.localScale = localScale;
        gameObject.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
        Collider collider = gameObject.GetComponent<Collider>();
        if (collider != null)
        {
            Object.DestroyImmediate(collider);
        }
        MeshRenderer meshRenderer = gameObject.GetComponent<MeshRenderer>();
        meshRenderer.sharedMaterial = material;
        meshRenderer.shadowCastingMode = ShadowCastingMode.On;
        meshRenderer.receiveShadows = true;
        return gameObject;
    }

    private static void AddSegment(Transform parent, string name, Mesh mesh, Material material, Vector3 offset, Vector3 axis, float force, float phase, float z)
    {
        GameObject pivot = new GameObject(name + " control spring");
        pivot.transform.SetParent(parent, false);
        pivot.transform.localPosition = offset;
        PaperControlPointSpring spring = pivot.AddComponent<PaperControlPointSpring>();
        SerializedObject springObject = new SerializedObject(spring);
        springObject.FindProperty("idleForce").floatValue = force;
        springObject.FindProperty("axis").vector3Value = axis;
        springObject.FindProperty("phase").floatValue = phase;
        springObject.ApplyModifiedPropertiesWithoutUndo();

        GameObject segment = CreateMeshObject(name, pivot.transform, mesh, material, new Vector3(-offset.x, -offset.y, z), new Vector3(2.35f, 2.35f, 1f));
        PaperVertexSpringDeformer deformer = segment.AddComponent<PaperVertexSpringDeformer>();
        SetVertexDeformer(deformer, 0.025f, 3.6f, 2.1f, 0.014f, true);
    }

    private static void SetVertexDeformer(PaperVertexSpringDeformer deformer, float amplitude, float frequency, float waveSpeed, float horizontalDrift, bool useSegmentBias)
    {
        SerializedObject serializedObject = new SerializedObject(deformer);
        serializedObject.FindProperty("amplitude").floatValue = amplitude;
        serializedObject.FindProperty("frequency").floatValue = frequency;
        serializedObject.FindProperty("waveSpeed").floatValue = waveSpeed;
        serializedObject.FindProperty("horizontalDrift").floatValue = horizontalDrift;
        serializedObject.FindProperty("useSegmentBias").boolValue = useSegmentBias;
        serializedObject.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void DeleteRoot(string name)
    {
        GameObject gameObject = GameObject.Find(name);
        if (gameObject != null)
        {
            Object.DestroyImmediate(gameObject);
        }
    }
}
