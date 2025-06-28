using UnityEngine;
using System.Collections.Generic;
using UnityEngine.AI;
using UnityEngine.Rendering;
using TMPro;
using static BasicAttackAIModule;
using static UnityEngine.Rendering.DebugUI.Table;
using Unity.VisualScripting;
using static Utilities;
using System.Linq;
using UnityEngine.EventSystems;

public class MoveUnitCommand : InputCommand {
    public const string commandName = "Move Unit Command";
    public ulong unitID;
    public Vector3 position;

    public void Execute()
    {
        Unit unit = UnitManager.Instance.GetUnit(unitID);
        if (unit)
        {
            MovableUnit movableUnit = (MovableUnit)unit;
            if (StatComponent.IsUnitAliveOrValid(movableUnit))
            {
                ulong newCrowdID = ++UnitManager.crowdIDCounter;
                movableUnit.ResetUnit(true);
                //movableUnit.movementComponent.StartPathfind(position);
                movableUnit.SetAIModule(UnitAIModule.AIModule.BasicMovementAIModule, position, newCrowdID);
                //movableUnit.movementComponent.crowdID = newCrowdID;
            }
        }
    }
}

public class MoveUnitsCommand : InputCommand
{
    public const string commandName = "Move Units Command";
    public List<ulong> unitIDs;
    public Vector3 position;
    public bool IsAttackMove = false;

    public void ArrangeUnits_New(List<Unit> units, Vector3 position, ulong crowdID)
    {
        if (units == null || units.Count == 0) return;

        ulong newCrowdID = ++UnitManager.crowdIDCounter;

        // 1. Build formation positions
        const float unitWidth = 0.14f;
        const float interUnitSpacing = 0.14f;
        float spacing = unitWidth + interUnitSpacing;
        int totalUnits = units.Count;
        int columns = Mathf.CeilToInt(Mathf.Sqrt(totalUnits));
        List<Vector3> formationOffsets = new List<Vector3>();

        Vector3 center = Vector3.zero;
        for (int i = 0; i < totalUnits; i++)
        {
            float row = Mathf.Floor(i / (float)columns);
            float col = i % columns;

            float unitsInRow = Mathf.Min(columns, totalUnits - row * columns);
            float totalRowWidth = (unitsInRow - 1) * spacing;
            float offsetX = -totalRowWidth / 2f;

            float x = col * spacing + offsetX;
            float z = row * spacing;

            formationOffsets.Add(new Vector3(x, 0, -z)); // negative z for forward
            center += units[i].transform.position;
        }

        center /= totalUnits;

        // 2. Build cost matrix
        float[,] costMatrix = new float[totalUnits, totalUnits];
        for (int i = 0; i < totalUnits; i++)
        {
            Vector3 unitPos = units[i].transform.position;
            for (int j = 0; j < totalUnits; j++)
            {
                Vector3 targetPos = position + formationOffsets[j];
                costMatrix[i, j] = Vector3.SqrMagnitude(unitPos - targetPos); // Use squared for performance
            }
        }

        // 3. Solve assignment
        List<int> assignment = HungarianAlgorithm.Solve(costMatrix).ToList();
        assignment.Reverse();

        // 4. Issue move commands
        Vector3 startPathfindingPostion = units[0].transform.position;
        for (int i = 0; i < totalUnits; i++)
        {
            Unit unit = units[i];
            MovableUnit movableUnit = (MovableUnit)unit;
            if (StatComponent.IsUnitAliveOrValid(movableUnit))
            {
                int assignedIndex = assignment[i];
                Vector3 offset = formationOffsets[assignedIndex];
                MoveToCommand(unit, position, newCrowdID, offset, center);
            }
        }
    }

    public static List<List<Unit>> ClusterUnits(List<Unit> units, float clusterRadius)
    {
        List<List<Unit>> clusters = new();
        HashSet<Unit> visited = new();

        foreach (Unit unit in units)
        {
            if (visited.Contains(unit)) continue;

            List<Unit> cluster = new();
            Queue<Unit> queue = new();
            queue.Enqueue(unit);
            visited.Add(unit);

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                cluster.Add(current);

                foreach (var other in units)
                {
                    if (!visited.Contains(other) &&
                        Vector3.Distance(current.transform.position, other.transform.position) < clusterRadius)
                    {
                        queue.Enqueue(other);
                        visited.Add(other);
                    }
                }
            }

            clusters.Add(cluster);
        }

