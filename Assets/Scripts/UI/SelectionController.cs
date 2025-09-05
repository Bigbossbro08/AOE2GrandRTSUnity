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
using Assimp.Unmanaged;

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

        //if (Input.GetKeyDown(KeyCode.F3))
        //{
        //    if (DeterministicUpdateManager.Instance.IsPaused())
        //    {
        //        ResumeGameCommand resumeGameCommand = new ResumeGameCommand();
        //        resumeGameCommand.action = ResumeGameCommand.commandName;
        //        InputManager.Instance.SendInputCommand(resumeGameCommand);
        //    }
        //    else
        //    {
        //        PauseGameCommand pauseGameCommand = new PauseGameCommand();
        //        pauseGameCommand.action = PauseGameCommand.commandName;
        //        InputManager.Instance.SendInputCommand(pauseGameCommand);
        //    }
        //}

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
        Ray ray = new Ray(targetPosition + Vector3.up * 100, Vector3.down * 200);// Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, float.MaxValue, layer))
        {
            if (NavMesh.SamplePosition(hit.point, out NavMeshHit navHit, 20f, navAreaMask))
            {
                ray = new Ray(navHit.position + Vector3.up * 100, Vector3.down * 200);
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
                    if (u.playerId == UnitManager.localPlayerId)
                        ids.Add(u.GetUnitID());
                }
            }


            if (ids.Count > 0)
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
                        if (u.playerId == UnitManager.localPlayerId)
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

    // Check if a sprite's screen bounds overlap with the selection rect
    bool IsSpriteInSelection(Rect selectionRect, SpriteRenderer spriteRenderer, Camera cam)
    {
        Bounds worldBounds = spriteRenderer.bounds;
        Vector3 screenMin = cam.WorldToScreenPoint(worldBounds.min);
        Vector3 screenMax = cam.WorldToScreenPoint(worldBounds.max);
        Rect spriteScreenRect = Rect.MinMaxRect(screenMin.x, screenMin.y, screenMax.x, screenMax.y);
        return selectionRect.Overlaps(spriteScreenRect);
    }

    bool IsPointInSprite(Vector2 point, SpriteRenderer spriteRenderer, Camera cam)
    {
        Bounds worldBounds = spriteRenderer.bounds;
        Vector3 screenMin = cam.WorldToScreenPoint(worldBounds.min);
        Vector3 screenMax = cam.WorldToScreenPoint(worldBounds.max);

        // Create a rect from the sprite's screen bounds
        Rect spriteScreenRect = Rect.MinMaxRect(
            Mathf.Min(screenMin.x, screenMax.x),
            Mathf.Min(screenMin.y, screenMax.y),
            Mathf.Max(screenMin.x, screenMax.x),
            Mathf.Max(screenMin.y, screenMax.y)
        );

        return spriteScreenRect.Contains(point);
    }

    void EndDragging()
    {
        endScreenPos = Input.mousePosition;
        
        // Convert screen coordinates to a normalized rect
        float x = Mathf.Min(startScreenPos.x, endScreenPos.x);
        float y = Mathf.Min(startScreenPos.y, endScreenPos.y);
        float width = Mathf.Abs(endScreenPos.x - startScreenPos.x);
        float height = Mathf.Abs(endScreenPos.y - startScreenPos.y);

        List<Unit> selectedUnits = new List<Unit>();
        Rect selectionRect = new Rect(x, y, width, height);
        //SelectObjects();
        var activeVisuals = SpriteManager.Instance.activeVisuals;
        foreach (var vis in activeVisuals)
        {
            SpriteRenderer spriteRenderer = vis.Value.spriteReader.GetSpriteRenderer();
            if (IsSpriteInSelection(selectionRect, spriteRenderer, Camera.main))
            {
                selectedUnits.Add(vis.Value.core);
            }
        }
        List<Unit> filteredUnits = selectedUnits;
        selectionPanel.SetupUnitSelection(filteredUnits);
        commandPanelUI.FigureoutPanelFromSelection(filteredUnits);
        isDragging = false;

        startScreenPos = Input.mousePosition;
    }
    
    private GameObject GetObjectUnderMouse()
    {
        int layerMask = 1 << 6; // Layer 6
        //Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        Vector3 screenPoint = Input.mousePosition;// Camera.main.WorldToScreenPoint(Input.mousePosition);
        var activeVisuals = SpriteManager.Instance.activeVisuals;
        GameObject retObj = null;
        foreach (var vis in activeVisuals)
        {
            SpriteRenderer spriteRenderer = vis.Value.spriteReader.GetSpriteRenderer();
            if (IsPointInSprite(screenPoint, spriteRenderer, Camera.main))
            {
                retObj = vis.Value.core.gameObject;
                if (selectedObject == retObj)
                {
                    continue;
                }
                break;
            }
        }
        
        //if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, layerMask))
        //{
        //    return hit.transform.gameObject;
        //}

        return retObj;
    }


    [SerializeField] private float mouseDownTime = 0.0f;
    [SerializeField] private float doubleClickTime = 0.3f;
    [SerializeField] private float dragThreshold = 5f; // In pixels
    [SerializeField] private GameObject selectedObject = null;
    [SerializeField] bool mouseButtonDown = false;
    [SerializeField] bool startCheckForDoubleClick = false;
    [SerializeField] bool doubleClickHappened = false;
    //[SerializeField] bool draggingBehavior = false;

    void UpdateMouseButton()
    {
        if (startCheckForDoubleClick)
        {
            if (Time.time - mouseDownTime > doubleClickTime)
            {
                startCheckForDoubleClick = false;
            }
        }
        
        if (Input.GetMouseButtonDown(0))
        {
            startScreenPos = Input.mousePosition;
            if (startCheckForDoubleClick)
            {
                HandleDoubleClick();
                startCheckForDoubleClick = false;
                doubleClickHappened = true;
            }
            mouseButtonDown = true;
        }

        if (mouseButtonDown)
        {
            Vector2 mouseDownPosition = startScreenPos;
            endScreenPos = Input.mousePosition;

            // Check if we should start dragging
            if (Vector2.Distance(mouseDownPosition, endScreenPos) > dragThreshold)
            {
                isDragging = true;
                doubleClickHappened = false;
                startCheckForDoubleClick = false;
            }
        }

        if (Input.GetMouseButtonUp(0))
        {
            if (isDragging)
            {
                EndDragging();
                isDragging = false;
                startCheckForDoubleClick = false;
                doubleClickHappened = false;
            }
            else
            {
                if (doubleClickHappened)
                {
                    doubleClickHappened = false;
                }
                else
                {

                    if (!startCheckForDoubleClick)
                    {
                        HandleSingleClick();
                        startCheckForDoubleClick = true;
                        mouseDownTime = Time.time;
                    }
                }
            }

            mouseButtonDown = false;
        }
    }

    private void HandleSingleClick()
    {
        List<Unit> filteredUnits = new List<Unit>();
        selectedObject = GetObjectUnderMouse();
        if (selectedObject)
        {
            Debug.Log($"Single Click on {selectedObject.name}");

            MovableUnit controllableUnit = selectedObject.GetComponentInParent<MovableUnit>();
            if (controllableUnit == null)
            {
                controllableUnit = selectedObject.GetComponent<MovableUnit>();
            }

            if (controllableUnit)
            {
                filteredUnits.Add(controllableUnit);
            }
        }

        selectionPanel.SetupUnitSelection(filteredUnits);
        commandPanelUI.FigureoutPanelFromSelection(filteredUnits);
    }

    private void HandleDoubleClick()
    {
        if (selectedObject == null)
        {
            return;
        }
        Debug.Log($"Double Click on {selectedObject.name}");
        SelectSimilarVisibleObjects(selectedObject);
    }

    void HandleSelectionInput()
    {
        UpdateMouseButton();
    }

    void SelectSimilarVisibleObjects(GameObject referenceObj)
    {
        if (referenceObj == null)
        {
            return;
        }
        string referenceTag = referenceObj.tag;
        Debug.Log($"{referenceObj.name}");
        // or referenceObj.GetComponent<Unit>().unitType, etc.

        MovableUnit referenceUnit = referenceObj.GetComponent<MovableUnit>();
        if (referenceUnit == null)
        {
            referenceUnit = referenceObj.GetComponentInParent<MovableUnit>();
        }
        if (referenceUnit == null)
        {

            return;
        }

        Plane[] planes = GeometryUtility.CalculateFrustumPlanes(Camera.main);
        //List<GameObject> selectedObjects = new List<GameObject>();
        List<Unit> selectedUnits = new List<Unit>();

        foreach (var obj in GameObject.FindGameObjectsWithTag(referenceTag))
        {
            MovableUnit controllableUnit = obj.GetComponentInParent<MovableUnit>();
            if (controllableUnit == null)
            {
                controllableUnit = obj.GetComponent<MovableUnit>();
            }
            if (controllableUnit == null)
            {
                continue;
            }
            if (referenceUnit.playerId != controllableUnit.playerId)
            {
                continue;
            }
            if (controllableUnit.unitDataName != referenceUnit.unitDataName)
            {
                continue;
            }
            
            //Renderer rend = obj.GetComponentInChildren<Renderer>();
            //if (rend != null)
            //{
            //    if (GeometryUtility.TestPlanesAABB(planes, rend.bounds))
            //    {
            //        //selectedObjects.Add(obj);
            //        selectedUnits.Add(referenceUnit);
            //    }
            //}
            if (!selectedUnits.Contains(controllableUnit))
                selectedUnits.Add(controllableUnit);
        }

        MovableUnit selectedShip = null;

        List<Unit> filteredUnits = new List<Unit>();

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

        // Handle your selection system here
        Debug.Log($"Selected {filteredUnits.Count} {referenceTag} objects.");

        selectionPanel.SetupUnitSelection(filteredUnits);
        commandPanelUI.FigureoutPanelFromSelection(filteredUnits);
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