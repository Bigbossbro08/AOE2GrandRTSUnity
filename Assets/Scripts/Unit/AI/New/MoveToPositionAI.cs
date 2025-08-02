using CoreGameUnitAI;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class MoveToPositionAI : IAIController
{
    public enum State
    {
        WaitingForPath,
        MoveTowardsPoint,
        ReachedDestination
    }

    UnitAIController controller = null;
    ulong crowdId = 0;
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
        self.ResetUnit();
        movementComponent.crowdID = crowdId;
        if (pathPoints != null && pathPoints.Count != 0)
        {
            movementComponent.SetPositionData(pathPoints);
            desiredState = State.MoveTowardsPoint;
            currentState = State.MoveTowardsPoint;
            pathPoints.Clear();
            pathPoints = null;
        }
        else
        {
            currentState = State.WaitingForPath;
            desiredState = State.MoveTowardsPoint;
            movementComponent.StartPathfind(controller.context.destination);
        }
        HandleStateChange();
    }

    public void Exit()
    {

    }

    void HandleStateChange()
    {
        if (desiredState != currentState)
        {
            switch (desiredState)
            {
                case State.WaitingForPath:
                    {
                        controller.GetSelf().movementComponent.StartPathfind(controller.context.destination);
                    }
                    break;
                case State.MoveTowardsPoint:
                    {

                    }
                    break;
                case State.ReachedDestination:
                    {
                        controller.GetSelf().movementComponent.Stop();
                    }
                    break;
                default:
                    break;
            }
            // Move towards attack target and if close enough then attack already
            currentState = desiredState;
        }
    }

    public void Update(float dt)
    {
        HandleStateChange();

        switch (currentState)
        {
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

    private void Process_MoveTowardsPoint(float dt)
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
