using Assimp;
using Newtonsoft.Json;
using NUnit.Framework;
using NUnit.Framework.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UIElements;
using static Codice.Client.Commands.WkTree.WorkspaceTreeNode;
using static MovableUnit;
using static PathfinderTest;
using static PlasticPipe.PlasticProtocol.Client.ConnectionCreator.PlasticProtoSocketConnection;
using static Unit;
using static UnitManager.UnitJsonData;
using static UnityEditor.Experimental.GraphView.GraphView;
using static UnityEngine.GraphicsBuffer;

[RequireComponent(typeof(MovementComponent))]
public class MovableUnit : Unit, IDeterministicUpdate, MapLoader.IMapSaveLoad
{
    [JsonObject(MemberSerialization.OptIn)]
    public class MovableUnitData : UnitData
    {
        [JsonProperty] public CommonStructures.SerializableVector2Int lastGridCell;
        [JsonProperty] public bool enabled;
        public MovableUnitData() { type = "MovableUnitData"; }
    }

    public StatComponent statComponent;
    public MovementComponent movementComponent;
    public ActionComponent actionComponent;
    public Rigidbody _rigidbody;

    public UnitTypeComponent unitTypeComponent;
    public UnitAIModule aiModule;

    public Transform aiTransformHolder;

    public UnitAIModule.AIModule defaultModule = UnitAIModule.AIModule.BasicAttackAIModule;
    public List<object> defaultAiModuleArgs = new List<object>() { null, true };
    public System.Action<ulong> OnRelease = (id) => { };

    [SerializeField] DeterministicVisualUpdater DeterministicVisualUpdater;

    Vector2Int lastGridCell;

    public string standSprite = "idle_archer";

    public string walkSprite = "move_archer";

    public class ShipData
    {
        public bool isShipMode = false;
        public Transform transform = null;
        public MovableUnit movableUnit = null;
        public GameObject shipDeck = null;
        public Rigidbody rigidbody = null;
        public NavMeshSurface navMeshSurface = null;
        public List<NavMeshLink> navMeshLinks = new List<NavMeshLink>();
        public bool isDocked = false;
        public bool dockAgainstShip = false;
        public bool checkUndockableAtStop = false;
        public List<MovableUnit> unitsOnShip = new List<MovableUnit>();
        public PropUnit dockedProp = null;
        public NavMeshObstacle navMeshObstacle = null;
        public MeshCollider selectionCollider = null;
        string dockedPropName = null;
        public bool canBeUndocked = false;

        private MissionCollisionTriggerChecker shipCollisionTrigger = null;

        void OnCrewRelease(ulong id)
        {
            Unit unit = UnitManager.Instance.GetUnit(id);
            MovableUnit movableUnit = unit as MovableUnit;
            if (unitsOnShip.Contains(movableUnit))
            {
                unitsOnShip.Remove(movableUnit);
            }
            movableUnit.OnRelease -= OnCrewRelease;
        }

        void OnShipColliderEnter(Collider other)
        {
            if (other.CompareTag("Military Unit"))
            {
                MovableUnit movableUnit = other.GetComponent<MovableUnit>();
                if (movableUnit && StatComponent.IsUnitAliveOrValid(movableUnit) && !movableUnit.shipData.isShipMode)
                {
                    if (unitsOnShip.Count == 0)
                    {
                        this.movableUnit.playerId = movableUnit.playerId;
                        this.movableUnit.DeterministicVisualUpdater.RefreshVisuals();
                    }

                    movableUnit.OnRelease += OnCrewRelease;

                    unitsOnShip.Add(movableUnit);
                    Debug.Log($"Unit: {movableUnit.id} added to ship with health {movableUnit.statComponent.GetHealth()}: {this.movableUnit.id}: and count on ship is {unitsOnShip.Count}");
                }
            }

            canBeUndocked = unitsOnShip.Count > 0;
        }

        void OnShipColliderExit(Collider other) { 
            if (other.CompareTag("Military Unit"))
            {
                MovableUnit movableUnit = other.GetComponent<MovableUnit>();
                if (movableUnit && unitsOnShip.Contains(movableUnit))
                {
                    movableUnit.OnRelease -= OnCrewRelease;
                    unitsOnShip.Remove(movableUnit);
                    Debug.Log($"Unit: {movableUnit.id} removed from ship: {this.movableUnit.id}: and count on ship is {unitsOnShip.Count}");
                }
            }

            canBeUndocked = unitsOnShip.Count > 0;
        }

