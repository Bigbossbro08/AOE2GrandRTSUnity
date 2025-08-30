using CoreGameUnitAI;
using Unity.Mathematics;
using UnityEngine;

public class MoveToTargetAI : IAIController
{
    public enum State
    {
        MovingTowardsTarget,
        CloseToTarget
    }

    public State desiredState;
    public State currentState;
    protected Vector3 targetPosition = float3.zero;
    protected float repathTimer = 0f;

    // Repath data
    const float thredholdForRepath = 0.14f * 1.1f;
    const float thredholdForRepathSqr = thredholdForRepath * thredholdForRepath;
    const float timeToRepath = 0.5f;

    protected ulong crowdId = 0;

    protected UnitAIController controller = null;
    public MoveToTargetAI(UnitAIController controller, MovableUnit target)
    {
        this.controller = controller;
        this.controller.context.target = target;
        this.desiredState = State.MovingTowardsTarget;
        ulong newCrowdID = ++UnitManager.crowdIDCounter;
        crowdId = newCrowdID;
    }

    public virtual void OnCloseToTarget()
    {

    }

    bool HandleStateChange()
    {
        if (desiredState != currentState)
        {
            switch (desiredState)
            {
                case State.MovingTowardsTarget:
                    {
                        controller.GetSelf().movementComponent.StartPathfind(controller.GetTargetPosition());
                    }
                    break;
                case State.CloseToTarget:
                    {
                        controller.GetSelf().movementComponent.Stop();
                        OnCloseToTarget();
                    }
                    break;
                default:
                    break;
            }
            currentState = desiredState;
            return true;
        }
        return false;
    }

    public void Enter()
    {
        MovementComponent movementComponent = controller.GetMovementComponent();
        movementComponent.StartPathfind(targetPosition);
        movementComponent.crowdID = crowdId;
        HandleStateChange();
    }

    public void Exit()
    {
        controller.context.target = null;
    }

    public void PerformMovement(float dt)
    {
        float deltaTime = dt;
        MovableUnit self = controller.GetSelf();
        Vector3 newTargetPosition = controller.GetTargetPosition();
        Vector3 diff = targetPosition - newTargetPosition;
        if (math.lengthsq(diff) > thredholdForRepathSqr)
        {
            controller.Repath(newTargetPosition, ref targetPosition);
        }

        bool repathCheck = false;
        repathTimer += deltaTime;
        if (repathTimer > timeToRepath)
        {
            repathCheck = true;
            repathTimer = 0;
        }

        if (repathCheck && self.movementComponent.movementState == MovementComponent.State.Idle)
        {
            controller.Repath(newTargetPosition, ref targetPosition);
        }
    }

    public virtual void Process_MovingTowardsTarget(float dt)
    {
        if (!controller.IsTargetWithinLineOfSight(controller.context.combatComponent.lineOfSight))
        {
            controller.RevertToPreviousAI();
            return;
        }
        if (controller.IsTargetWithinRange(0.0f))
        {
            desiredState = State.CloseToTarget;
        }

        PerformMovement(dt);
    }

    public virtual void Process_CloseToTarget()
    {
        if (!controller.IsTargetWithinLineOfSight(controller.context.combatComponent.lineOfSight))
        {
            controller.RevertToPreviousAI();
            return;
        }

        if (!controller.IsTargetWithinRange(0.0f))
        {
            desiredState = State.MovingTowardsTarget;
        }
    }

    public void Update(float dt)
    {
        if (HandleStateChange())
        {
            return;
        }

        switch (currentState)
        {
            case State.MovingTowardsTarget:
                Process_MovingTowardsTarget(dt);
                break;
            case State.CloseToTarget:
                Process_CloseToTarget();
                break;
            default:
                break;
        }
    }
}
