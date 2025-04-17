using Newtonsoft.Json;
using System.Collections.Generic;
using UnityEngine;
using static MapLoader;

public class DeterministicVisualUpdater : MonoBehaviour, IDeterministicUpdate, IMapSaveLoad
{
    [JsonObject(MemberSerialization.OptIn)]
    public class DeterministicVisualUpdaterData : SaveLoadData
    {
        [JsonProperty] public float elapsedFixedTime = 0.0f;
        [JsonProperty] public bool isLooping = true;
        [JsonProperty] public float duration = 1.0f;
        [JsonProperty] public string spriteName = "";

        public DeterministicVisualUpdaterData() { type = "DeterministicVisualUpdaterData"; }
    }

    public float elapsedFixedTime { get; private set; }
    public string spriteName { get; private set; }
    public bool isLooping = true;
    public float duration = 1.0f;

    public delegate void OnPlayOrResumeDelegate(bool resume);
    public event OnPlayOrResumeDelegate OnPlayOrResumeEvent;

    public delegate void OnStopOrPauseDelegate(bool stop);
    public event OnStopOrPauseDelegate OnStopOrPauseEvent;

    public delegate void OnSetSpriteNameDelegate(string name);
    public event OnSetSpriteNameDelegate OnSetSpriteNameEvent;

    public delegate void OnLoadDelegate();
    public event OnLoadDelegate OnLoadEvent;

    public void PlayOrResume(bool resume)
    {
        if (!resume)
        {
            StopOrPause(true);
        }
        enabled = true;
        OnPlayOrResumeEvent?.Invoke(resume);
    }

    public void SetSpriteName(string name, bool force)
    {
        if (spriteName != name || force)
        {
            OpenageSpriteLoader.ReturnMinimalisticData returnMinimalisticData = OpenageSpriteLoader.Instance.RequestMinimalSpriteData(name);
            if (returnMinimalisticData != null)
            {
                isLooping = returnMinimalisticData.isLooping;
                duration = returnMinimalisticData.duration;
            } else
            {
                Debug.LogError("Failed to retrieve minimal sprite data! Determinism is probably failed!");
            }
            OnSetSpriteNameEvent?.Invoke(name);
        }
        spriteName = name;
    }

    public void StopOrPause(bool stop)
    {
        if (stop)
        {
            elapsedFixedTime = 0;
        }
        enabled = false;
        OnStopOrPauseEvent?.Invoke(stop);
    }

    private void OnEnable()
    {
        DeterministicUpdateManager.Instance.Register(this);
    }

    private void OnDisable()
    {
        DeterministicUpdateManager.Instance.Unregister(this);
    }

    public void DeterministicUpdate(float deltaTime, ulong tickID)
    {
        elapsedFixedTime += deltaTime;
        if (elapsedFixedTime >= duration)
        {
            if (!isLooping)
            {
                enabled = false;
            }
        }
    }

    public SaveLoadData Save()
    {
        DeterministicVisualUpdaterData deterministicVisualUpdaterData = new DeterministicVisualUpdaterData()
        {
            elapsedFixedTime = elapsedFixedTime,
            isLooping = isLooping,
            duration = duration,
            spriteName = spriteName,
        };
        return deterministicVisualUpdaterData;
    }

    public void Load(SaveLoadData data)
    {
        DeterministicVisualUpdaterData deterministicVisualUpdaterData = data as DeterministicVisualUpdaterData;
        if (deterministicVisualUpdaterData == null)
        {
            return;
        }

        elapsedFixedTime = deterministicVisualUpdaterData.elapsedFixedTime;
        //SetSpriteName(deterministicVisualUpdaterData.spriteName, true);
        isLooping = deterministicVisualUpdaterData.isLooping;
        duration = deterministicVisualUpdaterData.duration;
        spriteName = deterministicVisualUpdaterData.spriteName;
        OnSetSpriteNameEvent?.Invoke(spriteName);
    }

    public void PostLoad(SaveLoadData data)
    {
        OnLoadEvent?.Invoke();
        Debug.Log("Post Load called");
    }
}