        internal bool IsDrivable()
        {
            return unitsOnShip.Count != 0;
        }

        public void Initialize(UnitManager.UnitJsonData unitData, 
            Transform transform, 
            Rigidbody rigidbody, 
            MovableUnit movableUnit)
        {
            canBeUndocked = false;
            this.transform = transform;
            this.movableUnit = movableUnit;
            rigidbody.isKinematic = true;
            this.rigidbody = rigidbody;
            Transform visualTransform = transform.Find("Visual");
            if (visualTransform != null)
            {
                Transform spriteTransform = visualTransform.Find("SpriteGraphic");
                if (spriteTransform != null)
                {
                    SpriteRenderer spriteRenderer = spriteTransform.GetComponent<SpriteRenderer>();
                    spriteRenderer.sortingOrder = 0;
                }
            }

            if (!string.IsNullOrEmpty(unitData.ship_data.deck_mesh_name))
            {
                AssimpMeshLoader.MeshReturnData meshReturnData = AssimpMeshLoader.Instance.LoadMeshFromAssimp(unitData.ship_data.deck_mesh_name);

                UnityEngine.Mesh mesh = AssimpMeshLoader.ScaleMesh(meshReturnData.mesh, unitData.ship_data.deck_mesh_size);

                shipDeck = new GameObject("ShipDeck");
                shipDeck.layer = 2;
                MeshCollider meshCollider = shipDeck.AddComponent<MeshCollider>();
                meshCollider.sharedMesh = mesh;
                meshCollider.convex = true;
                meshCollider.isTrigger = true;

                shipCollisionTrigger = shipDeck.AddComponent<MissionCollisionTriggerChecker>();
                shipCollisionTrigger.OnTriggerEnterCallback += OnShipColliderEnter;
                shipCollisionTrigger.OnTriggerExitCallback += OnShipColliderExit;

                Transform triggerHolder = transform.Find("Triggers");
                shipDeck.transform.SetParent(triggerHolder != null ? triggerHolder : transform, false);
                if (triggerHolder)
                {
                    Transform checkCollider = triggerHolder.Find("CheckCollider");
                    if (checkCollider)
                    {
                        checkCollider.gameObject.SetActive(false);
                    }
                }
            }

            if (!string.IsNullOrEmpty(unitData.ship_data.navmesh_name))
            {
                UnityEngine.Mesh mesh = null;
                Transform triggerHolder = transform.Find("Triggers");
                if (triggerHolder)
                {
                    AssimpMeshLoader.MeshReturnData meshReturnData = AssimpMeshLoader.Instance.LoadMeshFromAssimp(unitData.ship_data.navmesh_name);
                    float size = unitData.ship_data.navmesh_size;
                    mesh = AssimpMeshLoader.ScaleMesh(meshReturnData.mesh, size);

                    mesh.RecalculateBounds();
                    mesh.RecalculateNormals();
                    GameObject navMesh = new GameObject("Navmesh");
                    navMesh.transform.SetParent(triggerHolder.transform, false);

                    GameObject navMeshHolder = new GameObject("NavmeshContents");
                    MeshFilter navMeshFilter = navMeshHolder.AddComponent<MeshFilter>();
                    navMeshFilter.mesh = mesh;
                    MeshRenderer navMeshRenderer = navMeshHolder.AddComponent<MeshRenderer>();
                    GameObject navmeshObstacleGO = new GameObject("NavmeshObstacle");
                    navmeshObstacleGO.transform.SetParent(navMeshHolder.transform);
                    navMeshObstacle = navmeshObstacleGO.AddComponent<NavMeshObstacle>();
                    navMeshObstacle.carving = true;
                    Utilities.FitNavMeshObstacleToMesh(navMeshFilter, navMeshObstacle);

                    navMeshHolder.transform.SetParent(navMesh.transform, false);

                    navMeshSurface = navMesh.AddComponent<NavMeshSurface>();
                    navMeshSurface.collectObjects = CollectObjects.Children;
                    navMeshSurface.useGeometry = UnityEngine.AI.NavMeshCollectGeometry.RenderMeshes;
                    navMeshSurface.defaultArea = 0;
                    navMeshSurface.BuildNavMesh();

                    if (unitData.ship_data.navlinks != null)
                    {
                        int counter = 0;
                        foreach (var navLinkData in unitData.ship_data.navlinks)
                        {
                            GameObject navlinkGO = new GameObject($"Navlink Data {counter}");
                            NavMeshLink navMeshLink = navlinkGO.AddComponent<NavMeshLink>();
                            navMeshLink.startPoint = (Vector3)navLinkData.start;
                            navMeshLink.endPoint = (Vector3)navLinkData.end;
                            navMeshLink.width = navLinkData.width;
                            navMeshLink.area = navLinkData.area_type;
                            //navMeshLink.autoUpdate = true;
                            navlinkGO.transform.SetParent(navMeshHolder.transform, false);
                            navMeshLink.UpdateLink();
                            navMeshLinks.Add(navMeshLink);
                            counter++;
                        }
                    }
                }

                Transform selectionHolder = transform.Find("Selection");
                if (selectionHolder)
                {
                    Transform selctionMeshHolder = selectionHolder.Find("SelectionCapsule");
                    if (selctionMeshHolder && mesh)
                    {
                        CapsuleCollider selectionCapsuleCollider = selctionMeshHolder.gameObject.GetComponent<CapsuleCollider>();
                        if (selectionCapsuleCollider)
                        {
                            selectionCapsuleCollider.enabled = false;
                        }
                        selectionCollider = selctionMeshHolder.gameObject.AddComponent<MeshCollider>();
                        selectionCollider.sharedMesh = mesh;
                        selectionCollider.convex = true;
                        selectionCollider.isTrigger = true;
                    }
                }
            }

            if (!string.IsNullOrEmpty(unitData.ship_data.dockedProp))
            {
                dockedPropName = unitData.ship_data.dockedProp;
                UnitManager.UnitJsonData.Prop propData = UnitManager.Instance.LoadPropJsonData(dockedPropName);
                if (propData != null)
                {
                    System.Action<Unit> dockSpawnAction = (unit) =>
                    {
                        PropUnit propUnit = unit as PropUnit;
                        propUnit.unitDataName = dockedPropName;
                        propUnit.transform.SetPositionAndRotation(transform.position, transform.rotation);
                        propUnit.spriteName = propData.graphics;
                    };
                    dockedProp = UnitManager.Instance.GetPropUnitFromPool(dockSpawnAction);
                }
            }

            movableUnit.movementComponent.SetState(MovementComponent.MovementFlag.IsWater);
            isDocked = true;
            isShipMode = true;
        }

