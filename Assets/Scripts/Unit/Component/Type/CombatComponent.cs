using System.Collections.Generic;
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
    public float damage = 0;
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
}