        return clusters;
    }

    void MoveToCommand(Unit unit, Vector3 position, ulong newCrowdID, Vector3 offset = default, Vector3? startPosition = null)
    {
        MovableUnit movableUnit = (MovableUnit)unit;
        if (StatComponent.IsUnitAliveOrValid(movableUnit))
        {
            movableUnit.ResetUnit(true);
            Vector3 diff = startPosition.Value - position;
            float sqrMagnitude = diff.sqrMagnitude;
            const float distanceForFastRearrange = 5;
            const float distanceForFastRearrangeSqr = distanceForFastRearrange * distanceForFastRearrange;
            if (sqrMagnitude < distanceForFastRearrangeSqr)
            {
                startPosition = null;
            }

            if (startPosition == null)
            {
                startPosition = unit.transform.position;
            }
            movableUnit.SetAIModule(IsAttackMove ? UnitAIModule.AIModule.AttackMoveAIModule : UnitAIModule.AIModule.BasicMovementAIModule, 
                position, newCrowdID, offset, startPosition);
            //movableUnit.SetAIModule(UnitAIModule.AIModule.BasicMovementAIModule, position, newCrowdID);
        }
    }

    public void MoveUnitsToTargetInFormation(Vector3 destination, List<Unit> units)
    {
        List<List<Unit>> clusters = ClusterUnits(units, 5f); // 5 units apart same formation

        foreach (var cluster in clusters)
        {
            ulong newCrowdID = ++UnitManager.crowdIDCounter;
            if (cluster.Count == 1)
            {
                // Move alone
                MoveToCommand(cluster[0], destination, newCrowdID);
            }
            else
            {
                // Formation move
                ArrangeUnits_New(cluster, destination, newCrowdID);
                //ArrangeClusterFormation(destination, cluster);
            }
        }
    }
    public void Execute()
    {
        List<Unit> units = new List<Unit>();

        int unitCount = unitIDs.Count;
        for (int i = 0; i < unitCount; i++)
        {
            Unit unit = UnitManager.Instance.GetUnit(unitIDs[i]);
            if (unit && unit.GetType() == typeof(MovableUnit))
            {
                units.Add(unit);
            }
        }
        MoveUnitsToTargetInFormation(position, units);

        return;
    }
}

public class MoveShipUnitCommand : InputCommand
{
    public const string commandName = "Move Ship Unit Command";
    public ulong unitID;
    public Vector3 position;

    public void Execute()
    {
        Unit unit = UnitManager.Instance.GetUnit(unitID);
        if (unit)
        {
            ShipUnit shipUnit = (ShipUnit)unit;
            shipUnit.StartPathfind(position);
        }
    }
}

public class PauseGameCommand : InputCommand
{
    public const string commandName = "Pause Game Command";

    public void Execute()
    {
        if (!DeterministicUpdateManager.Instance.IsPaused())
        {
            DeterministicUpdateManager.Instance.Pause();
        }
    }
}

public class ResumeGameCommand : InputCommand
{
    public const string commandName = "Resume Game Command";

    public void Execute()
    {
        if (DeterministicUpdateManager.Instance.IsPaused())
        {
            DeterministicUpdateManager.Instance.Resume();
        }
    }
}

public class DeleteUnitsCommand : InputCommand
{
    public const string commandName = "Delete Units Command";
    public List<ulong> unitIDs;

    public void Execute()
    {
        foreach (ulong unitID in unitIDs)
        {
            Unit unit = UnitManager.Instance.GetUnit(unitID);
            MovableUnit movableUnit = (MovableUnit)unit;
            if (movableUnit && StatComponent.IsUnitAliveOrValid(movableUnit))
            {
                StatComponent.KillUnit(movableUnit);
            }
        }
    }
}

public class MoveShipToDockCommand : InputCommand
{
    public const string commandName = "Move Ship to Dock Command";
    public ulong unitID;
    public Vector3 position;
    public Vector3 targetToDock;

    public void Execute()
    {
        Unit unit = UnitManager.Instance.GetUnit(unitID);
        if (unit)
        {
            ShipUnit shipUnit = (ShipUnit)unit;
            DebugExtension.DebugWireSphere(position, Color.red, 0.1f, 5.0f);
            DebugExtension.DebugWireSphere(targetToDock, Color.cyan, 0.1f, 5.0f);
            shipUnit.DockAt(position, targetToDock);
            // shipUnit.StartPathfind(position);
        }
    }
}