        public void SetDockedMode(bool docked, bool dockAgainstShip = false)
        {
            if (!isShipMode) return;

            if (docked == isDocked) return;

            if (docked)
            {
                movableUnit.movementComponent.Stop();
                navMeshSurface.defaultArea = dockAgainstShip ? 4 : 0;
                navMeshSurface.BuildNavMesh();

                IEnumerator<IDeterministicYieldInstruction> DelayedLinkUpdate()
                {
                    yield return new DeterministicWaitForSeconds(0);

                    foreach (var navLink in navMeshLinks)
                    {
                        navLink.gameObject.SetActive(true);
                        navLink.UpdateLink();
                    }
                }
                foreach (var navLink in navMeshLinks)
                {
                    navLink.gameObject.SetActive(true);
                    navLink.UpdateLink();
                }
                DeterministicUpdateManager.Instance.CoroutineManager.StartCoroutine(DelayedLinkUpdate());
                isDocked = true;

                foreach (var unit in unitsOnShip)
                {
                    Vector3 lastPosition = unit.movementComponent.GetLastPointInPathfinding();
                    lastPosition = transform.InverseTransformPoint(lastPosition);
                    IEnumerator<IDeterministicYieldInstruction> DelayedMove()
                    {
                        yield return new DeterministicWaitForSeconds(0);

                        Transform parent = transform;
                        unit.movementComponent.StartPathfind(parent.TransformPoint(lastPosition));
                    }

                    // TODO: Add proper cleanup of timer
                    // DeterministicUpdateManager.Instance.timer.AddTimer(0, DelayedMove);
                    DeterministicUpdateManager.Instance.CoroutineManager.StartCoroutine(DelayedMove());
                    unit.transform.SetParent(null, true);
                    unit.movementComponent.Stop();
                    unit.movementComponent.rb.isKinematic = false;
                }

                UnitManager.UnitJsonData.Prop propData = UnitManager.Instance.LoadPropJsonData(dockedPropName);
                if (propData != null)
                {
                    System.Action<Unit> dockSpawnAction = (unit) =>
                    {
                        PropUnit propUnit = unit as PropUnit;
                        propUnit.unitDataName = dockedPropName;
                        propUnit.transform.SetPositionAndRotation(transform.position, transform.rotation);
                        propUnit.spriteName = propData.graphics;
                    };
                    dockedProp = UnitManager.Instance.GetPropUnitFromPool(dockSpawnAction);
                }

                // should be set kinematic here because movement stop command made it non-kinematic.
                rigidbody.isKinematic = true;
                navMeshObstacle.enabled = true;
            }
            else
            {
                navMeshObstacle.enabled = false;
                navMeshSurface.defaultArea = 4; // On ship surface
                navMeshSurface.BuildNavMesh();
                foreach (var navLink in navMeshLinks)
                {
                    navLink.gameObject.SetActive(false);
                    navLink.UpdateLink();
                }

                foreach (var unit in unitsOnShip)
                {
                    Vector3 lastPosition = unit.movementComponent.GetLastPointInPathfinding();
                    lastPosition = transform.InverseTransformPoint(lastPosition);
                    System.Action DelayedMove = () =>
                    {
                        Transform parent = transform;
                        unit.movementComponent.StartPathfind(parent.TransformPoint(lastPosition));
                    };

                    // TODO: Add proper cleanup of timer
                    DeterministicUpdateManager.Instance.timer.AddTimer(0, DelayedMove);
                    unit.transform.SetParent(transform, true);
                    unit.movementComponent.Stop();
                    unit.movementComponent.rb.isKinematic = true;
                }

                if (dockedProp)
                {
                    UnitManager.Instance.ReleasePropUnitFromPool(dockedProp);
                    dockedProp = null;
                }
                rigidbody.isKinematic = false;

                isDocked = false;
            }
        }

