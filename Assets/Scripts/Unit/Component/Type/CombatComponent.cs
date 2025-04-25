using UnityEngine;

public class CombatComponent : UnitTypeComponent
{
    public enum Stance
    {
        Aggressive,
        StandGround,
        NoAttack
    }

    public float attackRange = 0f;
    public Stance stance;
}
