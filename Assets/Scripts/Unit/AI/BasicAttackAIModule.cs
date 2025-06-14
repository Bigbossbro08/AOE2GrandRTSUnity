using System;
using System.Collections.Generic;
using System.IO;
using Unity.VisualScripting;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UIElements;

public class BasicAttackAIModule : UnitAIModule, IDeterministicUpdate, MapLoader.IMapSaveLoad
{
    public struct HeapUnitNode : IComparable<HeapUnitNode>
    {
        public int Index;      // index in the original unit list
        public float Distance; // used for sorting

        public HeapUnitNode(int index, float distance)
        {
            Index = index;
            Distance = distance;
        }

        public int CompareTo(HeapUnitNode other)
        {
            return Distance.CompareTo(other.Distance); // MinHeap
        }
    }

    public enum State
    {
        LookingForTarget,
        MoveTowardsTarget,
        AttackTarget
    }

    State currentState;

    MovableUnit self;
    CombatComponent combatComponent = null; // Cache
    MovableUnit target;

    float lookupTimer = 0;
    const float lookupTimerLength = 1.0f;

    const float thresholdForAttackState = (0.14f + 0.05f);
    const float thresholdForAttackStateSqr = thresholdForAttackState * thresholdForAttackState;

    //const float thresholdForMovingState = (0.14f + 0.05f) * 2;
    //const float thresholdForMovingStateSqr = thresholdForMovingState * thresholdForMovingState;

    const float thresholdForTarget = (float)(0.14 * DeterministicUpdateManager.FixedStep);
    const float thresholdForTargetSqr = thresholdForTarget * thresholdForTarget;

    float3 targetPosition;

    bool IsUpdateable()
    {
        return StatComponent.IsUnitAliveOrValid(self);
    }

    void ProcessLookForTarget()
    {
        if (self.movementComponent.movementState == MovementComponent.State.Idle)
        {
            const float lineOfSight = 5f;
            // Do lookup operation here
            List<Unit> units = UnitManager.Instance.spatialHashGrid.QueryInRadius(self.transform.position, lineOfSight);
            MinHeap<HeapUnitNode> unitHeap = new MinHeap<HeapUnitNode>();

            for (int i = 0; i < units.Count; i++)
            {
                Unit unit = units[i];
                if (unit == null) continue;
                if (unit == self) continue;
                if (unit.playerId == self.playerId) continue;
                if (unit.GetType() != typeof(MovableUnit)) continue;
                if (!StatComponent.IsUnitAliveOrValid((MovableUnit)unit)) continue;

                float sqrDistance = (self.transform.position - unit.transform.position).sqrMagnitude;
                unitHeap.Push(new HeapUnitNode(i, sqrDistance));
            }
            if (unitHeap.Count > 0)
            {
                HeapUnitNode heapUnitNode = unitHeap.Pop();
                Unit targetUnit = units[heapUnitNode.Index];
                if (targetUnit)
                {
                    this.target = (MovableUnit)targetUnit;
                    ChangeState(State.MoveTowardsTarget);
                }
            }
        }
    }

    void State_LookingForTarget(float deltaTime)
    {
        if (lookupTimer > lookupTimerLength)
        {
            ProcessLookForTarget();
            lookupTimer = 0;
        }
        lookupTimer += deltaTime;
    }

    Vector3 GetPositionCloseToTarget()
    {
        float3 diff = targetPosition - (float3)self.transform.position;
        float distanceSqr = math.lengthsq(diff);
        diff = math.normalize(diff);

        float distance = thresholdForAttackState * .99f;
        //float closeDistance = thresholdForAttackState * .99f;
        //if (self.unitTypeComponent != null && self.unitTypeComponent.GetType() == typeof(CombatComponent))
        //{
        //    distance = Mathf.Sqrt(distanceSqr);
        //    CombatComponent combatComponent = self.unitTypeComponent as CombatComponent; 
        //    distance = Mathf.Clamp(distance, closeDistance, combatComponent.attackRange);
        //    Debug.Log($"{distance} {Mathf.Sqrt(distanceSqr)} {distanceSqr} {closeDistance} {combatComponent.attackRange}");
        //}
        distance = 0;
        diff *= distance;
        return targetPosition + diff;
    }

    void SetTarget(MovableUnit target, bool updateTargetPosition = true)
    {
        if (target)
        {
            this.target = target;
            self.movementComponent.SetTargetToIgnore(target.id);
            if (updateTargetPosition)
            {
                targetPosition = target.transform.position;
            }
        }
    }

    bool IsTargetWithinRange(bool useThreshold = false)
    {
        float cmpSqr = thresholdForAttackStateSqr;
        if (self.unitTypeComponent != null && self.unitTypeComponent.GetType() == typeof(CombatComponent))
        {
            CombatComponent combatComponent = self.unitTypeComponent as CombatComponent;
            cmpSqr = combatComponent.attackRange * 1.1f;
            cmpSqr *= cmpSqr;
        }
        if (cmpSqr < thresholdForAttackStateSqr || cmpSqr == 0)
        {
            cmpSqr = thresholdForAttackStateSqr;
        }
        if ((target.transform.position - self.transform.position).sqrMagnitude < cmpSqr)
        {
            return true;
        }
        return false;
    }