        public void Deinitialize(Transform transform, Rigidbody rigidbody)
        {
            // Remove navmesh data & links
            if (navMeshSurface != null)
            {
                navMeshSurface.RemoveData();
                navMeshSurface = null;
            }

            Transform visualTransform = transform.Find("Visual");
            if (visualTransform != null)
            {
                Transform spriteTransform = visualTransform.Find("SpriteGraphic");
                if (spriteTransform != null)
                {
                    SpriteRenderer spriteRenderer = spriteTransform.GetComponent<SpriteRenderer>();
                    spriteRenderer.sortingOrder = 1;
                }
            }

            if (selectionCollider)
            {
                Destroy(selectionCollider);
            }

            if (shipDeck)
            {
                Destroy(shipDeck);
            }

            rigidbody.isKinematic = false;

            Transform triggerHolder = transform.Find("Triggers");
            if (triggerHolder)
            {
                Transform checkCollider = triggerHolder.Find("CheckCollider");
                if (checkCollider)
                {
                    checkCollider.gameObject.SetActive(true);
                }

                Transform navMeshTransform = triggerHolder.Find("NavMesh");
                if (navMeshTransform)
                {
                    Destroy(navMeshTransform.gameObject);
                }
            }
            navMeshLinks.Clear();
            if (shipCollisionTrigger)
            {
                shipCollisionTrigger.OnTriggerEnterCallback -= OnShipColliderEnter;
                shipCollisionTrigger.OnTriggerExitCallback -= OnShipColliderExit;
                shipCollisionTrigger = null;
            }
            
            // Unlink units on ship
            foreach (var unit in unitsOnShip)
            {
                if (unit)
                {
                    unit.transform.SetParent(null, true);
                }
            }
            unitsOnShip.Clear();

            if (dockedProp)
            {
                UnitManager.Instance.ReleasePropUnitFromPool(dockedProp);
                dockedProp = null;
            }

            if (navMeshObstacle)
            {
                navMeshObstacle = null;
            }

            Transform selectionHolder = transform.Find("Selection");
            if (selectionHolder)
            {
                Transform selctionMeshHolder = selectionHolder.Find("SelectionCapsule");
                if (selctionMeshHolder)
                {
                    CapsuleCollider selectionCapsuleCollider = selctionMeshHolder.gameObject.GetComponent<CapsuleCollider>();
                    if (selectionCapsuleCollider)
                    {
                        selectionCapsuleCollider.enabled = true;
                    }
                    MeshCollider selectionMeshCollider = selctionMeshHolder.gameObject.GetComponent<MeshCollider>();
                    if (selectionMeshCollider)
                    {
                        Destroy(selectionMeshCollider);
                    }
                }
            }

            dockedPropName = null;
            isDocked = false;
            isShipMode = false;
            this.transform = null;
            movableUnit = null;
            this.rigidbody = null;
        }
    }

