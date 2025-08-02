using CoreGameUnitAI;
using UnityEngine;

public class IdleAI : IAIController
{
    UnitAIController contrller;
    SearchForEnemy searchForEnemy = null;

    public IdleAI(UnitAIController controller, float? lineOfSight) {
        this.contrller = controller;
        if (lineOfSight.HasValue)
        {
            searchForEnemy = new SearchForEnemy(contrller.context.self, lineOfSight.Value, 0.5f); // TODO: Add a data entry in unit datas
        }
    }

    public void Enter()
    {
        contrller.context.self.ResetUnit();
    }

    public void Exit()
    {

    }

    public void Update(float dt)
    {
        if (searchForEnemy != null)
        {
            if (searchForEnemy.Update(dt, out MovableUnit enemyUnit))
            {
                // Go To Combat State
                contrller.SetAI(new AttackAI(contrller, enemyUnit));
            }
        } 
    }
}
