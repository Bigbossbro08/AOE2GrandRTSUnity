using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

public class DotsPhysicsTransformSync : MonoBehaviour, IDeterministicUpdate, IDeterministicPostPhysicsUpdate
{
    [SerializeField] private Unit unit;

    public void DeterministicUpdate(float deltaTime, ulong tickID)
    {
        //var em = UnitManager.Instance.ecsEntityManager;
        //em.SetComponentData(unit.entity, LocalTransform.FromPositionRotationScale(
        //    unit.transform.position,
        //    unit.transform.rotation,
        //    1.0f
        //));
    }

    private void OnEnable()
    {
        DeterministicUpdateManager.Instance.Register(this);
        DeterministicUpdateManager.Instance.RegisterPostPhysics(this);
    }

    private void OnDisable()
    {
        DeterministicUpdateManager.Instance.Unregister(this);
        DeterministicUpdateManager.Instance.UnregisterPostPhysics(this);
    }

    public void DeterministicPostPhysicsUpdate(float deltaTime, ulong tickID)
    {
        //var em = UnitManager.Instance.ecsEntityManager;
        //var ecsLocalTransform = em.GetComponentData<LocalTransform>(unit.entity);
        //unit.transform.position = ecsLocalTransform.Position;
        //unit.transform.rotation = ecsLocalTransform.Rotation;
    }
}