    public ShipData shipData;
    public MeshCollider meshCollider = null;

    private void Awake()
    {
        shipData = new ShipData();
        movementComponent = GetComponent<MovementComponent>();
    }

    public void ResetToDefaultModule()
    {
        SetAIModule(defaultModule, defaultAiModuleArgs.ToArray());
    }

    public void SetAIModule(UnitAIModule.AIModule newModuleType, params object[] aiArgs)
    {
        string moduleName = newModuleType.ToString();

        // Format args to string
        string formattedArgs = aiArgs == null || aiArgs.Length == 0
        ? "(no args)"
            : string.Join(", ", aiArgs.Select(arg => arg?.ToString() ?? "null"));

        NativeLogger.Log($"Unit {id} running the module {moduleName} and arguments {formattedArgs}");

        aiModule.enabled = false;
        Transform aiModuleTransform = aiTransformHolder.Find(moduleName);
        aiModule = aiModuleTransform.GetComponent<UnitAIModule>();

        switch (newModuleType)
        {
            case UnitAIModule.AIModule.BasicAttackAIModule:
                {
                    BasicAttackAIModule basicAttackAIModule = (BasicAttackAIModule)aiModule;
                    MovableUnit target = (MovableUnit)aiArgs[0];
                    bool autoSearchable = (bool)aiArgs[1];
                    bool isTargeted = false;
                    if (aiArgs.Length >= 3)
                    {
                        isTargeted = (bool)aiArgs[2];
                    }
                    basicAttackAIModule.InitializeAI(this, target, autoSearchable, isTargeted);
                }
                break;
            case UnitAIModule.AIModule.BasicMovementAIModule:
                {
                    Vector3 position = (Vector3)aiArgs[0];
                    ulong crowdId = (ulong)aiArgs[1];
                    Vector3 offset = Vector3.zero;
                    Vector3? startPosition = null;
                    if (aiArgs.Length == 3)
                    {
                        offset = (Vector3)aiArgs[2];
                    }
                    if (aiArgs.Length == 4)
                    {
                        offset = (Vector3)aiArgs[2];
                        startPosition = (Vector3)aiArgs[3];
                    }
                    BasicMovementAIModule basicMovementAIModule = (BasicMovementAIModule)aiModule;
                    basicMovementAIModule.InitializeAI(this, position, crowdId, offset, startPosition);
                }
                break;
            case UnitAIModule.AIModule.TargetFollowingMovementAIModule:
                {
                    TargetFollowingMovementAIModule targetFollowingMovementAIModule = (TargetFollowingMovementAIModule)aiModule;
                    MovableUnit target = (MovableUnit)aiArgs[0];
                    ulong crowdId = (ulong)aiArgs[1];
                    Vector3 offset = (Vector3)aiArgs[2];
                    targetFollowingMovementAIModule.InitializeAI(this, target, crowdId, offset);
                }
                break;
            case UnitAIModule.AIModule.AttackMoveAIModule:
                {
                    Vector3 position = (Vector3)aiArgs[0];
                    ulong crowdId = (ulong)aiArgs[1];
                    Vector3 offset = Vector3.zero;
                    Vector3? startPosition = null;
                    if (aiArgs.Length == 3)
                    {
                        offset = (Vector3)aiArgs[2];
                    }
                    if (aiArgs.Length == 4)
                    {
                        offset = (Vector3)aiArgs[2];
                        startPosition = (Vector3)aiArgs[3];
                    }
                    AttackMoveAIModule attackMoveAIModule = (AttackMoveAIModule)aiModule;
                    attackMoveAIModule.InitializeAI(this, position, crowdId, offset, startPosition);
                }
                break;
            //case UnitAIModule.AIModule.BasicShipAIModule:
            //    {
            //        IdleUnitAIModule basicShipUnitAIModule = (IdleUnitAIModule)aiModule;
            //        MovableUnit target = (MovableUnit)aiArgs[0];
            //        bool autoSearchable = (bool)aiArgs[1];
            //        bool isTargeted = false;
            //        if (aiArgs.Length >= 3)
            //        {
            //            isTargeted = (bool)aiArgs[2];
            //        }
            //        basicShipUnitAIModule.InitializeAI(this, target, autoSearchable, isTargeted);
            //    }
            //    break;
            default:
                break;
        }
    }

    public DeterministicVisualUpdater GetDeterministicVisualUpdater()
    {
        return DeterministicVisualUpdater;
    }

