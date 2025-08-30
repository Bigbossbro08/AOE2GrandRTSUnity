using CoreGameUnitAI;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

[System.Serializable]
public class MoveToPositionAI : IAIController
{
    public enum State
    {
        WaitingForPath,
        MoveTowardsPoint,
        ReachedDestination
    }

    protected UnitAIController controller = null;
    protected ulong crowdId = 0;
    List<Vector3> pathPoints = null;
    State desiredState = State.WaitingForPath;
    State currentState = State.WaitingForPath;

    public MoveToPositionAI(UnitAIController controller, Vector3 position, ulong newCrowdID)
    {
        this.controller = controller;
        this.controller.context.destination = position;
        crowdId = newCrowdID;
        pathPoints = null;
        this.desiredState = State.WaitingForPath;
        this.currentState = State.WaitingForPath;
    }

    public MoveToPositionAI(UnitAIController controller, List<Vector3> pathPoints, ulong newCrowdID)
    {
        this.controller = controller;
        Debug.Assert(pathPoints != null);
        if (pathPoints.Count > 0)
        {
            controller.context.destination = pathPoints[pathPoints.Count - 1];
        }
        crowdId = newCrowdID;
    }

    public void Enter()
    {
        MovableUnit self = controller.GetSelf();
        MovementComponent movementComponent = self.movementComponent;
        //self.ResetUnit(true);
        self.ResetUnitWithoutMovementStop();
        movementComponent.crowdID = crowdId;
        desiredState = State.WaitingForPath;
        currentState = State.MoveTowardsPoint;
        //HandleStateChange(true);
    }

    public void Exit()
    {
        if (pathPoints != null)
        {
            pathPoints.AddRange(controller.GetMovementComponent().GetPathPositions());
        }
    }

    public virtual void OnReachedDestination()
    {

    }

    bool HandleStateChange(bool force = false)
    {
        if (desiredState != currentState || force)
        {
            switch (desiredState)
            {
                case State.WaitingForPath:
                    {
                        //controller.GetSelf().movementComponent.StartPathfind(controller.context.destination);
                        if (pathPoints != null && pathPoints.Count != 0)
                        {
                            controller.GetMovementComponent().SetPositionData(pathPoints);
                            pathPoints.Clear();
                        }
                        else
                        {
                            controller.GetMovementComponent().StartPathfind(controller.context.destination);
                        }
                    }
                    break;
                case State.MoveTowardsPoint:
                    {

                    }
                    break;
                case State.ReachedDestination:
                    {
                        controller.GetSelf().movementComponent.Stop();
                        OnReachedDestination();
                    }
                    break;
                default:
                    break;
            }
            // Move towards attack target and if close enough then attack already
            currentState = desiredState;
            return true;
        }
        return false;
    }

    public void Update(float dt)
    {
        if (HandleStateChange())
        {
            return;
        }

        switch (currentState)
        {
            case State.WaitingForPath:
                Process_WaitingForPath();
                break;
            case State.MoveTowardsPoint:
                Process_MoveTowardsPoint(dt);
                break;
            case State.ReachedDestination:
                Process_ReachedDestination();
                break;
            default:
                break;
        }
    }

    private void Process_WaitingForPath()
    {
        if (!controller.GetMovementComponent().HasPathPositions())
        {
            return;
        }
        desiredState = State.MoveTowardsPoint;
    }

    public virtual void Process_MoveTowardsPoint(float dt)
    {
        if (controller.context.self.movementComponent.movementState == MovementComponent.State.Idle)
        {
            desiredState = State.ReachedDestination;
        }
    }

    private void Process_ReachedDestination()
    {
        controller.RevertToPreviousAI();
    }
}
