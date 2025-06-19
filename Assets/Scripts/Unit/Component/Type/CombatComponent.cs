using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CombatComponent : UnitTypeComponent, IDeterministicUpdate
{
    public enum Stance
    {
        Aggressive,
        StandGround,
        NoAttack
    }

    public string attackSprite = "";
    //public float damage = 0;
    public float attackRange = 0f;
    public float attackDelay = 0f;
    public float currentAttackDelay = 0f;
    [SerializeField] public List<ActionComponent.ActionEvent> actionEvents = new List<ActionComponent.ActionEvent>();
    public Vector3? projectile_offset = null;
    public string projectile_unit = null;
    public Stance stance;

    private void Awake()
    {
        enabled = false;
    }

    private void OnEnable()
    {
        currentAttackDelay = 0f;
        DeterministicUpdateManager.Instance.Register(this);
    }

    private void OnDisable()
    {
        DeterministicUpdateManager.Instance.Unregister(this);
        currentAttackDelay = 0f;
    }

    public void StartDelay()
    {
        if (attackDelay == 0.0f) return;
        enabled = true;
    }

    public bool IsAttackDelayInProgress() { return currentAttackDelay > 0; }

    public void DeterministicUpdate(float deltaTime, ulong tickID)
    {
        currentAttackDelay += deltaTime;
        if (currentAttackDelay > attackDelay)
        {
            currentAttackDelay = 0f;
            enabled = false;
        }
    }

    public static float CalculateDamageData
        (UnitManager.UnitJsonData.DamageData sourceDamageData, 
         UnitManager.UnitJsonData.DamageData targetDamageData)
    {
        var baseArmor = 1000; //targetDamageData.baseArmor;

        // 1) Build lookup dictionaries once per call (you could cache these if you prefer)
        var attackDict = sourceDamageData.attackValues
                         .ToDictionary(e => e.armorClass, e => e.value);
        var armorDict = targetDamageData.armorValues
                         .ToDictionary(e => e.armorClass, e => e.value);

        // 2) Sum up AoE2 damage: sum(max(atk_i - defArmor_i, 0))
        int totalDamage = 0;
        foreach (var kv in attackDict)
        {
            int atk = kv.Value;
            int defArmor = armorDict.TryGetValue(kv.Key, out var ar)
                            ? ar
                            : baseArmor;
            totalDamage += Mathf.Max(atk - defArmor, 0);
        }

        // 3) Guarantee at least 1 damage
        totalDamage = Mathf.Max(totalDamage, 1);
        return totalDamage;
    }
}
