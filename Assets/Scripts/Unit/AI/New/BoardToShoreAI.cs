using CoreGameUnitAI;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class BoardToShoreAI : MoveToPositionAI, IAIController
{
    public BoardToShoreAI(UnitAIController controller, Vector3 position, ulong newCrowdID) : base(controller, position, newCrowdID)
    {
    }


    public override void OnReachedDestination()
    {
        BoardToShore();
    }

    void BoardToShore()
    {
        MovableUnit self = controller.GetSelf();
        if (!self.IsShip()) return;
        if (self.shipData.isDocked) return;
        // Do some check to find undockable spot. As in proper ground. Find friendly navmesh at navmesh links perhaps??
        IEnumerator<IDeterministicYieldInstruction> DelayedDocking()
        {
            yield return new DeterministicWaitForSeconds(0);
            if (!self.shipData.isDocked && !self.movementComponent.AnyPathOperationInProgress())
            {
                foreach (var navlink in self.shipData.navMeshLinks)
                {
                    int navAreaMask = 1;
                    if (NavMesh.SamplePosition(navlink.transform.TransformPoint(navlink.endPoint), out NavMeshHit navHit, navlink.width, navAreaMask))
                    {
                        DebugExtension.DebugWireSphere(navHit.position, Color.green, 0.2f, 5f);
                        self.shipData.SetDockedMode(true);
                        break;
                    }
                }
            }
        }
        DeterministicUpdateManager.Instance.CoroutineManager.StartCoroutine(DelayedDocking());
    }
}
