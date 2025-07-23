using UnityEngine;

public class BoardshipUnitAIModule : TargetFollowingMovementAIModule
{
    void AlignSideBySide(Transform a, Transform b, float moveCloserBy)
    {
        // Vector between objects
        Vector3 dir = b.position - a.position;

        // Get side-facing direction (perpendicular to dir)
        Vector3 sideDir = Vector3.Cross(Vector3.up, dir).normalized;

        // We should do this mainly due to npcs of the ship trying to board each other at same side due to how navmesh is setup.
        // So link 1 and link 2 should left/right by default.
        a.rotation = Quaternion.LookRotation(sideDir, Vector3.up);
        b.rotation = Quaternion.LookRotation(-sideDir, Vector3.up);

        // Move both toward each other along dir
        Vector3 center = (a.position + b.position) * 0.5f;

        // Normalize the direction between them
        Vector3 towardEachOther = (a.position - b.position).normalized;

        // Move each one closer to center by half the distance
        a.position -= towardEachOther * (moveCloserBy * 0.5f);
        b.position += towardEachOther * (moveCloserBy * 0.5f);
    }

    public override void OnChangeState(State newState)
    {
        if (newState == State.CloseToTarget)
        {
            if (!target.IsShip()) return;
            if (target.shipData.isDocked) { return; }
            if (target.playerId == self.playerId) { return; } // Temporary
            const float sideToSideDistance = 0.27f;
            AlignSideBySide(target.transform, self.transform, sideToSideDistance);
            MovableUnit.ShipData.DockAgainstAnotherShip(target, self);
            Debug.Log("Boarded ship?");
        }
        base.OnChangeState(newState);
    }
}