    public MovableUnit GetBoardedShip()
    {
        if (movementComponent.IsOnShip())
        {
            return transform.parent.GetComponent<MovableUnit>();
        }
        return null;
    }

    public bool IsShip()
    {
        return shipData != null && shipData.isShipMode;
    }

    public CustomSpriteLoader.IconReturnData GetSpriteIcon()
    {
        UnitManager.UnitJsonData militaryUnit = UnitManager.Instance.LoadUnitJsonData(unitDataName);
        if (militaryUnit == null)
        {
            NativeLogger.Error($"Failed to load unit from data {unitDataName}");
            return null;
        }
        CustomSpriteLoader.IconReturnData iconReturnData = CustomSpriteLoader.Instance.LoadIconSprite(militaryUnit.icon);
        return iconReturnData;
    }

    void LoadMovableData(string unitDataName, bool callVisualUpdate = false)
    {
        UnitManager.UnitJsonData militaryUnit = UnitManager.Instance.LoadUnitJsonData(unitDataName);
        if (militaryUnit == null)
        {
            NativeLogger.Error($"Failed to load unit from data {unitDataName}");
            return;
        }
        gameObject.tag = "Military Unit";

        if (militaryUnit.rotation_speed.HasValue)
        {
            movementComponent.rotationSpeed = militaryUnit.rotation_speed.Value;
        }
        else
        {
            movementComponent.rotationSpeed = 360.0f;
        }

        if (militaryUnit.use_steering.HasValue && militaryUnit.use_steering.Value == true)
        {
            movementComponent.SetState(MovementComponent.MovementFlag.UseSteering);
        }
        else
        {
            movementComponent.RemoveState(MovementComponent.MovementFlag.UseSteering);
        }

        statComponent.SetHealth(militaryUnit.hp);
        standSprite = militaryUnit.standing;
        walkSprite = militaryUnit.walking;

        if (militaryUnit.collisionData == null)
        {
            if (meshCollider)
                Destroy(meshCollider);
            CapsuleCollider capsuleCollider = GetComponent<CapsuleCollider>();
            capsuleCollider.enabled = true;
            movementComponent.solidCollider = capsuleCollider;
        } 
        else
        {
            if (!string.IsNullOrEmpty(militaryUnit.collisionData.name))
            {
                AssimpMeshLoader.MeshReturnData meshReturnData = AssimpMeshLoader.Instance.LoadMeshFromAssimp(militaryUnit.collisionData.name);
                float size = militaryUnit.collisionData.size == null ? 1 : militaryUnit.collisionData.size.Value;
                UnityEngine.Mesh mesh = AssimpMeshLoader.ScaleMesh(meshReturnData.mesh, size);
                meshCollider = gameObject.AddComponent<MeshCollider>();
                meshCollider.sharedMesh = mesh;
                meshCollider.convex = true;

                movementComponent.solidCollider = meshCollider;
                CapsuleCollider capsuleCollider = GetComponent<CapsuleCollider>();
                capsuleCollider.enabled = false;
            }
        }

        if (militaryUnit.ship_data == null)
        {
            shipData.Deinitialize(transform, _rigidbody);
        }
        else
        {
            shipData.Initialize(militaryUnit, transform, _rigidbody, this);
        }

        if (militaryUnit.lockedAngle == null)
        {
            movementComponent.RemoveState(MovementComponent.MovementFlag.LockedRotation);
            movementComponent.directionCount = 8;
        }
        else
        {
            movementComponent.SetState(MovementComponent.MovementFlag.LockedRotation);
            movementComponent.directionCount = militaryUnit.lockedAngle.directionCount;
        }

        if (movementComponent)
        {
            movementComponent.movementSpeed = militaryUnit.movement_speed;
        }

        UnitManager.UnitJsonData.DamageData damageData = militaryUnit.damageData;
        if (statComponent)
        {
            statComponent.damageData = damageData;
            statComponent.SetHealth(militaryUnit.hp);
        }

        CombatComponent combatComponent = unitTypeComponent as CombatComponent;
        if (combatComponent)
        {
            combatComponent.attackSprite = militaryUnit.attacking;
            combatComponent.attackRange = militaryUnit.attack_range == null ? 0.0f : militaryUnit.attack_range.Value;
            combatComponent.attackDelay = militaryUnit.attack_delay;
            combatComponent.actionEvents.Clear();
            combatComponent.projectile_offset = (Vector3?)militaryUnit.projectile_offset;
            combatComponent.projectile_unit = militaryUnit.projectile_unit;

            if (militaryUnit.combatActionEvents != null)
            {
                for (int i = 0; i < militaryUnit.combatActionEvents.Count; i++)
                {
                    UnitManager.UnitJsonData.CombatActionEvent eventData = militaryUnit.combatActionEvents[i];
                    if (System.Enum.TryParse(eventData.eventType, out UnitEventHandler.EventID eventID))
                    {
                        switch (eventID)
                        {
                            case UnitEventHandler.EventID.OnAttack:
                            case UnitEventHandler.EventID.OnProjectileAttack:
                                {
                                    ActionComponent.ActionEvent attackEvent
                                        = new ActionComponent.ActionEvent(eventData.time, eventID, new List<object>() { id, 0, damageData });
                                    combatComponent.actionEvents.Add(attackEvent);
                                }
                                break;
                        }
                    }
                }
            }
        }

        if (callVisualUpdate) {
            System.Action action = () =>
            {
                if (DeterministicVisualUpdater)
                {
                    DeterministicVisualUpdater.SetSpriteName(standSprite, true);
                    DeterministicVisualUpdater.PlayOrResume(true);
                    DeterministicVisualUpdater.playerId = playerId;
                    DeterministicVisualUpdater.RefreshVisuals();
                }
            };
            //if (!IsShip())
            {
                defaultModule = UnitAIModule.AIModule.BasicAttackAIModule;
                defaultAiModuleArgs = new List<object>() { null, true };
            }
            //else
            //{
            //    defaultModule = UnitAIModule.AIModule.BasicShipAIModule;
            //    defaultAiModuleArgs = new List<object> { null, true };
            //}
            ResetToDefaultModule();
            DeterministicUpdateManager.Instance.timer.AddTimer(0.2f, action);
        }
    }

