using Newtonsoft.Json;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static PathfinderTest;
using static Unit;
using static UnityEngine.GraphicsBuffer;
using static UnityEngine.Rendering.GPUSort;

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

    public UnitTypeComponent unitTypeComponent;
    public UnitAIModule aiModule;

    public Transform aiTransformHolder;

    public UnitAIModule.AIModule defaultModule = UnitAIModule.AIModule.BasicAttackAIModule;
    public List<object> defaultAiModuleArgs = new List<object>() { null, true };

    [SerializeField] DeterministicVisualUpdater DeterministicVisualUpdater;

    int actionBlock = 0;

    Vector2Int lastGridCell;

    public string standSprite = "idle_archer";

    public string walkSprite = "move_archer";

    public void IncrementActionBlock()
    {
        actionBlock++;
    }

    public void DecrementActionBlock()
    {
        actionBlock--;
    }

    private void Awake()
    {
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
                    basicAttackAIModule.InitializeAI(this, target, autoSearchable);
                }
                break;
            case UnitAIModule.AIModule.BasicMovementAIModule:
                {
                    Vector3 position = (Vector3)aiArgs[0];
                    ulong crowdId = (ulong)aiArgs[1];

                    BasicMovementAIModule basicMovementAIModule = (BasicMovementAIModule)aiModule;
                    basicMovementAIModule.InitializeAI(this, position, crowdId);
                }
                break;
            default:
                break;
        }
    }

    public DeterministicVisualUpdater GetDeterministicVisualUpdater()
    {
        return DeterministicVisualUpdater;
    }

    void LoadMovableData(string unitDataName, bool callVisualUpdate = false)
    {
        UnitManager.MilitaryUnit militaryUnit = UnitManager.Instance.LoadMilitaryUnit(unitDataName);
        gameObject.tag = "Military Unit";
        statComponent.SetHealth(militaryUnit.hp);
        standSprite = militaryUnit.standing;
        walkSprite = militaryUnit.walking;

        if (movementComponent)
        {
            movementComponent.movementSpeed = militaryUnit.movement_speed;
        }

        CombatComponent combatComponent = unitTypeComponent as CombatComponent;
        if (combatComponent)
        {
            combatComponent.attackSprite = militaryUnit.attacking;
            combatComponent.damage = militaryUnit.damage;
            combatComponent.attackRange = militaryUnit.attack_range;
            combatComponent.attackDelay = militaryUnit.attack_delay;
            combatComponent.actionEvents.Clear();

            for (int i = 0; i < militaryUnit.combatActionEvents.Count; i++)
            {
                UnitManager.MilitaryUnit.CombatActionEvent eventData = militaryUnit.combatActionEvents[i];
                if (System.Enum.TryParse(eventData.eventType, out UnitEventHandler.EventID eventID))
                {
                    ActionComponent.ActionEvent attackEvent 
                        = new ActionComponent.ActionEvent(eventData.time, eventID, new List<object>() { id, 0, militaryUnit.damage });
                    combatComponent.actionEvents.Add(attackEvent);
                }
            }
        }

        //if (actionComponent)
        //{
        //    List<ActionComponent.ActionEvent> actionEvents = new List<ActionComponent.ActionEvent>() { 
        //        // ulong selfId = (ulong)obj[0];
        //        // ulong targetId = (ulong)obj[1];
        //        // float damage = (float)obj[2];
        //        new ActionComponent.ActionEvent(militaryUnit.attack_delay, UnitEventHandler.EventID.OnAttack, new List<object>() { id, 0, militaryUnit.damage })
        //    };
        //    actionComponent.SetActionSprite(militaryUnit.attacking, "", actionEvents);
        //}

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

            ResetToDefaultModule();
            //Transform BasicAttackAIModuleTransform = aiTransformHolder.Find("BasicAttackAIModule");
            //if (aiModule && aiModule.GetType() == typeof(BasicAttackAIModule))
            //{
            //    BasicAttackAIModule basicAttackAIModule = (BasicAttackAIModule)aiModule;
            //    basicAttackAIModule.InitializeAI(this, null, true);
            //}
            DeterministicUpdateManager.Instance.timer.AddTimer(0.2f, action);
        }
    }

    public void ResetUnit(bool avoidActionReset = false)
    {
        if (movementComponent)
        {
            movementComponent.Stop();
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
        Initialize();
        LoadMovableData(unitDataName, true);
        UnitManager.Instance.spatialHashGrid.Register(this);
        lastGridCell = SpatialHashGrid.GetCell(transform.position);

        DeterministicUpdateManager.Instance.Register(this);
        if (movementComponent)
        {
            movementComponent.OnStartMoving += MovementComponent_OnStartMoving;
            movementComponent.OnStopMoving += MovementComponent_OnStopMoving;
            movementComponent.OnMoving += MovementComponent_OnMoving;
        }
    }

    private void OnDisable()
    {
        DeterministicUpdateManager.Instance.Register(this);
        if (movementComponent)
        {
            movementComponent.OnStartMoving -= MovementComponent_OnStartMoving;
            movementComponent.OnStopMoving -= MovementComponent_OnStopMoving;
            movementComponent.OnMoving -= MovementComponent_OnMoving;
        }
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
        //UnitManager.Instance.spatialHashGrid.UpdateUnit(gameObject);
    }

    private void MovementComponent_OnStopMoving()
    {
        if (DeterministicVisualUpdater)
        {
            string sprite = standSprite;
            if (actionBlock > 0)
                sprite = actionComponent.spriteName;
            DeterministicVisualUpdater.SetSpriteName(standSprite, true);
            DeterministicVisualUpdater.PlayOrResume(false);
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
        //MovableUnitData movableUnitData = new MovableUnitData
        //{
        //    id = id,
        //    unitDataName = unitDataName,
        //    position = (CommonStructures.SerializableVector3)transform.position,
        //    eulerAngles = (CommonStructures.SerializableVector3)transform.eulerAngles,
        //};
        //
        //List<MapLoader.SaveLoadData> components = new List<MapLoader.SaveLoadData>
        //{
        //    DeterministicVisualUpdater.Save(),
        //    movementComponent.Save()
        //};
        //movableUnitData.components.AddRange(components);
        //
        //return movableUnitData;

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
        if (actionBlock > 0)
        {
            if (movementComponent)
            {
                movementComponent.Stop(false);
            }
        }

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
