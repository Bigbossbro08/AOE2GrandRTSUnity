using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using static MovableUnit;

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
        enabled = false;
    }

    public string m_DataPath = "E:\\repos\\AOE2GrandRTSUnityFiles\\";

    public static string GetDataPath()
    {
        return Instance.m_DataPath;
    }

    public static void SetDataPath(string value)
    {
        Instance.m_DataPath = value;
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        StartCoroutine(Load());
        //GameManager.Instance.IncrementLoadCount();
    }

    IEnumerator Load()
    {
        string json = File.ReadAllText(m_DataPath + "saves/save.json"); 
        
        var settings = new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.Auto, // Preserve derived types
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore // Ignore self-referencing loops
        };
        // Deserialize GameState (the SaveLoadData converter will be used automatically)
        GameState state = JsonConvert.DeserializeObject<GameState>(json, settings);

        foreach (var unitData in state.units)
        {
            switch (unitData.type)
            {
                case "MovableUnitData":
                    MovableUnit.MovableUnitData movableUnitData = unitData as MovableUnit.MovableUnitData;
                    MovableUnit movableUnit = UnitManager.Instance.movableUnitPool.Get(); //Instantiate(UnitManager.Instance.movableUnitPrefab);
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
        File.WriteAllText(m_DataPath + "saves/save.json", json);

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
        Save();
    }
}
