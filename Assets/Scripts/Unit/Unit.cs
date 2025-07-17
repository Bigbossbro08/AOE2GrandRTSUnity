using Newtonsoft.Json;
using System.Collections.Generic;
using UnityEngine;

public class Unit : MonoBehaviour, MapLoader.IMapSaveLoad
{
    [JsonObject(MemberSerialization.OptIn)]
    public class UnitData : MapLoader.SaveLoadData
    {
        [JsonProperty] public ulong id;
        [JsonProperty] public string unitDataName;
        [JsonProperty] public CommonStructures.SerializableVector3 position;
        [JsonProperty] public CommonStructures.SerializableVector3 eulerAngles;

        //[JsonProperty(ItemConverterType = typeof(MapLoader.SaveLoadDataConverter))]
        [JsonProperty]
        public List<MapLoader.SaveLoadData> components = new List<MapLoader.SaveLoadData>();

        public UnitData() { type = "UnitData"; }
    }

    public ulong playerId = 0; // By default nature player

    public ulong id = 0;

    public string unitDataName = "military_units\\archer";

    public ulong GetUnitID()
    {
        return id;
    }

    protected void Initialize()
    {
        id = UnitManager.Instance.Register(this);
    }

    protected void OnRemove()
    {
        UnitManager.Instance.UnRegister(this);
    }

    public virtual bool IsSelectable()
    {
        return false;
    }

    public void Load(MapLoader.SaveLoadData data)
    {
        UnitData unitData = data as UnitData;
        id = unitData.id;
        unitDataName = unitData.unitDataName;
        transform.position = (Vector3)unitData.position;
        transform.eulerAngles = (Vector3)unitData.eulerAngles;
        UnitManager.Instance.ForceRegister(this, unitData.id);
    }

    MapLoader.SaveLoadData MapLoader.IMapSaveLoad.Save()
    {
        UnitData data = new UnitData
        {
            id = id,
            unitDataName = unitDataName,
            position = (CommonStructures.SerializableVector3)transform.position,
            eulerAngles = (CommonStructures.SerializableVector3)transform.eulerAngles,
        };

        return data;
    }

    public void PostLoad(MapLoader.SaveLoadData data)
    {
        UnitData unitData = data as UnitData;
        UnitManager.Instance.ForceRegister(this, unitData.id);
    }
}