    void ChangeState(State newState)
    {
        if (newState == currentState) return;
        // Used for cleaning up
        switch (newState)
        {
            case State.LookingForTarget:
                {
                    target = null;
                    self.ResetUnit();
                    //ulong newCrowdID = ++UnitManager.crowdIDCounter;
                    //self.movementComponent.crowdID = newCrowdID;
                    lookupTimer = 0;
                }
                break;
            case State.MoveTowardsTarget:
                {
                    //SetTarget(target);
                    self.movementComponent.SetTargetToIgnore(target.id);
                    foreach (var action in self.actionComponent.actions)
                    {
                        if (action.eventId == UnitEventHandler.EventID.OnAttack || action.eventId == UnitEventHandler.EventID.OnProjectileAttack)
                        {
                            action.parameters[1] = target.id;
                        }
                    }
                    
                    targetPosition = target.transform.position;
                    self.movementComponent.StartPathfind(GetPositionCloseToTarget(), true);
                    lookupTimer = 0;
                }
                break;
            case State.AttackTarget:
                {
                    RotateTowardsTarget();
                    if (CanPerformAttack())
                    {
                        PerformAttackAction();
                    }
                    lookupTimer = 0;
                }
                break;
            default:
                break;
        }
        currentState = newState;
    }

    void RotateTowardsTarget()
    {
        Vector3 diff = target.transform.position - self.transform.position;
        diff = diff.normalized;
        if (diff != Vector3.zero)
        {
            float yAngle = Mathf.Atan2(diff.x, diff.z) * Mathf.Rad2Deg;
            self.transform.eulerAngles = new Vector3(0, yAngle, 0);
        }
    }

    void PerformAttackAction()
    {
        if (!self.actionComponent.IsPlayingAction())
        {
            self.movementComponent.Stop();
            self.actionComponent.StartAction();
            combatComponent.StartDelay();
        }
    }

    void State_MoveTowardsTarget(float deltaTime)
    {
        if (!StatComponent.IsUnitAliveOrValid(target))
        {
            ChangeState(State.LookingForTarget);
            return;
        }

        //if ((target.transform.position - self.transform.position).sqrMagnitude < thresholdForAttackStateSqr)
        if (IsTargetWithinRange())
        {
            ChangeState(State.AttackTarget);
            return;
        }

        void Repath(Vector3 newTargetPosition)
        {
            ulong newCrowdID = ++UnitManager.crowdIDCounter;
            self.movementComponent.crowdID = newCrowdID;   targetPosition = newTargetPosition;
            self.movementComponent.StartPathfind(GetPositionCloseToTarget(), true);
        }

        float3 newTargetPosition = target.transform.position;
        float3 diff = targetPosition - newTargetPosition;
        if (math.lengthsq(diff) > thresholdForTargetSqr)
        {
            Repath(newTargetPosition);
            //targetPosition = newTargetPosition;
            //self.movementComponent.StartPathfind(GetPositionCloseToTarget(), true);
        }

        bool repathCheck = false;
        if (lookupTimer > lookupTimerLength)
        {
            repathCheck = true;
            lookupTimer = 0;
        }
        lookupTimer += deltaTime;

        if (repathCheck && self.movementComponent.movementState == MovementComponent.State.Idle)
        {
            Repath(newTargetPosition);
        }
        //self.combatComponent.StartAction();
    }

    bool CanPerformAttack()
    {
        if (combatComponent)
        {
            return !combatComponent.IsAttackDelayInProgress();
        }
        return true;
    }

    void State_AttackTarget()
    {
        if (!StatComponent.IsUnitAliveOrValid(target))
        {
            ChangeState(State.LookingForTarget);
            return;
        }

        Vector3 newTargetPosition = target.transform.position;
        //if ((newTargetPosition - self.transform.position).sqrMagnitude > thresholdForMovingStateSqr)
        if (!IsTargetWithinRange())
        {
            Vector3 diff = newTargetPosition - self.transform.position;
            ChangeState(State.MoveTowardsTarget);
            return;
        }

        RotateTowardsTarget();
        if (CanPerformAttack())
        {
            PerformAttackAction();
        }
    }

    bool isUpdateBlockable()
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

        if (isUpdateBlockable())
        {
            return;
        }

        switch (currentState)
        {
            case State.LookingForTarget:
                {
                    State_LookingForTarget(deltaTime);
                }
                break;
            case State.MoveTowardsTarget:
                {
                    State_MoveTowardsTarget(deltaTime);
                }
                break;
            case State.AttackTarget:
                {
                    State_AttackTarget();
                }
                break;
            default:
                break;
        }
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
        combatComponent = null;
        lookupTimer = 0.0f;
        target = null;
        currentState = State.LookingForTarget;
    }

    public void InitializeAI(MovableUnit self, MovableUnit target = null, bool autoSearchable = true)
    {
        if (StatComponent.IsUnitAliveOrValid(self))
        {
            this.self = self;

            if (self.unitTypeComponent.GetType() == typeof(CombatComponent))
            {
                CombatComponent combatComponent = self.unitTypeComponent as CombatComponent;
                this.combatComponent = combatComponent;
                NativeLogger.Log($"ID:{self.id}, Action Count: {combatComponent.actionEvents.Count}");
                self.actionComponent.SetActionSprite(combatComponent.attackSprite, "CombatEndAction", combatComponent.actionEvents);
            }
            
            bool shouldBeEnabled = false;
            if (target)
            {
                this.target = target;
                ChangeState(State.MoveTowardsTarget);
                shouldBeEnabled = true;
            }
            else if (autoSearchable)
            {
                ChangeState(State.LookingForTarget);
                shouldBeEnabled = true;
            }
            enabled = shouldBeEnabled;
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
