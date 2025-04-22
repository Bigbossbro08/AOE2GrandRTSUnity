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

    private void DeterministicVisualUpdater_OnStopOrPauseEvent(bool stop)
    {
        if (stop)
        {
            //elapsedTime = 0.0f;
        }
        //enabled = false;
    }

    private void DeterministicVisualUpdater_OnPlayOrResumeEvent(bool resume)
    {
        if (!resume)
        {
            //elapsedTime = 0.0f;
        }
        //enabled = true;
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
            Debug.Log("UPDATED PLAYER COLOR");
            block.SetColor("_HighlightColor", playerColor);
            spriteRenderer.SetPropertyBlock(block);
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

    static int PredictCurrentFrameFromDuration(float elapsedTime, float duration, int frameCount)
    {
        // Calculate looping frame index
        int newFrame = Mathf.FloorToInt((elapsedTime / duration) * frameCount) % frameCount;
        return newFrame;
    }

    void NewUpdateFrame(float deltaTime)
    {
        if (deterministicVisualUpdater == null) return;

        if (spriteFrames.Count == 0) return;

        int frameCount = spriteFrames.Count;

        // Calculate looping frame index
        int newFrame = Mathf.FloorToInt((deterministicVisualUpdater.elapsedFixedTime / deterministicVisualUpdater.duration) * frameCount) % frameCount;

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
