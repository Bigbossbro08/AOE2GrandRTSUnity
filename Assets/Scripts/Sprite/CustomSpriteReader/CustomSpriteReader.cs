using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.U2D;
using static TMPro.SpriteAssetUtilities.TexturePacker_JsonArray;

public class CustomSpriteReader : MonoBehaviour
{
    public string spriteName = "sprite_atlas"; // No extension
    [SerializeField] SpriteRenderer _renderer = null;

    [SerializeField] List<Sprite> sprites = new List<Sprite>();

    [SerializeField]
    private DeterministicVisualUpdater deterministicVisualUpdater;

    [SerializeField]
    private Transform mainTransform;

    [SerializeField]
    private Color playerColor;

    int currentFrame = 0;
    float angleOffset = 0.0f;
    float height = 0.0f;
    int currentAngleIndex = 0;
    int angleCount = 8;

    void Start()
    {
        transform.eulerAngles = new Vector3(30, -45, 0);
        //StartCoroutine(PlayAnimation());
    }

    void Initialize(string spriteName)
    {
        this.spriteName = spriteName;
        CustomSpriteLoader.SpriteReturnData spriteReturnData = CustomSpriteLoader.Instance.LoadSprite(spriteName);
        if (spriteReturnData != null)
        {
            angleOffset = spriteReturnData.rotation_offset;
            angleCount = spriteReturnData.sprites.Count;
            float angle = mainTransform.eulerAngles.y + angleOffset;
            int newAngleIndex = CustomSpriteLoader.GetFixed8DirectionAngle(angle, angleCount);
            currentAngleIndex = newAngleIndex;
            sprites = spriteReturnData.sprites[currentAngleIndex];
            height = spriteReturnData.height;
            transform.localPosition = new Vector3(0, height, 0);
        }
    }

    int GetFrame()
    {
        int frameCount = sprites.Count;
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

        if (sprites.Count == 0) return;

        // Calculate looping frame index
        int newFrame = GetFrame();

        ValidateSprites();
        PlaySprites(newFrame);
    }

    bool ValidateSprites(bool forceUpdate = false)
    {
        CustomSpriteLoader.SpriteReturnData spriteReturnData = null;
        if (forceUpdate)
        {
            spriteReturnData = CustomSpriteLoader.Instance.LoadSprite(spriteName);
            DeterministicVisualUpdater_OnRefreshEvent();
        }
        bool isValid = false;
        float angle = mainTransform.eulerAngles.y + angleOffset;
        int newAngleIndex = CustomSpriteLoader.GetFixed8DirectionAngle(angle, angleCount);
        
        if (newAngleIndex != currentAngleIndex || forceUpdate)
        {
            currentAngleIndex = newAngleIndex;
            if (spriteReturnData == null)
                spriteReturnData = CustomSpriteLoader.Instance.LoadSprite(spriteName);

            if (spriteReturnData != null)
            {
                sprites = spriteReturnData.sprites[currentAngleIndex];
                isValid = true;
            }
        }
        return isValid;
    }

    void PlaySprites(int newFrameID)
    {
        if (_renderer == null) return;
        if (_renderer.material == null) return;
        if (sprites.Count == 0) { return; }
        _renderer.sprite = sprites[currentFrame];
    }

    private void Update()
    {
        transform.localPosition = new Vector3(0, height, 0);
        transform.eulerAngles = new Vector3(30, -45, 0);
        NewUpdateFrame(Time.deltaTime);
    }

    private void OnEnable()
    {
        if (deterministicVisualUpdater)
        {
            deterministicVisualUpdater.RefreshVisuals();
            deterministicVisualUpdater.OnPlayOrResumeEvent += DeterministicVisualUpdater_OnPlayOrResumeEvent;
            deterministicVisualUpdater.OnStopOrPauseEvent += DeterministicVisualUpdater_OnStopOrPauseEvent;
            deterministicVisualUpdater.OnSetSpriteNameEvent += DeterministicVisualUpdater_OnSetSpriteNameEvent;
            deterministicVisualUpdater.OnLoadEvent += DeterministicVisualUpdater_OnLoadEvent;
            deterministicVisualUpdater.OnRefreshEvent += DeterministicVisualUpdater_OnRefreshEvent;
        }
    }


    private void OnDisable()
    {
        if (deterministicVisualUpdater)
        {
            deterministicVisualUpdater.OnPlayOrResumeEvent -= DeterministicVisualUpdater_OnPlayOrResumeEvent;
            deterministicVisualUpdater.OnStopOrPauseEvent -= DeterministicVisualUpdater_OnStopOrPauseEvent;
            deterministicVisualUpdater.OnSetSpriteNameEvent -= DeterministicVisualUpdater_OnSetSpriteNameEvent;
            deterministicVisualUpdater.OnLoadEvent -= DeterministicVisualUpdater_OnLoadEvent;
            deterministicVisualUpdater.OnRefreshEvent -= DeterministicVisualUpdater_OnRefreshEvent;
        }
    }

    private void DeterministicVisualUpdater_OnPlayOrResumeEvent(bool resume)
    {
        if (!resume)
        {
            //elapsedTime = 0.0f;
        }
        DeterministicVisualUpdater_OnRefreshEvent();
    }

    private void DeterministicVisualUpdater_OnStopOrPauseEvent(bool stop)
    {
        if (stop)
        {
            //elapsedTime = 0.0f;
        }
        DeterministicVisualUpdater_OnRefreshEvent();
    }

    private void DeterministicVisualUpdater_OnSetSpriteNameEvent(string name)
    {
        this.spriteName = name;
        Initialize(name);
        DeterministicVisualUpdater_OnRefreshEvent();
    }

    private void DeterministicVisualUpdater_OnLoadEvent()
    {
        throw new System.NotImplementedException();
    }

    private void DeterministicVisualUpdater_OnRefreshEvent()
    {
        PlayerData playerData = UnitManager.Instance.GetPlayerData(deterministicVisualUpdater.playerId);
        if (playerData != null)
        {
            // TODO: Add player color support
            CustomSpriteLoader.SpriteReturnData spriteReturnData = CustomSpriteLoader.Instance.LoadSprite(spriteName);
            _renderer.material.mainTexture = spriteReturnData.mainTexture;
            MaterialPropertyBlock block = new MaterialPropertyBlock();
            _renderer.GetPropertyBlock(block);
            playerColor = playerData.color;
            block.SetTexture("_MainTex", spriteReturnData.mainTexture);
            block.SetTexture("_MaskTex", spriteReturnData.maskTexture);
            block.SetColor("_PlayerColor", playerColor);
            _renderer.SetPropertyBlock(block);

            NewUpdateFrame(Time.deltaTime);
        }
    }
}
