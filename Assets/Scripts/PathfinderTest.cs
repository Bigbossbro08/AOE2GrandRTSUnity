using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(MovementComponent))]
public class PathfinderTest : MonoBehaviour
{
    Vector2Int startNode;
    Vector2Int endNode;
    MovementComponent movementComponent;

    public enum UnitID : int
    {
        VILLAGER_MALE = 83,
        VILLAGER_FEMALE = 293,
        SCOUT_CAVALRY = 448,
        TOWN_CENTER = 109
    }
    
    GridGeneration GetGridGeneration()
    {
        return GridGeneration.Instance;
    }

    class MapData
    {
        Vector2Int offset = Vector2Int.zero;
        int size = 120;
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        movementComponent = GetComponent<MovementComponent>();
    }

    void StartPathfind(Vector3 newPosition)
    {
        GridGeneration GridGeneration = GetGridGeneration();
        Vector3 worldPos = newPosition;

        int mapSize = 120;
        //Vector2Int offsetPoint = GridGeneration.PredictOffsetFromPosition(worldPos, mapSize);
        //offsetPoint = Vector2Int.one;
        //var grids = GridGeneration.GenerateGridForPathfinding(offsetPoint, mapSize);

        startNode = new Vector2Int((int)transform.position.x, (int)transform.position.z);
        startNode = GridGeneration.ClampNode(startNode);

        endNode = new Vector2Int((int)worldPos.x, (int)worldPos.z);
        endNode = GridGeneration.ClampNode(endNode);

        Vector2Int midNode = (startNode + endNode) / 2;
        Vector2Int offsetNode = midNode - Vector2Int.one * (160 / 2);
        if (endNode.x > offsetNode.x + mapSize - 1) endNode.x = offsetNode.x + mapSize - 1;
        if (endNode.y > offsetNode.y + mapSize - 1) endNode.y = offsetNode.y + mapSize - 1;
        offsetNode = GridGeneration.ClampNode(offsetNode);
        Debug.Log($"{offsetNode}, {endNode}");

        var grids = GridGeneration.GenerateGridForPathfinding(offsetNode, mapSize);

        if (grids[endNode.x, endNode.y] == false)
        {
            Vector2Int? newEndNode = GridGeneration.FindClosestNonObstacle(grids, endNode, mapSize);
            endNode = newEndNode.Value;
        }
        var raw_points = AStarPathfinding.FindPath(startNode, endNode, grids);
        // var points = GridGeneration.SmoothPath(raw_points, grids);
        var points = raw_points;
        for (int i = 1; i < points.Count; i++)
        {
            Vector3 startPos = new Vector3(points[i - 1].x + 0.5f, 0, points[i - 1].y + 0.5f);
            Vector3 endPos = new Vector3(points[i].x + 0.5f, 0, points[i].y + 0.5f);
            DebugExtension.DebugWireSphere(startPos, Color.blue, 0.5f, 5);
            DebugExtension.DebugWireSphere(endPos, Color.blue, 0.5f, 5);
            //Debug.Log($"{points[i - 1]} and {points[1]}");
        }

        if (movementComponent && points != null && points.Count > 0)
        {
            List<Vector3> positions = new List<Vector3>();
            for (int i = 0; i < points.Count; i++)
            {
                Vector3 pos = new Vector3(points[i].x + 0.5f, 0, points[i].y + 0.5f);
                positions.Add(pos);
            }
            positions.Add(new Vector3(worldPos.x, 0, worldPos.z));
            movementComponent.SetPositionData(positions);
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetMouseButtonDown(2))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                GridGeneration GridGeneration = GetGridGeneration();
                Vector3 worldPos = hit.point;
                int mapSize = 120;
                Vector2Int offsetPoint = GridGeneration.PredictOffsetFromPosition(worldPos, mapSize);
                var grids = GridGeneration.GenerateGridForPathfinding(offsetPoint, mapSize);
                for (int i = 0; i < mapSize; i++)
                {
                    for (int j = 0; j < mapSize; j++)
                    {
                        worldPos = new Vector3((int)worldPos.x, 0.0f, (int)worldPos.z);
                        Vector3 newPos = worldPos + new Vector3(i + 0.5f, 0, j + 0.5f);
                        DebugExtension.DebugWireSphere(newPos, grids[i, j] ? Color.green : Color.red, 0.5f, 5);
                        Debug.DrawRay(worldPos + new Vector3(0.5f, 0, 0.5f), Vector3.up, Color.yellow, 10f);
                    }
                }
            }
            return;
        }

        if (Input.GetMouseButtonDown(1))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                StartPathfind(hit.point);
            }
            return;
        }

        if (Input.GetKey(KeyCode.Space))
        {
            int mapSize = 120;
            int halfSize = mapSize / 2;
            GridGeneration GridGeneration = GetGridGeneration();
            startNode = new Vector2Int((int)transform.position.x, (int)transform.position.z);
            startNode = GridGeneration.ClampNode(startNode);
            Vector2Int offset = startNode - Vector2Int.one * halfSize;
            offset = GridGeneration.ClampNode(offset);

            string arguments = $"{offset.x} {offset.y} {mapSize}";

            PythonComponent pythonComponent = GameManager.Instance.PythonComponent;
            if (pythonComponent)
            {
                pythonComponent.unitDataList.Clear();
                UnitData unitData = new UnitData {
                    _player = 1,
                    x = transform.position.x - offset.x,
                    y = transform.position.z - offset.y,
                    z = 0.0f,
                    reference_id = 0,
                    unit_const = (int)UnitID.SCOUT_CAVALRY,
                    status = 2,
                    rotation = 0.0f,
                    initial_animation_frame = 0,
                    garrisoned_in_id = -1,
                    caption_string_id = -1
                };
                pythonComponent.unitDataList.Add(unitData);
                // Start base build
                pythonComponent.RunPythonScript(arguments);
            }
        }
    }
}
