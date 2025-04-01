using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Windows;
using static TMPro.SpriteAssetUtilities.TexturePacker_JsonArray;

//using static TMPro.SpriteAssetUtilities.TexturePacker_JsonArray;

using File = System.IO.File;

public class SpriteTextureParser : MonoBehaviour
{
    //public string spriteFilePath;
    //public string textureFilePath;
    public string path = @"E:/repos/AOE2GrandRTSUnityFiles/converted/hd_base/data/game_entity/generic/archer/graphics/";
    public SpriteRenderer spriteRenderer;
    public float animationSpeed = 0.1f;

    private struct FrameData
    {
        public int index;
        public int subTextureIndex;
        public float duration;
        public bool mirror;
    }

    private struct TextureData
    {
        public string imageFile;
        public int width;
        public int height;
        public List<SubTexture> subTextures;
    }

    private struct SubTexture
    {
        public int x, y, width, height, offsetX, offsetY;
    }

    private class SpriteData
    {
        public class LayerData
        {
            public enum Mode : UInt16
            {
                Off,
                Once,
                Loop
            }
            public Mode mode;
            public int position = 20;
            public float timePerFrame = 0.07f;

            public LayerData(Mode mode, int position, float timePerFrame)
            {
                this.mode = mode;
                this.position = position;
                this.timePerFrame = timePerFrame;
            }
        }

        public class FrameData
        {
            public int frame_idx;
            public int angle;
            public int layer_id;
            public int image_id;
            public int subtex_id;

            public FrameData(int frame_idx, int angle, int layer_id, int image_id, int subtex_id)
            {
                this.frame_idx = frame_idx;
                this.angle = angle;
                this.layer_id = layer_id;
                this.image_id = image_id;
                this.subtex_id = subtex_id;
            }
        }

        public Dictionary<int, string> textureNames = new Dictionary<int, string>();
        public float scaleFactor = 1.0f; // Will be used for future
        public Dictionary<int, LayerData> layerDatas = new Dictionary<int, LayerData>();
        public Dictionary<int, int> angles = new Dictionary<int, int>();
    }

    private Texture2D mainTexture;
    private List<Sprite> extractedSprites = new List<Sprite>();
    private List<FrameData> animationFrames = new List<FrameData>();
    private int currentFrame = 0;
    private float timer = 0f;

    private void Start()
    {
        string basePath = "E:\\repos\\AOE2GrandRTSUnityFiles\\converted\\hd_base\\data\\game_entity\\generic\\archer\\graphics\\";
        //string spriteFilePath = basePath + "attack_archer.sprite";
        //if (!File.Exists(spriteFilePath))
        //{
        //    spriteFilePath = EditorUtility.OpenFilePanel("Find location to sprite file", Application.dataPath, "sprite");
        //}
        string textureFilePath = basePath + "attack_archer_2.texture";
        //ParseSpriteFile(spriteFilePath);
        //if (!File.Exists(spriteFilePath))
        //{
        //    Debug.LogError($"Missing sprite file! and locations are {spriteFilePath}");
        //    return;
        //}

        //if (!File.Exists(textureFilePath))
        //{
        //    textureFilePath = EditorUtility.OpenFilePanel("Find location to sprite file", Path.GetDirectoryName(spriteFilePath), "texture");
        //}

        if (!File.Exists(textureFilePath))
        {
            Debug.LogError($"Missing texture file! and locations are {textureFilePath}");
            return;
        }

        TextureData textureData = ParseTextureFile(textureFilePath);
        string imagePath = Path.Combine(Path.GetDirectoryName(textureFilePath), textureData.imageFile);
        LoadTexture(imagePath);
        ExtractSubTextures(textureData);
        //ParseSpriteFile(spriteFilePath);
    }

    private void Update()
    {
        if (spriteRenderer == null) return;

        float yRotation = transform.eulerAngles.y;
        int directionIndex = GetDirectionIndex(yRotation);
        PlayAnimation(directionIndex);
    }

    private int GetDirectionIndex(float yRotation)
    {
        int totalDirections = 8;
        float anglePerDirection = 360f / totalDirections;
        return Mathf.RoundToInt(yRotation / anglePerDirection) % totalDirections;
    }

    private void PlayAnimation(int directionIndex)
    {
        timer += Time.deltaTime;
        //FrameData frame = animationFrames[currentFrame];

        float duration = 2;
        int frameCount = extractedSprites.Count / 5;

        // Calculate looping frame index
        int newFrame = Mathf.FloorToInt((timer / duration) * frameCount) % frameCount;

        // Calculate looping frame index
        //int newFrame = Mathf.FloorToInt((timer / 2) * frameCount) % frameCount;

        //currentFrame = (currentFrame + 1) % frameCount;

        //bool mirror = frame.mirror || directionIndex >= 4;
        //spriteRenderer.flipX = mirror;
        //spriteRenderer.sprite = extractedSprites[frame.subTextureIndex];
        spriteRenderer.sprite = extractedSprites[newFrame];
    }

