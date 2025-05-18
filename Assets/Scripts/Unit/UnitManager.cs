using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Pool;
using static TMPro.SpriteAssetUtilities.TexturePacker_JsonArray;

public class PlayerData
{
    public ulong id;
    public Color color;

    public PlayerData(ulong id, Color color)
    {
        this.id = id;
        this.color = color;
    }
}

[RequireComponent(typeof(SpatialHashGrid))]
public class UnitManager : MonoBehaviour
{
    public class UnitJsonData
    {
        public class ProjectileUnit
        {
            [JsonProperty("type")]
            public string type;

            [JsonProperty("damage")]
            public float damage = 6.0f;

            [JsonProperty("projectile_speed")]
            public float? projectile_speed = 5.0f;
        }

        public class CombatActionEvent
        {
            public string eventType;
            public float time;
        }

        [JsonProperty("hp")]
        public float hp = 45.0f;

        [JsonProperty("movement_speed")]
        public float movement_speed = 0.96f;

        [JsonProperty("attack_delay")]
        public float attack_delay = 1.0f;

        [JsonProperty("attack_range")]
        public float? attack_range = 0.0f;

        [JsonProperty("attack_events")]
        public List<CombatActionEvent> combatActionEvents = new List<CombatActionEvent>();

        [JsonProperty("projectile_offset")]
        public CommonStructures.SerializableVector3? projectile_offset;

        [JsonProperty("projectile_unit")]
        public string projectile_unit = "";

        [JsonProperty("damage")]
        public float damage = 6.0f;

        [JsonProperty("standing")]
        public string standing = "archer_standing";

        [JsonProperty("walking")]
        public string walking = "archer_walking";

        [JsonProperty("attacking")]
        public string attacking = "archer_attacking";

        [JsonProperty("dying")]
        public string dying = "archer_dying";

        [JsonProperty("corpse")]
        public string corpse = "archer_corpse";
    }

    public static UnitManager Instance { get; private set; }

    public SpatialHashGrid spatialHashGrid;

    public static ulong counter = 1;
    public static ulong crowdIDCounter = 0;

    public ObjectPool<MovableUnit> movableUnitPool;
    public ObjectPool<ShipUnit> shipUnitPool;
    public ObjectPool<DeadUnit> deadUnitPool;
    public ObjectPool<ProjectileUnit> projectileUnitPool;

    Dictionary<ulong, PlayerData> players = new Dictionary<ulong, PlayerData>();
    Dictionary<ulong, Unit> units = new Dictionary<ulong, Unit>(5000);

    public MovableUnit movableUnitPrefab;
    public ShipUnit shipUnitPrefab;
    public DeadUnit deadUnitPrefab;
    public ProjectileUnit projectileUnitPrefab;

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

        counter = 1;
        crowdIDCounter = 0;

        //TODO: Make proper player data systerm
        players.Add(0, new PlayerData(0, Color.white));
        if (ColorUtility.TryParseHtmlString("#87CEEB", out Color playerOneColor))
        {
            players.Add(1, new PlayerData(1, playerOneColor));
        }
        if (ColorUtility.TryParseHtmlString("#780606", out Color playerTwoColor))
        {
            players.Add(2, new PlayerData(2, playerTwoColor));
        }
    }

    private void Start()
    {
        movableUnitPool = new ObjectPool<MovableUnit>(SpawnMovableUnit, GetMovableUnitfromPool, ReleaseMovableUnitfromPool, DestroyMovableUnitfromPool, false, 200, 5000);
        shipUnitPool = new ObjectPool<ShipUnit>(SpawnShipUnit, GetShipUnitFromPool, ReleaseShipUnitFromPool, DestroyShipUnitFromPool);
        deadUnitPool = new ObjectPool<DeadUnit>(SpawnDeadUnit, GetDeadUnitFromPool, ReleaseDeadUnitFromPool, DestroyDeadUnitFromPool);
        projectileUnitPool = new ObjectPool<ProjectileUnit>(SpawnProjectileUnit, GetProjectileUnitFromPool, ReleaseProjectileUnitFromPool, DestroyProjectileUnitFromPool);
    }

    private DeadUnit SpawnDeadUnit()
    {
        DeadUnit deadUnit = Instantiate(deadUnitPrefab);
        return deadUnit;
    }

    private void GetDeadUnitFromPool(DeadUnit unit)
    {
        unit.gameObject.SetActive(true);
    }

    private void ReleaseDeadUnitFromPool(DeadUnit unit)
    {
        if (units.ContainsKey(unit.id))
        {
            units.Remove(unit.id);
        }
        unit.gameObject.SetActive(false);
    }

    private void DestroyDeadUnitFromPool(DeadUnit unit)
    {
        if (units.ContainsKey(unit.id))
        {
            units.Remove(unit.id);
        }
        Destroy(unit);
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

    #region ProjectileUnit
    private void GetProjectileUnitFromPool(ProjectileUnit unit)
    {
        unit.gameObject.SetActive(true);
    }

    private ProjectileUnit SpawnProjectileUnit()
    {
        ProjectileUnit movableUnit = Instantiate(projectileUnitPrefab);
        return movableUnit;
    }

    private void ReleaseProjectileUnitFromPool(ProjectileUnit unit)
    {
        if (units.ContainsKey(unit.id))
        {
            units.Remove(unit.id);
        }
        unit.gameObject.SetActive(false);
    }

    private void DestroyProjectileUnitFromPool(ProjectileUnit unit)
    {
        if (units.ContainsKey(unit.id))
        {
            units.Remove(unit.id);
        }
        Destroy(unit);
    }
    #endregion

    #region BasicUnit
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
    #endregion

    public PlayerData GetPlayerData(ulong id)
    {
        if (players.TryGetValue(id, out PlayerData playerData)) return playerData;
        return null;
    }

    public UnitJsonData LoadUnitJsonData(string name)
    {
        if (!name.StartsWith("military_units\\"))
        {
            Debug.Log("It doesnt start with the name military_units");
            return null;
        }
        string jsonPath = Path.Combine(dataPath, name + ".json");
        UnitJsonData militaryUnit = JsonConvert.DeserializeObject<UnitJsonData>(File.ReadAllText(jsonPath));
        return militaryUnit;
    }

    public UnitJsonData.ProjectileUnit LoadProjectileJsonData(string name)
    {
        string jsonPath = Path.Combine(dataPath, name + ".json");
        UnitJsonData.ProjectileUnit projectileUnit = JsonConvert.DeserializeObject<UnitJsonData.ProjectileUnit>(File.ReadAllText(jsonPath));
        return projectileUnit;
    }
}
