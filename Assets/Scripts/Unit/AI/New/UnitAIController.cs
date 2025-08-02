using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace CoreGameUnitAI
{
    public class AIContext
    {
        public MovableUnit self;
        public MovableUnit target;
        public float3 initial;
        public float3 destination;

        // Cache
        public CombatComponent combatComponent = null;

        public AIContext(MovableUnit self)
        {
            this.self = self;
            CombatComponent combatComponent = self.unitTypeComponent as CombatComponent;
            this.combatComponent = combatComponent;
            this.target = null;
        }
    }

    public interface IAIController
    {
        void Enter(); // Setup logic
        void Exit();  // Cleanup
        void Update(float dt); // Per tick update
    }

    public class UnitAIController
    {
        public AIContext context;
        private Stack<IAIController> aiStack = new Stack<IAIController>();
        private IAIController currentAI;

        public UnitAIController(MovableUnit self, IAIController defaultAI)
        {
            this.context = new AIContext(self);
            this.aiStack = new Stack<IAIController>();
            DefaultAI = defaultAI;
        }

        /// <summary>
        /// Optional fallback AI to return to when the stack is empty.
        /// </summary>
        public IAIController DefaultAI { get; set; }

        public MovableUnit GetSelf()
        {
            return context.self;
        }

        public MovableUnit GetTarget()
        {
            return context.target;
        }

        public Vector3 GetSelfPosition()
        {
            return context.self.transform.position;
        }

        public Vector3 GetTargetPosition()
        {
            return context.target.transform.position;
        }

        public float GetSqrMagnitudeToTarget()
        {
            float diffBetweenTargetSqr = (GetSelfPosition() - GetTargetPosition()).sqrMagnitude;
            return diffBetweenTargetSqr;
        }

        public bool IsAttackable(float givenRange)
        {
            if (StatComponent.IsUnitAliveOrValid(context.target))
            {
                if (GetSqrMagnitudeToTarget() < givenRange * givenRange)
                {
                    return true;
                }
            }
            context.target = null;
            return false;
        }

        public void Repath(Vector3 newTargetPosition, ref Vector3 targetPosition)
        {
            MovableUnit self = GetSelf();
            MovableUnit target = GetTarget();
            targetPosition = newTargetPosition;
            self.movementComponent.StartPathfind(GetTargetPosition(), true);
        }

        public bool IsTargetWithinRange(float givenLineOfSight)
        {
            if (GetSqrMagnitudeToTarget() < (givenLineOfSight) * (givenLineOfSight))
            {
                return true;
            }
            return false;
        }

        public void RotateTowardsTarget()
        {
            MovableUnit self = context.self;
            MovableUnit target = context.target;
            if (self.IsShip())
            {
                return;
            }
            Vector3 diff = target.transform.position - self.transform.position;
            diff = diff.normalized;
            if (diff != Vector3.zero)
            {
                float yAngle = Mathf.Atan2(diff.x, diff.z) * Mathf.Rad2Deg;
                self.transform.eulerAngles = new Vector3(0, yAngle, 0);
            }
        }

        public void PerformAttackAction()
        {
            //RotateTowardsTarget();
            MovableUnit self = context.self;
            MovableUnit target = context.target;

            self.movementComponent.Stop();
            if (context.combatComponent && context.combatComponent.IsAttackDelayInProgress())
            {
                return;
            }
            if (self.actionComponent.IsPlayingAction())
            {
                return;
            }

            if (self.IsShip())
            {
                if (target.IsShip()) { }
                return;
            }
            self.movementComponent.Stop();
            self.actionComponent.StartAction();
            context.combatComponent.StartDelay();
        }

        public void SetAI(IAIController newAI, bool pushPrevious = true)
        {
            if (newAI == null)
            {
                NativeLogger.Warning("SetAI was called with null. Ignoring.");
                return;
            }

            if (pushPrevious && currentAI != null)
                aiStack.Push(currentAI);

            currentAI?.Exit();
            currentAI = newAI;
            currentAI.Enter();
        }

        public void RevertToPreviousAI()
        {
            currentAI?.Exit();

            if (aiStack.Count > 0)
            {
                currentAI = aiStack.Pop();
            }
            else if (DefaultAI != null)
            {
                currentAI = DefaultAI;
            }
            else
            {
                currentAI = null;
                NativeLogger.Warning("AI stack empty. No DefaultAI set. Unit is now idle.");
            }

            currentAI?.Enter();
        }

        public void ClearAI()
        {
            aiStack.Clear();
            currentAI?.Exit();

            if (DefaultAI != null)
            {
                currentAI = DefaultAI;
                currentAI.Enter();
            }
            else
            {
                currentAI = null;
                NativeLogger.Warning("AI cleared. No DefaultAI set. Unit is now idle.");
            }
        }

        bool IsUpdateable()
        {
            return StatComponent.IsUnitAliveOrValid(context.self);
        }

        bool IsUpdateBlockable()
        {
            return context.self.actionComponent.IsPlayingAction();
        }

        public void Update(float dt)
        {
            if (IsUpdateable())
            {
                return;
            }

            if (IsUpdateBlockable())
            {
                return;
            }
            if (currentAI != null)
            {
                currentAI.Update(dt);
            }
        }

        public IAIController GetCurrentAI() => currentAI;
    }
}