using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Assimp.Unmanaged;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using static MovableUnit;
using Unity.AI.Navigation;
using static UnitManager;

public class MapLoader : MonoBehaviour
{
    // Custom converter for SaveLoadData and its derived types
    public class SaveLoadDataConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            // Allow for any type that inherits from SaveLoadData.
            return typeof(SaveLoadData).IsAssignableFrom(objectType);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            JObject jsonObject = JObject.Load(reader);

            if (!jsonObject.ContainsKey("type"))
            {
                throw new JsonSerializationException("Missing 'type' property in JSON.");
            }

            string type = jsonObject["type"].ToString();
            SaveLoadData instance = type switch
            {
                "MovableUnitData" => new MovableUnitData(),
                "UnitData" => new Unit.UnitData(),
                "MovementComponentData" => new MovementComponent.MovementComponentData(),
                "DeterministicVisualUpdaterData" => new DeterministicVisualUpdater.DeterministicVisualUpdaterData(),
                _ => throw new JsonSerializationException($"Unknown type: {type}")
            };

            serializer.Populate(jsonObject.CreateReader(), instance);
            return instance;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            if (value is SaveLoadData data)
            {
                JObject jsonObject = JObject.FromObject(value, serializer);
                jsonObject.AddFirst(new JProperty("type", data.type)); // Ensure type is written

                jsonObject.WriteTo(writer);
            }
            else
            {
                throw new JsonSerializationException($"Unexpected type: {value?.GetType()}");
            }
        }
    }

    // Apply the converter attribute so all derived types are handled by the converter.
    //[JsonConverter(typeof(SaveLoadDataConverter))]
    [JsonObject(MemberSerialization.OptIn)]
    public abstract class SaveLoadData
    {
        [JsonProperty] public string type; // This field tells us what type the instance is.
    }

    public interface IMapSaveLoad
    {
        void Load(SaveLoadData data);
        void PostLoad(SaveLoadData data);
        SaveLoadData Save();
    }

    [JsonObject(MemberSerialization.OptIn)]
    public class GameState
    {
        [JsonProperty] public ulong currentFrame;
        [JsonProperty] public int randomSeed;
        [JsonProperty] public ulong unitCounter;
        [JsonProperty] public ulong crowdIDCounter;
        [JsonProperty] public List<Unit.UnitData> units;
    }

    [System.Serializable]
    public class GameData
    {
        public string m_DataPath = "E:\\repos\\AOE2GrandRTSUnityFiles\\";
    }

    [System.Serializable]
    public class GameMapData
    {
        [System.Serializable]
        public class GameMetaData
        {
            [JsonProperty("visual_scale")]
            public CommonStructures.SerializableVector3 visualScale;
            [JsonProperty("navmesh_rotation_offset")]
            public CommonStructures.SerializableVector3 navMeshRotationOffset;
            [JsonProperty("navmesh_scale")]
            public CommonStructures.SerializableVector3 navMeshScale;
            [JsonProperty("assimp_navmesh_scale")]
            public float assimpNavmeshScale = 1.0f;
        }

        [System.Serializable]
        public class UnitDataJson
        {
            [System.Serializable]
            public class ControllableUnits
            {
                [JsonProperty("uid")]
                public int Uid { get; set; }

                [JsonProperty("player")]
                public ulong Player { get; set; }

                [JsonProperty("unit_name")]
                public string UnitName { get; set; }

                [JsonProperty("position")]
                public CommonStructures.SerializableVector3 Position { get; set; }

                [JsonProperty("angle")]
                public float Angle { get; set; }
            }
            
            [JsonProperty("controllable_units")]
            public List<ControllableUnits> controllableUnits { get; set; }

            [JsonProperty("prop_units")]
            public List<ControllableUnits> propUnits { get; set; }
        }

        public GameObject holderObject;
        public GameObject visualHolderObject;
        public List<GameObject> mapChunkVisuals = new List<GameObject>();

        public GameObject navMeshHolderObject;
        public NavMeshModifier landNavMeshHolderObject;
        public NavMeshModifier waterNavMeshHolderObject;
    }

    public GameMapData mapDataInstance = null;
    public static MapLoader Instance;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
        }
        else
        {
            Instance = this;
        }
        if (String.IsNullOrEmpty(configFileLocation))
        {
            configFileLocation = Path.Combine(Application.dataPath, "../gameConfig.json");
        }
        LoadOrCreateJson(configFileLocation);
        enabled = false;
    }

    public GameData gameData = new GameData();
    string configFileLocation = "";

    public static string GetDataPath()
    {
        return Instance.gameData.m_DataPath;
    }

    void LoadOrCreateJson(string fullPath)
    {
        if (File.Exists(fullPath))
        {
            try
            {
                string json = File.ReadAllText(fullPath);
                gameData = JsonConvert.DeserializeObject<GameData>(json) ?? new GameData();
            }
            catch
            {
                gameData = new GameData();
                SaveJson(fullPath);
            }
        }
        else
        {
            gameData = new GameData();
            SaveJson(fullPath);
        }
    }

    public void SaveJson(string fullPath)
    {
        string json = JsonUtility.ToJson(gameData, true);
        File.WriteAllText(fullPath, json);
        NativeLogger.Log("Saved data to: " + fullPath);
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        //StartCoroutine(Load());
        //GameManager.Instance.IncrementLoadCount();
    }

    public bool LoadMap(string path)
    {
        try
        {
            string mainpath = Path.Combine(MapLoader.GetDataPath(), path);

            string metaDataPath = Path.Combine(mainpath, "level.json");
            GameMapData.GameMetaData gameMapMetaData = JsonConvert.DeserializeObject<GameMapData.GameMetaData>(File.ReadAllText(metaDataPath));

            //if (!File.Exists(mainpath))
            //{
            //    Debug.LogError($"File not found at {mainpath}");
            //    return false;
            //}

            string folderPath = Path.Combine(mainpath, "level_bg");
            string[] files = Directory.GetFiles(folderPath, "*.png");

            GameObject mapHolder = new GameObject($"Map Holder");
            mapDataInstance = new GameMapData();
            mapDataInstance.holderObject = mapHolder;

            GameObject visualHolder = new GameObject($"Visual Holder");
            visualHolder.transform.SetParent(mapHolder.transform, true);
            mapDataInstance.visualHolderObject = visualHolder;

            Regex pattern = new Regex(@"tile_(-?\d+)_(-?\d+)\.png");
            foreach (string file in files)
            {
                string fileName = Path.GetFileName(file);
                Match match = pattern.Match(fileName);

                if (match.Success)
                {
                    int row = int.Parse(match.Groups[1].Value);
                    int col = int.Parse(match.Groups[2].Value);

                    Debug.Log($"File: {fileName} | Row: {row} | Col: {col}");

                    // Load texture
                    byte[] bytes = File.ReadAllBytes(file);
                    Texture2D texture = new Texture2D(2, 2);
                    texture.LoadImage(bytes);
                    texture.filterMode = FilterMode.Point;
                    // Load map background texture

                    GameObject chunkVisualObj = new GameObject($"Visual Chunk: tile_{row}_{col}");
                    SpriteRenderer chunkVisualRenderer = chunkVisualObj.AddComponent<SpriteRenderer>();
                    chunkVisualRenderer.sortingOrder = -9999;
                    Sprite sprite = Sprite.Create(
                        texture,
                        new Rect(0, 0, texture.width, texture.height),
                        new Vector2(0.5f, 0.5f), // Pivot in the center
                        100.0f                   // Pixels Per Unit (adjust if needed)
                    );
                    chunkVisualRenderer.sprite = sprite;
                    chunkVisualObj.transform.position = new Vector3(row * 10, col * 10, 0f);
                    chunkVisualObj.transform.SetParent(visualHolder.transform, true);
                    mapDataInstance.mapChunkVisuals.Add( chunkVisualObj );
                }
                else
                {
                    Debug.LogWarning($"File name didn't match pattern: {fileName}");
                }
            }
            visualHolder.transform.eulerAngles = new Vector3(30, -45, 0);
            visualHolder.transform.localScale = (Vector3)gameMapMetaData.visualScale;
            GameObject navmeshHolder = new GameObject("Navmesh Holder");
            navmeshHolder.transform.SetParent(mapHolder.transform, true);
            NavMeshSurface navMeshSurface = navmeshHolder.AddComponent<NavMeshSurface>();
            navMeshSurface.collectObjects = CollectObjects.Children;
            navMeshSurface.useGeometry = UnityEngine.AI.NavMeshCollectGeometry.PhysicsColliders;
            mapDataInstance.navMeshHolderObject = navmeshHolder;

            {
                GameObject landNavmeshObj = new GameObject("Land Nav Mesh");
                string landPath = Path.Combine(path, "land");
                AssimpMeshLoader.MeshReturnData meshReturnData = AssimpMeshLoader.Instance.LoadMeshFromAssimp(landPath); 
                float size = gameMapMetaData.assimpNavmeshScale;
                UnityEngine.Mesh mesh = AssimpMeshLoader.ScaleMesh(meshReturnData.mesh, size);
                MeshCollider meshCollider = landNavmeshObj.AddComponent<MeshCollider>();
                meshCollider.sharedMesh = mesh;
                landNavmeshObj.transform.SetParent(navmeshHolder.transform, true);
                NavMeshModifier navMeshModifier = landNavmeshObj.AddComponent<NavMeshModifier>();
                navMeshModifier.overrideArea = true;
                navMeshModifier.area = 0;
                mapDataInstance.landNavMeshHolderObject = navMeshModifier;
            }

            {
                GameObject waterNavmeshObj = new GameObject("Water Nav Mesh");
                string waterPath = Path.Combine(path, "water");
                AssimpMeshLoader.MeshReturnData meshReturnData = AssimpMeshLoader.Instance.LoadMeshFromAssimp(waterPath);
                float size = gameMapMetaData.assimpNavmeshScale;
                UnityEngine.Mesh mesh = AssimpMeshLoader.ScaleMesh(meshReturnData.mesh, size);
                MeshCollider meshCollider = waterNavmeshObj.AddComponent<MeshCollider>();
                meshCollider.sharedMesh = mesh;
                waterNavmeshObj.transform.SetParent(navmeshHolder.transform, true);
                NavMeshModifier navMeshModifier = waterNavmeshObj.AddComponent<NavMeshModifier>();
                navMeshModifier.overrideArea = true;
                navMeshModifier.area = 3;
                mapDataInstance.waterNavMeshHolderObject = navMeshModifier;
            }
            navmeshHolder.transform.eulerAngles = (Vector3)gameMapMetaData.navMeshRotationOffset;
            navmeshHolder.transform.localScale = (Vector3)gameMapMetaData.navMeshScale;
            navMeshSurface.BuildNavMesh();

            string unitsPath = Path.Combine(mainpath, "units.json");
            GameMapData.UnitDataJson unitDataJson = JsonConvert.DeserializeObject<GameMapData.UnitDataJson>(File.ReadAllText(unitsPath));

            foreach (var cu in unitDataJson.controllableUnits)
            {
                System.Action<Unit> PreSpawnAction = (unit) =>
                {
                    unit.playerId = cu.Player;
                    unit.transform.position = (Vector3)cu.Position;
                    unit.transform.eulerAngles = new Vector3(0, cu.Angle, 0);
                    unit.unitDataName = cu.UnitName;
                };
                UnitManager.Instance.GetMovableUnitFromPool(PreSpawnAction);
            }

            foreach (var pu in unitDataJson.propUnits)
            {
                System.Action<Unit> PreSpawnAction = (unit) =>
                {
                    unit.playerId = pu.Player;
                    unit.transform.position = (Vector3)pu.Position;
                    unit.transform.eulerAngles = new Vector3(0, pu.Angle, 0);
                    unit.unitDataName = pu.UnitName;
                };
                UnitManager.Instance.GetPropUnitFromPool(PreSpawnAction);
            }

            return true;
        }
        catch (Exception e) {
            Debug.LogException(e);
            return false;
        }
    }

    IEnumerator Load()
    {
        string json = File.ReadAllText(gameData.m_DataPath + "saves/save.json"); 
        
        var settings = new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.Auto, // Preserve derived types
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore // Ignore self-referencing loops
        };
        // Deserialize GameState (the SaveLoadData converter will be used automatically)
        GameState state = JsonConvert.DeserializeObject<GameState>(json, settings);

        DeterministicUpdateManager.Instance.tickCount = state.currentFrame;
        DeterministicUpdateManager.Instance.seed = state.randomSeed;
        UnitManager.counter = state.unitCounter;
        UnitManager.crowdIDCounter = state.crowdIDCounter;

        foreach (var unitData in state.units)
        {
            switch (unitData.type)
            {
                case "MovableUnitData":
                    MovableUnit.MovableUnitData movableUnitData = unitData as MovableUnit.MovableUnitData;
                    MovableUnit movableUnit = UnitManager.Instance.GetMovableUnitFromPool(); //Instantiate(UnitManager.Instance.movableUnitPrefab);
                    movableUnit.Load(movableUnitData);
                    break;
                case "ShipUnitData":
                    ShipUnit.ShipUnitData shipUnitData = unitData as ShipUnit.ShipUnitData;
                    ShipUnit shipUnit = UnitManager.Instance.shipUnitPool.Get();
                    shipUnit.Load(shipUnitData);
                    break;
                // Add additional cases if you have more types.
                default:
                    Debug.LogWarning($"Unhandled unit type: {unitData.type}");
                    break;
            }
        }

        yield return new WaitForFixedUpdate();
        yield return new WaitForFixedUpdate();
        yield return new WaitForFixedUpdate();

        // Post Load is mainly for visual load
        foreach (var unitData in state.units)
        {
            switch (unitData.type)
            {
                case "MovableUnitData":
                    MovableUnit.MovableUnitData movableUnitData = unitData as MovableUnit.MovableUnitData;
                    MovableUnit movableUnit = UnitManager.Instance.GetUnit(movableUnitData.id) as MovableUnit;
                    movableUnit.PostLoad(movableUnitData);
                    break;
                case "ShipUnitData":
                    ShipUnit.ShipUnitData shipUnitData = unitData as ShipUnit.ShipUnitData;
                    ShipUnit shipUnit = UnitManager.Instance.GetUnit(shipUnitData.id) as ShipUnit;
                    shipUnit.PostLoad(shipUnitData);
                    break;
                // Add additional cases if you have more types.
                default:
                    Debug.LogWarning($"Unhandled unit type: {unitData.type}");
                    break;
            }
        }

        DeterministicUpdateManager.Instance.tickCount = state.currentFrame;
        DeterministicUpdateManager.Instance.seed = state.randomSeed;
        UnitManager.counter = state.unitCounter;
        UnitManager.crowdIDCounter = state.crowdIDCounter;

        GameManager.Instance.IncrementLoadCount();
    }

    void Save()
    {
        Dictionary<ulong, Unit> units = UnitManager.Instance.GetAllUnits();
        List<Unit.UnitData> unitDatas = new List<Unit.UnitData>();

        foreach (var unit in units)
        {
            if (unit.Value is IMapSaveLoad saveLoad)
            {
                SaveLoadData saveLoadData = saveLoad.Save();
                if (saveLoadData is MovableUnit.MovableUnitData movableUnitData)
                {
                    Debug.Log($"Saving unit: {JsonConvert.SerializeObject(movableUnitData, Formatting.Indented)}");
                    unitDatas.Add(movableUnitData);
                } else if (saveLoadData is ShipUnit.ShipUnitData shipUnitData)
                {
                    unitDatas.Add(shipUnitData);
                }
                //Debug.Log($"Saving unit: {JsonConvert.SerializeObject(saveLoadData, Formatting.Indented)}"); // Debug
            }
        }

        GameState state = new GameState
        {
            currentFrame = DeterministicUpdateManager.Instance.tickCount,
            randomSeed = DeterministicUpdateManager.Instance.seed,
            unitCounter = UnitManager.counter,
            crowdIDCounter = UnitManager.crowdIDCounter,
            units = unitDatas
        };

        var settings = new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.Auto,
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore // Prevent self-referencing loops
        };

        string json = JsonConvert.SerializeObject(state, Formatting.Indented, settings);
        File.WriteAllText(gameData.m_DataPath + "saves/save.json", json);

        //Dictionary<ulong, Unit> units = UnitManager.Instance.GetAllUnits();
        //List<SaveLoadData> unitDatas = new List<SaveLoadData>();
        //foreach (var unit in units)
        //{
        //    if (unit.Value is IMapSaveLoad saveLoad)
        //    {
        //        SaveLoadData saveLoadData = saveLoad.Save();
        //        Debug.Log(saveLoadData);
        //        unitDatas.Add(saveLoadData);
        //    }
        //}
        //
        //GameState state = new GameState
        //{
        //    currentFrame = DeterministicUpdateManager.Instance.tickCount,
        //    randomSeed = DeterministicUpdateManager.Instance.seed,
        //    unitCounter = UnitManager.counter,
        //    crowdIDCounter = UnitManager.crowdIDCounter,
        //    units = unitDatas //UnitManager.Instance.GetAllUnits().Select(unit => ((IMapSaveLoad)unit.Value).Save()).ToList()
        //};
        //
        //var settings = new JsonSerializerSettings
        //{
        //    TypeNameHandling = TypeNameHandling.Auto, // Preserve derived types
        //};
        //string json = JsonConvert.SerializeObject(state, Formatting.Indented);
        //File.WriteAllText(m_DataPath + "saves/save.json", json);
    }

    //void Save()
    //{
    //    GameState state = new GameState
    //    {
    //        currentFrame = DeterministicUpdateManager.Instance.tickCount,
    //        randomSeed = DeterministicUpdateManager.Instance.seed,
    //        unitCounter = UnitManager.counter,
    //        crowdIDCounter = UnitManager.crowdIDCounter,
    //        units = UnitManager.Instance.GetAllUnits().Select(unit => unit.Save()).ToList()
    //    };
    //
    //    //GameState state = new GameState
    //    //{
    //    //    currentFrame = DeterministicUpdateManager.Instance.tickCount,
    //    //    randomSeed = DeterministicUpdateManager.Instance.seed,
    //    //    unitCounter = UnitManager.counter,
    //    //    crowdIDCounter = UnitManager.crowdIDCounter,
    //    //    units = new List<SaveLoadData>(),
    //    //};
    //    //
    //    //Dictionary<ulong, Unit> unitsDict = UnitManager.Instance.GetAllUnits();
    //    //foreach (var kvp in unitsDict)
    //    //{
    //    //    ulong unitId = kvp.Key;
    //    //    Unit unit = kvp.Value;
    //    //    if (unit is IMapSaveLoad unitSaveLoad)
    //    //    {
    //    //        SaveLoadData saveLoadData = unitSaveLoad.Save();
    //    //        state.units.Add(saveLoadData);
    //    //    }
    //    //}
    //
    //    string json = JsonConvert.SerializeObject(state, Formatting.Indented);
    //    File.WriteAllText(m_DataPath + "saves/save.json", json);
    //}

    private void OnApplicationQuit()
    {
        //Save();
    }
}
