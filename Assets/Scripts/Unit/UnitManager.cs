using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Pool;
using static TMPro.SpriteAssetUtilities.TexturePacker_JsonArray;

[RequireComponent(typeof(SpatialHashGrid))]
public class UnitManager : MonoBehaviour
{
    public class MilitaryUnit
    {
        public string standing = "archer_standing";
        public string walking = "archer_walking";
        public string attacking = "archer_attacking";
        public string dying = "archer_dying";
        public string corpse = "archer_corpse";
    }

    public static UnitManager Instance { get; private set; }

    public SpatialHashGrid spatialHashGrid;

    public static ulong counter = 0;
    public static ulong crowdIDCounter = 0;

    public ObjectPool<MovableUnit> movableUnitPool;
    public ObjectPool<ShipUnit> shipUnitPool;

    Dictionary<ulong, Unit> units = new Dictionary<ulong, Unit>(5000);

    public MovableUnit movableUnitPrefab;
    public ShipUnit shipUnitPrefab;

    public string dataPath = "E:\\repos\\AOE2GrandRTSUnityFiles\\data";

    private void Awake()
    {
        dataPath = Path.Combine(MapLoader.GetDataPath(), "data");

        // If there is an instance, and it's not me, delete myself.
        if (Instance != null && Instance != this)
        {
            Destroy(this);
        }
        else
        {
            Instance = this;
        }

        counter = 0;
        crowdIDCounter = 0;
    }

    private void Start()
    {
        movableUnitPool = new ObjectPool<MovableUnit>(SpawnMovableUnit, GetMovableUnitfromPool, ReleaseMovableUnitfromPool, DestroyMovableUnitfromPool, false, 200, 5000);
        shipUnitPool = new ObjectPool<ShipUnit>(SpawnShipUnit, GetShipUnitFromPool, ReleaseShipUnitFromPool, DestroyShipUnitFromPool);
    }

    #region Ship
    private ShipUnit SpawnShipUnit()
    {
        ShipUnit shipUnit = Instantiate(shipUnitPrefab);
        return shipUnit;
    }

    private void GetShipUnitFromPool(ShipUnit unit)
    {
        unit.gameObject.SetActive(true);
    }

    private void ReleaseShipUnitFromPool(ShipUnit unit)
    {
        if (units.ContainsKey(unit.id))
        {
            units.Remove(unit.id);
        }
        unit.gameObject.SetActive(false);
    }

    private void DestroyShipUnitFromPool(ShipUnit unit)
    {
        if (units.ContainsKey(unit.id))
        {
            units.Remove(unit.id);
        }
        Destroy(unit);
    }
    #endregion

    #region MovableUnit
    private void GetMovableUnitfromPool(MovableUnit unit)
    {
        unit.gameObject.SetActive(true);
    }

    private MovableUnit SpawnMovableUnit()
    {
        MovableUnit movableUnit = Instantiate(movableUnitPrefab);
        return movableUnit;
    }

    private void ReleaseMovableUnitfromPool(MovableUnit unit)
    {
        if (units.ContainsKey(unit.id))
        {
            units.Remove(unit.id);
        }
        unit.gameObject.SetActive(false);
        //movableUnitPool.Release(unit);
    }

    private void DestroyMovableUnitfromPool(MovableUnit unit)
    {
        if (units.ContainsKey(unit.id))
        {
            units.Remove(unit.id);
        }
        Destroy(unit);
    }
    #endregion

    public ulong Register(Unit unit)
    {
        ulong id = counter++;
        if (!units.ContainsKey(id))
        {
            units.Add(id, unit);
            return id;
        }
        Debug.LogWarning("Key is added already");
        return id;
    }

    public void ForceRegister(Unit unit, ulong id)
    {
        if (unit == null) return;

        // Remove previously appointed key
        if (units.ContainsKey(unit.id))
        {
            units.Remove(unit.id);
        }

        if (units.ContainsKey(id))
        {
            units.Remove(id);
        }
        unit.id = id;
        units.Add(id, unit);
    }

    public Unit GetUnit(ulong id)
    {
        if (units.TryGetValue(id, out Unit unit)) return unit;
        return null;
    }
    public Dictionary<ulong, Unit> GetAllUnits()
    {
        return units;
    }

    public void UnRegister(Unit unit)
    {
        ulong id = unit.GetUnitID();
        if (units.ContainsKey(id))
        {
            units.Remove(id);
        }
    }

    public MilitaryUnit LoadMilitaryUnit(string name)
    {
        if (!name.StartsWith("military_units\\"))
        {
            Debug.Log("It doesnt start with the name military_units");
            return null;
        }
        string jsonPath = Path.Combine(dataPath, name + ".json");
        MilitaryUnit militaryUnit = JsonUtility.FromJson<MilitaryUnit>(File.ReadAllText(jsonPath));
        return militaryUnit;
    }
}
