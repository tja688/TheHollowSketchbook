using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

public static class CardboardCutoutGenerator
{
    private const string SourceTexturePath = "Assets/tests/Pictures/Monsters/硬纸板老鼠.png";
    private const string GeneratedRoot = "Assets/CardboardCutout/Generated";
    private const string MeshDir = GeneratedRoot + "/Meshes";
    private const string MaterialDir = GeneratedRoot + "/Materials";
    private const string PrefabDir = GeneratedRoot + "/Prefabs";
    private const string TextureDir = GeneratedRoot + "/Textures";
    private const float PixelsPerUnit = 100f;
    private const float Thickness = 0.08f;

    [MenuItem("Tools/Cardboard Cutout/Build Rat Cardboard Cutout")]
    public static void BuildRatCardboardCutout()
    {
        EnsureFolders();
        Texture2D source = LoadReadableTexture(SourceTexturePath);
        if (source == null)
        {
            Debug.LogError($"Missing source texture: {SourceTexturePath}");
            return;
        }

        int width = source.width;
        int height = source.height;
        int minDim = Mathf.Min(width, height);
        bool[] cutMask = BuildManualAlphaMask(source);
        if (cutMask == null)
        {
            return;
        }

        float simplifyPx = Mathf.Clamp(minDim * 0.004f, 2f, 8f);
        float resampleSpacing = Mathf.Clamp(minDim * 0.008f, 4f, 12f);

        cutMask = KeepLargestComponent(cutMask, width, height);
        cutMask = FillHoles(cutMask, width, height);

        List<Vector2> contour = ExtractContour(cutMask, width, height);
        if (contour.Count < 3)
        {
            Debug.LogError("Failed to extract a valid cardboard contour from source alpha.");
            return;
        }
        contour = DouglasPeucker(contour, simplifyPx);
        contour = ResampleClosed(contour, resampleSpacing);
        EnsureClockwise(contour);

        SaveMaskPreview(cutMask, width, height, TextureDir + "/Rat_Cardboard_CutMask.png");
        Texture2D edgeTexture = CreateEdgeTexture(TextureDir + "/Cardboard_Edge_Repeat.png");
        Mesh mesh = CreateExtrudedMesh(contour, width, height, MeshDir + "/Rat_Cardboard.asset");
        Material front = CreateFrontMaterial(source, MaterialDir + "/Rat_Cardboard_Front.mat");
        Material back = CreateColorMaterial(MaterialDir + "/Rat_Cardboard_Back.mat", new Color(0.66f, 0.52f, 0.32f, 1f), 0.13f);
        Material edge = CreateEdgeMaterial(edgeTexture, MaterialDir + "/Rat_Cardboard_Edge.mat");
        GameObject prefab = CreatePrefab(mesh, front, back, edge, PrefabDir + "/Rat_Cardboard.prefab");
        PlaceInScene(prefab);

        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        Debug.Log("Rat cardboard cutout generated and placed in scene.");
    }

    private static void EnsureFolders()
    {
        EnsureFolder("Assets", "CardboardCutout");
        EnsureFolder("Assets/CardboardCutout", "Generated");
        EnsureFolder(GeneratedRoot, "Meshes");
        EnsureFolder(GeneratedRoot, "Materials");
        EnsureFolder(GeneratedRoot, "Prefabs");
        EnsureFolder(GeneratedRoot, "Textures");
    }

    private static void EnsureFolder(string parent, string name)
    {
        string path = parent + "/" + name;
        if (!AssetDatabase.IsValidFolder(path))
        {
            AssetDatabase.CreateFolder(parent, name);
        }
    }

    private static Texture2D LoadReadableTexture(string path)
    {
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer != null && !importer.isReadable)
        {
            importer.isReadable = true;
            importer.alphaIsTransparency = true;
            importer.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }

