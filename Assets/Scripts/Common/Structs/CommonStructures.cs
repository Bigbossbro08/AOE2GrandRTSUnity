using Newtonsoft.Json;
using UnityEngine;

public class CommonStructures
{
    [JsonObject(MemberSerialization.OptIn)]
    public struct SerializableVector3
    {
        [JsonProperty] public float x;
        [JsonProperty] public float y;
        [JsonProperty] public float z;

        public SerializableVector3(float x, float y, float z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }

        // Explicit conversion from Vector3 -> SerializableVector3
        public static explicit operator SerializableVector3(Vector3 v) => new SerializableVector3(v.x, v.y, v.z);

        // Explicit conversion from SerializableVector3 -> Vector3
        public static explicit operator Vector3(SerializableVector3 v) => new Vector3(v.x, v.y, v.z);
    }

    [JsonObject(MemberSerialization.OptIn)]
    public struct SerializableVector2Int
    {
        [JsonProperty] public int x;
        [JsonProperty] public int y;

        public SerializableVector2Int(int x, int y)
        {
            this.x = x;
            this.y = y;
        }

        // Explicit conversion from Vector3 -> SerializableVector3
        public static explicit operator SerializableVector2Int(Vector2Int v) => new SerializableVector2Int(v.x, v.y);

        // Explicit conversion from SerializableVector3 -> Vector3
        public static explicit operator Vector2Int(SerializableVector2Int v) => new Vector2Int(v.x, v.y);
    }
}
