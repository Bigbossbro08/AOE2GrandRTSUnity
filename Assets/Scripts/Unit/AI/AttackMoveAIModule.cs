using UnityEngine;
using UnityEngine.UIElements;

public class AttackMoveAIModule : BasicMovementAIModule
{
    float lookUptimer = 0.0f;

    public override void Process_MoveTowardsPoint()
    {
        lookUptimer += DeterministicUpdateManager.FixedStep;
        if (lookUptimer > 1.0f)
        {
            const float lineOfSight = 5.0f;
            if (BasicAttackAIModule.FindForEnemyUnit(self, lineOfSight, out MovableUnit enemyUnit))
            {
                if (enemyUnit != null)
                {
                    MovableUnit cachedSelf = self;
                    NativeLogger.Log($"FOUND ENEMY! and moving to attack against by {self.name} to {enemyUnit.name}");
                    self.ResetUnit(true);
                    self.SetAIModule(UnitAIModule.AIModule.BasicAttackAIModule, enemyUnit, false, false);
                    //cachedSelf.overrideModule = AIModule.AttackMoveAIModule;
                    //cachedSelf.overrideModuleArgs = new System.Collections.Generic.List<object> {
                    //    position, cachedSelf.movementComponent.crowdID, offset, startPosition
                    //};
                    return;
                }
            }
            lookUptimer = 0.0f;
        }

        base.Process_MoveTowardsPoint();
    }

    public override void OnChangeState(State newState, bool force)
    {
        base.OnChangeState(newState, force);
        switch (newState)
        {
            case State.MoveTowardsPoint:
                lookUptimer = 0.0f;
                break;
            case State.ReachedDestination:
                {
                    lookUptimer = 0.0f;
                }
                break;
            default:
                break;
        }
    }
}
