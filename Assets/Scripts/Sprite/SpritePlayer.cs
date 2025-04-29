using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class SpritePlayer : MonoBehaviour
{
    string spriteName = "archer_standing";
    int currentAngleIndex = 0;
    bool isMirrored = false;

    [SerializeField]
    private DeterministicVisualUpdater deterministicVisualUpdater;

    [SerializeField]
    private Transform mainTransform;

    private List<Sprite> spriteFrames = new List<Sprite>();

    [SerializeField]
    SpriteRenderer spriteRenderer;

    [SerializeField] Color playerColor = Color.red;

    [SerializeField] int currentFrame = 0;

    private void DeterministicVisualUpdater_OnStopOrPauseEvent(bool stop)
    {
        if (stop)
        {
            //elapsedTime = 0.0f;
        }
        //enabled = false;
        //DeterministicVisualUpdater_OnRefreshEvent();
    }

    private void DeterministicVisualUpdater_OnPlayOrResumeEvent(bool resume)
    {
        if (!resume)
        {
            //elapsedTime = 0.0f;
        }
        //enabled = true;
        DeterministicVisualUpdater_OnRefreshEvent();
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        transform.eulerAngles = new Vector3(30, -45, 0);
    }

    void PlaySprites(int newFrameID, bool isMirrored)
    {
        if (spriteRenderer == null) return;
        if (spriteRenderer.material == null) return;
        if (spriteFrames.Count == 0) { return; }

        MaterialPropertyBlock block = new MaterialPropertyBlock();
        spriteRenderer.GetPropertyBlock(block);
        block.SetColor("_HighlightColor", playerColor);
        if (spriteFrames.Count > 0)
        {
            block.SetTexture("_MainTex", spriteFrames[0].texture);
        }
        spriteRenderer.SetPropertyBlock(block);
        spriteRenderer.flipX = isMirrored;
        spriteRenderer.sprite = spriteFrames[newFrameID];
    }

    void ValidateSprites(bool forceUpdate = false)
    {
        int newAngle = OpenageSpriteLoader.PredictAngleBasedOnRotation(mainTransform.eulerAngles.y,true);
        if (currentAngleIndex != newAngle || forceUpdate)
        {
            isMirrored = OpenageSpriteLoader.CanBeMirrored(mainTransform.eulerAngles.y);

            OpenageSpriteLoader.ReturnSpriteData spriteData = OpenageSpriteLoader.Instance.RequestSprite(spriteName, mainTransform.eulerAngles.y, true);
            if (spriteData != null)
            {
                spriteFrames = spriteData.sprites;
            }
            currentAngleIndex = newAngle;
        }
    }

    private void OnEnable()
    {
        deterministicVisualUpdater.RefreshVisuals();
        deterministicVisualUpdater.OnPlayOrResumeEvent += DeterministicVisualUpdater_OnPlayOrResumeEvent;
        deterministicVisualUpdater.OnStopOrPauseEvent += DeterministicVisualUpdater_OnStopOrPauseEvent;
        deterministicVisualUpdater.OnSetSpriteNameEvent += DeterministicVisualUpdater_OnSetSpriteNameEvent;
        deterministicVisualUpdater.OnLoadEvent += DeterministicVisualUpdater_OnLoadEvent;
        deterministicVisualUpdater.OnRefreshEvent += DeterministicVisualUpdater_OnRefreshEvent;
    }

    private void DeterministicVisualUpdater_OnLoadEvent()
    {
        ValidateSprites(true);
    }

    private void DeterministicVisualUpdater_OnSetSpriteNameEvent(string name)
    {
        this.spriteName = name;
        ValidateSprites(true);
    }

    private void DeterministicVisualUpdater_OnRefreshEvent()
    {
        PlayerData playerData = UnitManager.Instance.GetPlayerData(deterministicVisualUpdater.playerId);
        if (playerData != null)
        {
            MaterialPropertyBlock block = new MaterialPropertyBlock();
            spriteRenderer.GetPropertyBlock(block);
            playerColor = playerData.color;
            block.SetColor("_HighlightColor", playerColor);
            spriteRenderer.SetPropertyBlock(block);

            int newFrame = GetFrame();

            if (spriteFrames[newFrame])
            {
                spriteRenderer.sprite = spriteFrames[newFrame];
            }
            isMirrored = OpenageSpriteLoader.CanBeMirrored(mainTransform.eulerAngles.y);
            spriteRenderer.flipX = isMirrored;
        }
    }

    private void OnDisable()
    {
        deterministicVisualUpdater.OnPlayOrResumeEvent -= DeterministicVisualUpdater_OnPlayOrResumeEvent;
        deterministicVisualUpdater.OnStopOrPauseEvent -= DeterministicVisualUpdater_OnStopOrPauseEvent;
        deterministicVisualUpdater.OnSetSpriteNameEvent -= DeterministicVisualUpdater_OnSetSpriteNameEvent;
        deterministicVisualUpdater.OnLoadEvent -= DeterministicVisualUpdater_OnLoadEvent;
        deterministicVisualUpdater.OnRefreshEvent -= DeterministicVisualUpdater_OnRefreshEvent;
    }

    //int GetFrame()
    //{
    //    int frameCount = spriteFrames.Count;
    //
    //    float elapsedTime = deterministicVisualUpdater.elapsedFixedTime;
    //    float duration = deterministicVisualUpdater.duration;
    //
    //    // Calculate looping frame index
    //    int newFrame = Mathf.FloorToInt((elapsedTime / duration) * frameCount) % frameCount;
    //
    //    if (elapsedTime == duration && newFrame == 0)
    //    {
    //        Debug.LogAssertion($"Error in calculation. Values are frameCount:{frameCount} elapsedTime:{elapsedTime} duration:{duration} newFrame:{newFrame}");
    //    }
    //    return newFrame;
    //}

    int GetFrame()
    {
        int frameCount = spriteFrames.Count;
        float elapsedTime = deterministicVisualUpdater.elapsedFixedTime;
        float duration = deterministicVisualUpdater.duration;

        // Handle edge case where elapsedTime equals/exceeds duration
        float adjustedTime = elapsedTime - 1e-6f; // Tiny epsilon to prevent wrap-around

        // Calculate frame index
        int newFrame = Mathf.FloorToInt((adjustedTime / duration) * frameCount);

        // Loop the animation and clamp to valid frames
        newFrame = newFrame % frameCount;
        newFrame = Mathf.Clamp(newFrame, 0, frameCount - 1);
        if (!deterministicVisualUpdater.isLooping && elapsedTime >= duration)
        {
            //Debug.Log($"Frame isn't looping but value changed back to 0 {newFrame < currentFrame}");
            newFrame = frameCount - 1;
        }
        currentFrame = newFrame;

        return newFrame;
    }

    void NewUpdateFrame(float deltaTime)
    {
        if (deterministicVisualUpdater == null) return;

        if (spriteFrames.Count == 0) return;

        // Calculate looping frame index
        int newFrame = GetFrame();

        ValidateSprites();
        PlaySprites(newFrame, isMirrored);
    }

    [ContextMenu("Take snapshot of current sprite")]
    //public static void SaveSpriteAsPNG(Sprite sprite)
    public void SaveSpriteAsPNG()
    {
        Sprite sprite = spriteRenderer.sprite;
        if (sprite == null)
        {
            Debug.LogError("Please select a sprite in the Project window!");
            return;
        }

        string path = EditorUtility.SaveFilePanel("Save PNG", Application.dataPath, "ExportedSprite", "png");
        if (string.IsNullOrEmpty(path))
            return;

        // Create texture
        Texture2D texture = new Texture2D((int)sprite.rect.width, (int)sprite.rect.height);
        Color[] pixels = sprite.texture.GetPixels(
            (int)sprite.rect.x, (int)sprite.rect.y,
            (int)sprite.rect.width, (int)sprite.rect.height);
        texture.SetPixels(pixels);
        texture.Apply();

        // Save PNG
        byte[] bytes = texture.EncodeToPNG();
        System.IO.File.WriteAllBytes(path, bytes);
        Debug.Log($"Sprite saved to: {path}");

        // Cleanup
        DestroyImmediate(texture);
    }

    // Update is called once per frame
    void Update()
    {
        transform.eulerAngles = new Vector3(30, -45, 0);
        NewUpdateFrame(Time.deltaTime);
    }

    bool IsObjectInView(GameObject obj)
    {
        Plane[] frustumPlanes = GeometryUtility.CalculateFrustumPlanes(Camera.main);
        Bounds bounds = obj.GetComponent<Collider>().bounds; // Use collider bounds

        return GeometryUtility.TestPlanesAABB(frustumPlanes, bounds);
    }
}
