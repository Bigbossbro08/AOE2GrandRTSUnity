using UnityEngine;

public class TriggerBoxToForceRaycast : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent(out MovementComponent movementComponent))
        {
            movementComponent.IncrementActivateRaycastCounter();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.TryGetComponent(out MovementComponent movementComponent))
        {
            movementComponent.DecrementActivateRaycastCounter();
        }
    }
}
