using System;
using TMPro;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UIElements;

public class TargetFollowingMovementAIModule : UnitAIModule, IDeterministicUpdate, MapLoader.IMapSaveLoad
{
    public enum State
    {
        MoveTowardsTarget,
        CloseToTarget,
        NoTarget,
        FindTarget
    }

    State desiredState;
    State currentState;
    MovableUnit self;
    MovableUnit target;
    float3 targetPosition;
    bool resetTargetOnClose = false;
    Vector3 offset;

    const float thresholdForRepath = (float)(0.14 * DeterministicUpdateManager.FixedStep);
    const float thresholdForRepathSqr = thresholdForRepath * thresholdForRepath;

    const float thresholdForTarget = (0.14f + 0.05f);
    const float thresholdForTargetSqr = thresholdForTarget * thresholdForTarget;

    void ChangeState(State newState)
    {
        if (newState == currentState) return;

        if (!StatComponent.IsUnitAliveOrValid(target))
        {
            newState = State.NoTarget;
        }

        // Used for cleaning up
        switch (newState)
        {
            case State.MoveTowardsTarget:
                {
                    DoTargetFollow();
                }
                break;
            case State.CloseToTarget:
                {
                    self.movementComponent.Stop();
                }
                break;
            case State.NoTarget:
                {
                    self.ResetToDefaultModule();
                    enabled = false;
                }
                break;
        }
        currentState = newState;
    }

    bool CanTargetFollow()
    {
        if (self.actionComponent.IsPlayingAction())
            return false;
        return true;
    }

    private void DoTargetFollow(bool force = false)
    {
        if (!CanTargetFollow()) { return; }
        float3 newTargetPosition = target.transform.TransformPoint(offset);
        float3 diff = targetPosition - newTargetPosition;
        if (math.lengthsq(diff) > thresholdForRepathSqr || force)
        {
            targetPosition = newTargetPosition;
            self.movementComponent.StartPathfind(targetPosition, true);
        }
    }

    public void InitializeAI(MovableUnit self, MovableUnit target, ulong crowdId, Vector3 offset, bool resetTargetOnClose = false)
    {
        if (StatComponent.IsUnitAliveOrValid(self) && StatComponent.IsUnitAliveOrValid(target))
        {
            this.resetTargetOnClose = resetTargetOnClose;
            this.self = self;
            this.target = target;
            self.movementComponent.crowdID = crowdId;
            this.offset = offset;

            // For hacky trigger rn
            currentState = State.CloseToTarget;
            desiredState = State.MoveTowardsTarget;
            enabled = true;
            return;
        }

        if (enabled)
        {
            enabled = false;
        }
    }

    bool IsUpdateable()
    {
        return StatComponent.IsUnitAliveOrValid(self);
    }

    bool IsUpdateBlockable()
    {
        return self.actionComponent.IsPlayingAction();
    }

    void Process_MoveTowardsTarget()
    {
        //Debug.Log("Processing MoveTowardsTarget");
        DoTargetFollow();

        float3 diff = (float3)target.transform.TransformPoint(offset) - (float3)self.transform.position;
        if (math.lengthsq(diff) < thresholdForTargetSqr)
        {
            desiredState = State.CloseToTarget;
        }
    }

    void Process_CloseToTarget()
    {
        if (resetTargetOnClose)
        {
            desiredState = State.NoTarget;
            return;
        }

        //Debug.Log("Processing CloseToTarget");
        float3 diff = (float3)target.transform.TransformPoint(offset) - (float3)self.transform.position;
        if (math.lengthsq(diff) > thresholdForTargetSqr)
        {
            desiredState = State.MoveTowardsTarget;
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

        if (desiredState != currentState)
        {
            ChangeState(currentState);
            desiredState = currentState;
            return;
        }

        switch (currentState)
        {
            case State.MoveTowardsTarget:
                {
                    Process_MoveTowardsTarget();
                }
                break;
            case State.CloseToTarget:
                {
                    Process_CloseToTarget();
                }
                break;
            case State.NoTarget:
                break;
            case State.FindTarget:
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
