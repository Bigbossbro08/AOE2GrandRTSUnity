using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class CustomSpriteLoader : MonoBehaviour
{
    [System.Serializable]
    public class SpriteInfo
    {
        public string name;
        public int x;
        public int y;
        public int width;
        public int height;
        public int pivot_x;
        public int pivot_y;
    }

    [System.Serializable]
    public class SpriteSheet
    {
        public bool isLooping;
        public float rotation_offset;
        public float height;
        public float duration;
        public int frames_per_angle;
        public List<SpriteInfo> sprite_data = new List<SpriteInfo>();
    }

    [System.Serializable]
    public class SpriteReturnData {
        public bool isLooping;
        public float rotation_offset = 0.0f;
        public float height;
        public float duration;
        public int frames_per_angle;
        public Texture2D mainTexture;
        public Texture2D maskTexture;
        public Dictionary<int, List<Sprite>> sprites = new Dictionary<int, List<Sprite>>();

        public SpriteReturnData(
            bool isLooping, 
            float rotation_offset, 
            float height, 
            float duration, 
            int framesPerAngle, 
            Dictionary<int, List<Sprite>> sprites,
            Texture2D mainTexture,
            Texture2D maskTexture
            )
        {
            this.isLooping = isLooping;
            this.rotation_offset = rotation_offset;
            this.height = height;
            this.duration = duration;
            this.frames_per_angle = framesPerAngle;
            this.sprites = sprites;
            this.mainTexture = mainTexture;
            this.maskTexture = maskTexture;
        }
    }

    Dictionary<string, SpriteReturnData> spriteDictionary = new Dictionary<string, SpriteReturnData>();

    public static CustomSpriteLoader Instance { get; private set; }

    private void Awake()
    {
        // If there is an instance, and it's not me, delete myself.

        if (Instance != null && Instance != this)
        {
            Destroy(this);
        }
        else
        {
            Instance = this;
        }
    }

    SpriteSheet SpriteMetadata(string path)
    {
        if (!File.Exists(path)) { return null; }
        string json = File.ReadAllText(path);
        return JsonConvert.DeserializeObject<SpriteSheet>(json);
    }

    Texture2D SpriteImage(string path)
    {
        if (!File.Exists(path)) { return null; }
        byte[] fileData = File.ReadAllBytes(path);
        Texture2D tex = new Texture2D(2, 2); //new Texture2D(2, 2);
        if (tex.LoadImage(fileData)) return tex;
        return null;
    }

    // Ensures the angle is always within [0, 360)
    public static float NormalizeAngle(float angle, float offset=0.0f)
    {
        angle %= 360; // Keep within -360 to 360
        if (angle < 0) angle += 360; // Convert negatives to positive
        return angle;
    }

    // Snaps the angle to the nearest 8 directions (0, 45, 90, ..., 315)
    public static int GetFixed8DirectionAngle(float angle)
    {
        angle = NormalizeAngle(angle); // Ensure within 0-360
        int index = (int)Mathf.Round(angle / 45.0f) % 8; // Find closest direction index
        return index; // Convert back to angle
    }

    private Dictionary<int, List<Sprite>> ProcessSprites(SpriteSheet metadata, Texture2D texture)
    {
        var spriteGroups = new Dictionary<int, List<Sprite>>();
        int framesPerGroup = metadata.frames_per_angle;

        for (int i = 0; i < metadata.sprite_data.Count; i++)
        {
            var data = metadata.sprite_data[i];
            int groupKey = i / framesPerGroup;

            if (!spriteGroups.ContainsKey(groupKey))
            {
                spriteGroups[groupKey] = new List<Sprite>();
            }

            var rect = new Rect(data.x, texture.height - data.y - data.height, data.width, data.height);
            var pivot = new Vector2((float)data.pivot_x / data.width, 1 - (float)data.pivot_y / data.height);
            Sprite sprite = Sprite.Create(texture, rect, pivot, 100f); // 100 pixels per unit
            sprite.name = data.name;
            spriteGroups[groupKey].Add(sprite);
        }

        return spriteGroups;
    }

    public SpriteReturnData LoadSprite(string file)
    {
        SpriteReturnData spriteReturnData = null;
        try
        {
            if (spriteDictionary.ContainsKey(file))
            {
                spriteReturnData = spriteDictionary[file];
                return spriteReturnData;
            }
            string path = Path.Combine(MapLoader.GetDataPath());
            //string path = Path.Combine("E:\\repos\\AOE2GrandRTSUnityFiles\\", "new_format");
            string jsonPath = Path.Combine(path, $"{file}.json");
            SpriteSheet metadata = SpriteMetadata(jsonPath);
            string pngPath = Path.Combine(path, $"{file}.png");

            Texture2D texture = SpriteImage(pngPath);
            texture.name = $"{file}.png";
            texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.Apply();

            string pColorPath = Path.Combine(path, $"{file}_p.png");
            Texture2D maskTexture = SpriteImage(pColorPath);
            maskTexture.name = $"{file}.png";
            maskTexture.filterMode = FilterMode.Point;
            maskTexture.wrapMode = TextureWrapMode.Clamp;
            maskTexture.Apply();

            Dictionary<int, List<Sprite>> groupedSprites = ProcessSprites(metadata, texture);

            var result = new SpriteReturnData(
                metadata.isLooping,
                metadata.rotation_offset,
                metadata.height,
                metadata.duration,
                metadata.frames_per_angle,
                groupedSprites,
                texture,
                maskTexture
            );

            spriteDictionary.Add(file, result);
            return result;
        }
        catch (System.Exception e)
        {
            NativeLogger.Error($"{e.Message} {e.StackTrace}");
        }
        return spriteReturnData;
    }
}
