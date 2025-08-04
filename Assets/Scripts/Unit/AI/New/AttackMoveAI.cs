using CoreGameUnitAI;
using System.Collections.Generic;
using UnityEngine;

public class AttackMoveAI : MoveToPositionAI, IAIController
{
    SearchForEnemy searchForEnemy = null;

    public AttackMoveAI(UnitAIController controller, Vector3 position, ulong newCrowdID) : 
        base(controller, position, newCrowdID)
    {
        searchForEnemy = new SearchForEnemy(this.controller.context.self, this.controller.context.combatComponent.lineOfSight, 0.5f);
    }

    public AttackMoveAI(UnitAIController controller, List<Vector3> pathPoints, ulong newCrowdID) :
        base(controller, pathPoints, newCrowdID) {
        searchForEnemy = new SearchForEnemy(this.controller.context.self, this.controller.context.combatComponent.lineOfSight, 0.5f);
    }

    public override void Process_MoveTowardsPoint(float dt)
    {
        if (searchForEnemy.Update(dt, out MovableUnit enemyUnit))
        {
            // Go To Combat State
            controller.SetAI(new AttackAI(controller, enemyUnit));
            return;
        }
        base.Process_MoveTowardsPoint(dt);
    }
}