    private static bool[] BuildManualAlphaMask(Texture2D texture)
    {
        Color32[] pixels = texture.GetPixels32();
        bool[] mask = new bool[pixels.Length];
        int solidCount = 0;
        int transparentCount = 0;
        for (int i = 0; i < pixels.Length; i++)
        {
            mask[i] = pixels[i].a > 20;
            if (mask[i]) solidCount++;
            if (pixels[i].a < 250) transparentCount++;
        }

        if (transparentCount == 0)
        {
            Debug.LogError($"Manual Alpha mode requires a transparent PNG. Source has no transparent pixels: {SourceTexturePath}");
            return null;
        }

        if (solidCount < 3)
        {
            Debug.LogError($"Manual Alpha mode could not find enough visible pixels in: {SourceTexturePath}");
            return null;
        }

        return mask;
    }

    private static bool[] BuildContentMask(Texture2D texture)
    {
        int width = texture.width;
        int height = texture.height;
        Color32[] pixels = texture.GetPixels32();
        bool[] mask = new bool[pixels.Length];
        bool hasAlpha = false;
        for (int i = 0; i < pixels.Length; i++)
        {
            if (pixels[i].a < 250)
            {
                hasAlpha = true;
                break;
            }
        }

        if (hasAlpha)
        {
            for (int i = 0; i < pixels.Length; i++)
            {
                mask[i] = pixels[i].a > 20;
            }
            return mask;
        }

        Color32[] corners =
        {
            pixels[0], pixels[width - 1], pixels[(height - 1) * width], pixels[height * width - 1]
        };
        float br = 0f;
        float bg = 0f;
        float bb = 0f;
        for (int i = 0; i < corners.Length; i++)
        {
            br += corners[i].r;
            bg += corners[i].g;
            bb += corners[i].b;
        }
        br *= 0.25f;
        bg *= 0.25f;
        bb *= 0.25f;

        for (int i = 0; i < pixels.Length; i++)
        {
            float dr = pixels[i].r - br;
            float dg = pixels[i].g - bg;
            float db = pixels[i].b - bb;
            float delta = Mathf.Sqrt(dr * dr + dg * dg + db * db) / 441.7f;
            mask[i] = delta > 0.10f;
        }
        return mask;
    }

    private static bool[] Dilate(bool[] source, int width, int height, int radius)
    {
        bool[] horizontal = new bool[source.Length];
        for (int y = 0; y < height; y++)
        {
            int count = 0;
            for (int x = -radius; x <= width + radius - 1; x++)
            {
                int add = x + radius;
                int remove = x - radius - 1;
                if (add >= 0 && add < width && source[y * width + add]) count++;
                if (remove >= 0 && remove < width && source[y * width + remove]) count--;
                if (x >= 0 && x < width) horizontal[y * width + x] = count > 0;
            }
        }

        bool[] result = new bool[source.Length];
        for (int x = 0; x < width; x++)
        {
            int count = 0;
            for (int y = -radius; y <= height + radius - 1; y++)
            {
                int add = y + radius;
                int remove = y - radius - 1;
                if (add >= 0 && add < height && horizontal[add * width + x]) count++;
                if (remove >= 0 && remove < height && horizontal[remove * width + x]) count--;
                if (y >= 0 && y < height) result[y * width + x] = count > 0;
            }
        }
        return result;
    }

    private static bool[] Erode(bool[] source, int width, int height, int radius)
    {
        bool[] inverted = new bool[source.Length];
        for (int i = 0; i < source.Length; i++) inverted[i] = !source[i];
        inverted = Dilate(inverted, width, height, radius);
        for (int i = 0; i < inverted.Length; i++) inverted[i] = !inverted[i];
        return inverted;
    }

    private static bool[] Close(bool[] source, int width, int height, int radius)
    {
        return Erode(Dilate(source, width, height, radius), width, height, radius);
    }

    private static bool[] Open(bool[] source, int width, int height, int radius)
    {
        return Dilate(Erode(source, width, height, radius), width, height, radius);
    }

