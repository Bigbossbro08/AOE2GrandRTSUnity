using UnityEngine;

public class TriggerHelperForCapsuleCollider : MonoBehaviour
{
    [SerializeField] MovementComponent movementComponent;

    private void OnTriggerEnter(Collider other)
    {
        if (movementComponent == null) return;
        movementComponent.OnCustomTriggerEnter(other);
    }

    private void OnTriggerExit(Collider other)
    {
        if (movementComponent == null) return;
        movementComponent.OnCustomTriggerExit(other);
    }
}
