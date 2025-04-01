using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro;
using Unity.AI.Navigation;
using UnityEditor.Rendering.LookDev;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Tilemaps;
using UnityEngine.UIElements;

/// <summary>
/// A set of extension methods for the <see cref="Color32"/> struct.
/// </summary>
public static class Color32Extensions
{
    /// <summary>
    /// Returns true if the <see cref="Color32.r">red,</see> <see cref="Color32.g">green,</see>
    /// <see cref="Color32.b">blue,</see> and <see cref="Color32.a">alpha</see> channels of both
    /// <see cref="Color32">Color32s</see> passed in are equal.
    /// </summary>
    public static bool IsEqualTo(this Color32 first, Color32 second) =>
        first.r == second.r && first.g == second.g && first.b == second.b && first.a == second.a;
}

public class GridGeneration : MonoBehaviour
{
    public Tilemap tilemap;
    public Sprite sprite;
    public TileBase greenTile;
    public TileBase shallowWater;
    public TileBase deepWater;
    public TileBase beachTile;
    public TileBase forestTile;

    public float thresholdToRegenerate = 20.0f;
    public int tileAmountToRegenerate = 120;

    Color32[] pixels = new Color32[256];
    private Vector2Int pixelSize;

    public Dictionary<string, Color32> sampleColors = new Dictionary<string, Color32>();

    private NavMeshData navMeshData; 
    private Dictionary<Vector2Int, NavMeshDataInstance> navMeshChunks = new Dictionary<Vector2Int, NavMeshDataInstance>();
    Dictionary<Tuple<Vector2Int, Vector2Int>, List<NavMeshLink>> navLinks = new Dictionary<Tuple<Vector2Int, Vector2Int>, List<NavMeshLink>>();
    //List<NavMeshLink> navLinks = new List<NavMeshLink>();
    List<NavMeshBuildSource> navMeshBuildSources = new List<NavMeshBuildSource>();

    const float bakeDelay = 0.1f;