    private static float[] BoxBlur(bool[] source, int width, int height, int radius)
    {
        float[] horizontal = new float[source.Length];
        float[] result = new float[source.Length];
        int size = radius * 2 + 1;
        for (int y = 0; y < height; y++)
        {
            int count = 0;
            for (int x = -radius; x <= width + radius - 1; x++)
            {
                int add = Mathf.Clamp(x + radius, 0, width - 1);
                int remove = Mathf.Clamp(x - radius - 1, 0, width - 1);
                if (source[y * width + add]) count++;
                if (source[y * width + remove]) count--;
                if (x >= 0 && x < width) horizontal[y * width + x] = count / (float)size;
            }
        }

        for (int x = 0; x < width; x++)
        {
            float sum = 0f;
            for (int y = -radius; y <= height + radius - 1; y++)
            {
                int add = Mathf.Clamp(y + radius, 0, height - 1);
                int remove = Mathf.Clamp(y - radius - 1, 0, height - 1);
                sum += horizontal[add * width + x];
                sum -= horizontal[remove * width + x];
                if (y >= 0 && y < height) result[y * width + x] = sum / size;
            }
        }
        return result;
    }

    private static bool[] Threshold(float[] source, float threshold)
    {
        bool[] result = new bool[source.Length];
        for (int i = 0; i < source.Length; i++) result[i] = source[i] >= threshold;
        return result;
    }

    private static bool[] KeepLargestComponent(bool[] source, int width, int height)
    {
        int[] labels = new int[source.Length];
        int bestLabel = 0;
        int bestCount = 0;
        int label = 1;
        Queue<int> queue = new Queue<int>();
        int[] offsets = { 1, -1, width, -width };

        for (int i = 0; i < source.Length; i++)
        {
            if (!source[i] || labels[i] != 0) continue;
            int count = 0;
            labels[i] = label;
            queue.Enqueue(i);
            while (queue.Count > 0)
            {
                int current = queue.Dequeue();
                count++;
                int x = current % width;
                for (int j = 0; j < offsets.Length; j++)
                {
                    int next = current + offsets[j];
                    if (next < 0 || next >= source.Length) continue;
                    if ((offsets[j] == 1 && x == width - 1) || (offsets[j] == -1 && x == 0)) continue;
                    if (source[next] && labels[next] == 0)
                    {
                        labels[next] = label;
                        queue.Enqueue(next);
                    }
                }
            }
            if (count > bestCount)
            {
                bestCount = count;
                bestLabel = label;
            }
            label++;
        }

        bool[] result = new bool[source.Length];
        for (int i = 0; i < result.Length; i++) result[i] = labels[i] == bestLabel;
        return result;
    }

    private static bool[] FillHoles(bool[] source, int width, int height)
    {
        bool[] outside = new bool[source.Length];
        Queue<int> queue = new Queue<int>();
        for (int x = 0; x < width; x++)
        {
            EnqueueOutside(x, width, source, outside, queue);
            EnqueueOutside((height - 1) * width + x, width, source, outside, queue);
        }
        for (int y = 0; y < height; y++)
        {
            EnqueueOutside(y * width, width, source, outside, queue);
            EnqueueOutside(y * width + width - 1, width, source, outside, queue);
        }

        while (queue.Count > 0)
        {
            int current = queue.Dequeue();
            int x = current % width;
            int[] nexts = { current + 1, current - 1, current + width, current - width };
            for (int i = 0; i < nexts.Length; i++)
            {
                int next = nexts[i];
                if (next < 0 || next >= source.Length) continue;
                if ((next == current + 1 && x == width - 1) || (next == current - 1 && x == 0)) continue;
                EnqueueOutside(next, width, source, outside, queue);
            }
        }

        bool[] result = new bool[source.Length];
        for (int i = 0; i < result.Length; i++) result[i] = source[i] || !outside[i];
        return result;
    }

    private static void EnqueueOutside(int index, int width, bool[] source, bool[] outside, Queue<int> queue)
    {
        if (!source[index] && !outside[index])
        {
            outside[index] = true;
            queue.Enqueue(index);
        }
    }

