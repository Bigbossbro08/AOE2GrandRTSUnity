using System;
using System.Collections.Generic;
using UnityEngine;

public class AStarPathfinding
{
    public class Node : IComparable<Node>
    {
        public Vector2Int Position;
        public Node Parent;
        public int G, H;
        public int F => G + H;

        public Node(Vector2Int pos, Node parent, int g, int h)
        {
            Position = pos;
            Parent = parent;
            G = g;
            H = h;
        }

        public int CompareTo(Node other) => F.CompareTo(other.F);
    }

    private static readonly Vector2Int[] Directions = {
        new Vector2Int(0, 1), new Vector2Int(1, 0),
        new Vector2Int(0, -1), new Vector2Int(-1, 0)
    };

    public static List<Vector2Int> FindPath(Vector2Int start, Vector2Int goal, bool[,] grid)
    {
        MinHeap<Node> openSet = new MinHeap<Node>();
        HashSet<Vector2Int> closedSet = new HashSet<Vector2Int>();

        openSet.Push(new Node(start, null, 0, Heuristic(start, goal)));

        while (openSet.Count > 0)
        {
            Node current = openSet.Pop();
            if (current.Position == goal) return ConstructPath(current);

            closedSet.Add(current.Position);

            foreach (Vector2Int dir in Directions)
            {
                Vector2Int neighborPos = current.Position + dir;
                if (!IsValid(neighborPos, grid) || closedSet.Contains(neighborPos)) continue;

                int gCost = current.G + 1;
                Node neighbor = new Node(neighborPos, current, gCost, Heuristic(neighborPos, goal));

                openSet.Push(neighbor);
            }
        }

        return new List<Vector2Int>(); // No path found
    }

    private static List<Vector2Int> ConstructPath(Node node)
    {
        List<Vector2Int> path = new List<Vector2Int>();
        while (node != null)
        {
            path.Add(node.Position);
            node = node.Parent;
        }
        path.Reverse();
        return path;
    }

    private static bool IsValid(Vector2Int pos, bool[,] grid)
    {
        return pos.x >= 0 && pos.y >= 0 && pos.x < grid.GetLength(0) && pos.y < grid.GetLength(1) && grid[pos.x, pos.y];
    }

    private static int Heuristic(Vector2Int a, Vector2Int b)
    {
        return Math.Abs(a.x - b.x) + Math.Abs(a.y - b.y); // Manhattan Distance
    }
}
