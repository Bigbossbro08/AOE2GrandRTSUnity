using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class DockToShoreUnitAIModule : BasicMovementAIModule
{
    public override void OnChangeState(State newState, bool force)
    {
        if (newState == State.ReachedDestination && !force)
        {
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
                base.OnChangeState(newState, force);
            }
            DeterministicUpdateManager.Instance.CoroutineManager.StartCoroutine(DelayedDocking());
        }
    }
}
