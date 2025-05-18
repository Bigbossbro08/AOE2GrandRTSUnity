using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

public class UnitEventHandler : MonoBehaviour
{
    public static UnitEventHandler Instance;

    public enum EventID : int
    {
        None = 0,
        OnAttack,
        OnDeath,
        OnActionEnd,
        OnProjectileAttack
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
        }
        else
        {
            Instance = this;
        }

        RegisterEvents();
    }

    const int MAX_EVENT_COUNT = 1000; // For now

    private Action<object[]>[] handlers = new Action<object[]>[MAX_EVENT_COUNT];
    
    void RegisterEvents()
    {
        RegisterEvent((int)EventID.OnAttack, Event_OnAttack);
        RegisterEvent((int)EventID.OnDeath, Event_OnDeath);
        RegisterEvent((int)EventID.OnActionEnd, Event_OnActionEnd);
        RegisterEvent((int)EventID.OnProjectileAttack, Event_OnProjectileAttack);
    }

    public void RegisterEvent(int id, Action<object[]> handler)
    {
        handlers[id] += handler;
    }

    Dictionary<string, EventID> nameToId = new() {
        { "OnAttack", EventID.OnAttack },
        { "OnDeath", EventID.OnDeath },
        { "OnActionEnd", EventID.OnActionEnd },
        { "OnProjectileAttack", EventID.OnProjectileAttack }
    };

    public void CallEvent(string name, params object[] args)
    {
        if (nameToId.TryGetValue(name, out var id))
        {
            handlers[(int)id]?.Invoke(args); // O(1) after mapping
        }
    }

    public void CallEventByID(EventID eventID, params object[] args)
    {
        if (eventID.Equals(EventID.None)) return;

        // Format args to string
        string formattedArgs = args == null || args.Length == 0
            ? "(no args)"
            : string.Join(", ", args.Select(arg => arg?.ToString() ?? "null"));

        NativeLogger.Log($"Called unit event: UnitEventHandler.EventID.{eventID.ToString()}: {formattedArgs}");
        handlers[(int)eventID]?.Invoke(args); // O(1) after mapping
    }

    private static void Event_OnAttack(object[] obj)
    {
        ulong selfId = (ulong)obj[0];
        ulong targetId = (ulong)obj[1];
        if (selfId == 0 || targetId == 0) return;
        float damage = (float)obj[2];
        Unit targetUnit = UnitManager.Instance.GetUnit(targetId);
        if (targetUnit && targetUnit.GetType() == typeof(MovableUnit))
        {
            StatComponent.DamageUnit((MovableUnit)targetUnit, damage);
        }
        //Debug.Log($"OnAttack Event fired and values are {selfId}, {targetId}, {damage}");
    }

    private void Event_OnProjectileAttack(object[] obj)
    {
        ulong selfId = (ulong)obj[0];
        ulong targetId = (ulong)obj[1];
        if (selfId == 0 || targetId == 0) return;
        float damage = (float)obj[2];

        // TODO: possibly add some projectile data so that you can ensure which exact projectile to send
        Unit selfUnit = UnitManager.Instance.GetUnit(selfId);
        Unit targetUnit = UnitManager.Instance.GetUnit(targetId);
        if (selfUnit && selfUnit.GetType() == typeof(MovableUnit) && 
            targetUnit && targetUnit.GetType() == typeof(MovableUnit))
        {
            MovableUnit selfMovableUnit = (MovableUnit)selfUnit;
            CombatComponent combatComponent = selfMovableUnit.unitTypeComponent as CombatComponent;
            if (combatComponent != null)
            {
                UnitManager.UnitJsonData.ProjectileUnit projectileUnitData = UnitManager.Instance.LoadProjectileJsonData(combatComponent.projectile_unit);
                // TODO: use dynamic projectile speed using 
                float velocitySpeed = (projectileUnitData != null && projectileUnitData.projectile_speed.HasValue) ? projectileUnitData.projectile_speed.Value : 5f;
                float distance = Vector3.Distance(selfUnit.transform.position, targetUnit.transform.position);
                float time = distance / velocitySpeed;
                //float time = 1.0f;
                // TODO: make it more data based
                Vector3 startPosition = selfMovableUnit.transform.TransformPoint(combatComponent.projectile_offset != null ? combatComponent.projectile_offset.Value : Vector3.zero);
                // TODO: make accuracy more data based
                Vector3 targetPosition = ProjectileUnit.GetInaccurateTarget(startPosition, targetUnit.transform.position, 80, 15);
                ProjectileUnit projectile = UnitManager.Instance.projectileUnitPool.Get();
                projectile.SetProjectileData(selfMovableUnit, damage, combatComponent.projectile_unit);
                projectile.LaunchWithVelocity(startPosition, targetPosition, time);
            }
        }
    }

    private void Event_OnDeath(object[] obj)
    {
        ulong selfId = (ulong)obj[0];
        Unit unit = UnitManager.Instance.GetUnit(selfId);
        if (unit)
        {
            MovableUnit movableUnit = (MovableUnit)unit;
            if (movableUnit)
            {
                UnitManager.UnitJsonData militaryUnit = UnitManager.Instance.LoadUnitJsonData(movableUnit.unitDataName);
                movableUnit.ResetUnit();
                movableUnit.actionComponent.SetActionSprite(militaryUnit.dying, "DeathEndAction");
                movableUnit.standSprite = militaryUnit.corpse;
                movableUnit.walkSprite = militaryUnit.corpse;
                movableUnit.actionComponent.StartAction();
            }
        }
        //Debug.Log($"OnDeath Event fired and values are {selfId}");
    }

    private void Event_OnActionEnd(object[] obj)
    {
        string actionEndType = (string)obj[0];
        ulong selfId = (ulong)obj[1];

        switch (actionEndType)
        {
            case "DeathEndAction":
                {
                    Unit unit = UnitManager.Instance.GetUnit(selfId);
                    if (unit)
                    {
                        //unit.gameObject.SetActive(false);
                        MovableUnit movableUnit = (MovableUnit)unit;
                        if (movableUnit)
                        {
                            UnitManager.UnitJsonData militaryUnit = UnitManager.Instance.LoadUnitJsonData(movableUnit.unitDataName);
                            movableUnit.walkSprite = militaryUnit.corpse;
                            movableUnit.standSprite = militaryUnit.corpse;
                            DeadUnit deadUnit = UnitManager.Instance.deadUnitPool.Get();
                            deadUnit.playerId = movableUnit.playerId;
                            deadUnit.transform.position = movableUnit.transform.position;
                            deadUnit.transform.rotation = movableUnit.transform.rotation;
                            deadUnit.unitDataName = movableUnit.unitDataName;
                            deadUnit.SetVisual(militaryUnit.corpse);
                            UnitManager.Instance.movableUnitPool.Release(movableUnit);
                        }
                        //{
                        //    UnitManager.MilitaryUnit militaryUnit = UnitManager.Instance.LoadMilitaryUnit(movableUnit.unitDataName);
                        //}
                    }
                }
                break;
            case "CombatEndAction":
                {

                }
                break;
        }

        //Debug.Log($"OnActionEnd Event fired and values are {actionEndType}");
    }
}
