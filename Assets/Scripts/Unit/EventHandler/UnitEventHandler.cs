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
        OnProjectileAttack,
        OnCorpseSpawn,
        OnUnitSpawn,
        OnUnitRemove,
        OnUnitKilled
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

    private void OnDestroy()
    {
        UnRegisterEvents();
    }

    const int MAX_EVENT_COUNT = 1000; // For now

    private Action<object[]>[] handlers = new Action<object[]>[MAX_EVENT_COUNT];
    
    void RegisterEvents()
    {
        RegisterEvent((int)EventID.OnAttack, Event_OnAttack);
        RegisterEvent((int)EventID.OnDeath, Event_OnDeath);
        RegisterEvent((int)EventID.OnActionEnd, Event_OnActionEnd);
        RegisterEvent((int)EventID.OnProjectileAttack, Event_OnProjectileAttack);
        RegisterEvent((int)EventID.OnCorpseSpawn, Event_OnCorpseSpawn);
        RegisterEvent((int)EventID.OnUnitSpawn, Event_OnUnitSpawn);
        RegisterEvent((int)EventID.OnUnitRemove, Event_OnUnitRemove);
        RegisterEvent((int)EventID.OnUnitKilled, Event_OnUnitKilled);
    }

    void UnRegisterEvents()
    {
        UnRegisterEvent((int)EventID.OnAttack, Event_OnAttack);
        UnRegisterEvent((int)EventID.OnDeath, Event_OnDeath);
        UnRegisterEvent((int)EventID.OnActionEnd, Event_OnActionEnd);
        UnRegisterEvent((int)EventID.OnProjectileAttack, Event_OnProjectileAttack);
        UnRegisterEvent((int)EventID.OnCorpseSpawn, Event_OnCorpseSpawn);
        UnRegisterEvent((int)EventID.OnUnitSpawn, Event_OnUnitSpawn);
        UnRegisterEvent((int)EventID.OnUnitRemove, Event_OnUnitRemove);
        UnRegisterEvent((int)EventID.OnUnitKilled, Event_OnUnitKilled);
    }

    public void RegisterEvent(int id, Action<object[]> handler)
    {
        handlers[id] += handler;
    }

    public void UnRegisterEvent(int id, Action<object[]> handler)
    {
        handlers[id] -= handler;
    }

    Dictionary<string, EventID> nameToId = new() {
        { "OnAttack", EventID.OnAttack },
        { "OnDeath", EventID.OnDeath },
        { "OnActionEnd", EventID.OnActionEnd },
        { "OnProjectileAttack", EventID.OnProjectileAttack }
    };

    //public void CallEvent(string name, params object[] args)
    //{
    //    if (nameToId.TryGetValue(name, out var id))
    //    {
    //        handlers[(int)id]?.Invoke(args); // O(1) after mapping
    //    }
    //}

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
        if (obj == null || obj.Length < 3)
        {
            NativeLogger.Error("Event_OnAttack received invalid arguments.");
            return;
        }

        if (!(obj[0] is ulong selfId) || selfId == 0 ||
            !(obj[1] is ulong targetId) || targetId == 0 ||
            !(obj[2] is UnitManager.UnitJsonData.DamageData damageData))
        {
            NativeLogger.Error("Event_OnAttack received invalid or malformed parameters.");
            return;
        }

        Unit targetUnit = UnitManager.Instance.GetUnit(targetId);

        if (targetUnit is not MovableUnit movableTarget)
        {
            NativeLogger.Error("Event_OnAttack called on invalid target unit type.");
            return;
        }

        if (!StatComponent.DamageUnit(movableTarget, damageData))
        {
            // Target did not die, so nothing to report.
            return;
        }

        // Handle the kill event.
        Unit killerUnit = GetKillerUnit(selfId);

        if (killerUnit != null)
        {
            Instance.CallEventByID(EventID.OnUnitKilled, killerUnit.id, targetId);
        }
        else
        {
            NativeLogger.Warning($"Unit killed by unknown source. selfId: {selfId}, targetId: {targetId}");
        }
    }

    private static Unit GetKillerUnit(ulong selfId)
    {
        var projectile = UnitManager.Instance.GetUnit(selfId) as ProjectileUnit;

        if (projectile != null)
        {
            var source = projectile.GetSourceUnit();
            return source ?? projectile;
        }

        return UnitManager.Instance.GetUnit(selfId);
    }

    private void Event_OnProjectileAttack(object[] obj)
    {
        ulong selfId = (ulong)obj[0];
        ulong targetId = (ulong)obj[1];
        if (selfId == 0 || targetId == 0) return;
        UnitManager.UnitJsonData.DamageData damage = (UnitManager.UnitJsonData.DamageData)obj[2];

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
                Vector3 projectile_offset = combatComponent.projectile_offset != null ? combatComponent.projectile_offset.Value : Vector3.zero;
                Vector3 startPosition = selfMovableUnit.transform.TransformPoint(projectile_offset);
                float enemyHeight = 0.35f;
                // TODO: make accuracy more data based
                Vector3 targetPosition = ProjectileUnit.GetInaccurateTarget(startPosition, targetUnit.transform.position + Vector3.up * enemyHeight, 80, 5);
                //NativeLogger.Log($" ");
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
                StatComponent.KillUnit(movableUnit);
            }
        }
        NativeLogger.Log($"OnDeath Event fired and values are {selfId}");
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
                            movableUnit.statComponent.OnDeathCallback?.Invoke(movableUnit.id);
                            UnitManager.Instance.ReleaseMovableUnitFromPool(movableUnit);
                            //UnitManager.Instance.movableUnitPool.Release(movableUnit);
                            CallEventByID(EventID.OnCorpseSpawn, movableUnit.id, deadUnit.id);
                        }
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

    private void Event_OnCorpseSpawn(object[] obj)
    {
        ulong selfId = (ulong)obj[0];
        ulong corpseId = (ulong)obj[1];
    }

    private void Event_OnUnitSpawn(object[] obj)
    {
        ulong selfId = (ulong)obj[0];
    }

    private void Event_OnUnitRemove(object[] obj)
    {
        ulong selfId = (ulong)obj[0];
    }

    private void Event_OnUnitKilled(object[] obj)
    {
        ulong selfId = (ulong)obj[0];
        ulong targetId = (ulong)obj[1];

        NativeLogger.Log($"{selfId} has killed {targetId}");
    }
}
