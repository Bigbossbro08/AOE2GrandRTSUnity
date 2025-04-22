using UnityEngine;
using System.Collections.Generic;
using UnityEngine.AI;
using UnityEngine.Rendering;
using TMPro;

public class MoveUnitCommand : InputCommand {
    public const string commandName = "Move Unit Command";
    public ulong unitID;
    public Vector3 position;

    public void Execute()
    {
        Unit unit = UnitManager.Instance.GetUnit(unitID);
        if (unit)
        {
            ulong newCrowdID = ++UnitManager.crowdIDCounter;
            MovableUnit movableUnit = (MovableUnit)unit;
            movableUnit.ResetUnit();
            movableUnit.movementComponent.StartPathfind(position);
            movableUnit.movementComponent.crowdID = newCrowdID;
        }
    }
}

public class MoveUnitsCommand : InputCommand
{
    public const string commandName = "Move Units Command";
    public List<ulong> unitIDs;
    public Vector3 position;

    public void Execute()
    {
        int unitCount = unitIDs.Count;

        int gridSize = Mathf.CeilToInt(Mathf.Sqrt(unitCount)); // Define grid size
        float spacing = 0.6f; // Distance between units
        int lastRowCount = unitCount % gridSize; // Units in the last row

        ulong newCrowdID = ++UnitManager.crowdIDCounter;

        for (int i = 0; i < unitCount; i++)
        {
            Unit unit = UnitManager.Instance.GetUnit(unitIDs[i]);
            if (unit)
            {
                int row = i / gridSize;
                int col = i % gridSize;

                // Handle last row centering
                if (row == unitCount / gridSize && lastRowCount != 0)
                {
                    float lastRowOffset = ((gridSize - lastRowCount) * spacing) / 2;
                    col += Mathf.FloorToInt(lastRowOffset / spacing); // Shift towards center
                }
                // Center the entire grid around targetPosition
                Vector3 gridCenterOffset = new Vector3((gridSize - 1) * spacing / 2, 0, (gridSize - 1) * spacing / 2);

                Vector3 offset = new Vector3(col * spacing, 0, row * spacing) -
                                 new Vector3(gridSize * spacing / 2, 0, gridSize * spacing / 2);

                Vector3 unitTarget = position + gridCenterOffset + offset;

                MovableUnit movableUnit = (MovableUnit)unit;
                movableUnit.ResetUnit();
                movableUnit.movementComponent.StartPathfind(unitTarget);
                movableUnit.movementComponent.crowdID = newCrowdID;
            }
        }
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
        Unit unit = UnitManager.Instance.GetUnit(unitID);
        Unit targetUnit = UnitManager.Instance.GetUnit(targetID);
        if (unit && targetUnit)
        {
            MovableUnit movableUnit = (MovableUnit)unit;
            MovableUnit movableTargetUnit = (MovableUnit)targetUnit;
            if (movableUnit.aiModule)
            {
                BasicAttackAIModule basicAttackAIModule = (BasicAttackAIModule)movableUnit.aiModule;
                if (basicAttackAIModule)
                {
                    movableUnit.ResetUnit();
                    basicAttackAIModule.InitializeAI(movableUnit, movableTargetUnit);
                    ulong newCrowdID = ++UnitManager.crowdIDCounter;
                    movableUnit.movementComponent.crowdID = newCrowdID;
                    Debug.Log("Executing attack");
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

    private List<Unit> selectedUnits = new List<Unit>();

    private bool navalFocusSelect = false;
    private bool shoreMode = false;

    //GameObject testCube;

    void Start()
    {
        wallBuilder = GetComponent<WallBuilder>();
        //testCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            wallHitTest = !wallHitTest;
        }

        if (wallHitTest)
        {
            int layer = ~(1 << 2 | 1 << 3 | 1 << 6);
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, float.MaxValue, layer))
            {
                RaycastHit? newRayHit = FindProperHit(hit.point, 1);
                if (newRayHit.HasValue)
                {
                    DebugExtension.DebugWireSphere(newRayHit.Value.point, Color.cyan, 0.1f, 5.0f);
                }
            }
            return;
        }

        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            if (wallBuilder)
            {
                wallBuilder.enabled = !wallBuilder.enabled;
            }
        }

        if (wallBuilder.enabled)
        {
            return;
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

    void OnUnitSelectMode()
    {
        StopShoreMode();
        if (cameraMovement)
        {
            cameraMovement.SetNavalMode(navalFocusSelect);
        }
    }

    void OnNavalSelectMode()
    {
        StopShoreMode(); 
        if (cameraMovement)
        {
            cameraMovement.SetNavalMode(navalFocusSelect);
        }
    }

    void StartShoreMode()
    {
        shoreMode = true;
        Cursor.SetCursor(dockingTexture, new Vector2(dockingTexture.width / 2, dockingTexture.height / 2), CursorMode.Auto);
    }

    void StopShoreMode()
    {
        shoreMode = false;
        Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
    }

    void HandleNavalFocusOrder(ShipUnit ship, RaycastHit hit)
    {
        if (shoreMode)
        {
            Debug.Log("Shore mode order!");

            GridGeneration.Instance.FindShorePair(hit.point, out List<Vector3> positionPair);
            if (positionPair.Count == 2)
            {
                MoveShipToDockCommand moveShipToDockCommand = new MoveShipToDockCommand();
                moveShipToDockCommand.action = MoveShipToDockCommand.commandName;
                moveShipToDockCommand.unitID = ship.GetUnitID();
                moveShipToDockCommand.position = positionPair[1];
                moveShipToDockCommand.targetToDock = positionPair[0];
                InputManager.Instance.SendInputCommand(moveShipToDockCommand);
            }
            //if (NavMesh.SamplePosition(hitPoint, out NavMeshHit navHit, 2, 1 << 3))
            //{
            //    DebugExtension.DebugWireSphere(navHit.position, Color.yellow, 1f, 5.0f);
            //}

            StopShoreMode();
            return;
        }
        MoveShipUnitCommand moveShipUnitCommand = new MoveShipUnitCommand();
        moveShipUnitCommand.action = MoveShipUnitCommand.commandName;
        moveShipUnitCommand.unitID = ship.GetUnitID();
        moveShipUnitCommand.position = hit.point;
        InputManager.Instance.SendInputCommand(moveShipUnitCommand);
    }

    bool wallHitTest = false;

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

    void HandleCommandInput()
    {
        

        if (Input.GetKeyDown(KeyCode.N))
        {
            navalFocusSelect = !navalFocusSelect;
            if (navalFocusSelect) 
                OnNavalSelectMode();
            else 
                OnUnitSelectMode();
        }

        if (Input.GetKeyDown(KeyCode.K))
        {
            if (shoreMode)
            {
                StopShoreMode();
            } else
            {
                StartShoreMode();
            }
        }

        if (Input.GetMouseButtonDown(1))
        {
            if (navalFocusSelect)
                Debug.Log("Starting Naval Select");

            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            int layer = ~(1 << 2 | 1 << 3);
            if (Physics.Raycast(ray, out RaycastHit hit, float.MaxValue, layer))
            {
                List<ulong> ids = new List<ulong>();
                foreach (Unit u in selectedUnits)
                {
                    if (navalFocusSelect)
                    {
                        if (u.GetType() == typeof(ShipUnit))
                        {
                            ShipUnit ship = (ShipUnit)u;
                            HandleNavalFocusOrder(ship, hit);
                            continue;
                        }
                    }
                    else
                    {
                        //if (u.GetType() == typeof (MovableUnit))
                        //{
                        //    MoveUnitCommand moveUnitCommand = new MoveUnitCommand();
                        //    moveUnitCommand.action = MoveUnitCommand.commandName;
                        //    moveUnitCommand.unitID = u.GetUnitID();
                        //    moveUnitCommand.position = hit.point;
                        //    InputManager.Instance.SendInputCommand(moveUnitCommand);
                        //}
                        if (u.GetType() == typeof (MovableUnit))
                        {
                            //MoveUnitCommand moveUnitCommand = new MoveUnitCommand();
                            //moveUnitCommand.action = MoveUnitCommand.commandName;
                            //moveUnitCommand.unitID = u.GetUnitID();
                            //moveUnitCommand.position = hit.point;
                            //InputManager.Instance.SendInputCommand(moveUnitCommand);
                            ids.Add(u.GetUnitID());
                        }
                    }
                }
                if (!navalFocusSelect)
                {
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
                        InputManager.Instance.SendInputCommand(moveUnitsCommand);
                    } else if (ids.Count == 1)
                    {
                        if (IsTargetingUnit(hit, out MovableUnit targetMovableUnit))
                        {
                            AttackUnitCommand attackUnitCommand = new AttackUnitCommand();
                            attackUnitCommand.action = AttackUnitCommand.commandName;
                            attackUnitCommand.unitID = ids[0];
                            attackUnitCommand.targetID = targetMovableUnit.id;
                            InputManager.Instance.SendInputCommand(attackUnitCommand);
                        }
                        else
                        {
                            MoveUnitCommand moveUnitCommand = new MoveUnitCommand();
                            moveUnitCommand.action = MoveUnitCommand.commandName;
                            moveUnitCommand.unitID = ids[0];
                            moveUnitCommand.position = hit.point;
                            InputManager.Instance.SendInputCommand(moveUnitCommand);
                        }
                    }
                }
                DebugExtension.DebugWireSphere(hit.point, Color.cyan, 0.1f, 5.0f);
            }
        }
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
            isDragging = false;
            endScreenPos = Input.mousePosition;
            //TestSelection();

            //if (Vector2.Distance(startScreenPos, endScreenPos) < clickThreshold)
            //{
            //    SelectSingleObject();
            //}
            //else
            //{
            SelectObjects();
            //}
        }

        if (isDragging)
        {
            endScreenPos = Input.mousePosition;

            //TestSelection();

            // Debug draw
            //DebugDrawBox(center, size, rotation);

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
        selectedUnits.Clear();
        const string militaryUnitTag = "Military Unit";
        const string shipUnitTag = "Ship Unit";
        foreach (Collider hit in hits)
        {
            if (navalFocusSelect)
            {
                GameObject obj = FindParentByTag(hit.transform, shipUnitTag);
                if (obj != null && obj.TryGetComponent(out ShipUnit shipUnit))
                {
                    selectedUnits.Add(shipUnit);
                    Debug.Log("Selected unit:" + obj.gameObject.name);
                }
            } else
            {
                GameObject obj = FindParentByTag(hit.transform, militaryUnitTag);
                if (obj != null && obj.TryGetComponent(out MovableUnit unit))
                {
                    selectedUnits.Add(unit);
                    Debug.Log("Selected unit:" + obj.gameObject.name);
                }
            }
        }
        Debug.Log("Selected Units: " + hits.Length);
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

    private void OnDrawGizmos()
    {
        // Box Selection Debugging
        //if (isDragging)
        //{
        //    Gizmos.color = Color.cyan;
        //    Vector3[] corners = GetWorldSelectionBoxCorners(startScreenPos, endScreenPos);
        //
        //    // Draw selection box edges
        //    Gizmos.DrawLine(corners[0], corners[1]);
        //    Gizmos.DrawLine(corners[1], corners[3]);
        //    Gizmos.DrawLine(corners[3], corners[2]);
        //    Gizmos.DrawLine(corners[2], corners[0]);
        //}
    }

    void DrawScreenRect(Rect rect, Color color)
    {
        GUI.color = color;
        GUI.DrawTexture(rect, Texture2D.whiteTexture);
        GUI.color = Color.white;
    }
}