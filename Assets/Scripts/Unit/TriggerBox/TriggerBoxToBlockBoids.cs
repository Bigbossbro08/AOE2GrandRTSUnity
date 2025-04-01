using UnityEngine;

public class TriggerBoxToBlockBoids : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent(out MovementComponent movementComponent))
        {
            movementComponent.IncrementBlockBoidsCounter();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.TryGetComponent(out MovementComponent movementComponent))
        {
            movementComponent.DecrementBlockBoidsCounter();
        }
    }
}
