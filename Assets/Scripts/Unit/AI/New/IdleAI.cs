using CoreGameUnitAI;
using UnityEngine;

[System.Serializable]
public class IdleAI : IAIController
{
    UnitAIController controller;
    SearchForEnemy searchForEnemy = null;

    public IdleAI(UnitAIController controller) {
        this.controller = controller;
    }

    public void Enter()
    {
        searchForEnemy = new SearchForEnemy(this.controller.context.self, this.controller.context.combatComponent.lineOfSight, 0.5f);
        controller.context.self.ResetUnit();
    }

    public void Exit()
    {

    }

    public void Update(float dt)
    {
        if (searchForEnemy != null)
        {
            bool checkForTimeout = searchForEnemy.TimedOut;
            if (searchForEnemy.Update(dt, out MovableUnit enemyUnit))
            {
                // Go To Combat State
                controller.SetAI(new AttackAI(controller, enemyUnit));
                return;
            }

            if (checkForTimeout && enemyUnit == null)
                controller.RevertToPreviousAI();
        }
    }
}
