using UnityEngine;

public class StatComponent : MonoBehaviour
{
    private float health = 45f;

    public float GetHealth() {  return health; }

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

    public static void DamageUnit(MovableUnit targetUnit, float damage)
    {
        float targetHealth = targetUnit.statComponent.GetHealth();
        targetHealth -= damage;
        targetUnit.statComponent.SetHealth(targetHealth, targetUnit);
    }

    public static bool IsUnitAliveOrValid(MovableUnit unit)
    {
        if (!unit) return false;
        if (UnitManager.Instance.GetUnit(unit.id) == null) return false;
        if (unit.statComponent.GetHealth() <= 0.0f) return false;
        return true;
    }
}
