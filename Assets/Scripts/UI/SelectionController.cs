using UnityEngine;
using System.Collections.Generic;
using UnityEngine.AI;
using UnityEngine.Rendering;
using TMPro;
using static BasicAttackAIModule;
using Unity.VisualScripting;
using static Utilities;
using System.Linq;
using UnityEngine.EventSystems;

public class SelectionController : MonoBehaviour
{
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

    [SerializeField] CommandPanelUI commandPanelUI;

    void Start()
    {
        wallBuilder = GetComponent<WallBuilder>();
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
        Ray ray = new Ray(targetPosition + Vector3.up * 200, Vector3.down * 200);// Camera.main.ScreenPointToRay(Input.mousePosition);
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
        int layer = ~(1 << 2 | 1 << 3 | 1 << 6 | 1 << 30);
        Ray ray = new Ray(targetPosition + Vector3.up * 200, Vector3.down * 400);// Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, float.MaxValue, layer))
        {
            if (NavMesh.SamplePosition(hit.point, out NavMeshHit navHit, 20f, navAreaMask))
            {
                ray = new Ray(navHit.position + Vector3.up * 200, Vector3.down * 400);
                RaycastHit[] hits = Physics.RaycastAll(ray, float.MaxValue, layer);
                foreach (var h in hits)
                {
                    if (h.collider.TryGetComponent(out MovableUnit movableUnit))
                    {
                        if (!movableUnit.IsShip())
                        {
                            continue;
                        }
                    }
                    return h;
                }
                //if (Physics.Raycast(ray, out hit, float.MaxValue, layer))
                //{
                //    return hit;
                //}
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
                //commandPanelUI.FigureoutPanelFromSelection(selectionPanel.GetSelectedUnits());
                commandPanelUI.HandleOrder(hit);
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
        List<Unit> selectedUnits = new List<Unit>();
        List<Unit> filteredUnits = new List<Unit>();

        //Physics.SyncTransforms();
        GetSelectionBoxTransform(out Vector3 center, out Vector3 size);
        Vector2 xySize = new Vector2(Mathf.Abs(startScreenPos.x - endScreenPos.x), Mathf.Abs(startScreenPos.y - endScreenPos.y));
        if (xySize.magnitude < 1)
        {
            //int layer = ~(1 << 2 | 1 << 3 | 1 << 6 | 1 << 30);
            int layer = 6;
            RaycastHit hit;
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out hit, 100000, layer))
            {
                Unit unit = hit.transform.GetComponent<Unit>();
                if (unit && unit.IsSelectable())
                {
                    selectedUnits.Add(unit);
                }
            }
        }
        else
        {
            Quaternion rotation = Quaternion.Euler(30, -45, 0);
            const int selectLayer = 1 << 3; // Selectables
            Collider[] hits = Physics.OverlapBox(center, size / 2, rotation, selectLayer);
            //selectedUnits.Clear();
            const string militaryUnitTag = "Military Unit";
            const string shipUnitTag = "Ship Unit";
            foreach (Collider hit in hits)
            {
                GameObject obj = FindParentByTag(hit.transform, militaryUnitTag);
                if (obj != null && obj.TryGetComponent(out MovableUnit unit))
                {
                    if (!unit.IsSelectable()) continue;
                    selectedUnits.Add(unit);
                }

                obj = FindParentByTag(hit.transform, shipUnitTag);
                if (obj != null && obj.TryGetComponent(out unit))
                {
                    if (!unit.IsSelectable()) continue;
                    selectedUnits.Add(unit);
                }
            }
        }

        MovableUnit selectedShip = null;

        for (int i = 0; i < selectedUnits.Count && i < 60; i++)
        {
            var unit = selectedUnits[i];
            if (unit.GetType() == typeof(MovableUnit))
            {
                MovableUnit movableUnit = (MovableUnit)unit;
                if (movableUnit.IsShip())
                {
                    selectedShip = movableUnit;
                    continue;
                }
                //if (movableUnit.movementComponent.IsOnShip()) continue;
                filteredUnits.Add(unit);
            }
        }

        if (filteredUnits.Count == 0 && selectedShip != null)
        {
            filteredUnits.Add(selectedShip);
        }

        selectionPanel.SetupUnitSelection(filteredUnits);
        commandPanelUI.FigureoutPanelFromSelection(filteredUnits);
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