using System.Collections.Generic;
using System;
using Unity.Mathematics;
using UnityEngine;
using TMPro;

public class IdleUnitAIModule : UnitAIModule, IDeterministicUpdate, MapLoader.IMapSaveLoad
{
    public enum State
    {
        Idle
    }

    State currentState;

    MovableUnit self;

    bool IsUpdateable()
    {
        return StatComponent.IsUnitAliveOrValid(self);
    }

    bool IsUpdateBlockable()
    {
        return self.actionComponent.IsPlayingAction();
    }

    public new void DeterministicUpdate(float deltaTime, ulong tickID)
    {
        if (!IsUpdateable())
        {
            enabled = false;
            return;
        }

        if (IsUpdateBlockable())
        {
            return;
        }

        switch (currentState)
        {
            case State.Idle:
                {
                    State_Idle(deltaTime);
                }
                break;
            default:
                break;
        }
    }

    private void State_Idle(float deltaTime)
    {

    }

    private void Awake()
    {
        //enabled = false;
    }

    private void OnEnable()
    {
        DeterministicUpdateManager.Instance.Register(this);
    }

    private void OnDisable()
    {
        DeterministicUpdateManager.Instance.Unregister(this);
        self = null;
        currentState = State.Idle;
    }

    public void InitializeAI(MovableUnit self)
    {
        if (StatComponent.IsUnitAliveOrValid(self))
        {
            this.self = self;
            return;
        }

        if (enabled)
        {
            enabled = false;
        }
    }

    public new void Load(MapLoader.SaveLoadData data)
    {
        throw new System.NotImplementedException();
    }

    public new void PostLoad(MapLoader.SaveLoadData data)
    {
        throw new System.NotImplementedException();
    }

    public new MapLoader.SaveLoadData Save()
    {
        throw new System.NotImplementedException();
    }
}
