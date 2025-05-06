using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.U2D;

public class CustomSpriteReader : MonoBehaviour
{
    [System.Serializable]
    public class SpriteInfo
    {
        public int x;
        public int y;
        public int width;
        public int height;
        public int pivot_x;
        public int pivot_y;
    }

    public string atlasTextureName = "sprite_atlas"; // No extension
    [SerializeField] SpriteRenderer _renderer = null;
    List<Sprite> sprites = new List<Sprite>();
    int currentFrame = 0;
    float currentTime = 0.0f;
    float timeBetweenFrame = 0.1f;

    void Start()
    {
        LoadSprite(atlasTextureName);
        //StartCoroutine(PlayAnimation());
    }

    Dictionary<string, SpriteInfo> SpriteMetadata(string path)
    {
        if (!File.Exists(path)) { return null; }
        string json = File.ReadAllText(path);
        return JsonConvert.DeserializeObject<Dictionary<string, SpriteInfo>>(json);
    }

    Texture2D SpriteImage(string path)
    {
        if (!File.Exists(path)) { return null; }
        byte[] fileData = File.ReadAllBytes(path);
        Texture2D tex = new Texture2D(2, 2);
        if (tex.LoadImage(fileData)) return tex;
        return null;
    }

    void LoadSprite(string file)
    {
        string path = "E:\\repos\\AOE2GrandRTSUnityFiles\\new_format";
        string jsonPath = Path.Combine(path, $"{file}.json");
        Dictionary<string, SpriteInfo> spriteMetadata = SpriteMetadata(jsonPath);
        string pngPath = Path.Combine(path, $"{file}.png");
        Texture2D texture = SpriteImage(pngPath); 
        
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.Apply();
        foreach (var kvp in spriteMetadata.OrderBy(k => k.Key))
        {
            var meta = kvp.Value;
            var rect = new Rect(meta.x, texture.height - meta.y - meta.height, meta.width, meta.height);
            var pivot = new Vector2((float)meta.pivot_x / meta.width, 1 - (float)meta.pivot_y / meta.height);
            Sprite sprite = Sprite.Create(texture, rect, pivot, 100f); // 100 pixels per unit
            sprites.Add(sprite);
        }
    }

    private void Update()
    {
        currentTime += Time.deltaTime;
        if (currentTime > timeBetweenFrame)
        {
            currentTime -= timeBetweenFrame;
            currentFrame++;
            if (currentFrame >= sprites.Count)
            {
                currentFrame = 0;
            }
        }

        _renderer.sprite = sprites[currentFrame];
    }
}
