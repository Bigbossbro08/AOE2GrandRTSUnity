using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Pool;
using static CustomSpriteLoader;
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
        public enum ArmorClass
        {
            BasePierce = 3,
            BaseMelee = 4,
            Infantry = 1,
            Cavalry = 8,
            // ... include all relevant classes (IDs from AoE2)
        }

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

        [System.Serializable]
        public class ArmorEntry
        {
            public ArmorClass armorClass;
            public int value;
        }

        [System.Serializable]
        public class DamageData
        {
            public List<ArmorEntry> attackValues = new();
            public List<ArmorEntry> armorValues = new();
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

        [JsonProperty("icon")]
        public string icon = "";
        
        [JsonProperty("damageData")]
        public DamageData damageData = new DamageData();

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
    
    Dictionary<string, UnitJsonData> unitData = new Dictionary<string, UnitJsonData>();
    Dictionary<string, UnitJsonData.ProjectileUnit> projectileData = new Dictionary<string, UnitJsonData.ProjectileUnit>();

    public MovableUnit movableUnitPrefab;
    public ShipUnit shipUnitPrefab;
    public DeadUnit deadUnitPrefab;
    public ProjectileUnit projectileUnitPrefab;

    private string dataPath = "E:\\repos\\AOE2GrandRTSUnityFiles\\data";

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
        Color playerColor = Color.white;
        players.Add(0, new PlayerData(0, playerColor));
        if (ColorUtility.TryParseHtmlString("#00BFFF", out playerColor))
        {
            players.Add(1, new PlayerData(1, playerColor));
        }
        if (ColorUtility.TryParseHtmlString("#FF0000", out playerColor))
        {
            players.Add(2, new PlayerData(2, playerColor));
        }
        if (ColorUtility.TryParseHtmlString("#BFFF00", out playerColor))
        {
            players.Add(3, new PlayerData(3, playerColor));
        }
        if (ColorUtility.TryParseHtmlString("#FFBF00", out playerColor))
        {
            players.Add(4, new PlayerData(4, playerColor));
        }
        if (ColorUtility.TryParseHtmlString("#40FF00", out playerColor))
        {
            players.Add(5, new PlayerData(5, playerColor));
        }
        if (ColorUtility.TryParseHtmlString("#BF00FF", out playerColor))
        {
            players.Add(6, new PlayerData(6, playerColor));
        }
        if (ColorUtility.TryParseHtmlString("#0080FF", out playerColor))
        {
            players.Add(7, new PlayerData(7, playerColor));
        }
        if (ColorUtility.TryParseHtmlString("#4000FF", out playerColor))
        {
            playerColor = Color.grey;
            players.Add(8, new PlayerData(8, playerColor));
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
        if (unitData.ContainsKey(name))
        {
            return unitData[name];
        }
        string jsonPath = Path.Combine(dataPath, name + ".json");
        UnitJsonData militaryUnit = JsonConvert.DeserializeObject<UnitJsonData>(File.ReadAllText(jsonPath));
        unitData.Add(name, militaryUnit);
        return militaryUnit;
    }

    public UnitJsonData.ProjectileUnit LoadProjectileJsonData(string name)
    {
        if (projectileData.ContainsKey(name))
        {
            return projectileData[name];
        }
        string jsonPath = Path.Combine(dataPath, name + ".json");
        UnitJsonData.ProjectileUnit projectileUnit = JsonConvert.DeserializeObject<UnitJsonData.ProjectileUnit>(File.ReadAllText(jsonPath));
        projectileData.Add(name, projectileUnit);
        return projectileUnit;
    }
}
