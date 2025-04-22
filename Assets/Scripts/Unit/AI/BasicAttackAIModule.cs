using System;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
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
    MovableUnit target;

    float lookupTimer = 0;
    const float lookupTimerLength = 1.0f;

    const float thresholdForAttackState = 0.14f * 2;
    const float thresholdForAttackStateSqr = thresholdForAttackState * 1.1f * thresholdForAttackState * 1.1f;

    const float thresholdForMovingState = (0.14f + 0.05f) * 2;
    const float thresholdForMovingStateSqr = thresholdForMovingState * thresholdForMovingState;

    const float thresholdForTarget = (float)(0.14 * DeterministicUpdateManager.FixedStep);
    const float thresholdForTargetSqr = thresholdForTarget * thresholdForTarget;

    Vector3 targetPosition;

    bool IsUpdateable()
    {
        return self;
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
        Vector3 diff = targetPosition - self.transform.position;
        diff = diff.normalized;
        diff *= thresholdForAttackState * .99f;
        return diff;
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

    void ChangeState(State newState)
    {
        // Used for cleaning up
        switch (newState)
        {
            case State.LookingForTarget:
                {
                    self.ResetUnit();
                    ulong newCrowdID = ++UnitManager.crowdIDCounter;
                    self.movementComponent.crowdID = newCrowdID;
                }
                break;
            case State.MoveTowardsTarget:
                {
                    //SetTarget(target);
                    self.movementComponent.SetTargetToIgnore(target.id);
                    targetPosition = target.transform.position;
                    self.movementComponent.StartPathfind(targetPosition + GetPositionCloseToTarget(), true);
                }
                break;
            case State.AttackTarget:
                {
                }
                break;
            default:
                break;
        }
        currentState = newState;
    }

    void State_MoveTowardsTarget()
    {
        if (!target)
        {
            ChangeState(State.LookingForTarget);
            return;
        }

        if ((target.transform.position - self.transform.position).sqrMagnitude < thresholdForAttackStateSqr)
        {
            ChangeState(State.AttackTarget);
            return;
        }

        if (self.actionComponent.IsPlayingAction())
        {
            return;
        }

        Vector3 newTargetPosition = target.transform.position;
        Vector3 diff = targetPosition - newTargetPosition;
        if (diff.sqrMagnitude > thresholdForTargetSqr)
        {
            targetPosition = newTargetPosition;
            self.movementComponent.StartPathfind(newTargetPosition + GetPositionCloseToTarget(), true);
        }

        //self.combatComponent.StartAction();
    }

    void State_AttackTarget()
    {
        if (!target)
        {
            ChangeState(State.LookingForTarget);
            return;
        }

        Vector3 newTargetPosition = target.transform.position;
        if ((newTargetPosition - self.transform.position).sqrMagnitude > thresholdForMovingStateSqr)
        {
            Vector3 diff = newTargetPosition - self.transform.position;
            ChangeState(State.MoveTowardsTarget);
            return;
        }

        if (!self.actionComponent.IsPlayingAction())
        {
            Vector3 diff = target.transform.position - self.transform.position;
            diff = diff.normalized;
            if (diff != Vector3.zero)
            {
                float yAngle = Mathf.Atan2(diff.x, diff.z) * Mathf.Rad2Deg;
                self.transform.eulerAngles = new Vector3(0, yAngle, 0);
            }
            self.movementComponent.Stop();
        }
        self.actionComponent.StartAction();
    }

    public new void DeterministicUpdate(float deltaTime, ulong tickID)
    {
        if (!IsUpdateable())
        {
            enabled = false;
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
                    State_MoveTowardsTarget();
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
        target = null;
        currentState = State.MoveTowardsTarget;
    }

    public void InitializeAI(MovableUnit self, MovableUnit target = null, bool autoSearchable = true)
    {
        if (self)
        {
            this.self = self;
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
