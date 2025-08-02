#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.AI;
using System.Collections.Generic;

public class NavMeshFromImageEditor : EditorWindow
{
    Texture2D navImage;
    float pixelScale = 1.0f;
    float colorTolerance = 8f;

    [MenuItem("Tools/NavMesh Generator From Image")]
    public static void ShowWindow()
    {
        GetWindow<NavMeshFromImageEditor>("NavMesh Image Generator");
    }

    void OnGUI()
    {
        GUILayout.Label("NavMesh From Image", EditorStyles.boldLabel);

        navImage = (Texture2D)EditorGUILayout.ObjectField("Input Image", navImage, typeof(Texture2D), false);
        pixelScale = EditorGUILayout.FloatField("Pixel Scale", pixelScale);
        colorTolerance = EditorGUILayout.Slider("Color Match Tolerance", colorTolerance, 0f, 32f);

        if (GUILayout.Button("Generate Walkable Mesh"))
        {
            if (navImage == null)
            {
                Debug.LogError("No image selected.");
                return;
            }

            GenerateWalkableMesh(navImage, pixelScale, (int)colorTolerance);
        }
    }

    void GenerateWalkableMesh(Texture2D image, float scale, int tolerance)
    {
        // Define color samples (your reference colors)
        Dictionary<string, Color32> sampleColors = new Dictionary<string, Color32>
        {
            ["WATER_DEEP"] = HexToColor32("#0f5e9c"),
            ["WATER_SHALLOW"] = HexToColor32("#2389da"),
            ["BEACH"] = HexToColor32("#C2B280"),
            ["FOREST_PINE"] = HexToColor32("#2C5F34"),
            ["GRASS_1"] = HexToColor32("#2E6F40")
        };

        Color32[] pixels = image.GetPixels32();
        int width = image.width;
        int height = image.height;
        bool[,] visited = new bool[width, height];

        GameObject root = new GameObject("GeneratedNavMeshRegions");

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                if (visited[x, y]) continue;

                Color32 color = pixels[x + y * width];
                if (!IsWalkable(color, sampleColors, tolerance)) continue;

                var region = FloodFill(x, y, width, height, pixels, visited, sampleColors, tolerance);
                if (region.Count > 0)
                    CreateRegionMesh(region, scale, root.transform);
            }
        }

        Debug.Log("Walkable regions generated. Add a NavMeshSurface and bake.");
        Selection.activeGameObject = root;
    }

    List<Vector2Int> FloodFill(int startX, int startY, int width, int height, Color32[] pixels, bool[,] visited,
                                Dictionary<string, Color32> sampleColors, int tolerance)
    {
        List<Vector2Int> region = new List<Vector2Int>();
        Queue<Vector2Int> queue = new Queue<Vector2Int>();
        queue.Enqueue(new Vector2Int(startX, startY));

        while (queue.Count > 0)
        {
            var p = queue.Dequeue();
            int x = p.x, y = p.y;
            if (x < 0 || y < 0 || x >= width || y >= height) continue;
            if (visited[x, y]) continue;

            Color32 c = pixels[x + y * width];
            if (!IsWalkable(c, sampleColors, tolerance)) continue;

            visited[x, y] = true;
            region.Add(p);

            queue.Enqueue(new Vector2Int(x + 1, y));
            queue.Enqueue(new Vector2Int(x - 1, y));
            queue.Enqueue(new Vector2Int(x, y + 1));
            queue.Enqueue(new Vector2Int(x, y - 1));
        }

        return region;
    }

    void CreateRegionMesh(List<Vector2Int> regionPixels, float scale, Transform parent)
    {
        GameObject regionObj = new GameObject("WalkableRegion");
        regionObj.transform.parent = parent;
        MeshFilter mf = regionObj.AddComponent<MeshFilter>();
        MeshRenderer mr = regionObj.AddComponent<MeshRenderer>();
        mr.sharedMaterial = AssetDatabase.GetBuiltinExtraResource<Material>("Default-Diffuse.mat");

        List<Vector3> verts = new List<Vector3>();
        List<int> tris = new List<int>();

        foreach (var p in regionPixels)
        {
            int startIdx = verts.Count;
            float px = p.x * scale;
            float py = p.y * scale;

            verts.Add(new Vector3(px, 0, py));
            verts.Add(new Vector3(px + scale, 0, py));
            verts.Add(new Vector3(px + scale, 0, py + scale));
            verts.Add(new Vector3(px, 0, py + scale));

            tris.Add(startIdx);
            tris.Add(startIdx + 1);
            tris.Add(startIdx + 2);
            tris.Add(startIdx);
            tris.Add(startIdx + 2);
            tris.Add(startIdx + 3);
        }

        Mesh mesh = new Mesh();
        mesh.SetVertices(verts);
        mesh.SetTriangles(tris, 0);
        mesh.RecalculateNormals();
        mf.sharedMesh = mesh;

        regionObj.AddComponent<MeshCollider>(); // Needed for NavMesh baking
    }

    bool IsWalkable(Color32 c, Dictionary<string, Color32> sampleColors, int tolerance)
    {
        return !ColorEqual(c, sampleColors["WATER_DEEP"], tolerance) &&
               !ColorEqual(c, sampleColors["WATER_SHALLOW"], tolerance) &&
               !ColorEqual(c, sampleColors["BEACH"], tolerance);
    }

    bool ColorEqual(Color32 a, Color32 b, int tolerance)
    {
        return Mathf.Abs(a.r - b.r) <= tolerance &&
               Mathf.Abs(a.g - b.g) <= tolerance &&
               Mathf.Abs(a.b - b.b) <= tolerance;
    }

    Color32 HexToColor32(string hex)
    {
        Color c;
        if (ColorUtility.TryParseHtmlString(hex, out c))
            return c;
        return Color.white;
    }
}
#endif
