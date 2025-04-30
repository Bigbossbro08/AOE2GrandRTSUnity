#define USE_ADD_NAVMESH_DATA
#undef USE_ADD_NAVMESH_DATA

using System.Linq;
using System.Collections.Generic;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;
using UnityEditor;
using System.Collections;
using Newtonsoft.Json;
using static PathfinderTest;

public class ShipSurfaceController : MonoBehaviour, MapLoader.IMapSaveLoad
{
    [SerializeField]
    public class ShipSurfaceControllerData : MapLoader.SaveLoadData
    {
        [JsonProperty] public bool m_isDocked;
        [JsonProperty] public List<ulong> unitIds = new List<ulong>();

        public ShipSurfaceControllerData() { type = "ShipSurfaceControllerData"; }
    }

    private bool m_isDocked = false;

    public bool IsDocked { get { return m_isDocked; } }
    public NavMeshSurface shipSurface;

    public MovementComponent movementComponent;

    [SerializeField]
    GameObject navMeshObject;

    Dictionary<ulong, Unit> units = new Dictionary<ulong, Unit>();

    [SerializeField]
    List<NavMeshLink> navMeshLinks = new List<NavMeshLink>();

    void BakeNavMeshArea()
    {
        navMeshObject.SetActive(true);
        if (m_isDocked)
        {
            if (shipSurface)
            {
                shipSurface.defaultArea = 0;
                shipSurface.BuildNavMesh();
            }
        }
        else
        {
            if (shipSurface)
            {
                shipSurface.defaultArea = 4;
                shipSurface.BuildNavMesh();
            }
        }

        navMeshObject.SetActive(false);
    }

    public void SetShipState(bool isDocked)
    {
        if (this.IsDocked == isDocked) return;

        if (isDocked)
        {
            if (movementComponent)
            {
                movementComponent.Stop();
            }

            foreach (var u in units)
            {
                if (u.Value is MovableUnit movableUnit)
                {
                    Vector3 lastPosition = movableUnit.movementComponent.GetLastPointInPathfinding();
                    lastPosition = movementComponent.transform.InverseTransformPoint(lastPosition);
                    movableUnit.movementComponent.Stop();
                    System.Action action = () =>
                    {
                        DelayedMove(movableUnit.movementComponent, movementComponent.transform, lastPosition);
                    };
                    DeterministicUpdateManager.Instance.timer.AddTimer(0, action);

                    movableUnit.transform.SetParent(null);
                }
            }

            foreach (NavMeshLink navMeshLink in navMeshLinks)
            {
                navMeshLink.gameObject.SetActive(true);
                navMeshLink.UpdateLink();
            }
        }
        else
        {
            foreach (var u in units)
            {
                if (u.Value is MovableUnit movableUnit)
                {
                    Debug.Log($"Undocking: {movableUnit.transform.name}");
                    if (movableUnit.TryGetComponent(out MovableUnit unit))
                    {
                        Vector3 lastPosition = unit.movementComponent.GetLastPointInPathfinding();
                        lastPosition = movementComponent.transform.InverseTransformPoint(lastPosition);
                        unit.movementComponent.Stop();
                        System.Action action = () =>
                        {
                            DelayedMove(unit.movementComponent, movementComponent.transform, lastPosition);
                        };

                        // TODO: Add proper cleanup of timer
                        DeterministicUpdateManager.Instance.timer.AddTimer(0, action);
                        //unit.movementComponent.LocalizeCurrentPathfindingPositionForShip(movementComponent.transform);
                    }
                    movableUnit.transform.SetParent(transform.root, true);
                }
            }

            foreach (NavMeshLink navMeshLink in navMeshLinks)
            {
                navMeshLink.gameObject.SetActive(false);
            }
        }
        this.m_isDocked = isDocked;
        BakeNavMeshArea();
    }

    void DelayedMove(MovementComponent moveComp, Transform parent, Vector3 position)
    {
        //action?.Invoke();
        moveComp.StartPathfind(parent.TransformPoint(position));

        Debug.Log("Started pathfinding again after on ship");
    }

    //private void FixedUpdate()
    //{
    //    if (IsDocked)
    //    {
    //        foreach (NavMeshLink link in navMeshLinks)
    //        {
    //            link.UpdateLink();
    //        }
    //    }
    //}

    private void OnTriggerEnter(Collider other)
    {
        //Debug.LogWarning(other.name);
        NativeLogger.Info(other.name, true);
        if (other.TryGetComponent(out MovableUnit unit))
        {
            units.TryAdd(unit.id, unit);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        NativeLogger.Info(other.name);
        if (other.TryGetComponent(out MovableUnit unit) && units.ContainsKey(unit.id))
        {
            units.Remove(unit.id);
            NativeLogger.Info("Removed unit: " + unit.name);
        }
        //Debug.Log($"{other.gameObject.name} has exited the ship");
        //if (units.Contains(other.transform.root.gameObject))
        //{
        //    units.Remove(other.transform.root.gameObject);
        //}
    }

    public void Load(MapLoader.SaveLoadData data)
    {
        ShipSurfaceControllerData shipSurfaceControllerData = data as ShipSurfaceControllerData;
        m_isDocked = shipSurfaceControllerData.m_isDocked;

        foreach (NavMeshLink navMeshLink in navMeshLinks)
        {
            navMeshLink.gameObject.SetActive(shipSurfaceControllerData.m_isDocked);
        }
        BakeNavMeshArea();
    }

    public void PostLoad(MapLoader.SaveLoadData data)
    {
        ShipSurfaceControllerData shipSurfaceControllerData = data as ShipSurfaceControllerData;
        if (shipSurfaceControllerData.m_isDocked)
        {
            foreach (NavMeshLink navMeshLink in navMeshLinks)
            {
                navMeshLink.gameObject.SetActive(true);
                navMeshLink.UpdateLink();
            }
        }
        else
        {
            foreach (var id in shipSurfaceControllerData.unitIds)
            {
                Unit unit = UnitManager.Instance.GetUnit(id);
                if (unit is MovableUnit movableUnit)
                {
                    movableUnit.transform.SetParent(transform.root);
                    units.Add(id, movableUnit);
                }
            }

            foreach (NavMeshLink navMeshLink in navMeshLinks)
            {
                navMeshLink.gameObject.SetActive(false);
            }
        }
    }

    public MapLoader.SaveLoadData Save()
    {
        List<ulong> unitIds = new List<ulong>();
        foreach (var u in units)
        {
            unitIds.Add(u.Key);
        }
        ShipSurfaceControllerData shipSurfaceControllerData = new ShipSurfaceControllerData
        {
            m_isDocked = m_isDocked,
            unitIds = unitIds
        };
        return shipSurfaceControllerData;
    }
}
