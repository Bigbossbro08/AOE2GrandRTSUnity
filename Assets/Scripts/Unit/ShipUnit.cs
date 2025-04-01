using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;
using static MapLoader;
using static MovableUnit;
using static ShipUnit;

public class ShipUnit : Unit, MapLoader.IMapSaveLoad
{
    [System.Serializable]
    public class ShipUnitData : UnitData
    {
        public bool canBeAutoReversable;
        public ShipUnitData() { type = "ShipUnitData"; }
    }

    public MovementComponent movementComponent;

    public bool canBeAutoReversable = false;
    
    [SerializeField]
    ShipSurfaceController shipSurfaceController;
    
    [SerializeField]
    ShipDockingHandler dockingHandler;

    private void OnEnable()
    {
        Initialize();
        gameObject.tag = "Ship Unit";
    }

    private void OnDisable()
    {
        OnRemove();
    }

    public void StartPathfind(Vector3 position)
    {
        dockingHandler.enabled = false;
        if (movementComponent == null) return;
        shipSurfaceController.SetShipState(false);
        movementComponent.StartPathfind(position);
        if (canBeAutoReversable)
        {
            movementComponent.SetState(MovementComponent.MovementFlag.IsAutoReverseable);
        } else
        {
            movementComponent.RemoveState(MovementComponent.MovementFlag.IsAutoReverseable);
        }
    }

    public void DockAt(Vector3 position, Vector3 targetToDock)
    {
        if (dockingHandler == null) return;
        dockingHandler.enabled = true;
        if (movementComponent == null) return;
        shipSurfaceController.SetShipState(false);
        dockingHandler.SetTargetPointToDock(targetToDock);
        movementComponent.SetState(MovementComponent.MovementFlag.IsAutoReverseable);
        movementComponent.StartPathfind(position);
    }

    public new void Load(MapLoader.SaveLoadData data)
    {
        ShipUnitData shipUnitData = data as ShipUnitData;
        unitDataName = shipUnitData.unitDataName;
        transform.position = (Vector3)shipUnitData.position;
        transform.eulerAngles = (Vector3)shipUnitData.eulerAngles;
        //enabled = shipUnitData.enabled;

        foreach (var c in shipUnitData.components)
        {
            switch (c.type)
            {
                case "ShipSurfaceControllerData":
                    shipSurfaceController.Load(c);
                    break;
                case "ShipDockingHandlerData":
                    dockingHandler.Load(c);
                    break;
                case "MovementComponentData":
                    movementComponent.Load(c);
                    break;
                default:
                    break;
            }
        }
        UnitManager.Instance.ForceRegister(this, shipUnitData.id);
    }

    public new void PostLoad(SaveLoadData data)
    {
        ShipUnitData shipUnitData = data as ShipUnitData;

        foreach (var c in shipUnitData.components)
        {
            switch (c.type)
            {
                case "ShipSurfaceControllerData":
                    shipSurfaceController.PostLoad(c);
                    break;
                case "ShipDockingHandlerData":
                    dockingHandler.PostLoad(c);
                    break;
                case "MovementComponentData":
                    movementComponent.PostLoad(c);
                    break;
                default:
                    break;
            }
        }
    }

    MapLoader.SaveLoadData MapLoader.IMapSaveLoad.Save()
    {
        // Ensure components do not create loops
        List<MapLoader.SaveLoadData> components = new List<MapLoader.SaveLoadData>();

        ShipUnitData shipUnitData = new ShipUnitData
        {
            id = id,
            unitDataName = unitDataName,
            position = (CommonStructures.SerializableVector3)transform.position,
            eulerAngles = (CommonStructures.SerializableVector3)transform.eulerAngles,
            canBeAutoReversable = this.canBeAutoReversable,
        };

        if (movementComponent)
        {
            components.Add(movementComponent.Save());
        }

        if (shipSurfaceController)
        {
            components.Add(shipSurfaceController.Save());
        }

        if (dockingHandler)
        {
            components.Add(dockingHandler.Save());
        }

        shipUnitData.components = components;

        //MapLoader.IMapSaveLoad[] adjacentComponents = GetComponents<MapLoader.IMapSaveLoad>();
        //foreach (var c in adjacentComponents)
        //{
        //    c.Save();
        //}
        return shipUnitData;
    }
}
