using UnityEngine;
using Unity.Mathematics;
using TMPro;

namespace CoreGameUnitAI
{
    [System.Serializable]
    public class AttackAI : MoveToTargetAI, IAIController
    {
        SearchForEnemy searchForEnemy = null;
        bool ordered = false;
        public AttackAI(UnitAIController controller, MovableUnit initialTarget = null, bool ordered = false) : base(controller, initialTarget)
        {
            this.ordered = ordered;
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
                currentState = desiredState;
            }
        }

        void ChangeTarget(MovableUnit target)
        {
            MovableUnit self = controller.GetSelf();
            if (target == null || target == self)
            {
                NativeLogger.Warning("AttackAI: Target is null or self, cannot change target.");
                controller.context.target = null;
                
                foreach (var action in self.actionComponent.actions)
                {
                    switch (action.eventId)
                    {
                        case UnitEventHandler.EventID.OnAttack:
                        case UnitEventHandler.EventID.OnProjectileAttack:
                            {
                                action.parameters[1] = 0;
                            }
                            break;
                    }
                }
                return;
            }
            controller.context.target = target;
            foreach (var action in self.actionComponent.actions)
            {
                switch (action.eventId)
                {
                    case UnitEventHandler.EventID.OnAttack:
                    case UnitEventHandler.EventID.OnProjectileAttack:
                        {
                            action.parameters[1] = (ulong)target.id;
                        }
                        break;
                }
            }
        }

        public new void Enter()
        {
            targetPosition = controller.GetTargetPosition();
            MovableUnit self = controller.GetSelf();
            if (!ordered)
            {
                searchForEnemy = new SearchForEnemy(self, self.movementComponent.radius * 3, 0.1f);
            }
            MovableUnit target = controller.GetTarget();
            CombatComponent combatComponent = controller.context.combatComponent;
            self.movementComponent.StartPathfind(targetPosition);
            self.movementComponent.crowdID = crowdId;
            self.actionComponent.SetActionSprite(combatComponent.attackSprite, "CombatEndAction", combatComponent.actionEvents);
            ChangeTarget(target);
            HandleStateChange();
        }

        public new void Exit()
        {
        }

        public override void Process_MovingTowardsTarget(float dt)
        {
            if (!controller.IsTargetWithinLineOfSight(controller.context.combatComponent.lineOfSight))
            {
                controller.RevertToPreviousAI();
                return;
            }
            if (controller.IsTargetWithinRange(controller.context.combatComponent.attackRange))
            {
                desiredState = State.CloseToTarget;
            }

            if (!ordered && searchForEnemy != null)
            {
                if (searchForEnemy.Update(dt, out MovableUnit enemyUnit))
                {
                    ChangeTarget(enemyUnit);
                    Debug.Log("Searching for closerby target");
                }
            }
        
            PerformMovement(dt);
        }

        public override void Process_CloseToTarget()
        {
            if (!controller.IsTargetWithinLineOfSight(controller.context.combatComponent.lineOfSight))
            {
                controller.RevertToPreviousAI();
                return;
            }

            if (!controller.IsTargetWithinRange(controller.context.combatComponent.attackRange))
            {
                desiredState = State.MovingTowardsTarget;
            }

            controller.RotateTowardsTarget();
            controller.PerformAttackAction();
        }

        public new void Update(float dt)
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