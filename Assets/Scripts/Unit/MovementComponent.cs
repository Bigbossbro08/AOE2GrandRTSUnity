using Newtonsoft.Json;
using NUnit;
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UIElements;
using static CommonStructures;
using static UnityEditor.Experimental.AssetDatabaseExperimental.AssetDatabaseCounters;
using static UnityEngine.UI.CanvasScaler;

public class MovementComponent : MonoBehaviour, IDeterministicUpdate, MapLoader.IMapSaveLoad
{
    [JsonObject(MemberSerialization.OptIn)]
    public class MovementComponentData : MapLoader.SaveLoadData
    {
        [JsonProperty] public bool enabled;

        [JsonProperty] public float radius;
        [JsonProperty] public float movementSpeed;
        [JsonProperty] public float rotationSpeed;

        [JsonProperty] public uint flags;
        //[JsonProperty] public bool controlRotation = true;
        //[JsonProperty] public bool isWater = false;
        //[JsonProperty] public bool isAutoReverseable = false;
        //[JsonProperty] public bool canApplyBoidsAvoidance = false;

        [JsonProperty] public ulong crowdID = 0;

        [JsonProperty] public int conditionToBlockBoids = 0;
        [JsonProperty] public int conditionToActivateRaycast = 0;

        [JsonProperty] public List<CommonStructures.SerializableVector3> positions = new List<CommonStructures.SerializableVector3>();

        public MovementComponentData() { type = "MovementComponentData"; }
    }

    // Basic Stats
    public float radius = 0.2f;
    public float movementSpeed = 0.92f;
    public float rotationSpeed = 5.0f;


    public enum State
    {
        Idle, Moving
    }

    public State movementState = State.Idle;

    [Flags]
    public enum MovementFlag : uint
    {
        None = 0,
        ControlRotation = 1,
        IsWater = 1 << 2,
        IsAutoReverseable = 1 << 3,
        CanApplyBoidsAvoidance = 1 << 4,
    }

    public MovementFlag flags;
    //public bool controlRotation = true;
    //public bool isWater = false;
    //public bool isAutoReverseable = false;
    //public bool canApplyBoidsAvoidance = false;

    public ulong crowdID = 0;
    
    int conditionToBlockBoids = 0;
    int conditionToActivateRaycast = 0;

    List<Vector3> positions = new List<Vector3>();

    public void IncrementBlockBoidsCounter()
    {
        conditionToBlockBoids++;
    }

    public void DecrementBlockBoidsCounter()
    {
        conditionToBlockBoids--;
    }


    public void IncrementActivateRaycastCounter()
    {
        conditionToActivateRaycast++;
    }

    public void DecrementActivateRaycastCounter()
    {
        conditionToActivateRaycast--;
    }

    public delegate void OnStartMovingDelegate();
    public event OnStartMovingDelegate OnStartMoving;
    public delegate void OnStopMovingDelegate();
    public event OnStopMovingDelegate OnStopMoving;
    public delegate void OnMovingDelegate();
    public event OnMovingDelegate OnMoving;

    // Check if a specific flag is set
    public bool HasState(MovementFlag state)
    {
        return (flags & state) == state;
    }

    // Add a flag
    public void SetState(MovementFlag state)
    {
        flags |= state;
    }

    // Remove a flag
    public void RemoveState(MovementFlag state)
    {
        flags &= ~state;
    }

    public void SetPositionData(List<Vector3> newPositions, bool notInvokeMove = false)
    {
        positions.Clear();
        if (newPositions.Count == 0)
        {
            return;
        }
        positions.AddRange(newPositions);
        if (!notInvokeMove) 
            OnStartMoving?.Invoke();
        
        if (movementState == State.Idle)
            movementState = State.Moving;
        
        if (!enabled) enabled = true;
    }

    public bool HasPathToFollow()
    {
        return positions.Count > 0;
    }

    public void Stop(bool invokeOnStop = true)
    {
        if (enabled)
        {
            if (positions.Count > 0)
            {
                if (invokeOnStop)
                    OnStopMoving?.Invoke(); 

                positions.Clear();
            }
            if (movementState == State.Moving)
                movementState = State.Idle;
            enabled = false;
        }
    }

    public List<Vector3> GetPathPositions() { return positions; }

    public static bool ShouldMoveForward(float currentAngle, float targetAngle)
    {
        // Find the shortest angle difference
        float angleDifference = Mathf.DeltaAngle(currentAngle, targetAngle);

        // Move forward if within �90�; otherwise, move backward
        return Mathf.Abs(angleDifference) < 90f;
    }

