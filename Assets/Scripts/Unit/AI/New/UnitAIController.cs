using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;

namespace CoreGameUnitAI
{
    [System.Serializable]
    public class AIContext
    {
        public MovableUnit self;
        public MovableUnit target;
        public float3 initial;
        public float3 destination;

        // Cache
        /// <summary>
        /// Cached combat component of self unit
        /// </summary>
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

    [System.Serializable]
    public class UnitAIController
    {
        public AIContext context;
        private Stack<IAIController> aiStack = new Stack<IAIController>();
        public List<string> aiStackVisualized = new List<string>();
        [SerializeField]
        private IAIController currentAI;
        public bool enabled = false;

        public class AiControllerParameter
        {
            public IAIController newAI;
            public bool pushPrevious = true;
            public bool clearAi = false;

            public AiControllerParameter(IAIController newAI, bool pushPrevious, bool clearAi)
            {
                this.newAI = newAI;
                this.pushPrevious = pushPrevious;
                this.clearAi = clearAi;
            }
        }

        AiControllerParameter aiControllerParameter = null;

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

        public MovementComponent GetMovementComponent()
        {
            return context.self.movementComponent;
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

        public bool IsTargetWithinLineOfSight(float givenLineOfSight)
        {
            if (StatComponent.IsUnitAliveOrValid(context.target))
            {
                if (GetSqrMagnitudeToTarget() < givenLineOfSight * givenLineOfSight)
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
            self.movementComponent.StartPathfind(newTargetPosition, true);
            targetPosition = newTargetPosition;
        }

        public bool IsTargetWithinRange(float givenRange)
        {
            float checkRange = Mathf.Max(context.self.movementComponent.radius + context.target.movementComponent.radius + 0.01f, givenRange);
            float sqrMagnitude = GetSqrMagnitudeToTarget();
            if (sqrMagnitude < (checkRange * checkRange))
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
            self.actionComponent.StartAction();
            context.combatComponent.StartDelay();
        }

        public void SetAI(IAIController newAI, bool pushPrevious = true, bool clearAi = false)
        {
            aiControllerParameter = new AiControllerParameter(newAI, pushPrevious, clearAi);
            //if (clearAi)
            //{
            //    ClearAI();
            //}
            //
            //if (newAI == null)
            //{
            //    NativeLogger.Warning("SetAI was called with null. Ignoring.");
            //    return;
            //}
            //
            //if (pushPrevious && currentAI != null)
            //    aiStack.Push(currentAI);
            //
            //currentAI?.Exit();
            //currentAI = newAI;
            //currentAI.Enter();
        }

        bool HandleAIParameter()
        {
            if (aiControllerParameter == null) return false;

            if (aiControllerParameter.clearAi)
            {
                ClearAI();
            }

            if (aiControllerParameter.newAI == null)
            {
                NativeLogger.Warning("SetAI was called with null. Ignoring.");
                aiControllerParameter = null;
                return true;
            }

            if (aiControllerParameter.pushPrevious && currentAI != null)
                aiStack.Push(currentAI);

            currentAI?.Exit();
            currentAI = aiControllerParameter.newAI;
            currentAI.Enter();

            aiControllerParameter = null;
            return true;
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
            if (!enabled) return false;
            if (!StatComponent.IsUnitAliveOrValid(context.self)) return false;
            if (context.self.actionComponent.IsPlayingAction()) return false;
            return true;
        }

        public void Update(float dt)
        {
            aiStackVisualized.Clear();
            aiStackVisualized.Add(currentAI.ToString());
            foreach(var i in aiStack)
            {
                aiStackVisualized.Add(i.ToString());
            }

            if (!IsUpdateable())
            {
                return;
            }

            if (HandleAIParameter())
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