    private static List<Vector2> ExtractContour(bool[] mask, int width, int height)
    {
        Dictionary<Vector2Int, List<Vector2Int>> edges = new Dictionary<Vector2Int, List<Vector2Int>>();
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                if (!mask[y * width + x]) continue;
                if (!GetMask(mask, width, height, x, y - 1)) AddEdge(edges, new Vector2Int(x, y), new Vector2Int(x + 1, y));
                if (!GetMask(mask, width, height, x + 1, y)) AddEdge(edges, new Vector2Int(x + 1, y), new Vector2Int(x + 1, y + 1));
                if (!GetMask(mask, width, height, x, y + 1)) AddEdge(edges, new Vector2Int(x + 1, y + 1), new Vector2Int(x, y + 1));
                if (!GetMask(mask, width, height, x - 1, y)) AddEdge(edges, new Vector2Int(x, y + 1), new Vector2Int(x, y));
            }
        }

        HashSet<long> visited = new HashSet<long>();
        List<Vector2> bestLoop = new List<Vector2>();
        float bestArea = 0f;
        foreach (KeyValuePair<Vector2Int, List<Vector2Int>> pair in edges)
        {
            Vector2Int start = pair.Key;
            for (int i = 0; i < pair.Value.Count; i++)
            {
                Vector2Int next = pair.Value[i];
                long firstKey = EdgeKey(start, next);
                if (visited.Contains(firstKey)) continue;

                List<Vector2> loop = new List<Vector2>();
                Vector2Int current = start;
                Vector2Int target = next;
                int guard = edges.Count * 4;
                while (guard-- > 0)
                {
                    long key = EdgeKey(current, target);
                    if (visited.Contains(key)) break;
                    visited.Add(key);
                    loop.Add(current);
                    current = target;
                    if (current == start) break;
                    if (!edges.TryGetValue(current, out List<Vector2Int> candidates)) break;

                    bool found = false;
                    for (int c = 0; c < candidates.Count; c++)
                    {
                        long candidateKey = EdgeKey(current, candidates[c]);
                        if (!visited.Contains(candidateKey))
                        {
                            target = candidates[c];
                            found = true;
                            break;
                        }
                    }
                    if (!found) break;
                }

                if (loop.Count >= 3)
                {
                    float area = Mathf.Abs(SignedArea(loop));
                    if (area > bestArea)
                    {
                        bestArea = area;
                        bestLoop = loop;
                    }
                }
            }
        }
        return bestLoop;
    }

    private static bool GetMask(bool[] mask, int width, int height, int x, int y)
    {
        return x >= 0 && x < width && y >= 0 && y < height && mask[y * width + x];
    }

    private static void AddEdge(Dictionary<Vector2Int, List<Vector2Int>> edges, Vector2Int start, Vector2Int end)
    {
        if (!edges.TryGetValue(start, out List<Vector2Int> list))
        {
            list = new List<Vector2Int>(1);
            edges.Add(start, list);
        }
        list.Add(end);
    }

    private static long EdgeKey(Vector2Int start, Vector2Int end)
    {
        return ((long)(ushort)start.x << 48) | ((long)(ushort)start.y << 32) | ((long)(ushort)end.x << 16) | (ushort)end.y;
    }

    private static List<Vector2> DouglasPeucker(List<Vector2> points, float epsilon)
    {
        if (points.Count < 4) return points;
        List<Vector2> open = new List<Vector2>(points.Count + 1);
        open.AddRange(points);
        open.Add(points[0]);
        bool[] keep = new bool[open.Count];
        keep[0] = true;
        keep[open.Count - 1] = true;
        SimplifySection(open, 0, open.Count - 1, epsilon * epsilon, keep);
        List<Vector2> result = new List<Vector2>();
        for (int i = 0; i < open.Count - 1; i++)
        {
            if (keep[i]) result.Add(open[i]);
        }
        return result.Count >= 3 ? result : points;
    }

    private static void SimplifySection(List<Vector2> points, int first, int last, float epsilonSqr, bool[] keep)
    {
        float maxDistance = 0f;
        int index = -1;
        Vector2 a = points[first];
        Vector2 b = points[last];
        for (int i = first + 1; i < last; i++)
        {
            float distance = DistancePointLineSqr(points[i], a, b);
            if (distance > maxDistance)
            {
                maxDistance = distance;
                index = i;
            }
        }
        if (maxDistance <= epsilonSqr || index < 0) return;
        keep[index] = true;
        SimplifySection(points, first, index, epsilonSqr, keep);
        SimplifySection(points, index, last, epsilonSqr, keep);
    }

    private static float DistancePointLineSqr(Vector2 p, Vector2 a, Vector2 b)
    {
        Vector2 ab = b - a;
        float t = ab.sqrMagnitude > 0.001f ? Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude) : 0f;
        return (p - (a + ab * t)).sqrMagnitude;
    }

    private static List<Vector2> ResampleClosed(List<Vector2> points, float spacing)
    {
        List<Vector2> result = new List<Vector2>(points.Count);
        for (int i = 0; i < points.Count; i++)
        {
            Vector2 a = points[i];
            Vector2 b = points[(i + 1) % points.Count];
            float length = Vector2.Distance(a, b);
            int steps = Mathf.Max(1, Mathf.RoundToInt(length / spacing));
            for (int s = 0; s < steps; s++)
            {
                result.Add(Vector2.Lerp(a, b, s / (float)steps));
            }
        }
        return result;
    }

    private static void EnsureClockwise(List<Vector2> points)
    {
        if (SignedArea(points) > 0f) points.Reverse();
    }

    private static float SignedArea(List<Vector2> points)
    {
        float area = 0f;
        for (int i = 0; i < points.Count; i++)
        {
            Vector2 a = points[i];
            Vector2 b = points[(i + 1) % points.Count];
            area += a.x * b.y - b.x * a.y;
        }
        return area * 0.5f;
    }

    private static List<Vector2> ApplyRoughCutNoise(List<Vector2> points, float roughnessPx, float frequency, float seed)
    {
        List<Vector2> result = new List<Vector2>(points.Count);
        for (int i = 0; i < points.Count; i++)
        {
            Vector2 prev = points[(i - 1 + points.Count) % points.Count];
            Vector2 current = points[i];
            Vector2 next = points[(i + 1) % points.Count];
            Vector2 tangent = (next - prev).normalized;
            Vector2 normal = new Vector2(-tangent.y, tangent.x);
            float noise = Mathf.PerlinNoise(i * frequency, seed) * 2f - 1f;
            result.Add(current + normal * noise * roughnessPx);
        }
        return result;
    }

    private static Mesh CreateExtrudedMesh(List<Vector2> contour, int width, int height, string path)
    {
        int n = contour.Count;
        if (n < 3)
        {
            throw new InvalidOperationException("Cardboard contour needs at least 3 points.");
        }
        Vector3[] vertices = new Vector3[n * 2 + n * 2];
        Vector2[] uvs = new Vector2[vertices.Length];
        for (int i = 0; i < n; i++)
        {
            Vector2 pixel = contour[i];
            float x = (pixel.x - width * 0.5f) / PixelsPerUnit;
            float y = (pixel.y - height * 0.5f) / PixelsPerUnit;
            vertices[i] = new Vector3(x, y, -Thickness * 0.5f);
            vertices[i + n] = new Vector3(x, y, Thickness * 0.5f);
            vertices[i + n * 2] = vertices[i];
            vertices[i + n * 3] = vertices[i + n];
            Vector2 uv = new Vector2(pixel.x / width, pixel.y / height);
            uvs[i] = uv;
            uvs[i + n] = uv;
        }

        float perimeter = 0f;
        float[] distance = new float[n + 1];
        for (int i = 0; i < n; i++)
        {
            perimeter += Vector2.Distance(contour[i], contour[(i + 1) % n]);
            distance[i + 1] = perimeter;
        }
        for (int i = 0; i < n; i++)
        {
            float u = distance[i] / Mathf.Max(1f, perimeter) * 8f;
            uvs[i + n * 2] = new Vector2(u, 0f);
            uvs[i + n * 3] = new Vector2(u, 1f);
        }

        int[] front = TriangulatePolygon(contour, false);
        int[] back = TriangulatePolygon(contour, true, n);
        int[] edge = new int[n * 6];
        int e = 0;
        int baseIndex = n * 2;
        for (int i = 0; i < n; i++)
        {
            int next = (i + 1) % n;
            int f0 = baseIndex + i;
            int b0 = baseIndex + n + i;
            int f1 = baseIndex + next;
            int b1 = baseIndex + n + next;
            edge[e++] = f0;
            edge[e++] = b0;
            edge[e++] = f1;
            edge[e++] = f1;
            edge[e++] = b0;
            edge[e++] = b1;
        }

        Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (mesh == null)
        {
            mesh = new Mesh();
            mesh.name = "Rat_Cardboard";
            AssetDatabase.CreateAsset(mesh, path);
        }
        mesh.Clear();
        mesh.vertices = vertices;
        mesh.uv = uvs;
        mesh.subMeshCount = 3;
        mesh.SetTriangles(front, 0);
        mesh.SetTriangles(back, 1);
        mesh.SetTriangles(edge, 2);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        EditorUtility.SetDirty(mesh);
        return mesh;
    }

    private static int[] TriangulatePolygon(List<Vector2> contour, bool reverse, int offset = 0)
    {
        List<int> indices = new List<int>(contour.Count);
        for (int i = 0; i < contour.Count; i++) indices.Add(i);
        if (SignedArea(contour) < 0f) indices.Reverse();

        List<int> triangles = new List<int>((contour.Count - 2) * 3);
        int guard = contour.Count * contour.Count;
        while (indices.Count > 3 && guard-- > 0)
        {
            bool clipped = false;
            for (int i = 0; i < indices.Count; i++)
            {
                int previousIndex = indices[(i - 1 + indices.Count) % indices.Count];
                int currentIndex = indices[i];
                int nextIndex = indices[(i + 1) % indices.Count];
                Vector2 previous = contour[previousIndex];
                Vector2 current = contour[currentIndex];
                Vector2 next = contour[nextIndex];
                if (Cross(current - previous, next - current) <= 0f) continue;
                if (ContainsAnyPoint(contour, indices, previousIndex, currentIndex, nextIndex)) continue;

                AddTriangle(triangles, previousIndex + offset, currentIndex + offset, nextIndex + offset, reverse);
                indices.RemoveAt(i);
                clipped = true;
                break;
            }
            if (!clipped) break;
        }

        if (indices.Count == 3)
        {
            AddTriangle(triangles, indices[0] + offset, indices[1] + offset, indices[2] + offset, reverse);
        }
        return triangles.ToArray();
    }

    private static bool ContainsAnyPoint(List<Vector2> contour, List<int> indices, int a, int b, int c)
    {
        Vector2 va = contour[a];
        Vector2 vb = contour[b];
        Vector2 vc = contour[c];
        for (int i = 0; i < indices.Count; i++)
        {
            int index = indices[i];
            if (index == a || index == b || index == c) continue;
            if (PointInTriangle(contour[index], va, vb, vc)) return true;
        }
        return false;
    }

    private static bool PointInTriangle(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
    {
        float ab = Cross(b - a, p - a);
        float bc = Cross(c - b, p - b);
        float ca = Cross(a - c, p - c);
        return ab >= 0f && bc >= 0f && ca >= 0f;
    }

    private static float Cross(Vector2 a, Vector2 b)
    {
        return a.x * b.y - a.y * b.x;
    }

    private static void AddTriangle(List<int> triangles, int a, int b, int c, bool reverse)
    {
        if (reverse)
        {
            triangles.Add(a);
            triangles.Add(c);
            triangles.Add(b);
        }
        else
        {
            triangles.Add(a);
            triangles.Add(b);
            triangles.Add(c);
        }
    }

    private static void SaveMaskPreview(bool[] mask, int width, int height, string path)
    {
        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        Color32[] pixels = new Color32[mask.Length];
        for (int i = 0; i < pixels.Length; i++) pixels[i] = mask[i] ? Color.white : Color.clear;
        texture.SetPixels32(pixels);
        texture.Apply();
        File.WriteAllBytes(path, texture.EncodeToPNG());
        AssetDatabase.ImportAsset(path);
    }

    private static Texture2D CreateEdgeTexture(string path)
    {
        const int width = 128;
        const int height = 32;
        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        Color32[] pixels = new Color32[width * height];
        Color top = new Color(0.70f, 0.56f, 0.35f, 1f);
        Color mid = new Color(0.26f, 0.16f, 0.08f, 1f);
        Color line = new Color(0.52f, 0.38f, 0.20f, 1f);
        for (int y = 0; y < height; y++)
        {
            float v = y / (height - 1f);
            for (int x = 0; x < width; x++)
            {
                float wave = Mathf.Sin(x * 0.42f) * 0.5f + 0.5f;
                Color c = v < 0.22f || v > 0.78f ? top : Color.Lerp(mid, line, wave * 0.55f);
                if (Mathf.Abs(v - 0.5f) < 0.11f && wave > 0.58f) c *= 0.72f;
                pixels[y * width + x] = c;
            }
        }
        texture.SetPixels32(pixels);
        texture.Apply();
        File.WriteAllBytes(path, texture.EncodeToPNG());
        AssetDatabase.ImportAsset(path);
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer != null)
        {
            importer.wrapMode = TextureWrapMode.Repeat;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }

    private static Material CreateFrontMaterial(Texture texture, string path)
    {
        Material material = LoadOrCreateMaterial(path);
        ConfigureLit(material, new Color(0.96f, 0.90f, 0.78f, 1f), 0.18f);
        material.SetTexture("_BaseMap", texture);
        material.SetTexture("_MainTex", texture);
        material.SetFloat("_Cull", 2f);
        EditorUtility.SetDirty(material);
        return material;
    }

    private static Material CreateColorMaterial(string path, Color color, float smoothness)
    {
        Material material = LoadOrCreateMaterial(path);
        ConfigureLit(material, color, smoothness);
        return material;
    }

    private static Material CreateEdgeMaterial(Texture texture, string path)
    {
        Material material = LoadOrCreateMaterial(path);
        ConfigureLit(material, Color.white, 0.1f);
        material.SetTexture("_BaseMap", texture);
        material.SetTexture("_MainTex", texture);
        material.SetFloat("_Cull", 0f);
        return material;
    }

    private static Material LoadOrCreateMaterial(string path)
    {
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            AssetDatabase.CreateAsset(material, path);
        }
        material.shader = Shader.Find("Universal Render Pipeline/Lit");
        return material;
    }

    private static void ConfigureLit(Material material, Color color, float smoothness)
    {
        material.SetColor("_BaseColor", color);
        material.SetColor("_Color", color);
        material.SetFloat("_Surface", 0f);
        material.SetFloat("_Blend", 0f);
        material.SetFloat("_AlphaClip", 0f);
        material.SetFloat("_Cutoff", 0.5f);
        material.SetFloat("_Smoothness", smoothness);
        material.SetFloat("_Metallic", 0f);
        material.SetFloat("_SpecularHighlights", 0f);
        material.SetFloat("_EnvironmentReflections", 0f);
        material.renderQueue = (int)RenderQueue.Geometry;
        material.SetOverrideTag("RenderType", "Opaque");
        material.DisableKeyword("_ALPHATEST_ON");
        material.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
        EditorUtility.SetDirty(material);
    }

    private static GameObject CreatePrefab(Mesh mesh, Material front, Material back, Material edge, string path)
    {
        GameObject root = new GameObject("Rat_Cardboard_Cutout");
        MeshFilter meshFilter = root.AddComponent<MeshFilter>();
        meshFilter.sharedMesh = mesh;
        MeshRenderer meshRenderer = root.AddComponent<MeshRenderer>();
        meshRenderer.sharedMaterials = new[] { front, back, edge };
        meshRenderer.shadowCastingMode = ShadowCastingMode.On;
        meshRenderer.receiveShadows = true;
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
        UnityEngine.Object.DestroyImmediate(root);
        return prefab;
    }

    private static void PlaceInScene(GameObject prefab)
    {
        GameObject existing = GameObject.Find("Cardboard_Rat_Generated");
        if (existing != null) UnityEngine.Object.DestroyImmediate(existing);
        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        instance.name = "Cardboard_Rat_Generated";
        instance.transform.position = new Vector3(0f, -0.1f, 0f);
        instance.transform.rotation = Quaternion.Euler(0f, 0f, 0f);
        instance.transform.localScale = Vector3.one * 0.42f;

        Camera camera = Camera.main;
        if (camera != null)
        {
            camera.transform.position = new Vector3(0f, 0.5f, -8.5f);
            camera.transform.rotation = Quaternion.Euler(4f, 0f, 0f);
            camera.fieldOfView = 42f;
        }

        Light light = UnityEngine.Object.FindObjectOfType<Light>();
        if (light != null)
        {
            light.transform.rotation = Quaternion.Euler(48f, -28f, 12f);
            light.intensity = 1.15f;
            light.shadows = LightShadows.Soft;
        }
    }
}
