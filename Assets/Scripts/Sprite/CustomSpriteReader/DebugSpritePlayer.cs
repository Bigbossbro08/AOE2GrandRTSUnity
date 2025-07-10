using UnityEngine;
using System.Collections.Generic;
using System.IO;

[RequireComponent(typeof(SpriteRenderer))]
public class DebugSpritePlayer : MonoBehaviour
{
    public string spriteName; // The name of the sprite file (without .json/.png)
    public float angle;       // Current direction angle (in degrees)

    private SpriteRenderer spriteRenderer;
    private CustomSpriteLoader.SpriteReturnData spriteData;

    private int currentFrame;
    private float timer;
    private int currentAngleIndex;
    private bool isPlaying = false;
    private int angleCount = 8;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    void Start()
    {
        spriteName = "data\\military_units\\Rodelero\\Rodelero_decay\\sprite_atlas";
        Load(spriteName);
    }

    public void Load(string spriteFile)
    {
        spriteData = CustomSpriteLoader.Instance.LoadSprite(spriteFile);
        angleCount = spriteData.sprites.Count;
        currentFrame = 0;
        timer = 0f;
        isPlaying = true;
        UpdateAngleGroup(); // Set currentAngleIndex based on initial angle
    }

    public void Play()
    {
        isPlaying = true;
    }

    public void Stop()
    {
        isPlaying = false;
    }

    void Update()
    {
        if (!isPlaying || spriteData == null) return;

        float frameDuration = spriteData.duration / spriteData.frames_per_angle;

        timer += Time.deltaTime;

        if (timer >= frameDuration)
        {
            timer -= frameDuration;
            currentFrame++;

            if (currentFrame >= spriteData.frames_per_angle)
            {
                if (spriteData.isLooping)
                {
                    currentFrame = 0;
                }
                else
                {
                    currentFrame = spriteData.frames_per_angle - 1;
                    isPlaying = false;
                }
            }

            RenderFrame();
        }

        // Optional: dynamically update direction
        UpdateAngleGroup(); // if direction can change dynamically
    }

    void UpdateAngleGroup()
    {
        int newAngleIndex = CustomSpriteLoader.GetFixed8DirectionAngle(angle + spriteData.rotation_offset, angleCount);
        if (newAngleIndex != currentAngleIndex)
        {
            currentAngleIndex = newAngleIndex;
            currentFrame = Mathf.Min(currentFrame, spriteData.frames_per_angle - 1);
            RenderFrame();
        }
    }

    void RenderFrame()
    {
        if (spriteData.sprites.TryGetValue(currentAngleIndex, out var frames) && frames.Count > currentFrame)
        {
            spriteRenderer.sprite = frames[currentFrame];
        }
    }

    /// <summary>
    /// Update the direction the sprite is facing (in degrees).
    /// </summary>
    /// <param name="newAngle">Angle in degrees (0 = right, 90 = up)</param>
    public void SetDirection(float newAngle)
    {
        angle = newAngle;
    }

    public void Restart()
    {
        currentFrame = 0;
        timer = 0;
        isPlaying = true;
        RenderFrame();
    }
}
