using Newtonsoft.Json;
using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;
using static PathfinderTest;
using static Unit;
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

    public UnitTypeComponent unitTypeComponent;
    public UnitAIModule aiModule;

    public Transform aiTransformHolder;

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

    public DeterministicVisualUpdater GetDeterministicVisualUpdater()
    {
        return DeterministicVisualUpdater;
    }

    void LoadMovableData(string unitDataName, bool callVisualUpdate = false)
    {
        UnitManager.MilitaryUnit militaryUnit = UnitManager.Instance.LoadMilitaryUnit(unitDataName);
        gameObject.tag = "Military Unit";
        standSprite = militaryUnit.standing;
        walkSprite = militaryUnit.walking;
        
        if (actionComponent)
        {
            List<ActionComponent.ActionEvent> actionEvents = new List<ActionComponent.ActionEvent>() { 
                // ulong selfId = (ulong)obj[0];
                // ulong targetId = (ulong)obj[1];
                // float damage = (float)obj[2];
                new ActionComponent.ActionEvent(1.0f, UnitEventHandler.EventID.OnAttack, new List<object>() { id, 0, 6.0f })
            };
            actionComponent.SetActionSprite(militaryUnit.attacking, "", actionEvents);
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

            Transform BasicAttackAIModuleTransform = aiTransformHolder.Find("BasicAttackAIModule");
            if (aiModule && aiModule.GetType() == typeof(BasicAttackAIModule))
            {
                BasicAttackAIModule basicAttackAIModule = (BasicAttackAIModule)aiModule;
                basicAttackAIModule.InitializeAI(this, null, true);
            }
            DeterministicUpdateManager.Instance.timer.AddTimer(0.2f, action);
        }
    }

    // TODO: Ensure how to reset back to its own default state as well. Like movement AI should fall back to unit's native AI
    public void ChangeAIModule(UnitAIModule.AIModule newModuleType)
    {
        // First clear up old AI Module
        // Then add new AI Module
        switch (newModuleType)
        {
            case UnitAIModule.AIModule.BasicAttackAIModule:
                break;
            default:
                break;
        }
    }

    public void ResetUnit()
    {
        if (movementComponent)
        {
            movementComponent.Stop();
            movementComponent.SetTargetToIgnore(null);
        }
        if (actionComponent)
        {
            actionComponent.StopAction();
            foreach (var action in actionComponent.actions)
            {
                if (action.eventId == UnitEventHandler.EventID.OnAttack && action.parameters.Length == 3)
                {
                    action.parameters[1] = 0;
                }
            }
        }
        if (aiModule)
        {
            if (!StatComponent.IsUnitAliveOrValid(this))
                aiModule.enabled = false;
        }
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

        Vector2Int newGridCell = SpatialHashGrid.GetCell(transform.position);
        if (lastGridCell != newGridCell)
        {
            UnitManager.Instance.spatialHashGrid.UpdateUnit(this, lastGridCell);
            lastGridCell = newGridCell;
        }
    }
}