    public void ResetUnit(bool avoidActionReset = false, bool stopPhysicsToo = false)
    {
        if (movementComponent)
        {
            movementComponent.Stop(stopPhysicsToo);
            movementComponent.SetTargetToIgnore(null);
        }
        if (actionComponent)
        {
            if (!avoidActionReset)
                actionComponent.StopAction();
            foreach (var action in actionComponent.actions)
            {
                if (action.eventId == UnitEventHandler.EventID.OnAttack && action.parameters.Length == 3)
                {
                    action.parameters[1] = (ulong)0;
                }
            }
        }
        //if (aiModule)
        //{
        //    if (!StatComponent.IsUnitAliveOrValid(this))
        //        aiModule.enabled = false;
        //}
    }

    private void OnEnable()
    {
        OnRelease = (id) => { };
        Initialize();
        LoadMovableData(unitDataName, true);
        UnitManager.Instance.spatialHashGrid.Register(this);
        lastGridCell = SpatialHashGrid.GetCell(transform.position);

        DeterministicUpdateManager.Instance.Register(this);
        if (movementComponent)
        {
            movementComponent.OnMovementStateChangeCallback += MovementComponent_OnMovementStateChange;
            movementComponent.OnMoving += MovementComponent_OnMoving;
        }
    }

    void MovementComponent_OnMovementStateChange(MovementComponent.State state)
    {
        if (state == MovementComponent.State.Idle)
        {
            MovementComponent_OnStopMoving();
        }
        else if (state == MovementComponent.State.Moving)
        {
            MovementComponent_OnStartMoving();
        }
    }

    private void OnDisable()
    {
        DeterministicUpdateManager.Instance.Register(this);
        if (movementComponent)
        {
            movementComponent.OnMovementStateChangeCallback += MovementComponent_OnMovementStateChange;
            movementComponent.OnMoving -= MovementComponent_OnMoving;
        }

        //transform.SetParent(null, true);

        UpdateGridCell();

        UnitManager.Instance.spatialHashGrid.Unregister(this);
    }

    private void MovementComponent_OnMoving()
    {
        if (movementComponent.movementState == MovementComponent.State.Moving &&
            DeterministicVisualUpdater.spriteName != walkSprite &&
            !actionComponent.IsPlayingAction())
        {
            DeterministicVisualUpdater.SetSpriteName(walkSprite, true);
        }
    }

