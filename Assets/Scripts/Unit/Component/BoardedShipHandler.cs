using UnityEngine;

public class BoardedShipHandler : MonoBehaviour
{
    public MovableUnit shipA = null;
    public MovableUnit shipB = null;

    public void RemoveReferences()
    {
        if (shipA != null)
        {
            Debug.Assert(shipA.shipData.isDocked);
            shipA.shipData.SetupDockedNavigation(false);
            shipA.shipData.boardedShipHandler = null;
        }
        if (shipB != null)
        {
            Debug.Assert(shipB.shipData.isDocked);
            shipB.shipData.SetupDockedNavigation(false);
            shipB.shipData.boardedShipHandler = null;
        }
        Destroy(this.gameObject);
    }
}