public class AttackUnitCommand : InputCommand
{
    public const string commandName = "Attack Unit Command";
    public ulong unitID;
    public ulong targetID;

    public void Execute()
    {
        if (unitID == targetID) return;
        Unit unit = UnitManager.Instance.GetUnit(unitID);
        Unit targetUnit = UnitManager.Instance.GetUnit(targetID);
        if (unit && targetUnit)
        {
            MovableUnit movableUnit = (MovableUnit)unit;
            if (StatComponent.IsUnitAliveOrValid(movableUnit))
            {
                MovableUnit movableTargetUnit = (MovableUnit)targetUnit;
                if (movableUnit.aiModule)
                {
                    movableUnit.ResetUnit(true);
                    ulong newCrowdID = ++UnitManager.crowdIDCounter;
                    movableUnit.SetAIModule(UnitAIModule.AIModule.BasicAttackAIModule, movableTargetUnit, true, true);
                }
            }
        }
    }
}

public class SelectionController : MonoBehaviour
{
    [SerializeField]
    CameraMovement cameraMovement;
    public LayerMask selectableLayer; // Units should be on this layer
    public Color selectionBoxColor = new Color(0, 1, 0, 0.2f);
    public Color selectionBorderColor = Color.green;

    const float clickThreshold = 5.0f;

    private Vector2 startScreenPos;
    private Vector2 endScreenPos;
    private bool isDragging = false;

    private WallBuilder wallBuilder;

    [SerializeField] Texture2D dockingTexture;

    [SerializeField] SelectionPanel selectionPanel;

    void Start()
    {
        wallBuilder = GetComponent<WallBuilder>();
    }

    bool AttackMove = false;

    public void ToggleAttackMove()
    {
        AttackMove = !AttackMove;
    }

    void Update()
    {
        // Ignore if pointer is over UI
        if (EventSystem.current.IsPointerOverGameObject())
        {
            if (isDragging && Input.GetMouseButtonUp(0))
            {
                EndDragging();
            }
            return;
        }

        if (Input.GetKeyDown(KeyCode.F3))
        {
            if (DeterministicUpdateManager.Instance.IsPaused())
            {
                ResumeGameCommand resumeGameCommand = new ResumeGameCommand();
                resumeGameCommand.action = ResumeGameCommand.commandName;
                InputManager.Instance.SendInputCommand(resumeGameCommand);
            }
            else
            {
                PauseGameCommand pauseGameCommand = new PauseGameCommand();
                pauseGameCommand.action = PauseGameCommand.commandName;
                InputManager.Instance.SendInputCommand(pauseGameCommand);
            }
        }

        if (Input.GetKeyDown(KeyCode.Alpha0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, float.MaxValue, ~LayerMask.GetMask("Ignore Raycast")))
            {
                GridGeneration.Instance.FindShorePair(hit.point, out List<Vector3> positionPair);

                if (positionPair.Count == 2)
                {
                    DebugExtension.DebugWireSphere(positionPair[0], Color.red, 0.1f, 5.0f);
                    DebugExtension.DebugWireSphere(positionPair[1], Color.cyan, 0.1f, 5.0f);
                }
            }
        }

