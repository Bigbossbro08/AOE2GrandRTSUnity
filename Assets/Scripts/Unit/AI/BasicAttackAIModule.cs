using UnityEngine;

public class BasicAttackAIModule : MonoBehaviour, IDeterministicUpdate, MapLoader.IMapSaveLoad
{
    public enum State
    {
        MoveTowardsTarget,
        AttackTarget
    }

    State currentState;
    MovableUnit self;
    MovableUnit target;

    const float thresholdForAttackState = 0.14f * 2;
    const float thresholdForAttackStateSqr = thresholdForAttackState * 1.1f * thresholdForAttackState * 1.1f;

    const float thresholdForMovingState = (0.14f + 0.05f) * 2;
    const float thresholdForMovingStateSqr = thresholdForMovingState * thresholdForMovingState;

    const float thresholdForTarget = (float)(0.14 * DeterministicUpdateManager.FixedStep);
    const float thresholdForTargetSqr = thresholdForTarget * thresholdForTarget;

    Vector3 targetPosition;

    bool IsUpdateable()
    {
        return self && target;
    }

    void State_MoveTowardsTarget()
    {
        if ((target.transform.position - self.transform.position).sqrMagnitude < thresholdForAttackStateSqr)
        {
            currentState = State.AttackTarget;
            return;
        }

        if (self.combatComponent.IsPlayingAction())
        {
            return;
        }

        Vector3 newTargetPosition = target.transform.position;
        Vector3 diff = targetPosition - newTargetPosition;
        if (diff.sqrMagnitude > thresholdForTargetSqr)
        {
            diff = targetPosition - self.transform.position;
            diff = diff.normalized;
            self.movementComponent.StartPathfind(newTargetPosition + diff.normalized * thresholdForAttackState, true);
            targetPosition = newTargetPosition;
        }

        //self.combatComponent.StartAction();
    }

    void State_AttackTarget()
    {
        Vector3 newTargetPosition = target.transform.position;
        if ((newTargetPosition - self.transform.position).sqrMagnitude > thresholdForMovingStateSqr)
        {
            Vector3 diff = newTargetPosition - self.transform.position;
            self.movementComponent.StartPathfind(newTargetPosition + diff.normalized * thresholdForAttackState, true);
            currentState = State.MoveTowardsTarget;
            return;
        }

        if (!self.combatComponent.IsPlayingAction())
        {
            Vector3 diff = target.transform.position - self.transform.position;
            diff = diff.normalized;
            if (diff != Vector3.zero)
            {
                float yAngle = Mathf.Atan2(diff.x, diff.z) * Mathf.Rad2Deg;
                self.transform.eulerAngles = new Vector3(0, yAngle, 0);
            }
        }
        self.combatComponent.StartAction();
    }

    public void DeterministicUpdate(float deltaTime, ulong tickID)
    {
        if (!IsUpdateable())
        {
            enabled = false;
            return;
        }

        switch (currentState)
        {
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
        enabled = false;
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

    public void InitializeAI(MovableUnit self, MovableUnit target)
    {
        if (self && target)
        {
            this.self = self;
            this.target = target;

            targetPosition = target.transform.position;

            Vector3 newTargetPosition = target.transform.position;
            Vector3 diff = targetPosition - newTargetPosition;

            self.movementComponent.StartPathfind(target.transform.position + diff.normalized * thresholdForAttackState);
            currentState = State.MoveTowardsTarget;
            enabled = true;
        }
    }

    public void Load(MapLoader.SaveLoadData data)
    {
        throw new System.NotImplementedException();
    }

    public void PostLoad(MapLoader.SaveLoadData data)
    {
        throw new System.NotImplementedException();
    }

    public MapLoader.SaveLoadData Save()
    {
        throw new System.NotImplementedException();
    }
}
