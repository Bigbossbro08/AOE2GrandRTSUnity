using UnityEngine;
using Unity.Mathematics;

namespace CoreGameUnitAI
{
    public class AttackAI : IAIController
    {
        public enum State
        {
            MovingTowardsTarget,
            CloseToTarget
        }

        const float thredholdForRepath = 0.14f * 1.1f;
        const float thredholdForRepathSqr = thredholdForRepath * thredholdForRepath;
        const float timeToRepath = 0.5f;

        UnitAIController controller = null;
        public State desiredState;
        public State currentState;
        Vector3 targetPosition = float3.zero;
        float repathTimer = 0f;
        float range = 4.0f;
        float lineOfSight = 5.0f;
        ulong crowdId = 0;

        public AttackAI(UnitAIController controller, MovableUnit initialTarget = null, float range = 4.0f, float lineOfSight = 5.0f)
        {
            this.controller = controller;
            this.controller.context.target = initialTarget;
            this.desiredState = State.MovingTowardsTarget;
            this.range = range;
            this.lineOfSight = lineOfSight;
            MovableUnit self = controller.context.self;
            ulong newCrowdID = ++UnitManager.crowdIDCounter;
            crowdId = newCrowdID;
        }

        void HandleStateChange()
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
                        }
                        break;
                    default:
                        break;
                }
                // Move towards attack target and if close enough then attack already
                currentState = desiredState;
            }
        }

        public void Enter()
        {
            controller.GetSelf().movementComponent.crowdID = crowdId;
            HandleStateChange();
        }

        public void Exit()
        {
        }

        void PerformMovement(float dt)
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

        void Process_MovingTowardsTarget(float dt)
        {
            if (!controller.IsAttackable(range))
            {
                controller.RevertToPreviousAI();
                return;
            }
            if (controller.IsTargetWithinRange(lineOfSight + 0.1f))
            {
                desiredState = State.CloseToTarget;
            }

            PerformMovement(dt);
        }

        void Process_CloseToTarget()
        {
            if (!controller.IsAttackable(range))
            {
                controller.RevertToPreviousAI();
                return;
            }

            if (!controller.IsTargetWithinRange(lineOfSight))
            {
                desiredState = State.MovingTowardsTarget;
            }

            controller.RotateTowardsTarget();
            controller.PerformAttackAction();
        }

        public void Update(float dt)
        {
            HandleStateChange();

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
}