    void ControlRotation(Vector3 newPosition, Vector3 oldPosition, float deltaTime)
    {
        Vector3 diff = newPosition - oldPosition;
        float yAngle = transform.localEulerAngles.y;
        if (diff.sqrMagnitude > 0)
        {
            diff = diff.normalized; 
            yAngle = Mathf.Atan2(diff.x, diff.z) * Mathf.Rad2Deg;
            bool isAutoReversable = HasState(MovementFlag.IsAutoReverseable);
            if (isAutoReversable)
                yAngle = ShouldMoveForward(transform.localEulerAngles.y, yAngle) ? yAngle : yAngle + 180;
            float rotationDelta = rotationSpeed * deltaTime;
            transform.localEulerAngles = new Vector3(0, Mathf.LerpAngle(transform.localEulerAngles.y, yAngle, rotationDelta), 0);
        }
    }

    void ControlPosition(float movementDeltaValue)
    {
        List<Unit> nearbyObjs = UnitManager.Instance.spatialHashGrid.QueryInRadius(transform.position, radius + 0.1f);

        Vector3 position = positions[0];

        Vector2 oldPosition2D = new Vector2(transform.localPosition.x, transform.localPosition.z);
        Vector2 newPosition2D = new Vector2(position.x, position.z);
        newPosition2D = Vector2.MoveTowards(oldPosition2D, newPosition2D, movementDeltaValue);

        //Vector3 oldPosition = new Vector3(oldPosition2D.x, transform.localPosition.y, oldPosition2D.y);
        //Vector3 newPosition = new Vector3(newPosition2D.x, transform.localPosition.y, newPosition2D.y); // Vector3.MoveTowards(oldPosition, position, movementDeltaValue);

        int areaMask = GetAreaMask();
        bool isUsingRaycast = conditionToActivateRaycast > 0;
        float y = position.y;
        if (isUsingRaycast)
        {
            Vector3 newPosition = new Vector3(newPosition2D.x, y, newPosition2D.y);
            RaycastHit? properRayHit = SelectionController.FindProperHit(newPosition, areaMask);
            if (properRayHit.HasValue)
            {
                y = properRayHit.Value.point.y;
            }

        }

        //newPosition = Vector3.MoveTowards(oldPosition, newPosition, movementDeltaValue);

        //if (newPosition == oldPosition) return;
        //Vector3 finalPosition = newPosition;

        Vector2 diff = newPosition2D - oldPosition2D; // newPosition - oldPosition;
        bool canApplyBoidsAvoidance = HasState(MovementFlag.CanApplyBoidsAvoidance);
        if (canApplyBoidsAvoidance && conditionToBlockBoids <= 0)
        {
            //List<Unit> nearbyObjs = UnitManager.Instance.spatialHashGrid.QueryInRadius(transform.position, radius);
            Vector2 direction = Vector2.zero;
            bool canChangePosition = nearbyObjs.Count != 0;
            Vector2 transform2Dpos = new Vector2(transform.localPosition.x, transform.localPosition.z);
            if (canChangePosition)
            {
                direction = diff.normalized;
                foreach (Unit obj in nearbyObjs)
                {
                    if (obj.gameObject == gameObject) continue;
                    if (obj.TryGetComponent(out MovementComponent otherMovementComp))
                    {
                        //if (otherMovementComp.crowdID == crowdID) 
                        //    continue;
                    }
                    if (obj.CompareTag("Ship Unit"))
                        continue;
                    Vector2 obj2Dpos = new Vector2(obj.transform.localPosition.x, obj.transform.localPosition.z);
                    direction += (transform2Dpos - obj2Dpos).normalized;
                }
                Debug.DrawRay(transform.position, new Vector3(direction.x, transform.position.y, direction.y));
                direction = direction.normalized * diff.magnitude;// movementDeltaValue;
            }
            if (direction != Vector2.zero && canChangePosition)
            {
                Vector2 newBoidsPosition = transform2Dpos + direction;
                if (NavMesh.SamplePosition(newBoidsPosition, out NavMeshHit hit, isUsingRaycast ? 20f : 0.5f, GetAreaMask()))
                {
                    newPosition2D = new Vector2(hit.position.x, hit.position.z);
                    if (!isUsingRaycast)
                    {
                        y = hit.position.y;
                    }
                    //newPosition = hit.position;
                }
            }
        }

        transform.localPosition = new Vector3(newPosition2D.x, y, newPosition2D.y); // newPosition;

        System.Action action = () =>
        {
            foreach (Unit obj in nearbyObjs)
            {
                if (obj.TryGetComponent(out MovementComponent otherMovement))
                {
                    if (obj.gameObject == gameObject) continue;
                    if (otherMovement.positions.Count != 0) continue;
                    if (otherMovement.crowdID != crowdID) continue;
                    Vector3 diffToOtherObj = transform.position - obj.transform.position;
                    float cmpFloat = movementDeltaValue + 2 * radius;
                    float cmpSqr = cmpFloat * cmpFloat;
                    if (diffToOtherObj.sqrMagnitude < cmpFloat)
                    {
                        Debug.Log($"{diffToOtherObj.sqrMagnitude} and {cmpFloat}");
                        Stop();
                        break;
                    }
                }
            }
        };

        // TODO: Add proper cleanup of timer
        DeterministicUpdateManager.Instance.timer.AddTimer(0, action);
    }

