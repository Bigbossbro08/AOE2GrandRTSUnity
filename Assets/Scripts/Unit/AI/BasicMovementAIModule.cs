using System;
using UnityEngine;
using UnityEngine.AI;
using static UnityEngine.GraphicsBuffer;

public class BasicMovementAIModule : UnitAIModule, IDeterministicUpdate, MapLoader.IMapSaveLoad
{
    public enum State
    {
        WaitingForPath,
        MoveTowardsPoint,
        ReachedDestination
    }

    State currentState;
    State desiredState;
    protected MovableUnit self;
    internal Vector3 position;
    internal Vector3 offset;
    internal Vector3? startPosition;

    void DoPathfind()
    {
        self.movementComponent.StartPathfind(position, offset: this.offset, startPosition: this.startPosition);
        position = self.movementComponent.GetLastPointInPathfinding();
    }

    public virtual void OnChangeState(State newState)
    {

    }

    void ChangeState(State newState, bool force = false)
    {
        if (newState == currentState && !force) { return; }

        // Used for cleaning up
        switch (newState)
        {
            case State.WaitingForPath:
                {
                    DoPathfind();
                }
                break;
            case State.MoveTowardsPoint:
                {

                }
                break;
            case State.ReachedDestination:
                {
                    self.movementComponent.Stop();
                    //if (self.shipData != null && self.shipData.isShipMode && self.shipData.isDocked)
                    //{
                    //    self.movementComponent.rb.isKinematic = true;
                    //}

                    self.ResetToDefaultModule();
                    enabled = false;
                }
                break;
        }
        OnChangeState(newState);
        currentState = newState;
    }

    bool IsUpdateable()
    {
        return StatComponent.IsUnitAliveOrValid(self);
    }

    bool IsUpdateBlockable()
    {
        return self.actionComponent.IsPlayingAction();
    }

    private void Process_WaitingForPath()
    {
        if (!self.movementComponent.HasPathPositions())
        {
            return;
        }
        SetDesiredState(State.MoveTowardsPoint);
    }

    public virtual void Process_MoveTowardsPoint()
    {
        if (self.movementComponent.movementState == MovementComponent.State.Idle)
        {
            SetDesiredState(State.ReachedDestination);
        }
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

        if (currentState != desiredState)
        {
            ChangeState(desiredState);
            currentState = desiredState;
        }

        switch (currentState)
        {
            case State.WaitingForPath:
                {
                    Process_WaitingForPath();
                }
                break;
            case State.MoveTowardsPoint:
                {
                    Process_MoveTowardsPoint();
                }
                break;
            case State.ReachedDestination:
                {
                    self.ResetToDefaultModule();
                    enabled = false;
                }
                break;
            default:
                break;
        }
    }

    private void OnEnable()
    {
        DeterministicUpdateManager.Instance.Register(this);
    }

    private void OnDisable()
    {
        DeterministicUpdateManager.Instance.Unregister(this);
        self = null;
        currentState = State.MoveTowardsPoint;
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

    public void SetDesiredState(State newState, bool force = false)
    {
        desiredState = newState;

        if (!IsUpdateBlockable())
        {
            ChangeState(desiredState, force);
            currentState = newState;
        }
    }

    public void InitializeAI(MovableUnit self, Vector3 position, ulong crowdId, Vector3 offset, Vector3? startPosition = null)
    {
        if (StatComponent.IsUnitAliveOrValid(self))
        {
            if (self.IsShip() && !self.shipData.IsDrivable())
            {
                return;
            }
            this.self = self;
            this.position = position;
            this.offset = offset;
            this.startPosition = startPosition;
            self.movementComponent.crowdID = crowdId;
            currentState = State.MoveTowardsPoint;
            SetDesiredState(State.WaitingForPath, true);
            enabled = true;
            return;
        }

        if (enabled)
        {
            enabled = false;
        }
    }
}
