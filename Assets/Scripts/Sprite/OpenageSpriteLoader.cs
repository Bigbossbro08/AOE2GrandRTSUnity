using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.U2D;
using static TMPro.SpriteAssetUtilities.TexturePacker_JsonArray;
using static UnityEditor.U2D.ScriptablePacker;
using Newtonsoft.Json;
using static UnityEditor.Experimental.GraphView.GraphView;
using System.Linq;

public class SpriteData
{
    public int x, y, w, h, cx, cy;
}

public class SpriteDataStorage
{
    // Texture ref to save
    public Texture2D texture = null;

    // list per angle
    public Dictionary<int, List<Sprite>> sprites = new Dictionary<int, List<Sprite>>();

    public float frameTime = 1.0f;

    // How many frames in each angle
    // public int framesPerAngle = 1;
}

public class NewSpriteDataStorage
{
    // Texture ref to save
    public Texture2D texture = null;

    public Dictionary<int, int> angles = new Dictionary<int, int>();

    // list per angle
    public Dictionary<int, List<Sprite>> sprites = new Dictionary<int, List<Sprite>>();

    public float frameTime = 1.0f;

    public bool isLooping = true;

    // How many frames in each angle
    // public int framesPerAngle = 1;
}

public class SpriteMetaDataJSON
{
    public int framesPerAngle = 1;
    public float frameTime = 1.0f;
}

public static class NewFormatLoader
{
    // Ensures the angle is always within [0, 360)
    public static float NormalizeAngle(float angle)
    {
        angle %= 360; // Keep within -360 to 360
        if (angle < 0) angle += 360; // Convert negatives to positive
        return angle;
    }

    // Snaps the angle to the nearest 8 directions (0, 45, 90, ..., 315)
    public static float GetFixed8DirectionAngle(float angle)
    {
        angle = NormalizeAngle(angle); // Ensure within 0-360
        int index = (int)Math.Round(angle / 45.0) % 8; // Find closest direction index
        return index * 45.0f; // Convert back to angle
    }

    public struct SubTexture
    {
        public int x, y, width, height, offsetX, offsetY;
    }

    public struct TextureData
    {
        public string imageFile;
        public int width;
        public int height;
        public List<SubTexture> subTextures;
    }

    public class SpriteData
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
        public List<FrameData> frameDatas = new List<FrameData>();
    }

    public static SpriteData ParseSpriteFile(string path)
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
                    spriteData.textureNames.Add(int.Parse(parts[1]), parts[2].Replace("\"", ""));
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
                case "frame":
                    int frameIndex = int.Parse(parts[1]);
                    int angle = int.Parse(parts[2]);
                    int layerID = int.Parse(parts[3]);
                    int imageID = int.Parse(parts[4]);
                    int subtexID = int.Parse(parts[5]);
                    SpriteData.FrameData frameData = new SpriteData.FrameData(frameIndex, angle, layerID, imageID, subtexID);
                    spriteData.frameDatas.Add(frameData);
                    break;
                default:
                    break;
            }
        }
        return spriteData;
    }

    public static TextureData ParseTextureFile(string path)
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
                        height = int.Parse(parts[4]),
                        offsetX = int.Parse(parts[5]),
                        offsetY = int.Parse(parts[6])
                    });
                    break;
            }
        }
        Debug.Log("Texture file parsed successfully: " + textureData.imageFile);
        return textureData;
    }

    public static Texture2D LoadTexture(string path)
    {
        Texture2D texture = new Texture2D(2, 2);
        if (!File.Exists(path))
        {
            Debug.LogError("Image file not found: " + path);
            return null;
        }
        byte[] imageData = File.ReadAllBytes(path);
        texture.LoadImage(imageData);
        Debug.Log("Image loaded successfully: " + path);
        return texture;
    }

    public static List<Sprite> ExtractSubTextures(Texture2D texture, TextureData textureData)
    {
        List<Sprite> sprites = new List<Sprite>();
        foreach (var subTex in textureData.subTextures)
        {
            //Rect rect = new Rect(subTex.x, textureData.height - subTex.y - subTex.height, subTex.width, subTex.height);

            // Correct Y-flip (Unity uses bottom-left origin)
            Rect rect = new Rect(subTex.x, textureData.height - subTex.y - subTex.height, subTex.width, subTex.height);

            // Normalize pivot correctly (invert Y-axis)
            Vector2 pivot = new Vector2((float)subTex.offsetX / subTex.width, 1f - (float)subTex.offsetY / subTex.height);

            Sprite sprite = Sprite.Create(texture, rect, pivot);
            sprites.Add(sprite);
        }
        Debug.Log("Extracted " + sprites.Count + " sprites from texture.");
        return sprites;
    }
}

