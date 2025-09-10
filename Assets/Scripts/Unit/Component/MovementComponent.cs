using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UIElements;
using static CombatComponent;
using static CommonStructures;

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
    public float movementSpeed = 0.96f;
    public float rotationSpeed = 5.0f;

    private float eulerAnglesY = 0.0f;
    [SerializeField] public Rigidbody rb;
    [SerializeField] public Collider solidCollider;
    [SerializeField] private Unit unit;

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
        LockedRotation = 1 << 5,
        UseSteering = 1 << 6
    }

    public int directionCount = 8;
    public MovementFlag flags;

    public ulong crowdID = 0;
    
    int conditionToBlockBoids = 0;
    int conditionToActivateRaycast = 0;
    List<ulong> idsToIgnore = new List<ulong>();

    List<Vector3> positions = new List<Vector3>();

    public enum PathfindingStatus { 
        NoPathfindingCalled,
        RequestedForPathfinding,
        CalledForCancellingOfPathfinding
    }

    private PathfindingStatus pathfindingStatus = PathfindingStatus.NoPathfindingCalled;

    public PathfindingStatus GetPathfindingStatus() => pathfindingStatus;
    public void SetPathfindingStatus(PathfindingStatus newStatus) => pathfindingStatus = newStatus;

    public System.Action<State> OnMovementStateChangeCallback = (state) => { };
    public System.Action OnMoving = () => { };

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

    private void ChangeState(State state)
    {
        if (movementState == state) return;
        OnMovementStateChangeCallback?.Invoke(state);
        movementState = state;
    }

    public void SetPositionData(List<Vector3> newPositions, bool notInvokeMove = false)
    {
        positions.Clear();
        if (newPositions.Count == 0)
        {
            return;
        }
        positions.AddRange(newPositions);
        //if (!notInvokeMove) 
        //    OnStartMoving?.Invoke();

        //if (movementState == State.Idle)
        //    movementState = State.Moving;
        ChangeState(State.Moving);


        if (!enabled) enabled = true;
    }

    public void Stop(bool invokeOnStop = true)
    {
        if (pathfindingStatus == PathfindingStatus.RequestedForPathfinding)
        {
            pathfindingStatus = PathfindingStatus.CalledForCancellingOfPathfinding;
        }

        if (positions.Count > 0)
        {
            //if (invokeOnStop)
            //    OnStopMoving?.Invoke(); 

            positions.Clear();
        }

        if (rb && !rb.isKinematic)
        {
            rb.linearVelocity = Vector3.zero;
            //if (IsOnShip())
            //{
            //    rb.isKinematic = true;
            //}
            //else
            //{
            //    rb.isKinematic = false;
            //}
        }
        if (enabled)
        {
            ChangeState(State.Idle);
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
        void SetAngle(float angle)
        {
            if (rb && rb.isKinematic)
            {
                rb.MoveRotation(Quaternion.Euler(new Vector3(0, angle, 0)));
                return;
            }
            transform.localEulerAngles = new Vector3(0, angle, 0);
        }

        Vector3 diff = newPosition - oldPosition;
        float yAngle = transform.localEulerAngles.y;
        if (HasState(MovementFlag.LockedRotation))
        {
            yAngle = eulerAnglesY;
        }
        if (diff.sqrMagnitude > 0)
        {
            diff = diff.normalized; 
            yAngle = Mathf.Atan2(diff.x, diff.z) * Mathf.Rad2Deg;
            bool isAutoReversable = HasState(MovementFlag.IsAutoReverseable);
            if (isAutoReversable)
                yAngle = ShouldMoveForward(transform.localEulerAngles.y, yAngle) ? yAngle : yAngle + 180;
            float rotationDelta = rotationSpeed * deltaTime;
            float finalEulerAnglesY = eulerAnglesY;
            if (HasState(MovementFlag.LockedRotation))
            {
                eulerAnglesY = Mathf.MoveTowardsAngle(eulerAnglesY, yAngle, rotationDelta);
                finalEulerAnglesY = Utilities.SnapToDirections(eulerAnglesY, directionCount);
                //transform.localEulerAngles = new Vector3(0, finalEulerAnglesY, 0);
                SetAngle(finalEulerAnglesY);
            }
            else
            {
                eulerAnglesY = Mathf.MoveTowardsAngle(transform.localEulerAngles.y, yAngle, rotationDelta);
                finalEulerAnglesY = eulerAnglesY;
                //transform.localEulerAngles = new Vector3(0, finalEulerAnglesY, 0);
                SetAngle(finalEulerAnglesY);
            }
        }
    }

    public static RaycastHit? FindProperHit(Vector3 targetPosition, int navAreaMask)
    {
        int layer = ~(1 << 2 | 1 << 3 | 1 << 6 | 1 << 30);
        Ray ray = new Ray(targetPosition + Vector3.up * 100, Vector3.down * 200);// Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, float.MaxValue, layer))
        {
            if (NavMesh.SamplePosition(hit.point, out NavMeshHit navHit, 20f, navAreaMask))
            {
                ray = new Ray(navHit.position + Vector3.up * 100, Vector3.down * 200);
                RaycastHit finalHit = hit;
                RaycastHit[] hits = Physics.RaycastAll(ray, float.MaxValue, layer);
                foreach (var h in hits)
                {
                    if (h.collider.TryGetComponent(out MovableUnit movableUnit))
                    {
                        if (!movableUnit.IsShip())
                        {
                            continue;
                        }
                        return h;
                    }
                    finalHit = h;
                }
                return finalHit;
                //if (Physics.Raycast(ray, out hit, float.MaxValue, layer))
                //{
                //    return hit;
                //}
            }
        }
        return null;
    }

    // Refactored ControlPosition with smaller responsibilities, early exits, and clearer flow
    void ControlPosition(float movementDelta, float deltaTime)
    {
        // 1. Determine target 2D position
        var oldPos2D = Utilities.ToVector2XZ(transform.localPosition);
        var desiredPos2D = Utilities.ToVector2XZ(positions[0]);
        var nextPos2D = Vector2.MoveTowards(oldPos2D, desiredPos2D, movementDelta);
        var nearbyObjs = UnitManager.Instance.spatialHashGrid.QueryInRadius(transform.position, radius + 0.14f);

        bool useSteering = HasState(MovementFlag.UseSteering);
        if (useSteering)
        {
            Vector2 toTarget = (desiredPos2D - oldPos2D).normalized;
            Vector2 forward2D = Utilities.ToVector2XZ(transform.forward).normalized;
            float angleDiff = Vector2.Angle(forward2D, toTarget);
            if (angleDiff > 90f) return; // Too sharp to move forward

            float distance = (nextPos2D - oldPos2D).magnitude;
            nextPos2D = oldPos2D + forward2D * distance;
        }

        // 2. Adjust height via raycast if needed
        //float targetY = positions[0].y;
        //if (UseRaycastHeight())
        //{
        //    if (TryGetGroundHeight(nextPos2D, positions[0].y, out float groundY))
        //    {
        //        targetY = groundY;
        //    }
        //}

        // 3. Apply boids avoidance if enabled
        if (ShouldApplyBoids())
        {
            //nextPos2D = ApplyBoidsAvoidance(oldPos2D, nextPos2D - oldPos2D, nearbyObjs);
            if (NewObstacleAvoidance(nearbyObjs, deltaTime, out Vector2 obstacleDirection))
            {
                Vector2 obstacleDir2D = obstacleDirection;//Utilities.ToVector2XZ(obstacleDirection);
                float distance = (nextPos2D - oldPos2D).magnitude;
                nextPos2D = oldPos2D + obstacleDir2D * distance;
            }
        }

        float targetY = transform.position.y;
        if (TryGetGroundHeight(nextPos2D, targetY, out float groundY))
        {
            targetY = groundY;
        }

        // 4. Finalize position using NavMesh sampling
        var finalPos = SampleNavMesh(nextPos2D, positions[0].y);
        //Debug.DrawLine(transform.position, new Vector3(transform.position.x, targetY, transform.position.z), Color.yellow);
        transform.localPosition = finalPos;
        Vector3 finalWorldPos = new Vector3(transform.position.x, targetY, transform.position.z);
        transform.position = finalWorldPos;
    }

    public static bool CapsuleCastMatchesCollider(
        CapsuleCollider capsule,
        Vector3 castDirection,
        float maxDistance,
        out RaycastHit hitInfo,
        int layerMask,
        QueryTriggerInteraction triggerInteraction,
        Collider targetCollider = null
    )
    {
        Vector3 worldCenter = capsule.transform.TransformPoint(capsule.center);
        float radius = capsule.radius * Mathf.Max(capsule.transform.lossyScale.x, capsule.transform.lossyScale.z); // Uniform scale safety

        float height = Mathf.Max(capsule.height, radius * 2f);
        float halfHeight = (height * 0.5f) - radius;

        Vector3 up = capsule.transform.up; // since aligned upwards (Y-axis)

        Vector3 p1 = worldCenter + up * halfHeight;
        Vector3 p2 = worldCenter - up * halfHeight;

        bool hit = Physics.CapsuleCast(p1, p2, radius, castDirection.normalized, out hitInfo, maxDistance, layerMask, triggerInteraction);

        return hit && (targetCollider == null || hitInfo.collider == targetCollider);
    }

    Vector3 _obstacleDir = Vector3.zero;
    float cooldownTimer = 0;

    bool NewObstacleAvoidance(List<Unit> nearbyObjs, float deltaTime, out Vector2 obstacleDirection)
    {
        if (solidCollider is not CapsuleCollider capsule)
        {
            obstacleDirection = Vector2.zero;
            return false;
        }

        var neighbors = nearbyObjs
                           .Where(u => u.gameObject != gameObject && !ShouldIgnore(u))
                           .ToList();

        for (int i = 0; i < neighbors.Count; i++)
        {
            Unit neighborUnit = neighbors[i];
            Vector3 direction = (neighborUnit.transform.position - transform.position).normalized;
            float angle = Vector3.Angle(transform.forward, direction);
            if (angle > 45)
            {
                continue;
            }
            float distance = direction.magnitude;
            cooldownTimer -= deltaTime;
            {
                if (CapsuleCastMatchesCollider(capsule, direction, distance, out RaycastHit hitInfo, -1, QueryTriggerInteraction.UseGlobal))
                {
                    if (cooldownTimer <= 0)
                    {
                        //Debug.Log(hitInfo.collider.name);

                        // Base avoidance is normal
                        Vector3 normal = hitInfo.normal;

                        // Get right vector from current forward (or desired move direction)
                        Vector3 right = Vector3.Cross(Vector3.up, direction.normalized);

                        // Bias factor (positive = right, negative = left)
                        float biasAmount = 0.5f; // adjust between -1 and 1
                        Vector3 biasedAvoidance = (normal + right * biasAmount).normalized;

                        _obstacleDir = biasedAvoidance;
                        cooldownTimer = 0.5f;
                    }

                    var targetRotation = Quaternion.LookRotation(transform.forward, _obstacleDir);
                    float rotationDelta = rotationSpeed * deltaTime;
                    var rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationDelta);

                    obstacleDirection = rotation * Vector2.up;
                    return true;
                }
            }
            
            break;
        }
        obstacleDirection = Vector2.zero;
        return false;
    }

    bool UseRaycastHeight() => conditionToActivateRaycast > 0;

    bool ShouldApplyBoids() => HasState(MovementFlag.CanApplyBoidsAvoidance) && conditionToBlockBoids <= 0;

    bool TryGetGroundHeight(Vector2 pos2D, float givenY, out float y)
    {
        var origin = new Vector3(pos2D.x, givenY, pos2D.y);
        if (SelectionController.FindProperHit(origin, GetAreaMask()) is RaycastHit hit)
        {
            y = hit.point.y;
            return true;
        }
        y = origin.y;
        return false;
    }

    Vector2 ApplyBoidsAvoidance(Vector2 currentPos, Vector2 delta, List<Unit> nearbyObjs)
    {
        var neighbors = nearbyObjs
                           .Where(u => u.gameObject != gameObject && !ShouldIgnore(u))
                           .ToList();
        if (!neighbors.Any()) return currentPos + delta;

        // Compute separation direction
        var separation = delta.normalized;
        foreach (var u in neighbors)
        {
            var otherPos = Utilities.ToVector2XZ(u.transform.localPosition);
            separation += (currentPos - otherPos).normalized;
        }
        separation = separation.normalized * delta.magnitude;

        // Sample on NavMesh
        var candidate = currentPos + separation;
        if (NavMesh.SamplePosition(Utilities.ToVector3(candidate, transform.localPosition.y), out NavMeshHit hit, UseRaycastHeight() ? 20f : 0.2f, GetAreaMask()))
        {
            return new Vector2(hit.position.x, hit.position.z);
        }
        return currentPos + separation;
    }

    bool ShouldIgnore(Unit u)
    {
        if (u.CompareTag("Ship Unit")) return true;
        if (u.GetType() == typeof(MovableUnit))
        {
            MovableUnit movableUnit = (MovableUnit)u;
            if (movableUnit != null)
            {
                if (movableUnit.movementComponent.crowdID == crowdID)
                {
                    return true;
                }
            }
        }
        return idsToIgnore.Contains(u.id);
    }

    public bool HasPathPositions() { return positions.Count != 0; }

    public bool IsPathfindingInQueue()
    {
        return pathfindingStatus == PathfindingStatus.RequestedForPathfinding;
    }

    public bool AnyPathOperationInProgress()
    {
        return HasPathPositions() || IsPathfindingInQueue();
    }

    Vector3 SampleNavMesh(Vector2 pos2D, float y)
    {
        // TODO make proper world postion!
        var worldPos = new Vector3(pos2D.x, y, pos2D.y);
        if (NavMesh.SamplePosition(worldPos, out var hit, 0.5f, GetAreaMask()) && false)
            return hit.position;
        return worldPos;
    }

    public void SetTargetToIgnore(ulong? id = null)
    {
        idsToIgnore.Clear();
        if (id != null)
        {
            idsToIgnore.Add(id.Value);
        }
    }

    public float GetRotationSpeed()
    {
        return rotationSpeed;
    }

    private void OnCollisionEnter(Collision collision)
    {
        Debug.Log($"On Collision Enter hit: {collision.collider.name} and called from {solidCollider.name}");
    }

    private void OnCollisionExit(Collision collision)
    {
        Debug.Log($"On Collision Exit hit: {collision.collider.name} and called from {solidCollider.name}");
    }

    public void OnCustomTriggerEnter(Collider other)
    {
        if (other.TryGetComponent(out MovementComponent otherMovementComponent))
        {
            if (otherMovementComponent.crowdID == crowdID)
            {
                //Physics.IgnoreCollision(solidCollider, otherMovementComponent.solidCollider, true);
                NativeLogger.Log($"Started ignoring collider: {other.name}");
            }
        }
    }

    public void OnCustomTriggerExit(Collider other)
    {
        if (other.TryGetComponent(out MovementComponent otherMovementComponent))
        {
            if (otherMovementComponent.crowdID == crowdID)
            {
                //Physics.IgnoreCollision(solidCollider, otherMovementComponent.solidCollider, false);
                NativeLogger.Log($"Remove ignoring collider: {other.name}");
            }
            else
            {
                if (Physics.GetIgnoreCollision(solidCollider, otherMovementComponent.solidCollider))
                {
                    //Physics.IgnoreCollision(solidCollider, otherMovementComponent.solidCollider, false);
                    NativeLogger.Log($"Remove ignoring collider: {other.name}");
                }
            }
        }
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

            bool controlRotation = HasState(MovementFlag.ControlRotation);
            if (controlRotation)
            {
                ControlRotation(newPosition, oldPosition, deltaTime);
            }

            ControlPosition(movementDeltaValue, deltaTime);

            Vector2 transformLocalPos2D = Utilities.ToVector2XZ(transform.localPosition);
            Vector2 position2D = Utilities.ToVector2XZ(position);
            if ((transformLocalPos2D - position2D).sqrMagnitude < movementDeltaValue * movementDeltaValue)
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
                //OnStopMoving?.Invoke();
                enabled = false;
            }
        }
    }

    private void OnEnable()
    {
        eulerAnglesY = transform.localEulerAngles.y;
        DeterministicUpdateManager.Instance.Register(this);

        if (rb)
        {
            rb.linearDamping = 5;
            //rb.isKinematic = false;
        }
    }

    private void OnDisable()
    {
        DeterministicUpdateManager.Instance.Unregister(this);
        ChangeState(State.Idle);
        //movementState = State.Idle;
        if (rb)
        {
            if (!rb.isKinematic)
                rb.linearVelocity = Vector3.zero;

            rb.linearDamping = 50;
            //rb.isKinematic = true;
        }
        //OnStopMoving?.Invoke();
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
        if (transform.parent && transform.parent.CompareTag("Military Unit"))
        {
            return true;
        }
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

    public int GetAreaMask()
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

    public void AddPointsWithOffset(List<Vector3> points, Vector3 offset)
    {
        if (points.Count == 0) return;
        this.positions.Clear();
        for (int i = 0; i < points.Count - 1; i++)
        {
            Vector3 nextPoint = points[i + 1];
            Vector3 currentPoint = points[i];
            Vector3 direction = nextPoint - currentPoint;
            direction = Vector3.Normalize(direction);

            // Build local-to-world matrix at this position
            Quaternion rotation = Quaternion.LookRotation(direction, Vector3.up);
            Matrix4x4 matrix = Matrix4x4.TRS(currentPoint, Quaternion.Euler(0, rotation.eulerAngles.y, 0), Vector3.one);

            // Transform offset from local to world space
            Vector3 offsetWorld = matrix.MultiplyPoint(offset);

            this.positions.Add(offsetWorld);
        }
    }

    public void StartPathfind(Vector3 newPosition, bool isMoving = false, Vector3 offset = default, Vector3? startPosition = null)
    {
        System.Action pathQueueAction = () =>
        {
            if (pathfindingStatus == PathfindingStatus.CalledForCancellingOfPathfinding)
            {
                pathfindingStatus = PathfindingStatus.NoPathfindingCalled;
                return;
            }

            if (unit && unit.GetType() == typeof(MovableUnit) && !StatComponent.IsUnitAliveOrValid((MovableUnit)unit))
            {
                return;
            }
            List<Vector3> pathPoints = new List<Vector3>();
            NavMeshPath path = new NavMeshPath();

            // Ensure the start & end points are on the NavMesh
            Vector3 start = startPosition == null ? GetClosestNavMeshPoint(transform.position) : GetClosestNavMeshPoint(startPosition.Value);
            Vector3 end = GetClosestNavMeshPoint(newPosition);
            int areaMask = GetAreaMask();

            //if (NavMesh.CalculatePath(start, end, isWater ? 1 << 3 : 1, path))
            if (GridGeneration.CalculatePath(start, end, areaMask, ref path))
            {
                //NativeLogger.Log($"Used pathfinding using navmesh and path count is {path.corners.Length}");
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
                    Matrix4x4 matrix;
                    List<Vector3> preProcessedPaths = new List<Vector3>();
                    if (startPosition.HasValue && startPosition != transform.position && path.corners.Length >= 2)
                    {
                        NavMeshPath subPath = new NavMeshPath();
                        Vector3 position = startPosition.Value; 
                        Vector3 direction = (path.corners[1] - startPosition.Value).normalized;
                        matrix = Matrix4x4.TRS(position, Quaternion.Euler(0, Quaternion.LookRotation(direction).eulerAngles.y, 0), Vector3.one);
                        position = matrix.MultiplyPoint(offset);
                        if (GridGeneration.CalculatePath(transform.position, position, areaMask, ref subPath))
                        {
                            for (int i = 1; i < subPath.corners.Length; i++)
                            {
                                preProcessedPaths.Add(subPath.corners[i]);
                                //DebugExtension.DebugWireSphere(subPath.corners[i], Color.red, 0.1f, 3f);
                            }
                        }
                    }
                    for (int i = 1; i < path.corners.Length; i++)
                    {
                        Vector3 prevPosition = path.corners[i - 1];
                        Vector3 position = path.corners[i];
                        Vector3 originalPosition = path.corners[i];
                        Vector3 direction = (position - prevPosition).normalized;
                        if (startPosition.HasValue)
                        {
                            matrix = Matrix4x4.TRS(position, Quaternion.Euler(0, Quaternion.LookRotation(direction).eulerAngles.y, 0), Vector3.one);
                            position = matrix.MultiplyPoint(offset);
                        }

                        preProcessedPaths.Add(position);
                        //DebugExtension.DebugWireSphere(position, Color.blue, 0.1f, 3f);
                    }

                    if (preProcessedPaths.Count > 0)
                    {
                        pathPoints.Add(preProcessedPaths[0]);
                        for (int i = 1; i < preProcessedPaths.Count; i++)
                        {
                            NavMeshPath subPath = new NavMeshPath();
                            Vector3 position = preProcessedPaths[i];
                            Vector3 prevPosition = preProcessedPaths[i - 1];
                            //if (false)
                            //{
                                if (NavMesh.Raycast(prevPosition, position, out NavMeshHit navHit, areaMask))
                                {
                                    if (GridGeneration.CalculatePath(prevPosition, position, areaMask, ref subPath))
                                    {
                                        for (int j = 0; j < subPath.corners.Length; j++)
                                        {
                                            pathPoints.Add(subPath.corners[j]);
                                        }
                                    }
                                }
                            //}
                            else
                            {
                                pathPoints.Add(position);
                            }
                        }

                        for (int i = 0; i < pathPoints.Count; i++)
                        {
                            Vector3 position = pathPoints[i];
                            RaycastHit? hit = SelectionController.FindProperHit(position, areaMask);
                            position = hit != null ? hit.Value.point : position;
                            pathPoints[i] = position;
                        }
                    }

                    //int counter = 0;
                    //foreach (Vector3 p in path.corners)
                    //{
                    //    if (counter == 0)
                    //    {
                    //        counter++;
                    //        continue;
                    //    }
                    //    Vector3 position = p;
                    //    RaycastHit? hit = SelectionController.FindProperHit(position, areaMask);
                    //    if (hit != null)
                    //    {
                    //        position = hit.Value.point;
                    //    }
                    //    pathPoints.Add(position);
                    //    counter++;
                    //}

                    //pathPoints.AddRange(path.corners);
                }

                SetPositionData(pathPoints, isMoving);
                pathfindingStatus = PathfindingStatus.NoPathfindingCalled;
                return;
            }
        };
        pathfindingStatus = PathfindingStatus.RequestedForPathfinding;
        PathfindingManager.Instance.RequestPathfinding(this, pathQueueAction, 0);
    }

    public void Load(MapLoader.SaveLoadData data)
    {
        MovementComponentData movementComponentData = data as MovementComponentData;
        radius = movementComponentData.radius;
        movementSpeed = movementComponentData.movementSpeed;
        rotationSpeed = movementComponentData.rotationSpeed;
        flags = (MovementFlag)movementComponentData.flags;
        //crowdID = movementComponentData.crowdID;
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

            //crowdID = crowdID,

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

    internal void FixGround()
    {
        //float targetY = transform.position.y;
        //Vector2 pos2D = Utilities.ToVector2XZ(transform.position);
        //Vector3 samplePos = SampleNavMesh(pos2D, targetY);
        //if (TryGetGroundHeight(pos2D, samplePos.y, out targetY))
        //{
        //    transform.position = new Vector3(pos2D.x, targetY, pos2D.y);
        //}
    }
}
