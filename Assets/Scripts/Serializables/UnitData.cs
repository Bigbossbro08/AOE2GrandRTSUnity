using System.Collections.Generic;
using System;
using UnityEngine;

[Serializable]  // Allows Unity's JsonUtility to serialize/deserialize
public class UnitData
{
    public int _player;
    public float x, y, z;
    public int reference_id, unit_const, status;
    public float rotation;
    public int initial_animation_frame, garrisoned_in_id, caption_string_id;
}

// Optional wrapper class (useful for JsonUtility)
[Serializable]
public class UnitDataList
{
    public List<UnitData> units = new List<UnitData>();
}
