using Newtonsoft.Json;
using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;
using static PathfinderTest;
using static Unit;

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

    // TEMP
    public BasicAttackAIModule basicAttackAIModule;

    public MovementComponent movementComponent;
    public ActionComponent combatComponent;

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
        if (combatComponent)
        {
            combatComponent.SetActionSprite(militaryUnit.attacking);
        }

        if (callVisualUpdate) {
            System.Action action = () =>
            {
                if (DeterministicVisualUpdater)
                {
                    DeterministicVisualUpdater.SetSpriteName(standSprite, true);
                    DeterministicVisualUpdater.PlayOrResume(true);
                }
            };

            DeterministicUpdateManager.Instance.timer.AddTimer(0.2f, action);
        }
    }

    public void ResetUnit()
    {
        if (movementComponent)
        {
            movementComponent.Stop();
        }
        if (combatComponent)
        {
            combatComponent.StopAction();
        }
        if (basicAttackAIModule)
        {
            basicAttackAIModule.enabled = false;
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
    }

    private void MovementComponent_OnMoving()
    {
        if (movementComponent.movementState == MovementComponent.State.Moving &&
            DeterministicVisualUpdater.spriteName != walkSprite &&
            !combatComponent.IsPlayingAction())
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
                sprite = combatComponent.spriteName;
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