    public static GridGeneration Instance;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
        }
        else
        {
            Instance = this;
        }
        enabled = false;
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        navMeshData = new NavMeshData();

        SetupSampleColors();

        if (sprite)
        {
            pixels = sprite.texture.GetPixels32();
            pixelSize = new Vector2Int(sprite.texture.width, sprite.texture.height);
        }
        //RegenerateMap(GetCoordinatOffseteForRegeneration(tileAmountToRegenerate), tileAmountToRegenerate);
        StartCoroutine(GenerateChunks());
    }

    //
    // Summary:
    //     Calculate a path between two points and store the resulting path.
    //
    // Parameters:
    //   sourcePosition:
    //     The initial position of the path requested.
    //
    //   targetPosition:
    //     The final position of the path requested.
    //
    //   areaMask:
    //     A bitfield mask specifying which NavMesh areas can be passed when calculating
    //     a path.
    //
    //   path:
    //     The resulting path.
    //
    // Returns:
    //     True if either a complete or partial path is found. False otherwise.
    public static bool CalculatePath(Vector3 sourcePosition, Vector3 targetPosition, int areaMask, ref NavMeshPath path)
    {
        Vector3 newSourcePosition = sourcePosition;
        Vector3 newTargetPosition = targetPosition;
        NavMeshHit? targetNavHit = SelectionController.FindProperNavHit(targetPosition, areaMask);
        if (targetNavHit.HasValue)
        {
            newTargetPosition = targetNavHit.Value.position;
        }
        NavMeshHit? sourceNavHit = SelectionController.FindProperNavHit(sourcePosition, areaMask);
        if (sourceNavHit.HasValue)
        {
            newSourcePosition = sourceNavHit.Value.position;
        }
        int area = (int)Mathf.Log(areaMask, 2);
        Instance.ChangeAllNavLinkArea(sourcePosition, area);
        return NavMesh.CalculatePath(newSourcePosition, newTargetPosition, areaMask, path);
    }

    public void FindShorePair(Vector3 position, out List<Vector3> positionList)
    {
        positionList = new List<Vector3>(3);
        Vector3 checkPosition = position;
        const int size = 120;
        Vector2Int chunkIndex = GetChunkIndex(position);
        Vector2Int start = new Vector2Int(Mathf.RoundToInt(position.x) - chunkIndex.x * size, Mathf.RoundToInt(position.z) - chunkIndex.y * size);
        bool[,] grid = GenerateGridForPathfinding(chunkIndex * size, size);
        if (grid[start.x, start.y])
        {
            // Water
            checkPosition = FindClosestLand(position, grid);
            Vector3 newPosition = checkPosition;
            checkPosition = FindClosestShoreline(newPosition, grid);

            positionList.Add(checkPosition);
            positionList.Add(newPosition);

            return;
        }
        checkPosition = FindClosestShoreline(position, grid);
        Vector3 landPosition = checkPosition;
        checkPosition = FindClosestLand(landPosition, grid);

        positionList.Add(landPosition);
        positionList.Add(checkPosition);
    }

    public Vector3 FindClosestLand(Vector3 position, bool[,] grid)
    {
        const int size = 120;
        Vector2Int chunkIndex = GetChunkIndex(position);
        Vector2Int start = new Vector2Int(Mathf.RoundToInt(position.x) - chunkIndex.x * size, Mathf.RoundToInt(position.z) - chunkIndex.y * size);

        grid = GenerateGridForPathfinding(chunkIndex * size, size);

        Vector2Int[] directions = {
            new Vector2Int( 0, 1 ),
            new Vector2Int(1, 0),
            new Vector2Int( 0, -1 ),
            new Vector2Int( -1, 0 )
        }; // Right, Down, Left, Up

        Queue<Vector2Int> queue = new Queue<Vector2Int>();
        HashSet<Vector2Int> visited = new HashSet<Vector2Int>();

        queue.Enqueue(start);
        visited.Add(start);

        bool IsValid(Vector2Int index)
        {
            return !visited.Contains(index);
        }

        while (queue.Count > 0)
        {
            Vector2Int dequeuedNode = queue.Dequeue();
            if (grid[dequeuedNode.x, dequeuedNode.y] == false)
            {
                Vector2Int newIndex = new Vector2Int(dequeuedNode.x + (chunkIndex.x * size), dequeuedNode.y + (chunkIndex.y * size));
                Vector3 pos = new Vector3(newIndex.x + 0.5f, 0, newIndex.y + 0.5f);
                return pos;
                //return dequeuedNode;
            }

            foreach (var dir in directions)
            {
                Vector2Int newIndex = dequeuedNode + dir;
                if (newIndex.x < 0) newIndex.x = 0;
                if (newIndex.x > size - 1) newIndex.x -= size - 1;
                if (newIndex.y < 0) newIndex.y = 0;
                if (newIndex.y > size - 1) newIndex.y -= size - 1;

                if (IsValid(newIndex))
                {
                    queue.Enqueue(newIndex);
                    visited.Add(newIndex);
                }
            }
        }

        return position;
        //return null; // No non-obstacle found
    }

    public Vector3 FindClosestShoreline(Vector3 position, bool[,] grid)
    {
        const int size = 120;
        Vector2Int chunkIndex = GetChunkIndex(position);
        Vector2Int start = new Vector2Int(Mathf.RoundToInt(position.x) - chunkIndex.x * size, Mathf.RoundToInt(position.z) - chunkIndex.y * size);


        Vector2Int[] directions = {
            new Vector2Int( 0, 1 ),
            new Vector2Int(1, 0),
            new Vector2Int( 0, -1 ),
            new Vector2Int( -1, 0 )
        }; // Right, Down, Left, Up

        Queue<Vector2Int> queue = new Queue<Vector2Int>();
        HashSet<Vector2Int> visited = new HashSet<Vector2Int>();

        queue.Enqueue(start);
        visited.Add(start);

        bool IsValid(Vector2Int index)
        {
            return !visited.Contains(index);
        }

        while (queue.Count > 0)
        {
            Vector2Int dequeuedNode = queue.Dequeue();
            if (grid[dequeuedNode.x, dequeuedNode.y] == true)
            {
                Vector2Int newIndex = new Vector2Int(dequeuedNode.x + (chunkIndex.x * size), dequeuedNode.y + (chunkIndex.y * size));
                Vector3 pos = new Vector3(newIndex.x + 0.5f, 0, newIndex.y + 0.5f);
                return pos;
                //return dequeuedNode;
            }

            foreach (var dir in directions)
            {
                Vector2Int newIndex = dequeuedNode + dir;
                if (newIndex.x < 0) newIndex.x = 0;
                if (newIndex.x > size - 1) newIndex.x -= size - 1;
                if (newIndex.y < 0) newIndex.y = 0;
                if (newIndex.y > size - 1) newIndex.y -= size - 1;

                if (IsValid(newIndex))
                {
                    queue.Enqueue(newIndex);
                    visited.Add(newIndex);
                }
            }
        }

        return position;
        //return null; // No non-obstacle found
    }

    void ChangeAllNavLinkArea(Vector3 position, int area)
    {
        Vector2Int startChunkIndex = GetChunkIndex(position);

        Vector2Int[] directions = {
            new Vector2Int(1, 0),  // Right
            new Vector2Int(-1, 0), // Left
            new Vector2Int(0, 1),  // Up
            new Vector2Int(0, -1)  // Down
        };

        foreach (var dir in directions)
        {
            Vector2Int neightborChunkIndex = startChunkIndex + dir;
            if (navLinks.TryGetValue(new Tuple<Vector2Int, Vector2Int>(startChunkIndex, neightborChunkIndex), out List<NavMeshLink> firstLinks))
            {
                foreach (var link in firstLinks)
                {
                    link.area = area;
                    link.UpdateLink();
                }
            }

            if (navLinks.TryGetValue(new Tuple<Vector2Int, Vector2Int>(neightborChunkIndex, startChunkIndex), out List<NavMeshLink> secondLinks))
            {
                foreach (var link in secondLinks)
                {
                    link.area = area;
                    link.UpdateLink();
                }
            }
        }

        //foreach (NavMeshLink link in navLinks)
        //{
        //    link.area = area;
        //    link.UpdateLink();
        //}
    }

    void ConnectNavMeshChunks(Vector2Int chunkPos, int chunkSize)
    {
        if (!navMeshChunks.ContainsKey(chunkPos)) return;
        Vector2Int[] directions = {
            //new Vector2Int(1, 0),  // Right
            new Vector2Int(-1, 0), // Left
            //new Vector2Int(0, 1),  // Up
            new Vector2Int(0, -1)  // Down
        };

        void MakeNavmeshLink(Vector3 start, Vector3 end, Vector2Int startChunkIndex, Vector2Int neightborChunkIndex)
        {
            NavMeshLink link = new GameObject($"NavMeshLink_{new Vector2Int((int)start.x, (int)start.z)}_{new Vector2Int((int)end.x, (int)end.z)}").AddComponent<NavMeshLink>();
            link.startPoint = start;
            link.endPoint = end;
            link.width = 1f;// chunkSize;  // Adjust width to cover path
            link.bidirectional = true;
            link.costModifier = 1f;  // Default movement cost
            link.area = NavMesh.AllAreas;
            link.transform.SetParent(transform); // Keep hierarchy clean
            Tuple<Vector2Int, Vector2Int> chunkPair = new Tuple<Vector2Int, Vector2Int>(startChunkIndex, neightborChunkIndex);
            if (!navLinks.ContainsKey(chunkPair))
            {
                navLinks.Add(chunkPair, new List<NavMeshLink>());
            }
            if (navLinks[chunkPair] != null)
            {
                navLinks[chunkPair].Add(link);
            }
        }

        void LinkBetweenChunk(Vector2Int mainChunkIndex, Vector2Int neighbouringChunkIndex)
        {
            Vector2Int diff = neighbouringChunkIndex - mainChunkIndex;
            Debug.Log(diff);
            if (diff == new Vector2Int(-1, 0))
            {
                // Top edge (left to right)
                for (int x = 0; x < chunkSize; x++)
                {
                    Vector3 start = new Vector3(0.5f, 0, x + 0.5f);
                    start.x += mainChunkIndex.x * chunkSize;
                    start.z += mainChunkIndex.y * chunkSize;
                    Vector3 end = start;
                    end.x -= 1;
                    //end.z += 1;
                    //Vector3 end = new Vector3(bottomEdge + 1 + 0.5f, 0, x);
                    //end.x += neighbouringChunkIndex.x * chunkSize;
                    //DebugExtension.DebugWireSphere(start, Color.blue, 0.25f, 20.0f);
                    //DebugExtension.DebugWireSphere(end, Color.red, 0.25f, 20.0f);
                    MakeNavmeshLink(start, end, mainChunkIndex, neighbouringChunkIndex);
                }
            }
            if (diff == new Vector2Int(0, -1))
            {
                // Left edge (bottom to top)
                for (int y = 0; y < chunkSize; y++)
                {
                    int rightEdge = chunkSize - 1;
                    Vector3 start = new Vector3(y + 0.5f, 0, 0.5f);
                    start.x += mainChunkIndex.x * chunkSize;
                    start.z += mainChunkIndex.y * chunkSize;
                    Vector3 end = start;
                    end.z -= 1;
                    //DebugExtension.DebugWireSphere(start, Color.blue, 0.25f, 20.0f);
                    //DebugExtension.DebugWireSphere(end, Color.red, 0.25f, 20.0f);
                    MakeNavmeshLink(start, end, mainChunkIndex, neighbouringChunkIndex);
                }
            }

            // Right edge (top to bottom, excluding corners)
            //for (int y = 1; y < chunkSize - 1; y++)
            //{
            //
            //}

            // Right edge (top to bottom, excluding corners)
            //for (int y = 0; y < chunkSize - 1; y++)
            //{
            //    int leftEdge = chunkSize - 1;
            //}

            // Bottom edge (right to left)
            //for (int x = chunkSize - 1; x >= 0; x--)
            //{
            //    int topEdge = 0;
            //}

            // Left edge (bottom to top, excluding corners)
            //for (int y = chunkSize - 2; y > 0; y--)
            //{
            //    int rightEdge = chunkSize - 1;
            //}

            // Left edge (bottom to top)
            //for (int y = chunkSize - 1; y > 0; y--)
            //{
            //    int rightEdge = 0;
            //}
        }

        foreach (var dir in directions)
        {
            Vector2Int neighborPos = chunkPos + dir;
            if (navMeshChunks.ContainsKey(neighborPos)) // Check if neighbor chunk exists
            {
                LinkBetweenChunk(chunkPos, neighborPos);
                Vector3 start = new Vector3(chunkPos.x * chunkSize + (dir.x * chunkSize * 0.5f), 0, chunkPos.y * chunkSize + (dir.y * chunkSize * 0.5f));
                Vector3 end = new Vector3(neighborPos.x * chunkSize - (dir.x * chunkSize * 0.5f), 0, neighborPos.y * chunkSize - (dir.y * chunkSize * 0.5f));

                NavMeshLink link = new GameObject($"NavMeshLink_{chunkPos}_{neighborPos}").AddComponent<NavMeshLink>();
                link.startPoint = start;
                link.endPoint = end;
                link.width = 2f; // chunkSize;  // Adjust width to cover path
                link.bidirectional = true;
                link.costModifier = 1f;  // Default movement cost
                link.transform.SetParent(transform); // Keep hierarchy clean
            }
        }
    }

    Vector2Int GetChunkIndex(Vector3 position)
    {
        const int chunkSize = 120;
        Vector3 floatIndex = position / chunkSize;
        Vector2Int returnIndex = new Vector2Int((int)floatIndex.x, (int)floatIndex.z);
        return returnIndex;
    }

    IEnumerator GenerateChunks()
    {
        const int chunkSize = 120;
        const int chunkAmount = 2; // For testing, adjust for larger maps

        for (int i = 0; i < chunkAmount; i++)
        {
            for (int j = 0; j < chunkAmount; j++)
            {
                Vector2Int offset = new Vector2Int(i * chunkSize, j * chunkSize);

                // Generate a new NavMeshData for each chunk
                NavMeshData navMeshData = new NavMeshData();
                NavMeshDataInstance instance = NavMesh.AddNavMeshData(navMeshData);
                navMeshChunks[new Vector2Int(i, j)] = instance;

                // Generate and update the NavMesh for this chunk
                StartCoroutine(UpdateNavMeshDataAsync(navMeshData, offset, chunkSize));

                yield return new WaitForSeconds(bakeDelay); // Delay to avoid lag spikes

                ConnectNavMeshChunks(new Vector2Int(i, j), chunkSize);
            }
        }

        GameManager.Instance.IncrementLoadCount();
    }

    IEnumerator UpdateNavMeshDataAsync(NavMeshData navMeshData, Vector2Int offset, int tileAmountToRegenerate)
    {
        List<NavMeshBuildSource> navMeshBuildSources = new List<NavMeshBuildSource>();

        void PerTileNavmeshGeneration(Vector3Int index, int area = 0)
        {
            Vector3 position = new Vector3(index.x + 0.5f, 0, index.y + 0.5f);
            const float tileSize = 1.0f;
            NavMeshBuildSource source = new NavMeshBuildSource();
            source.shape = NavMeshBuildSourceShape.Box;
            source.size = new Vector3(tileSize, 0, tileSize);
            source.transform = Matrix4x4.TRS(position, Quaternion.identity, Vector3.one);
            source.area = area;
            navMeshBuildSources.Add(source);
        }

        // Ensure new sources are generated per chunk
        for (int i = 0; i < tileAmountToRegenerate; i++)
        {
            for (int j = 0; j < tileAmountToRegenerate; j++)
            {
                Vector3Int new_index = new Vector3Int(i + offset.x, j + offset.y, 0);
                Color32 sampleColor32 = pixels[new_index.x * pixelSize.x + new_index.y];
                if (isWalkable(sampleColor32))
                {
                    PerTileNavmeshGeneration(new_index);
                }
                else
                {
                    PerTileNavmeshGeneration(new_index, 3);
                }
            }
        }

        // Correctly centered bounds for this chunk
        Vector3 chunkCenter = new Vector3(offset.x + tileAmountToRegenerate / 2, 1, offset.y + tileAmountToRegenerate / 2);
        Vector3 chunkSizeVector = new Vector3(tileAmountToRegenerate, 10, tileAmountToRegenerate);
        Bounds bounds = new Bounds(chunkCenter, chunkSizeVector);

        NavMeshBuildSettings settings = NavMesh.GetSettingsByID(0);

        // Call UpdateNavMeshDataAsync to avoid blocking the main thread
        yield return NavMeshBuilder.UpdateNavMeshDataAsync(navMeshData, settings, navMeshBuildSources, bounds);

        Debug.Log($"NavMesh Chunk at {offset} baked successfully.");
    }

    //IEnumerator GenerateChunks()
    //{
    //    const int chunkSize = 120;
    //    //const int chunkAmount = 1440 / chunkSize;
    //    const int chunkAmount = 2;
    //
    //    for (int i = 0; i < chunkAmount; i++)
    //    {
    //        for (int j = 0; j < chunkAmount; j++)
    //        {
    //            Vector2Int offset = new Vector2Int(i * chunkSize, j * chunkSize);
    //            Debug.Log(offset);
    //            NavMeshData navMeshData = GenerateNavmeshDataForGrid(offset, chunkSize);
    //            NavMeshDataInstance instance = NavMesh.AddNavMeshData(navMeshData);
    //            navMeshChunks[new Vector2Int(i, j)] = instance;
    //
    //            yield return new WaitForSeconds(bakeDelay);
    //        }
    //    }
    //}

    public NavMeshDataInstance? GetChunkInstanceFromGridpoint(Vector2Int node)
    {
        const int chunkSize = 120;

        Vector2Int chunkIndex = node / chunkSize;
        if (navMeshChunks.TryGetValue(chunkIndex, out var instance))
        {
            return instance;
        }
        return null;
    }

    public Vector2Int ClampNode(Vector2Int node)
    {
        if (node.x < 0) node.x = 0;
        if (node.x > pixelSize.x - 1) node.x -= pixelSize.x - 1;
        if (node.y < 0) node.y = 0;
        if (node.y > pixelSize.y - 1) node.y -= pixelSize.y - 1;
        return node;
    }

    public static Vector2Int? FindClosestNonObstacle(bool[,] grid, Vector2Int start, int size)
    {
        Vector2Int[] directions = { 
            new Vector2Int( 0, 1 ), 
            new Vector2Int(1, 0),
            new Vector2Int( 0, -1 ),
            new Vector2Int( -1, 0 )
        }; // Right, Down, Left, Up

        Queue<Vector2Int> queue = new Queue<Vector2Int>();
        HashSet<Vector2Int> visited = new HashSet<Vector2Int>();

        queue.Enqueue(start);
        visited.Add(start);

        bool IsValid(Vector2Int index)
        {
            return !visited.Contains(index);
        }

        while (queue.Count > 0)
        {
            Vector2Int dequeuedNode = queue.Dequeue();
            if (grid[dequeuedNode.x, dequeuedNode.y] == true)
            {
                return dequeuedNode;
            }

            foreach (var dir in directions)
            {
                Vector2Int newIndex = dequeuedNode + dir;
                if (newIndex.x < 0) newIndex.x = 0;
                if (newIndex.x > size - 1) newIndex.x -= size - 1;
                if (newIndex.y < 0) newIndex.y = 0;
                if (newIndex.y > size - 1) newIndex.y -= size - 1;

                if (IsValid(newIndex))
                {
                    queue.Enqueue(newIndex);
                    visited.Add(newIndex);
                }
            }
        }

        return null; // No non-obstacle found
    }

    public Vector2Int PredictOffsetFromPosition(Vector3 position, int size)
    {
        Vector2Int returnOffset = new Vector2Int((int)position.x, (int)position.z);
        returnOffset.x = Mathf.Clamp(returnOffset.x, 0, pixelSize.x - size);
        returnOffset.y = Mathf.Clamp(returnOffset.y, 0, pixelSize.y - size);
        return returnOffset;
    }

    public bool HasLineOfSight(Vector2Int start, Vector2Int end, bool[,] grid)
    {
        int x0 = start.x, y0 = start.y;
        int x1 = end.x, y1 = end.y;

        int dx = Mathf.Abs(x1 - x0);
        int dy = Mathf.Abs(y1 - y0);
        int sx = x0 < x1 ? 1 : -1;
        int sy = y0 < y1 ? 1 : -1;
        int err = dx - dy;

        while (true)
        {
            if (!grid[x0, y0]) return false; // If any cell is not walkable, return false
            if (x0 == x1 && y0 == y1) break;

            int e2 = 2 * err;
            if (e2 > -dy) { err -= dy; x0 += sx; }
            if (e2 < dx) { err += dx; y0 += sy; }
        }
        return true; // No obstacles found
    }

    public List<Vector2Int> SmoothPath(List<Vector2Int> path, bool[,] grid)
    {
        if (path == null || path.Count < 2) return path;

        List<Vector2Int> smoothPath = new List<Vector2Int> { path[0] };

        int i = 0;
        while (i < path.Count - 1)
        {
            int j = path.Count - 1; // Try to reach the last waypoint directly

            while (j > i + 1)
            {
                if (HasLineOfSight(path[i], path[j], grid)) // If direct path exists, skip points
                {
                    break;
                }
                j--; // Otherwise, move one step back
            }

            smoothPath.Add(path[j]);
            i = j;
        }

        return smoothPath;
    }

    public NavMeshData GenerateNavmeshDataForGrid(Vector2Int offset, int tileAmountToRegenerate)
    {
        void PerTileNavmeshGeneration(Vector3Int index, int area = 0)
        {
            Vector3 position = new Vector3(index.x, 0, index.y);
            //Debug.Log(position);
            const float tileSize = 1.0f;
            NavMeshBuildSource source = new NavMeshBuildSource();
            source.shape = NavMeshBuildSourceShape.Box;
            source.size = new Vector3(tileSize, 0, tileSize);
            source.transform = Matrix4x4.TRS(position, Quaternion.identity, Vector3.one);
            source.area = area; // Default walkable area
            navMeshBuildSources.Add(source);
        }

        for (int i = 0; i < tileAmountToRegenerate; i++)
        {
            for (int j = 0; j < tileAmountToRegenerate; j++)
            {
                Vector3Int new_index = new Vector3Int(i + offset.x, j + offset.y, 0);
                Color32 sampleColor32 = pixels[new_index.x * pixelSize.x + new_index.y];
                if (isWalkable(sampleColor32))
                {
                    PerTileNavmeshGeneration(new_index);
                }
                else
                {
                    PerTileNavmeshGeneration(new_index, 3);
                }
            }
        }
        Vector3 chunkCenter = new Vector3((offset.x + tileAmountToRegenerate / 2), 1, (offset.y + tileAmountToRegenerate / 2));
        Vector3 chunkSizeVector = new Vector3(tileAmountToRegenerate, 10, tileAmountToRegenerate); // Adjust height if needed

        //Vector3 chunkCenter = new Vector3(1440 / 2, 0, 1440 / 2);
        //Vector3 chunkSizeVector = new Vector3(1440 / 2, 1, 1440 / 2);

        Bounds bounds = new Bounds(chunkCenter, chunkSizeVector);
        NavMeshBuildSettings settings = NavMesh.GetSettingsByID(0);
        NavMeshBuilder.UpdateNavMeshData(navMeshData, settings, navMeshBuildSources, bounds);

        return navMeshData;
    }

    public bool[,] GenerateGridForPathfinding(Vector2Int offset, int tileAmountToRegenerate, bool isWater = false)
    {
        bool[,] result = new bool[tileAmountToRegenerate, tileAmountToRegenerate];
        offset.x = Mathf.Clamp(offset.x, 0, pixelSize.x - tileAmountToRegenerate);
        offset.y = Mathf.Clamp(offset.y, 0, pixelSize.y - tileAmountToRegenerate);

        for (int i = 0; i < tileAmountToRegenerate; i++)
        {
            for (int j = 0; j < tileAmountToRegenerate; j++)
            {
                Vector3Int new_index = new Vector3Int(i + offset.x, j + offset.y, 0);
                Color32 sampleColor32 = pixels[new_index.x * pixelSize.x + new_index.y];
                if (!isWalkable(sampleColor32, isWater))
                {
                    result[i, j] = false;
                    continue;
                }
                result[i, j] = true;
            }
        }
        return result;
    }

    void SetupSampleColors()
    {
        Color sampleColor = Color.white;
        if (ColorUtility.TryParseHtmlString("#0f5e9c", out sampleColor))
            sampleColors.Add("WATER_DEEP", sampleColor);

        if (ColorUtility.TryParseHtmlString("#2389da", out sampleColor))
            sampleColors.Add("WATER_SHALLOW", sampleColor);

        if (ColorUtility.TryParseHtmlString("#C2B280", out sampleColor))
            sampleColors.Add("BEACH", sampleColor);

        if (ColorUtility.TryParseHtmlString("#2C5F34", out sampleColor))
            sampleColors.Add("FOREST_PINE", sampleColor);

        if (ColorUtility.TryParseHtmlString("#2E6F40", out sampleColor))
            sampleColors.Add("GRASS_1", sampleColor);
    }

    bool isWalkable(Color32 sampleColor32, bool isWater = false)
    {
        if (isWater)
        {
            if (Color32Extensions.IsEqualTo(sampleColor32, sampleColors["WATER_SHALLOW"]) ||
                Color32Extensions.IsEqualTo(sampleColor32, sampleColors["WATER_DEEP"]) ||
                Color32Extensions.IsEqualTo(sampleColor32, sampleColors["BEACH"]))
            {
                return true;
            }
        }
        else
        {
            if (Color32Extensions.IsEqualTo(sampleColor32, sampleColors["GRASS_1"]) ||
                Color32Extensions.IsEqualTo(sampleColor32, sampleColors["BEACH"]))
            {
                return true;
            }
        }

        return false;
    }

    //Vector2Int GetCoordinatOffseteForRegeneration(int size)
    //{
    //    float halfSize = size / 2.0f;
    //    Vector2Int returnValue = new Vector2Int(Mathf.CeilToInt(transformToFollow.position.x - halfSize), Mathf.CeilToInt(transformToFollow.position.z - halfSize));
    //    return returnValue;
    //}

    void RegenerateMap(Vector2Int offset, int size)
    {
        offset.x = Mathf.Clamp(offset.x, 0, pixelSize.x - size);
        offset.y = Mathf.Clamp(offset.y, 0, pixelSize.y - size);

        tilemap.ClearAllTiles();

        for (int i = 0; i < size; i++)
        {
            for (int j = 0; j < size; j++)
            {
                Vector3Int new_index = new Vector3Int(i + offset.x, j + offset.y, 0);
                Color32 sampleColor32 = pixels[new_index.x * pixelSize.x + new_index.y];
                //Debug.Log($"{sampleColor32}");
                //Debug.Log(sampleColor.ToString());
                if (Color32Extensions.IsEqualTo(sampleColor32, sampleColors["WATER_DEEP"]))
                {
                    tilemap.SetTile(new_index, deepWater);
                    continue;
                }
                if (Color32Extensions.IsEqualTo(sampleColor32, sampleColors["WATER_SHALLOW"]))
                {
                    tilemap.SetTile(new_index, shallowWater);
                    continue;
                }
                if (Color32Extensions.IsEqualTo(sampleColor32, sampleColors["BEACH"]))
                {
                    tilemap.SetTile(new_index, beachTile);
                    continue;
                }
                if (Color32Extensions.IsEqualTo(sampleColor32, sampleColors["FOREST_PINE"]))
                {
                    tilemap.SetTile(new_index, forestTile);
                    continue;
                }


                tilemap.SetTile(new_index, greenTile);
            }
        }
    }
}
