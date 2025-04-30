using System;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

public class BasicMovementAIModule : UnitAIModule, IDeterministicUpdate, MapLoader.IMapSaveLoad
{
    public enum State
    {
        MoveTowardsPoint,
        ReachedDestination
    }

    State currentState;
    MovableUnit self;
    Vector3 position;
    ulong crowdId;
    bool isActionLocked = false;

    void ChangeState(State newState)
    {
        if (newState == currentState) return;
        // Used for cleaning up
        switch (newState)
        {
            case State.MoveTowardsPoint:
                {
                    isActionLocked = self.actionComponent.IsPlayingAction();
                    if (!isActionLocked)
                        self.movementComponent.StartPathfind(position);
                }
                break;
            case State.ReachedDestination:
                {
                    self.movementComponent.Stop();
                    self.ResetToDefaultModule();
                    enabled = false;
                }
                break;
        }
        currentState = newState;
    }

    bool IsUpdateable()
    {
        return StatComponent.IsUnitAliveOrValid(self);
    }

    void Process_MoveTowardsPoint()
    {
        if (isActionLocked && !self.actionComponent.IsPlayingAction())
        {
            self.movementComponent.StartPathfind(position);
            isActionLocked = false;
            return;
        }

        if (isActionLocked || self.actionComponent.IsPlayingAction())
        {
            return;
        }

        if (self.movementComponent.movementState == MovementComponent.State.Moving)
        {
            // TODO: Make proper solution for stop in group behavior or find better solutions
            //float radius = self.movementComponent.radius;
            //var nearbyObjs = UnitManager.Instance.spatialHashGrid.QueryInRadius(transform.position, radius + 0.05f);
            //foreach (Unit obj in nearbyObjs)
            //{
            //    if (obj.gameObject == self.gameObject) continue;
            //    if (obj.playerId != self.playerId) continue;
            //
            //    if (obj.GetType() != typeof(MovableUnit)) continue;
            //    MovableUnit otherMovableUnit = obj as MovableUnit;
            //
            //    if (!otherMovableUnit.aiModule) continue;
            //    if (otherMovableUnit.aiModule.GetType() != typeof(BasicMovementAIModule)) continue;
            //
            //    BasicMovementAIModule otherMovementAiModule = otherMovableUnit.aiModule as BasicMovementAIModule;
            //    if (otherMovementAiModule.currentState == State.ReachedDestination) continue;
            //    if (otherMovementAiModule.crowdId != crowdId) continue;
            //    Vector3 diffToOtherObj = self.transform.position - obj.transform.position;
            //    float cmpFloat = 2 * radius + 0.05f;
            //    float cmpSqr = cmpFloat * cmpFloat;
            //    if (diffToOtherObj.sqrMagnitude < cmpFloat)
            //    {
            //        ChangeState(State.ReachedDestination);
            //        break;
            //    }
            //}
            if (currentState == State.ReachedDestination) return;

            //Vector2 diff = MovementComponent.ToVector2(self.movementComponent.GetLastPointInPathfinding()) - MovementComponent.ToVector2(self.transform.position);
            //if (diff.sqrMagnitude < 0.0001f)
            //{
            //    ChangeState(State.ReachedDestination);
            //}
            //else
            //{
            //    Debug.Log(diff.sqrMagnitude);
            //}
            return;
        }

        Vector2 diff = MovementComponent.ToVector2(position) - MovementComponent.ToVector2(self.transform.position);
        if (self.movementComponent.movementState == MovementComponent.State.Idle && 
                !self.movementComponent.HasPathPositions() &&
                diff.sqrMagnitude < 0.0001f)
            ChangeState(State.ReachedDestination);
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
            case State.MoveTowardsPoint:
                {
                    Process_MoveTowardsPoint();
                }
                break;
            case State.ReachedDestination:
                {
                    self.ResetToDefaultModule();
                    enabled = false;
                }
                break;
            default:
                break;
        }
    }

    private void OnEnable()
    {
        DeterministicUpdateManager.Instance.Register(this);
    }

    private void OnDisable()
    {
        DeterministicUpdateManager.Instance.Unregister(this);
        self = null;
        currentState = State.MoveTowardsPoint;
        isActionLocked = false;
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

    internal void InitializeAI(MovableUnit self, Vector3 position, ulong crowdId)
    {
        if (StatComponent.IsUnitAliveOrValid(self))
        {
            this.self = self;
            this.position = position;
            this.crowdId = crowdId;
            isActionLocked = false;
            
            // For hacky trigger rn
            currentState = State.ReachedDestination;
            ChangeState(State.MoveTowardsPoint);
            enabled = true;
            return;
        }

        if (enabled)
        {
            enabled = false;
        }
    }
}