    private SpriteData ParseSpriteFile(string path)
    {
        SpriteData spriteData = new SpriteData();
        string[] lines = File.ReadAllLines(path);
        foreach (string line in lines)
        {
            if (string.IsNullOrEmpty(line) || line.StartsWith("#")) continue;
            string[] parts = line.Split(' ');
            switch (parts[0])
            {
                case "texture":
                    spriteData.textureNames.Add(int.Parse(parts[1]), parts[2]);
                    break;
                case "scalefactor":
                    spriteData.scaleFactor = float.Parse(parts[1]);
                    break;
                case "layer":
                    SpriteData.LayerData.Mode mode = SpriteData.LayerData.Mode.Off;
                    switch (parts[2])
                    {
                        case "mode=once":
                            mode = SpriteData.LayerData.Mode.Once;
                            break;
                        case "mode=loop":
                            mode = SpriteData.LayerData.Mode.Loop;
                            break;
                        default:
                            break;
                    }
                    
                    int position = int.Parse(parts[3].Substring(parts[3].IndexOf('=') + 1)); 
                    float timePerFrame = float.Parse(parts[4].Substring(parts[4].IndexOf('=') + 1)); //0.07f;
                    SpriteData.LayerData layerData = new SpriteData.LayerData(mode, position, timePerFrame);
                    spriteData.layerDatas.Add(int.Parse(parts[1]), layerData);
                    break;
                case "angle":
                    int mirrorAngle = int.Parse(parts[1]);
                    if (parts.Length == 3)
                    {
                        mirrorAngle = int.Parse(parts[2].Substring(parts[2].IndexOf('=') + 1));
    
                        // Extract the mirror_from value
                        //int mirrorFrom = int.Parse(parts[2].Split('=')[1]);
                    }
                    spriteData.angles.Add(int.Parse(parts[1]), mirrorAngle);
                    break;
    
                default:
                    break;
            }
            if (parts[0] == "frame")
            {
                FrameData frame = new FrameData
                {
                    index = int.Parse(parts[1]),
                    subTextureIndex = int.Parse(parts[2]),
                    duration = float.Parse(parts[3]),
                    mirror = parts.Length > 4 && parts[4] == "mirror"
                };
                animationFrames.Add(frame);
            }
        }
        Debug.Log("Sprite file parsed with " + animationFrames.Count + " frames.");
        return spriteData;
    }

    private TextureData ParseTextureFile(string path)
    {
        string[] lines = File.ReadAllLines(path);
        TextureData textureData = new TextureData { subTextures = new List<SubTexture>() };

        foreach (string line in lines)
        {
            string[] parts = line.Split(' ');
            switch (parts[0])
            {
                case "imagefile":
                    textureData.imageFile = parts[1].Trim('"');
                    break;
                case "size":
                    textureData.width = int.Parse(parts[1]);
                    textureData.height = int.Parse(parts[2]);
                    break;
                case "subtex":
                    textureData.subTextures.Add(new SubTexture
                    {
                        x = int.Parse(parts[1]),
                        y = int.Parse(parts[2]),
                        width = int.Parse(parts[3]),
                        height =  int.Parse(parts[4]),
                        offsetX = int.Parse(parts[5]),
                        offsetY = int.Parse(parts[6])
                    });
                    break;
            }
        }
        Debug.Log("Texture file parsed successfully: " + textureData.imageFile);
        return textureData;
    }

    private void LoadTexture(string path)
    {
        if (!File.Exists(path))
        {
            Debug.LogError("Image file not found: " + path);
            return;
        }
        byte[] imageData = File.ReadAllBytes(path);
        mainTexture = new Texture2D(2, 2);
        mainTexture.LoadImage(imageData);
        Debug.Log("Image loaded successfully: " + path);
    }

    private void ExtractSubTextures(TextureData textureData)
    {
        foreach (var subTex in textureData.subTextures)
        {
            //Rect rect = new Rect(subTex.x, textureData.height - subTex.y - subTex.height, subTex.width, subTex.height);
            
            // Correct Y-flip (Unity uses bottom-left origin)
            Rect rect = new Rect(subTex.x, textureData.height - subTex.y - subTex.height, subTex.width, subTex.height);

            // Normalize pivot correctly (invert Y-axis)
            Vector2 pivot = new Vector2((float)subTex.offsetX / subTex.width, 1f - (float)subTex.offsetY / subTex.height);

            Sprite sprite = Sprite.Create(mainTexture, rect, pivot);
            extractedSprites.Add(sprite);
        }
        Debug.Log("Extracted " + extractedSprites.Count + " sprites from texture.");
    }
}
