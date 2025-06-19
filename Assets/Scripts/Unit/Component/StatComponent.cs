using UnityEngine;
using System.Collections.Generic;

public class StatComponent : MonoBehaviour
{
    private float health = 45f;
    private float armor = 1f;

    public float GetHealth() {  return health; }
    public UnitManager.UnitJsonData.DamageData damageData = new UnitManager.UnitJsonData.DamageData();

    private void OnEnable()
    {
        health = 45f;
    }

    public void SetHealth(float health, MovableUnit unit = null)
    {
        float newHealth = health;
        if (newHealth <= 0.0f)
        {
            if (IsUnitAliveOrValid(unit))
                UnitEventHandler.Instance.CallEventByID(UnitEventHandler.EventID.OnDeath, unit.id);
            newHealth = 0.0f;
        }
        this.health = newHealth;
    }

    public static void DamageUnit(MovableUnit targetUnit, UnitManager.UnitJsonData.DamageData damageData)
    {
        float targetHealth = targetUnit.statComponent.GetHealth();

        float damageGiven = CombatComponent.CalculateDamageData(damageData, targetUnit.statComponent.damageData);

        targetHealth -= damageGiven;
        targetUnit.statComponent.SetHealth(targetHealth, targetUnit);
    }

    public static bool IsUnitAliveOrValid(MovableUnit unit)
    {
        if (!unit) return false;
        if (UnitManager.Instance.GetUnit(unit.id) == null) return false;
        if (unit.statComponent.GetHealth() <= 0.0f) return false;
        return true;
    }

    public static void KillUnit(MovableUnit targetUnit)
    {
        if (IsUnitAliveOrValid(targetUnit))
        {
            UnitManager.UnitJsonData militaryUnit = UnitManager.Instance.LoadUnitJsonData(targetUnit.unitDataName);
            targetUnit.ResetUnit(stopPhysicsToo: true);
            targetUnit.statComponent.health = 0;
            targetUnit.actionComponent.SetActionSprite(militaryUnit.dying, "DeathEndAction");
            targetUnit.standSprite = militaryUnit.corpse;
            targetUnit.walkSprite = militaryUnit.corpse;
            targetUnit.actionComponent.StartAction();
        }
    }
}