        HandleSelectionInput();
        HandleCommandInput();
    }

    public static NavMeshHit? FindProperNavHit(Vector3 targetPosition, int areaMask)
    {
        int layer = ~(1 << 2 | 1 << 3 | 1 << 6);
        Ray ray = new Ray(targetPosition + Vector3.up * 100, Vector3.down * 200);// Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, float.MaxValue, layer))
        {
            if (NavMesh.SamplePosition(hit.point, out NavMeshHit navHit, 20f, areaMask))
            {
                return navHit;
            }
        }
        return null;
    }

    public static RaycastHit? FindProperHit(Vector3 targetPosition, int navAreaMask)
    {
        int layer = ~(1 << 2 | 1 << 3 | 1 << 6);
        Ray ray = new Ray(targetPosition + Vector3.up * 100, Vector3.down * 200);// Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, float.MaxValue, layer))
        {
            if (NavMesh.SamplePosition(hit.point, out NavMeshHit navHit, 20f, navAreaMask))
            {
                ray = new Ray(navHit.position + Vector3.up * 100, Vector3.down * 200);
                if (Physics.Raycast(ray, out hit, float.MaxValue, layer))
                {
                    return hit;
                }
            }
        }
        return null;
    }

    bool IsTargetingUnit(RaycastHit hit, out MovableUnit unit)
    {
        unit = null;
        bool cmp1 = hit.transform.parent && hit.transform.parent.parent && hit.transform.parent.parent.CompareTag("Military Unit");
        bool cmp2 = hit.collider.CompareTag("Military Unit");
        if (cmp1 || cmp2)
        {
            unit = GetMovableUnitFromHit(hit);
            return true;
        }
        return false;
    }

    MovableUnit GetMovableUnitFromHit(RaycastHit hit)
    {
        bool cmp1 = hit.transform.parent && hit.transform.parent.parent && hit.transform.parent.parent.CompareTag("Military Unit");
        if (cmp1) {
            return hit.transform.parent.parent.GetComponent<MovableUnit>();
        }
        bool cmp2 = hit.collider.CompareTag("Military Unit");
        if (cmp2)
        {
            return hit.collider.GetComponent<MovableUnit>();
        }
        return null;
    }

    void HandleDeleteCommand()
    {
        if (Input.GetKeyDown(KeyCode.Delete))
        {
            List<ulong> ids = new List<ulong>();
            foreach (Unit u in selectionPanel.GetSelectedUnits())
            {
                if (u.GetType() == typeof(MovableUnit))
                {
                    if (u.playerId == 1)
                        ids.Add(u.GetUnitID());
                }
            }


            if (ids.Count > 1)
            {
                DeleteUnitsCommand deleteUnitsCommand = new DeleteUnitsCommand();
                deleteUnitsCommand.action = DeleteUnitsCommand.commandName;
                deleteUnitsCommand.unitIDs = new List<ulong>();
                deleteUnitsCommand.unitIDs.AddRange(ids);
                InputManager.Instance.SendInputCommand(deleteUnitsCommand);
            }
        }
    }

    void HandleOrderCommand()
    {
        if (Input.GetMouseButtonDown(1))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            int layer = ~(1 << 2 | 1 << 3);
            if (Physics.Raycast(ray, out RaycastHit hit, float.MaxValue, layer))
            {
                List<ulong> ids = new List<ulong>();
                foreach (Unit u in selectionPanel.GetSelectedUnits())
                {
                    if (u.GetType() == typeof(MovableUnit))
                    {
                        if (u.playerId == 1)
                            ids.Add(u.GetUnitID());
                    }
                }
                Vector3 position = hit.point;
                if (hit.collider.CompareTag("Wall Untagged"))
                {
                    if (NavMesh.SamplePosition(hit.point, out NavMeshHit navHit, 100, -1))
                    {
                        position = navHit.position;
                    }
                }
                if (ids.Count > 1)
                {
                    MoveUnitsCommand moveUnitsCommand = new MoveUnitsCommand();
                    moveUnitsCommand.action = MoveUnitsCommand.commandName;
                    moveUnitsCommand.unitIDs = new List<ulong>();
                    moveUnitsCommand.unitIDs.AddRange(ids);
                    moveUnitsCommand.position = hit.point;
                    moveUnitsCommand.IsAttackMove = AttackMove;
                    InputManager.Instance.SendInputCommand(moveUnitsCommand);
                    AttackMove = false;
                }
                //else if (ids.Count == 1)
                //{
                //    if (IsTargetingUnit(hit, out MovableUnit targetMovableUnit))
                //    {
                //        AttackUnitCommand attackUnitCommand = new AttackUnitCommand();
                //        attackUnitCommand.action = AttackUnitCommand.commandName;
                //        attackUnitCommand.unitID = ids[0];
                //        attackUnitCommand.targetID = targetMovableUnit.id;
                //        InputManager.Instance.SendInputCommand(attackUnitCommand);
                //    }
                //    else
                //    {
                //        MoveUnitCommand moveUnitCommand = new MoveUnitCommand();
                //        moveUnitCommand.action = MoveUnitCommand.commandName;
                //        moveUnitCommand.unitID = ids[0];
                //        moveUnitCommand.position = hit.point;
                //        InputManager.Instance.SendInputCommand(moveUnitCommand);
                //    }
                //}
                DebugExtension.DebugWireSphere(hit.point, Color.cyan, 0.1f, 5.0f);
            }
        }
    }

    void HandleCommandInput()
    {
        HandleDeleteCommand();
        HandleOrderCommand();
    }

    void EndDragging()
    {
        endScreenPos = Input.mousePosition;
        SelectObjects();
        isDragging = false;
    }

    void HandleSelectionInput()
    {
        if (Input.GetMouseButtonDown(0)) // Left click to start selection
        {
            startScreenPos = Input.mousePosition;
            isDragging = true;
        }
        else if (Input.GetMouseButtonUp(0)) // Release to finish selection
        {
            EndDragging();
        }

        if (isDragging)
        {
            endScreenPos = Input.mousePosition;
        }
    }

    bool CheckColliderTag(Collider hit, string tag)
    {
        return hit.gameObject.CompareTag(tag) ||
               hit.transform.parent.gameObject.CompareTag(tag) ||
               hit.transform.root.gameObject.CompareTag(tag);
    }

    GameObject FindParentByTag(Transform child, string tag)
    {
        if (child == null) return null;

        if (child.parent != null)
        {
            if (child.parent.CompareTag(tag))
                return child.parent.gameObject;
            else
                return FindParentByTag(child.parent, tag);
        }

        return null; // No matching parent found
    }

    void SelectObjects()
    {
        //Physics.SyncTransforms();
        GetSelectionBoxTransform(out Vector3 center, out Vector3 size);
        Quaternion rotation = Quaternion.Euler(30, -45, 0);
        const int selectLayer = 1 << 3; // Selectables
        Collider[] hits = Physics.OverlapBox(center, size / 2, rotation, selectLayer);
        List<Unit> selectedUnits = new List<Unit>();
        //selectedUnits.Clear();
        const string militaryUnitTag = "Military Unit";
        const string shipUnitTag = "Ship Unit";
        foreach (Collider hit in hits)
        {
            GameObject obj = FindParentByTag(hit.transform, militaryUnitTag);
            if (obj != null && obj.TryGetComponent(out MovableUnit unit))
            {
                //var icon = unit.GetSpriteIcon();
                selectedUnits.Add(unit);
                //Debug.Log("Selected unit:" + obj.gameObject.name);
            }
        }
        selectionPanel.SetupUnitSelection(selectedUnits);
        //Debug.Log("Selected Units: " + hits.Length);
    }

    void GetSelectionBoxTransform(out Vector3 position, out Vector3 size)
    {
        position = Vector3.zero;
        // Convert screen points to world positions
        Vector3[] worldCorners = new Vector3[8];
        worldCorners[0] = ScreenToWorldPoint(startScreenPos, Camera.main.nearClipPlane);
        worldCorners[1] = ScreenToWorldPoint(new Vector2(endScreenPos.x, startScreenPos.y), Camera.main.nearClipPlane);
        worldCorners[2] = ScreenToWorldPoint(endScreenPos, Camera.main.nearClipPlane);
        worldCorners[3] = ScreenToWorldPoint(new Vector2(startScreenPos.x, endScreenPos.y), Camera.main.nearClipPlane);
        worldCorners[4] = (worldCorners[0] + worldCorners[1]) / 2;
        worldCorners[5] = (worldCorners[1] + worldCorners[2]) / 2;
        worldCorners[6] = (worldCorners[2] + worldCorners[3]) / 2;
        worldCorners[7] = (worldCorners[3] + worldCorners[0]) / 2;


        // Create a selection box that extends into the scene
        Vector3 center = (worldCorners[0] + worldCorners[2]) / 2;

        float zSize = Camera.main.farClipPlane - Camera.main.nearClipPlane;

        size = new Vector3(
            (worldCorners[0] - worldCorners[1]).magnitude,
            (worldCorners[1] - worldCorners[2]).magnitude,
            zSize
        );

        position = center + Camera.main.transform.forward * (zSize / 2);
    }

    void TestSelection(GameObject testCube)
    {
        // Convert screen points to world positions
        Vector3[] worldCorners = new Vector3[8];
        worldCorners[0] = ScreenToWorldPoint(startScreenPos, Camera.main.nearClipPlane);
        worldCorners[1] = ScreenToWorldPoint(new Vector2(endScreenPos.x, startScreenPos.y), Camera.main.nearClipPlane);
        worldCorners[2] = ScreenToWorldPoint(endScreenPos, Camera.main.nearClipPlane);
        worldCorners[3] = ScreenToWorldPoint(new Vector2(startScreenPos.x, endScreenPos.y), Camera.main.nearClipPlane);
        worldCorners[4] = (worldCorners[0] + worldCorners[1]) / 2;
        worldCorners[5] = (worldCorners[1] + worldCorners[2]) / 2;
        worldCorners[6] = (worldCorners[2] + worldCorners[3]) / 2;
        worldCorners[7] = (worldCorners[3] + worldCorners[0]) / 2;

        DebugExtension.DebugWireSphere(worldCorners[0], Color.red, 0.2f);
        DebugExtension.DebugWireSphere(worldCorners[1], Color.red, 0.2f);
        DebugExtension.DebugWireSphere(worldCorners[2], Color.red, 0.2f);
        DebugExtension.DebugWireSphere(worldCorners[3], Color.red, 0.2f);

        DebugExtension.DebugWireSphere(worldCorners[4], Color.red, 0.2f);
        DebugExtension.DebugWireSphere(worldCorners[5], Color.red, 0.2f);
        DebugExtension.DebugWireSphere(worldCorners[6], Color.red, 0.2f);
        DebugExtension.DebugWireSphere(worldCorners[7], Color.red, 0.2f);

        // Create a selection box that extends into the scene
        Vector3 center = (worldCorners[0] + worldCorners[2]) / 2;
        DebugExtension.DebugWireSphere(center, Color.red, 0.2f);

        float zSize = Camera.main.farClipPlane - Camera.main.nearClipPlane;

        Vector3 size = new Vector3(
            (worldCorners[0] - worldCorners[1]).magnitude,
            (worldCorners[1] - worldCorners[2]).magnitude,
            zSize
        );

        Quaternion rotation = Quaternion.Euler(30, -45, 0); // Quaternion.LookRotation(Camera.main.transform.forward, Vector3.up);

        //Matrix4x4 matrix = new Matrix4x4();
        //matrix.SetTRS(center, rotation, size);
        testCube.transform.position = center + Camera.main.transform.forward * (zSize / 2);
        testCube.transform.rotation = rotation;
        testCube.transform.localScale = size;
        //DebugExtension.DebugLocalCube(matrix, Vector3.one);
    }

    Vector3 ScreenToWorldPoint(Vector2 screenPos, float depth)
    {
        Vector3 screenPoint = new Vector3(screenPos.x, screenPos.y, depth);
        return Camera.main.ScreenToWorldPoint(screenPoint);
    }

    void OnGUI()
    {
        if (isDragging)
        {
            Rect selectionRect = GetScreenRect(startScreenPos, endScreenPos);
            DrawScreenRect(selectionRect, selectionBoxColor);
            DrawScreenRectBorder(selectionRect, 2, selectionBorderColor);
        }
    }

    // Helper Functions for UI
    Rect GetScreenRect(Vector2 screenPos1, Vector2 screenPos2)
    {
        screenPos1.y = Screen.height - screenPos1.y;
        screenPos2.y = Screen.height - screenPos2.y;
        return new Rect(Mathf.Min(screenPos1.x, screenPos2.x), Mathf.Min(screenPos1.y, screenPos2.y),
                        Mathf.Abs(screenPos1.x - screenPos2.x), Mathf.Abs(screenPos1.y - screenPos2.y));
    }

    void DrawScreenRectBorder(Rect rect, float thickness, Color color)
    {
        DrawScreenRect(new Rect(rect.xMin, rect.yMin, rect.width, thickness), color); // Top
        DrawScreenRect(new Rect(rect.xMin, rect.yMax - thickness, rect.width, thickness), color); // Bottom
        DrawScreenRect(new Rect(rect.xMin, rect.yMin, thickness, rect.height), color); // Left
        DrawScreenRect(new Rect(rect.xMax - thickness, rect.yMin, thickness, rect.height), color); // Right
    }

    void DrawScreenRect(Rect rect, Color color)
    {
        GUI.color = color;
        GUI.DrawTexture(rect, Texture2D.whiteTexture);
        GUI.color = Color.white;
    }
}