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

    State desiredState;
    State currentState;

    MovableUnit self;
    CombatComponent combatComponent = null; // Cache
    MovableUnit target;

    float lookupTimer = 0;
    bool isTargeted = false;

    const float lookupTimerLength = 1.0f;

    const float thresholdForAttackState = (0.14f + 0.2f);
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

    public static bool FindForEnemyUnit(MovableUnit self, float lineOfSight, out MovableUnit enemyUnit)
    {
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
                enemyUnit = (MovableUnit)targetUnit;
                return true;
            }
        }
        enemyUnit = null;
        return false;
    }

    void ProcessLookForTarget()
    {
        if (self.movementComponent.movementState == MovementComponent.State.Idle)
        {
            const float lineOfSight = 5f;
            if (FindForEnemyUnit(self, lineOfSight, out MovableUnit targetUnit))
            {
                this.target = targetUnit;
                SetDesiredState(State.MoveTowardsTarget);
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
        return targetPosition;
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

    void ChangeState(State newState, bool force = false)
    {
        if (newState == currentState && !force) return;
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
            SetDesiredState(State.LookingForTarget);
            return;
        }

        if (IsTargetWithinRange())
        {
            SetDesiredState(State.AttackTarget);
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

        if (!isTargeted)
        {
            if (FindForEnemyUnit(self, thresholdForAttackState, out MovableUnit targetUnit))
            {
                this.target = targetUnit;
            }
        }
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
            SetDesiredState(State.LookingForTarget);
            isTargeted = false;
            return;
        }

        Vector3 newTargetPosition = target.transform.position;
        //if ((newTargetPosition - self.transform.position).sqrMagnitude > thresholdForMovingStateSqr)
        if (!IsTargetWithinRange())
        {
            Vector3 diff = newTargetPosition - self.transform.position;
            SetDesiredState(State.MoveTowardsTarget);
            return;
        }

        RotateTowardsTarget();
        if (CanPerformAttack())
        {
            PerformAttackAction();
        }
    }

    bool IsUpdateBlockable()
    {
        return self.actionComponent.IsPlayingAction();
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

    public void InitializeAI(MovableUnit self, MovableUnit target = null, bool autoSearchable = true, bool isTargeted = false)
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
                SetDesiredState(State.MoveTowardsTarget, true);
                shouldBeEnabled = true;
            }
            else if (autoSearchable)
            {
                SetDesiredState(State.LookingForTarget);
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