    private void MovementComponent_OnStopMoving()
    {
        if (DeterministicVisualUpdater)
        {
            string sprite = standSprite;
            DeterministicVisualUpdater.SetSpriteName(standSprite, true);
            DeterministicVisualUpdater.PlayOrResume(false);
        }
        if (shipData != null && shipData.isShipMode)
        {
            if (shipData.checkUndockableAtStop && !shipData.isDocked)
            {
                // Do some check to find undockable spot. As in proper ground. Find friendly navmesh at navmesh links perhaps??
                IEnumerator<IDeterministicYieldInstruction> DelayedDocking()
                {
                    yield return new DeterministicWaitForSeconds(0);
                    if (!movementComponent.AnyPathOperationInProgress())
                    {
                        foreach (var navlink in shipData.navMeshLinks)
                        {
                            int navAreaMask = 1;
                            if (NavMesh.SamplePosition(navlink.transform.TransformPoint(navlink.endPoint), out NavMeshHit navHit, navlink.width, navAreaMask))
                            {
                                DebugExtension.DebugWireSphere(navHit.position, Color.green, 0.2f, 5f);
                                shipData.SetDockedMode(true);
                                break;
                            }
                        }
                    }
                }
                DeterministicUpdateManager.Instance.CoroutineManager.StartCoroutine(DelayedDocking());
            }
            shipData.checkUndockableAtStop = false;
        }
    }

    private void MovementComponent_OnStartMoving()
    {
        if (DeterministicVisualUpdater)
        {
            DeterministicVisualUpdater.SetSpriteName(walkSprite, true);
            DeterministicVisualUpdater.PlayOrResume(false);
        }
    }

    public override bool IsSelectable()
    {
        if (IsShip())
        {
            if (shipData.unitsOnShip.Count > 0)
            {
                return true;
            }
            return false;
        }
        return true;
    }

    public new void Load(MapLoader.SaveLoadData data)
    {
        MovableUnitData movableUnitData = data as MovableUnitData;
        UnitManager.Instance.ForceRegister(this, movableUnitData.id);
        unitDataName = movableUnitData.unitDataName;
        lastGridCell = (Vector2Int)movableUnitData.lastGridCell;
        LoadMovableData(unitDataName);
        transform.position = (Vector3)movableUnitData.position;
        transform.eulerAngles = (Vector3)movableUnitData.eulerAngles;
        enabled = movableUnitData.enabled;

        foreach (var c in movableUnitData.components)
        {
            switch (c.type)
            {
                case "DeterministicVisualUpdaterData":
                    DeterministicVisualUpdater.Load(c);
                    break;
                case "MovementComponentData":
                    movementComponent.Load(c);
                    break;
                default:
                    break;
            }
        }
        UnitManager.Instance.ForceRegister(this, movableUnitData.id);
    }

    public new void PostLoad(MapLoader.SaveLoadData data)
    {
        MovableUnitData movableUnitData = data as MovableUnitData;
        foreach (var c in movableUnitData.components)
        {
            switch (c.type)
            {
                case "DeterministicVisualUpdaterData":
                    DeterministicVisualUpdater.PostLoad(c);
                    break;
                case "MovementComponentData":
                    movementComponent.PostLoad(c);
                    break;
                default:
                    break;
            }
        }
        UnitManager.Instance.ForceRegister(this, movableUnitData.id);
    }

    MapLoader.SaveLoadData MapLoader.IMapSaveLoad.Save()
    {
        MovableUnitData movableUnitData = new MovableUnitData
        {
            id = id,
            unitDataName = unitDataName,
            position = (CommonStructures.SerializableVector3)transform.position,
            eulerAngles = (CommonStructures.SerializableVector3)transform.eulerAngles,
            enabled = this.enabled,
            lastGridCell = (CommonStructures.SerializableVector2Int)lastGridCell
        };

        // Ensure components do not create loops
        List<MapLoader.SaveLoadData> components = new List<MapLoader.SaveLoadData>();

        var visualUpdaterData = DeterministicVisualUpdater.Save();
        if (visualUpdaterData != null)
            components.Add(visualUpdaterData);

        var movementData = movementComponent.Save();
        if (movementData != null)
            components.Add(movementData);

        movableUnitData.components = components;

        return movableUnitData;
    }

    public void DeterministicUpdate(float deltaTime, ulong tickID)
    {
        UpdateGridCell();
    }

    public void UpdateGridCell()
    {
        Vector2Int newGridCell = SpatialHashGrid.GetCell(transform.position);
        if (lastGridCell != newGridCell)
        {
            UnitManager.Instance.spatialHashGrid.UpdateUnit(this, lastGridCell);
            lastGridCell = newGridCell;
        }
    }
}