    public float GetRotationSpeed()
    {
        return rotationSpeed;
    }

    public void DeterministicUpdate(float deltaTime, ulong tickID)
    {
        if (positions.Count > 0)
        {
            bool isMarkedForDeletion = false;

            Vector3 position = positions[0];
            float movementDeltaValue = movementSpeed * deltaTime;

            Vector3 oldPosition = transform.localPosition;
            Vector3 newPosition = Vector3.MoveTowards(transform.localPosition, position, movementDeltaValue);
            ControlPosition(movementDeltaValue);

            bool controlRotation = HasState(MovementFlag.ControlRotation);
            if (controlRotation)
            {
                ControlRotation(newPosition, oldPosition, deltaTime);
            }

            if (Vector3.Distance(transform.localPosition, position) < movementDeltaValue)
            {
                isMarkedForDeletion = true;
            }

            if (isMarkedForDeletion && positions.Count > 0)
            {
                positions.RemoveAt(0);
            }

            OnMoving?.Invoke();

            if (positions.Count == 0)
            {
                OnStopMoving?.Invoke();
                enabled = false;
            }
        }
    }

    private void OnEnable()
    {
        DeterministicUpdateManager.Instance.Register(this);
    }

    private void OnDisable()
    {
        DeterministicUpdateManager.Instance.Unregister(this);
    }

    // Helper function to get the nearest point on the NavMesh
    private static Vector3 GetClosestNavMeshPoint(Vector3 position)
    {
        NavMeshHit hit;
        if (NavMesh.SamplePosition(position, out hit, 2f, NavMesh.AllAreas)) // 2f = search radius
        {
            return hit.position;
        }
        return position; // If no valid NavMesh position is found, return the original position (as fallback)
    }

    public bool IsOnShip()
    {
        if (transform.parent && transform.parent.CompareTag("Ship Unit"))
        {
            return true;
        }
        return false;
    }

    public Vector3 GetLastPointInPathfinding()
    {
        if (positions.Count > 0)
        {
            if (IsOnShip())
            {
                return transform.TransformPoint(positions[positions.Count - 1]);
            }
            return positions[positions.Count - 1];
        }
        return transform.position;
    }

    // Needs to be executed after ship is undocked
    public void LocalizeCurrentPathfindingPositionForShip(Transform shipTransform)
    {
        // TODO make it native to any type of transform. Firstly convert current positions to world and work on that reference
        if (transform.parent == null)
        {
            Debug.Log("Localization shouldn't be possible");
            return;
        }

        List<Vector3> newPositions = new List<Vector3>();
        foreach (var position in positions)
        {
            if (NavMesh.SamplePosition(position, out NavMeshHit hit, 20, 1 << 3))
            {
                Vector3 newPosition = shipTransform.InverseTransformPoint(hit.position);
                newPositions.Add(newPosition);
            }
        }

        if (newPositions.Count > 0)
        {
            positions.Clear();
            positions.AddRange(newPositions);
        } else
        {
            // Send no success message?
        }
    }

    int GetAreaMask()
    {
        int areaMask = 1;
        bool isWater = HasState(MovementFlag.IsWater);
        if (isWater)
        {
            areaMask = 1 << 3;
        }
        if (IsOnShip())
        {
            areaMask = 1 << 4;
        }
        return areaMask;
    }

