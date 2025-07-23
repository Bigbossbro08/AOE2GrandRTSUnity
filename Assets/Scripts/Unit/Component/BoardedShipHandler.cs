using UnityEngine;

public class BoardedShipHandler : MonoBehaviour
{
    public MovableUnit shipA = null;
    public MovableUnit shipB = null;

    public void RemoveReferences()
    {
        if (shipA != null)
        {
            shipA.shipData.boardedShipHandler = null;
        }
        if (shipB != null)
        {
            shipB.shipData.boardedShipHandler = null;
        }
        Destroy(this.gameObject);
    }
}
