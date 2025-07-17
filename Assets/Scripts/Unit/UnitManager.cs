using Newtonsoft.Json;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Pool;
using static CustomSpriteLoader;
using static PathfinderTest;
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
    [System.Serializable]
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

        [System.Serializable]
        public class Prop
        {
            [JsonProperty("graphics")]
            public string graphics = null;
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

        [System.Serializable]
        public class ShipData
        {
            public class NavLinkData
            {
                [JsonProperty("start")]
                public CommonStructures.SerializableVector3 start = new();

                [JsonProperty("end")]
                public CommonStructures.SerializableVector3 end = new();

                [JsonProperty("area_type")]
                public int area_type = 0;

                [JsonProperty("width")]
                public float width = 1.0f;
            }

            [JsonProperty("deck_mesh_name")]
            public string deck_mesh_name = null;

            [JsonProperty("deck_mesh_size")]
            public float deck_mesh_size = 1f;

            [JsonProperty("navmesh_name")]
            public string navmesh_name = null;

            [JsonProperty("navmesh_size")]
            public float navmesh_size = 1f;

            [JsonProperty("navlinks")]
            public List<NavLinkData> navlinks = new();

            [JsonProperty("docked")]
            public string dockedProp = null;
        }

        [System.Serializable]
        public class CollisionData
        {
            // For Convex Mesh
            [JsonProperty("name")]
            public string name = null;

            // For Convex Mesh
            [JsonProperty("size")]
            public float? size = 1f;

            // For Capsule or Sphere
            [JsonProperty("radius")]
            public float? radius = 0.14f;

            // For Capsule
            [JsonProperty("height")]
            public float? height = 0.54f;
        }

        [System.Serializable]
        public class LockedAngle
        {
            [JsonProperty("direction_count")]
            public int directionCount = 8;
        }

        [JsonProperty("hp")]
        public float hp = 45.0f;

        [JsonProperty("movement_speed")]
        public float movement_speed = 0.96f;

        [JsonProperty("rotation_speed")]
        public float? rotation_speed = 360.0f;

        [JsonProperty("use_steering")]
        public bool? use_steering = false;

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

        [JsonProperty("ship_data")]
        public ShipData ship_data = null;

        [JsonProperty("collision")]
        public CollisionData collisionData = null;

        [JsonProperty("locked_angle")]
        public LockedAngle lockedAngle = null;
    }

    public static UnitManager Instance { get; private set; }

    public SpatialHashGrid spatialHashGrid;

    public static ulong counter = 1;
    public static ulong crowdIDCounter = 0;

    private ObjectPool<MovableUnit> movableUnitPool;
    public ObjectPool<ShipUnit> shipUnitPool;
    private ObjectPool<DeadUnit> deadUnitPool;
    public ObjectPool<ProjectileUnit> projectileUnitPool;
    private ObjectPool<PropUnit> propUnitPool;

    Dictionary<ulong, PlayerData> players = new Dictionary<ulong, PlayerData>();
    Dictionary<ulong, Unit> units = new Dictionary<ulong, Unit>(5000);
    
    Dictionary<string, UnitJsonData> unitData = new Dictionary<string, UnitJsonData>();
    Dictionary<string, UnitJsonData.ProjectileUnit> projectileData = new Dictionary<string, UnitJsonData.ProjectileUnit>();
    Dictionary<string, UnitJsonData.Prop> propData = new Dictionary<string, UnitJsonData.Prop>();

    public MovableUnit movableUnitPrefab;
    public ShipUnit shipUnitPrefab;
    public DeadUnit deadUnitPrefab;
    public ProjectileUnit projectileUnitPrefab;
    public PropUnit propUnitPrefab;

    System.Action<Unit> PreSpawnAction = null;

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
        movableUnitPool = new ObjectPool<MovableUnit>(_SpawnMovableUnit, _GetMovableUnitfromPool, _ReleaseMovableUnitfromPool, _DestroyMovableUnitfromPool, false, 200, 5000);
        //shipUnitPool = new ObjectPool<ShipUnit>(SpawnShipUnit, GetShipUnitFromPool, ReleaseShipUnitFromPool, DestroyShipUnitFromPool);
        deadUnitPool = new ObjectPool<DeadUnit>(_SpawnDeadUnit, _GetDeadUnitFromPool, _ReleaseDeadUnitFromPool, _DestroyDeadUnitFromPool, false, 200, 5000);
        projectileUnitPool = new ObjectPool<ProjectileUnit>(SpawnProjectileUnit, GetProjectileUnitFromPool, ReleaseProjectileUnitFromPool, DestroyProjectileUnitFromPool);
        propUnitPool = new ObjectPool<PropUnit>(_SpawnPropUnit, _GetPropUnitFromPool, _ReleasePropUnitFromPool, _DestroyPropFromPool, false, 200, 5000);

    }

    public PropUnit GetPropUnitFromPool(System.Action<Unit> PreSpawnAction = null)
    {
        this.PreSpawnAction = PreSpawnAction;
        return propUnitPool.Get();
    }

    public void ReleasePropUnitFromPool(PropUnit unit)
    {
        propUnitPool.Release(unit);
    }

    private void _GetPropUnitFromPool(PropUnit unit)
    {
        PreSpawnAction?.Invoke(unit);
        unit.gameObject.SetActive(true);
        PreSpawnAction = null;
    }

    private PropUnit _SpawnPropUnit()
    {
        PropUnit propUnit = Instantiate(propUnitPrefab);
        return propUnit;
    }

    private void _ReleasePropUnitFromPool(PropUnit unit)
    {
        unit.transform.SetParent(null, true);
        if (units.ContainsKey(unit.id))
        {
            units.Remove(unit.id);
        }
        unit.gameObject.SetActive(false);

        //UnitEventHandler.Instance.CallEventByID(UnitEventHandler.EventID.OnUnitRemove, unit.id);
    }

    private void _DestroyPropFromPool(PropUnit unit)
    {
        if (units.ContainsKey(unit.id))
        {
            units.Remove(unit.id);
        }

        Destroy(unit);
    }

    private DeadUnit _SpawnDeadUnit()
    {
        DeadUnit deadUnit = Instantiate(deadUnitPrefab);
        return deadUnit;
    }

    public DeadUnit GetDeadUnitFromPool(System.Action<Unit> PreSpawnAction = null)
    {
        this.PreSpawnAction = PreSpawnAction;
        return deadUnitPool.Get();
    }

    public void ReleaseDeadUnitFromPool(DeadUnit deadUnit)
    {
        deadUnitPool.Release(deadUnit);
    }

    private void _GetDeadUnitFromPool(DeadUnit unit)
    {
        PreSpawnAction?.Invoke(unit);
        unit.gameObject.SetActive(true);
        PreSpawnAction = null;
    }

    private void _ReleaseDeadUnitFromPool(DeadUnit unit)
    {
        unit.transform.SetParent(null, true);
        if (units.ContainsKey(unit.id))
        {
            units.Remove(unit.id);
        }
        unit.gameObject.SetActive(false);
    }

    private void _DestroyDeadUnitFromPool(DeadUnit unit)
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
    private void _GetMovableUnitfromPool(MovableUnit unit)
    {
        PreSpawnAction?.Invoke(unit);
        unit.gameObject.SetActive(true);
        PreSpawnAction = null;
        UnitEventHandler.Instance.CallEventByID(UnitEventHandler.EventID.OnUnitSpawn, unit.id);
    }

    public MovableUnit GetMovableUnitFromPool(System.Action<Unit> PreSpawnAction = null)
    {
        this.PreSpawnAction = PreSpawnAction;
        return movableUnitPool.Get();
    }

    public void ReleaseMovableUnitFromPool(MovableUnit unit)
    {
        movableUnitPool.Release(unit);
    }

    private MovableUnit _SpawnMovableUnit()
    {
        MovableUnit movableUnit = Instantiate(movableUnitPrefab);
        return movableUnit;
    }

    private void _ReleaseMovableUnitfromPool(MovableUnit unit)
    {
        unit.OnRelease?.Invoke(unit.id);
        unit.transform.SetParent(null, true);

        if (unit.IsShip())
        {
            if (unit.shipData.unitsOnShip != null && unit.shipData.unitsOnShip.Count > 0)
            {
                foreach (var u in unit.shipData.unitsOnShip)
                {
                    NativeLogger.Log($"Unit id: {u.id} is alive when ship {unit.id} died");
                    u.transform.SetParent(null, true);
                    StatComponent.KillUnit(u);
                }
                unit.shipData.unitsOnShip.Clear();
            }
        }

        if (units.ContainsKey(unit.id))
        {
            units.Remove(unit.id);
        }

        unit.gameObject.SetActive(false);
        //unit.gameObject.transform.position = UnityEngine.Vector3.zero;

        UnitEventHandler.Instance.CallEventByID(UnitEventHandler.EventID.OnUnitRemove, unit.id);
        //movableUnitPool.Release(unit);
    }

    private void _DestroyMovableUnitfromPool(MovableUnit unit)
    {
        if (units.ContainsKey(unit.id))
        {
            units.Remove(unit.id);
        }

        UnitEventHandler.Instance.CallEventByID(UnitEventHandler.EventID.OnUnitRemove, unit.id);
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

    public UnitJsonData.Prop LoadPropJsonData(string name)
    {
        if (propData.ContainsKey(name))
        {
            return propData[name];
        }

        string jsonPath = Path.Combine(dataPath, name + ".json");
        UnitJsonData.Prop propUnit = JsonConvert.DeserializeObject<UnitJsonData.Prop>(File.ReadAllText(jsonPath));
        propData.Add(name, propUnit);
        return propUnit;
    }
}