    public void StartPathfind(Vector3 newPosition, bool isMoving = false)
    {
        List<Vector3> pathPoints = new List<Vector3>();
        NavMeshPath path = new NavMeshPath();

        // Ensure the start & end points are on the NavMesh
        Vector3 start = GetClosestNavMeshPoint(transform.position);
        Vector3 end = GetClosestNavMeshPoint(newPosition);
        int areaMask = GetAreaMask();

        //if (NavMesh.CalculatePath(start, end, isWater ? 1 << 3 : 1, path))
        if (GridGeneration.CalculatePath(start, end, areaMask, ref path))
        {
            Debug.Log($"Used pathfinding using navmesh and path count is {path.corners.Length}");
            if (IsOnShip())
            {
                int counter = 0;
                foreach (Vector3 p in path.corners)
                {
                    if (counter == 0)
                    {
                        counter++;
                        continue;
                    }
                    Vector3 localPoint = transform.parent.InverseTransformPoint(p);
                    pathPoints.Add(localPoint);
                    counter++;
                }
            }
            else
            {
                int counter = 0;
                foreach (Vector3 p in path.corners)
                {
                    if (counter == 0)
                    {
                        counter++;
                        continue;
                    }
                    Vector3 position = p;
                    RaycastHit? hit = SelectionController.FindProperHit(position, areaMask);
                    if (hit != null)
                    {
                        position = hit.Value.point;
                    }
                    pathPoints.Add(position);
                    counter++;
                }
                
                //pathPoints.AddRange(path.corners);
            }

            SetPositionData(pathPoints, isMoving);
            return;
        }

        try
        {
            Vector3 startPosition = transform.position;
            Vector3 worldPos = newPosition;

            int mapSize = 120;

            Vector2Int startNode = new Vector2Int((int)startPosition.x, (int)startPosition.z);
            startNode = GridGeneration.Instance.ClampNode(startNode);

            Vector2Int endNode = new Vector2Int((int)worldPos.x, (int)worldPos.z);
            endNode = GridGeneration.Instance.ClampNode(endNode);

            Vector2Int midNode = (startNode + endNode) / 2;
            Vector2Int offsetNode = midNode - Vector2Int.one * (160 / 2);
            if (endNode.x > offsetNode.x + mapSize - 1) endNode.x = offsetNode.x + mapSize - 1;
            if (endNode.y > offsetNode.y + mapSize - 1) endNode.y = offsetNode.y + mapSize - 1;
            offsetNode = GridGeneration.Instance.ClampNode(offsetNode);
            Debug.Log($"{offsetNode}, {endNode}");

            bool isWater = HasState(MovementFlag.IsWater);
            var grids = GridGeneration.Instance.GenerateGridForPathfinding(offsetNode, mapSize, isWater);

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

            if (points != null && points.Count > 0)
            {
                List<Vector3> positions = new List<Vector3>();
                for (int i = 0; i < points.Count; i++)
                {
                    Vector3 pos = new Vector3(points[i].x + 0.5f, 0, points[i].y + 0.5f);
                    positions.Add(pos);
                }
                positions.Add(new Vector3(worldPos.x, 0, worldPos.z));
                SetPositionData(positions);
            }
        }
        catch (System.Exception e)
        {
            // Do something???
        }
    }

    void OnDestroy()
    {
        if (OnStartMoving != null)
        {
            foreach (var d in OnStartMoving.GetInvocationList())
            {
                OnStartMoving -= (OnStartMovingDelegate)d;
            }
        }

        if (OnStopMoving != null)
        {
            foreach (var d in OnStopMoving.GetInvocationList())
            {
                OnStopMoving -= (OnStopMovingDelegate)d;
            }
        }

        if (OnMoving != null)
        {
            foreach (var d in OnMoving.GetInvocationList())
            {
                OnMoving -= (OnMovingDelegate)d;
            }
        }
    }

    public void Load(MapLoader.SaveLoadData data)
    {
        MovementComponentData movementComponentData = data as MovementComponentData;
        radius = movementComponentData.radius;
        movementSpeed = movementComponentData.movementSpeed;
        rotationSpeed = movementComponentData.rotationSpeed;
        flags = (MovementFlag)movementComponentData.flags;
        crowdID = movementComponentData.crowdID;
        conditionToBlockBoids = movementComponentData.conditionToBlockBoids;
        conditionToActivateRaycast = movementComponentData.conditionToActivateRaycast;
        positions = movementComponentData.positions.Select(v => (Vector3)v).ToList();

        enabled = movementComponentData.enabled;
    }

    public MapLoader.SaveLoadData Save()
    {
        MovementComponentData movementComponentData = new MovementComponentData()
        {
            enabled = enabled,

            radius = radius,
            movementSpeed = movementSpeed,
            rotationSpeed = rotationSpeed,

            flags = (uint)flags,
            //controlRotation = controlRotation,
            //isWater = isWater,
            //isAutoReverseable = isAutoReverseable,
            //canApplyBoidsAvoidance = canApplyBoidsAvoidance,

            crowdID = crowdID,

            conditionToBlockBoids = conditionToBlockBoids,
            conditionToActivateRaycast = conditionToActivateRaycast,

            positions = this.positions.Select(v => (SerializableVector3)v).ToList()
        };

        return movementComponentData;
    }

    public void PostLoad(MapLoader.SaveLoadData data)
    {
        // Force reload
        Load(data);
        //MovementComponentData movementComponentData = data as MovementComponentData;
        //enabled = movementComponentData.enabled;
    }
}