public class OpenageSpriteLoader : MonoBehaviour
{
    public class ReturnSpriteData
    {
        public List<Sprite> sprites;
        public float duration = 1.0f;
        public bool isMirrored = false;
        public bool isLooping = true;
    }

    public class ReturnMinimalisticData
    {
        public float duration = 1.0f;
        public bool isLooping = true;
    }

    public string graphicsPath = "C:\\Users\\USER\\source\\repos\\openage\\assets\\converted\\graphics\\";
    public Dictionary<string, SpriteDataStorage> spriteDataStorageDict = new Dictionary<string, SpriteDataStorage>();

    public string newGraphicsPath = "E:\\repos\\AOE2GrandRTSUnityFiles\\converted\\hd_base\\data\\game_entity\\"; //"E:\\repos\\AOE2GrandRTSUnityFiles\\converted\\hd_base\\data\\game_entity\\";
    
    public Dictionary<string, NewSpriteDataStorage> newSpriteDataStorageDict = new Dictionary<string, NewSpriteDataStorage>();

    public static OpenageSpriteLoader Instance { get; private set; }

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
        newGraphicsPath = "E:\\repos\\AOE2GrandRTSUnityFiles\\converted\\hd_base\\data\\game_entity\\";
    }

    public static int GetQuadrantIndex(float angle)
    {
        // Divide by 45° to get an index from 0 to 7
        return Mathf.FloorToInt(angle / 45f) % 8;
    }

    public static float NormalizedAngle(float angle)
    {
        // Normalize the angle between 0 and 360 degrees
        angle = angle % 360;
        if (angle < 0) angle += 360;
        return angle;
    }

    public static bool CanBeMirrored(float angle)
    {
        bool isMirrored = true;

        angle = NormalizedAngle(angle);

        if (angle > 135 && angle < 315)
        {
            isMirrored = false;
        }
        return isMirrored;
    }

    public static int PredictAngleBasedOnRotation(float angle, bool newMethod = false)
    {
        if (newMethod)
        {
            return (int)NewFormatLoader.GetFixed8DirectionAngle(angle - 135); ;
        }
        int angleIndex = 3;
        int index = GetQuadrantIndex(angle + 135 - 22.5f - 45);
        index -= 4;
        angleIndex = Mathf.Abs(index);
        return angleIndex;
    }

    private ReturnSpriteData OldRequestSprite(string spriteName, int angleIndex)
    {
        ReturnSpriteData returnSpriteData = null;

        if (spriteDataStorageDict.ContainsKey(spriteName))
        {
            returnSpriteData = new ReturnSpriteData()
            {
                sprites = spriteDataStorageDict[spriteName].sprites[angleIndex],
                duration = spriteDataStorageDict[spriteName].frameTime
            };
            return returnSpriteData;
        }

        string docPath = Path.Combine(graphicsPath, spriteName + ".docx");
        List<SpriteData> spriteDatas = LoadSpriteData(docPath);
        string pngPath = Path.Combine(graphicsPath, spriteName + ".png");
        Texture2D texture = LoadTextureFromFile(pngPath);
        List<Sprite> spriteFrames = PrepareSprites(spriteDatas, texture);
        string jsonPath = Path.Combine(graphicsPath, spriteName + ".json");
        SpriteMetaDataJSON spriteMetaData = JsonUtility.FromJson<SpriteMetaDataJSON>(File.ReadAllText(jsonPath));
        int framesPerAngle = spriteMetaData.framesPerAngle;
        int countOfSpriteAngles = (spriteFrames.Count / framesPerAngle);

        Dictionary<int, List<Sprite>> listOfSpriteFrames = new Dictionary<int, List<Sprite>>(countOfSpriteAngles + 1);
        for (int i = 0; i < countOfSpriteAngles; i++)
        {
            List<Sprite> newSpriteFrames = new List<Sprite>(framesPerAngle + 1);
            int j = 0;
            for (; j < spriteFrames.Count; j++)
            {
                newSpriteFrames.Add(spriteFrames[j]);
            }
            listOfSpriteFrames.Add(i, newSpriteFrames);
        }

        Debug.Log($"{spriteFrames.Count} and {listOfSpriteFrames.Count}");
        SpriteDataStorage spriteDataStorage = new SpriteDataStorage
        {
            texture = texture,
            sprites = listOfSpriteFrames,
            frameTime = spriteMetaData.frameTime
            //framesPerAngle = framesPerAngle
        };

        spriteDataStorageDict.Add(spriteName, spriteDataStorage);
        returnSpriteData = new ReturnSpriteData
        {
            sprites = spriteDataStorageDict[spriteName].sprites[angleIndex],
            duration = spriteDataStorageDict[spriteName].frameTime
        };
        return returnSpriteData;
    }

    private ReturnSpriteData NewSpriteRequest(string spriteName, int angleIndex)
    {
        ReturnSpriteData returnSpriteData = null;
        int newAngleIndex = 0;
        if (newSpriteDataStorageDict.ContainsKey(spriteName))
        {
            newAngleIndex = newSpriteDataStorageDict[spriteName].angles[angleIndex];
            returnSpriteData = new ReturnSpriteData()
            {
                sprites = newSpriteDataStorageDict[spriteName].sprites[newAngleIndex],
                duration = newSpriteDataStorageDict[spriteName].frameTime,
                isLooping = newSpriteDataStorageDict[spriteName].isLooping
            };
            return returnSpriteData;
        }
        
        //Debug.Log($"{newGraphicsPath} {spriteName}");
        string spritePath = Path.Combine(newGraphicsPath, spriteName + ".sprite");
        NewFormatLoader.SpriteData spriteData = NewFormatLoader.ParseSpriteFile(spritePath);

        string basePath = Path.GetDirectoryName(spritePath);
        // For now
        string textureName = spriteData.textureNames[0];
        Debug.Log(textureName);
        string texturePath = Path.Combine(basePath, textureName);
        NewFormatLoader.TextureData textureData = NewFormatLoader.ParseTextureFile(texturePath);
        string pngPath = Path.Combine(basePath, textureData.imageFile);
        Texture2D texture = NewFormatLoader.LoadTexture(pngPath);
        List<Sprite> spriteFrames = NewFormatLoader.ExtractSubTextures(texture, textureData);

        int countOfSpriteAngles = spriteData.angles.Count;
        Dictionary<int, List<Sprite>> listOfSpriteFrames = new Dictionary<int, List<Sprite>>(countOfSpriteAngles + 1);

        int currentFrameIdx = -1;

        foreach (var f in spriteData.frameDatas)
        {
            if (currentFrameIdx < f.frame_idx)
                currentFrameIdx = f.frame_idx;

            // Try to get the existing list; if missing, create and store it
            if (!listOfSpriteFrames.TryGetValue(f.angle, out List<Sprite> newSpriteFrames))
            {
                newSpriteFrames = new List<Sprite>();
                listOfSpriteFrames.Add(f.angle, newSpriteFrames);
            }

            // Ensure that spriteFrames[f.subtex_id] exists before adding
            if (spriteFrames[f.subtex_id] == null)
            {
                Debug.LogWarning($"Sprite frame at index {f.subtex_id} is null!");
                continue; // Skip adding to avoid errors
            }

            newSpriteFrames.Add(spriteFrames[f.subtex_id]); // Add sprite frame
        }

        currentFrameIdx++;

        // 0 for now
        float frameDuration = currentFrameIdx * spriteData.layerDatas[0].timePerFrame;

        NewSpriteDataStorage spriteDataStorage = new NewSpriteDataStorage
        {
            texture = texture,
            sprites = listOfSpriteFrames,
            frameTime = frameDuration,
            angles = spriteData.angles,
            isLooping = spriteData.layerDatas[0].mode == NewFormatLoader.SpriteData.LayerData.Mode.Loop
            //framesPerAngle = framesPerAngle
        };

        newSpriteDataStorageDict.Add(spriteName, spriteDataStorage);
        newAngleIndex = newSpriteDataStorageDict[spriteName].angles[angleIndex];
        returnSpriteData = new ReturnSpriteData()
        {
            sprites = newSpriteDataStorageDict[spriteName].sprites[newAngleIndex],
            duration = frameDuration,
            isLooping = newSpriteDataStorageDict[spriteName].isLooping
        };
        return returnSpriteData;
    }

    public bool LoadSprite(string spriteName, float angle = 0.0f)
    {
        int angleIndex = PredictAngleBasedOnRotation(angle, true);
        ReturnSpriteData returnSpriteData = NewSpriteRequest(spriteName, angleIndex);
        if (returnSpriteData != null) return true;
        return false;
    }

    public ReturnSpriteData RequestSprite(string spriteName, float angle, bool newFormat = false)
    {
        int angleIndex = PredictAngleBasedOnRotation(angle, newFormat);
        bool isMirrored = CanBeMirrored(angle);
        if (newFormat) {
            ReturnSpriteData returnSpriteData = NewSpriteRequest(spriteName, angleIndex);
            returnSpriteData.isMirrored = isMirrored;
            return returnSpriteData;
        } else {
            ReturnSpriteData returnSpriteData = OldRequestSprite(spriteName, angleIndex);
            returnSpriteData.isMirrored = isMirrored;
            return returnSpriteData;
        }
    }

    public ReturnMinimalisticData RequestMinimalSpriteData(string spriteName)
    {
        ReturnMinimalisticData returnMinimalisticData = null;
        if (newSpriteDataStorageDict.TryGetValue(spriteName, out NewSpriteDataStorage value))
        {
            returnMinimalisticData = new ReturnMinimalisticData()
            {
                isLooping = value.isLooping,
                duration = value.frameTime
            };
            return returnMinimalisticData;
        }

        ReturnSpriteData returnSpriteData = RequestSprite(spriteName, 0, true);
        if (returnSpriteData != null)
        {
            returnMinimalisticData = new ReturnMinimalisticData()
            {
                isLooping = returnSpriteData.isLooping,
                duration = returnSpriteData.duration
            };
            return returnMinimalisticData;
        }

        return returnMinimalisticData;
    }

    List<Sprite> PrepareSprites(List<SpriteData> spriteDatas, Texture2D texture)
    {
        List<Sprite> sprites = new List<Sprite>();
        foreach (var data in spriteDatas)
        {
            // Correct Y-flip (Unity uses bottom-left origin)
            Rect rect = new Rect(data.x, texture.height - data.y - data.h, data.w, data.h);

            // Normalize pivot correctly (invert Y-axis)
            Vector2 pivot = new Vector2((float)data.cx / data.w, 1f - (float)data.cy / data.h);

            Sprite sprite = Sprite.Create(texture, rect, pivot, 100f);
            sprites.Add(sprite);
        }
        return sprites;
    }

    List<SpriteData> LoadSpriteData(string docxFilePath)
    {
        if (!File.Exists(docxFilePath)) return null;
        string text = File.ReadAllText(docxFilePath);
        string[] lines = text.Split('\n');
        List<SpriteData> sprites = new List<SpriteData>();
        foreach (string line in lines)
        {
            if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#")) continue;

            string[] values = line.Split(',');
            if (values.Length < 6) continue;

            SpriteData data = new SpriteData
            {
                x = int.Parse(values[0]),
                y = int.Parse(values[1]),
                w = int.Parse(values[2]),
                h = int.Parse(values[3]),
                cx = int.Parse(values[4]),
                cy = int.Parse(values[5])
            };

            sprites.Add(data);
        }
        return sprites;
    }

    Texture2D LoadTextureFromFile(string path)
    {
        if (!File.Exists(path)) return null;
        byte[] fileData = File.ReadAllBytes(path);
        Texture2D tex = new Texture2D(2, 2);
        if (tex.LoadImage(fileData)) return tex;
        return null;
    }


}
