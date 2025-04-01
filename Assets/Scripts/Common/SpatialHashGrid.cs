using System;
using System.Collections.Generic;
using UnityEngine;

public class SpatialHashGrid : MonoBehaviour {
    private Dictionary<Vector2Int, HashSet<Unit>> grid = new Dictionary<Vector2Int, HashSet<Unit>>();
    private const int cellSize = 10;

    public static Vector2Int GetCell(Vector3 position)
    {
        // Convert screen space to world space if needed
        //Vector3 worldPos = Camera.main.ScreenToWorldPoint(new Vector3(position.x, position.y, Camera.main.nearClipPlane));

        return new Vector2Int(
            Mathf.FloorToInt(position.x / cellSize),
            Mathf.FloorToInt(position.z / cellSize)
        );
    }

    public void Register(Unit unit)
    {
        //Vector2 screenPos = Camera.main.WorldToScreenPoint(unit.transform.position);
        Vector2Int cell = GetCell(unit.transform.position);

        if (!grid.ContainsKey(cell))
            grid[cell] = new HashSet<Unit>();

        grid[cell].Add(unit);
    }

    public void Unregister(Unit unit)
    {
        //Vector2Int cell = GetCell(Camera.main.WorldToScreenPoint(unit.transform.position));
        Vector2Int cell = GetCell(unit.transform.position);
        if (grid.ContainsKey(cell))
        {
            grid[cell].Remove(unit);
            if (grid[cell].Count == 0) grid.Remove(cell); // Remove empty cells
        }
    }

    public void UpdateUnit(Unit unit, Vector2Int oldCell)
    {
        //Vector2Int oldCell = GetCell(unit.transform.position); //GetCell(Camera.main.WorldToScreenPoint(unit.transform.position));
        Vector2Int newCell = GetCell(unit.transform.position); //GetCell(Camera.main.WorldToScreenPoint(unit.transform.position));

        if (oldCell != newCell) // Only update if moved to a new cell
        {
            Unregister(unit);
            Register(unit);
        }
    }

    public bool TryGetUnitsInCell(Vector2Int cell, out HashSet<Unit> units)
    {
        return grid.TryGetValue(cell, out units);
    }

    // Add support for bigger radius
    public List<Unit> QueryInRadius(Vector3 position, float radius)
    {
        List<Unit> found = new List<Unit>();
        Vector2Int objectIndex = GetCell(position);
        for (int i = -1; i < 1; i++)
        {
            for (int j = -1; j < 1; j++)
            {
                Vector2Int cell = new Vector2Int(objectIndex.x + i, objectIndex.y + j);
                if (grid.TryGetValue(cell, out HashSet<Unit> cellObjs))
                {
                    foreach (Unit obj in cellObjs)
                    {
                        if (Vector3.SqrMagnitude(obj.transform.position - position) < radius)
                        {
                            found.Add(obj);
                        }
                    }
                }
            }
        } 
        return found;
    }

    //public List<GameObject> Query(Rect selectionBox)
    //{
    //    List<GameObject> found = new List<GameObject>();
    //    Vector2Int minCell = GetCell(new Vector2(selectionBox.xMin, selectionBox.yMin));
    //    Vector2Int maxCell = GetCell(new Vector2(selectionBox.xMax, selectionBox.yMax));
    //
    //    for (int x = minCell.x; x <= maxCell.x; x++)
    //    {
    //        for (int y = minCell.y; y <= maxCell.y; y++)
    //        {
    //            Vector2Int cell = new Vector2Int(x, y);
    //            if (grid.ContainsKey(cell))
    //            {
    //                foreach (GameObject obj in grid[cell])
    //                {
    //                    // Convert unit position to screen space
    //                    Vector2 screenPos = Camera.main.WorldToScreenPoint(obj.transform.position);
    //
    //                    if (IsSpriteInsideSelection(obj, selectionBox))
    //                    {
    //                        found.Add(obj);
    //                    }
    //                }
    //            }
    //        }
    //    }
    //    return found;
    //}

    //bool IsSpriteInsideSelection(GameObject obj, Rect selectionBox)
    //{
    //    SpriteRenderer spriteRenderer = obj.GetComponentInChildren<SpriteRenderer>();
    //    if (spriteRenderer == null) return false;
    //
    //    // Get sprite world bounds
    //    Bounds spriteBounds = spriteRenderer.bounds;
    //
    //    // Convert bounds to screen space
    //    Vector3 minScreen = Camera.main.WorldToScreenPoint(spriteBounds.min);
    //    Vector3 maxScreen = Camera.main.WorldToScreenPoint(spriteBounds.max);
    //
    //    Rect spriteScreenRect = new Rect(
    //        Mathf.Min(minScreen.x, maxScreen.x),
    //        Mathf.Min(minScreen.y, maxScreen.y),
    //        Mathf.Abs(maxScreen.x - minScreen.x),
    //        Mathf.Abs(maxScreen.y - minScreen.y)
    //    );
    //
    //    // Check for intersection with selection box
    //    return selectionBox.Overlaps(spriteScreenRect);
    //}
}