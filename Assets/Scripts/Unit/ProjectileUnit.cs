using System.Collections.Generic;
using UnityEngine;

public class ProjectileUnit : Unit, IDeterministicUpdate
{
    [SerializeField] private Rigidbody _rigidbody;
    [SerializeField] private DeterministicVisualUpdater visualUpdater;
    public string spriteName = "idle";
    private Unit sourceUnit = null;
    [SerializeField] private Collider _collider;
    UnitManager.UnitJsonData.DamageData damageData = new UnitManager.UnitJsonData.DamageData();

    private void OnEnable()
    {
        //transform.position = start.position;
        //LaunchWithVelocity(start.position, end.position, 1);
        Initialize();
        _rigidbody.isKinematic = false;
        _collider.enabled = true;
        DeterministicUpdateManager.Instance.Register(this);
    }

    public void SetVisual(string sprite)
    {
        Debug.Log("Should update visuals...");
        visualUpdater.CustomDeterministicVisualUpdate = CustomDeterministicVisualUpdate;
        visualUpdater.SetSpriteName(sprite, true);
        visualUpdater.PlayOrResume(false);
        visualUpdater.playerId = playerId;
        visualUpdater.RefreshVisuals();
    }

    void CustomDeterministicVisualUpdate()
    {
        float angle = Utilities.NormalizeAndMapTo01(transform.eulerAngles.x);
        float normalized = Mathf.InverseLerp(-90f, 90f, angle);
        visualUpdater.elapsedFixedTime = normalized;
    }

    private void OnDisable()
    {
        _rigidbody.isKinematic = true;
        _collider.enabled = false;
        DeterministicUpdateManager.Instance.Unregister(this);
    }

    public Unit GetSourceUnit() { return sourceUnit; }

    public void DeterministicUpdate(float deltaTime, ulong tickID)
    {
        if (_rigidbody.linearVelocity.sqrMagnitude > 0.01f)
            transform.forward = _rigidbody.linearVelocity.normalized;
        else
            enabled = false;
    }

    public void SetProjectileData(MovableUnit sourceUnit = null, UnitManager.UnitJsonData.DamageData damageData = default)
    {
        UnitManager.UnitJsonData.DamageData newDamageData = damageData;
        this.damageData = newDamageData;
        this.sourceUnit = sourceUnit;
    }

    public void LaunchWithVelocity(Vector3 start, Vector3 target, float time)
    {
        if (!enabled)
        {
            enabled = true;
        }
        _rigidbody.isKinematic = false;
        _collider.enabled = true;
        transform.position = start;
        Vector3 velocity = CalculateLaunchVelocity(start, target, time);
        _rigidbody.linearVelocity = velocity;
    }

    private Vector3 CalculateLaunchVelocity(Vector3 start, Vector3 target, float time)
    {
        Vector3 displacement = target - start;
        Vector3 gravity = Physics.gravity;

        return (displacement - 0.5f * gravity * time * time) / time;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.transform == this.transform) { return; }
        if (other.transform == sourceUnit.transform) return;
        if (other.TryGetComponent(out TriggerHelperForCapsuleCollider triggerHelperForCapsuleCollider))
        {
            return;
        }
        if (other.TryGetComponent(out ProjectileUnit projectileUnit))
        {
            enabled = false;
            return;
        }
        if (other.TryGetComponent(out MovableUnit otherMovableUnit))
        {
            if (otherMovableUnit.playerId == sourceUnit.playerId) return;

            NativeLogger.Log($"Projectile collision hit! Named {other.gameObject.name}");
            // Execute event
            UnitEventHandler.Instance.CallEventByID(UnitEventHandler.EventID.OnAttack, sourceUnit.id, otherMovableUnit.id, damageData);

            // Return to pool
            gameObject.SetActive(false);
        }
        NativeLogger.Log($"Collision hit at: {other.name}");
        //_rigidbody.isKinematic = true;
        //_collider.enabled = false;
        enabled = false;
    }

    private void OnCollisionEnter(Collision collision)
    {
        NativeLogger.Log($"Collision hit at: {collision.collider.name}");
        //_rigidbody.isKinematic = true;
        //_collider.enabled = false;
    }

    public static Vector3 GetInaccurateTarget(Vector3 origin, Vector3 target, float accuracy, float maxAngle = 5f)
    {
        Utilities.DeterministicRandom deterministicRandom = DeterministicUpdateManager.Instance.deterministicRandom;
        float roll = deterministicRandom.NextFloat(0f, 100f);

        if (roll <= accuracy)
            return target; // accurate shot

        // Deviation shot
        Vector3 direction = (target - origin).normalized;

        // Create a random deviation within a cone
        float angle = deterministicRandom.NextFloat(0f, maxAngle);
        float yaw = deterministicRandom.NextFloat(0f, 360f);

        Quaternion deviation = Quaternion.AngleAxis(angle, Quaternion.AngleAxis(yaw, direction) * Vector3.up);
        Vector3 deviatedDir = deviation * direction;

        float distance = Vector3.Distance(origin, target);
        return origin + deviatedDir * distance;
    }

    internal DeterministicVisualUpdater GetDeterministicVisualUpdater()
    {
        return visualUpdater;
    }
}
