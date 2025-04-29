using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class ActionComponent : MonoBehaviour, IDeterministicUpdate, MapLoader.IMapSaveLoad
{
    public class ActionEvent
    {
        public float timer = 0;
        public object[] parameters = null;
        public bool hasTriggered = false;
        public UnitEventHandler.EventID eventId;

        public ActionEvent(float timer, UnitEventHandler.EventID eventId, List<object> parameters)
        {
            this.timer = timer;
            this.eventId = eventId;
            this.parameters = parameters.ToArray();
        }
    }

    public List<ActionEvent> actions = new List<ActionEvent>();

    public void DeterministicUpdate(float deltaTime, ulong tickID)
    {
        if (currentTime < duration)
        {
            currentTime += deltaTime;

            int count = actions.Count;
            for (int i = 0; i < count; i++)
            {
                ActionEvent action = actions[i];
                if (action == null) continue;
                if (currentTime < action.timer) continue;
                if (action.hasTriggered) continue;
                action.hasTriggered = true;
                UnitEventHandler.Instance.CallEventByID(action.eventId, action.parameters);
            }
        } else
        {
            OnEndAction();
            StopAction();
        }
    }

    public void OnEndAction()
    {
        int count = actions.Count;
        if (count > 0)
        {
            for (int i = 0; i < count; i++)
            {
                ActionEvent action = actions[i];
                if (action == null) continue;
                
                if (!action.hasTriggered)
                    UnitEventHandler.Instance.CallEventByID(action.eventId, action.parameters);
                action.hasTriggered = false;
            }
        }
        UnitEventHandler.Instance.CallEventByID(UnitEventHandler.EventID.OnActionEnd, endActionType, movableUnit.id);
    }

    public float GetCurrentTime() { return currentTime; }

    public void StopAction()
    {
        if (!enabled) return;
        
        currentTime = 0;
        if (movableUnit)
        {
            DeterministicVisualUpdater deterministicVisualUpdater = movableUnit.GetDeterministicVisualUpdater();
            string sprite = movableUnit.standSprite;
            if (movableUnit.movementComponent.movementState == MovementComponent.State.Moving)
                sprite = movableUnit.walkSprite;

            deterministicVisualUpdater.SetSpriteName(sprite, true);
            deterministicVisualUpdater.PlayOrResume(false);
            movableUnit.DecrementActionBlock();
        }
        enabled = false;
    }

    public void Load(MapLoader.SaveLoadData data)
    {
        throw new System.NotImplementedException();
    }

    public void PostLoad(MapLoader.SaveLoadData data)
    {
        throw new System.NotImplementedException();
    }

    public MapLoader.SaveLoadData Save()
    {
        throw new System.NotImplementedException();
    }

    private void OnEnable()
    {
        DeterministicUpdateManager.Instance.Register(this);
    }

    private void OnDisable()
    {
        DeterministicUpdateManager.Instance.Unregister(this);
    }

    [SerializeField]
    MovableUnit movableUnit;

    public string spriteName = "attacking";
    float currentTime = 0.0f;
    float duration = 1.0f;
    public string endActionType = "";

    private void Awake()
    {
        enabled = false;
    }

    public void SetActionSprite(string spriteName, string endActionType = "", List<ActionEvent> actionEvents = null)
    {
        this.spriteName = spriteName;
        this.endActionType = endActionType;
        this.actions.Clear();
        if (actionEvents != null)
        {
            actions.AddRange(actionEvents);
        }
    }

    public bool IsPlayingAction()
    {
        return enabled;
    }

    public void StartAction()
    {
        if (enabled) { return; }
        OpenageSpriteLoader.ReturnMinimalisticData minimalisticData = OpenageSpriteLoader.Instance.RequestMinimalSpriteData(spriteName);
        if (minimalisticData != null)
        {
            duration = minimalisticData.duration;
            enabled = true;
            if (movableUnit)
            {
                movableUnit.GetDeterministicVisualUpdater().SetSpriteName(spriteName, true);
                movableUnit.GetDeterministicVisualUpdater().PlayOrResume(false);
                currentTime = 0;
                movableUnit.IncrementActionBlock();
            }
        }
    }
}
