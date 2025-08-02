using UnityEngine;

public class BoardshipUnitAIModule : TargetFollowingMovementAIModule
{
    // TODO make snap angles
    //void AlignSideBySide(Transform a, Transform b, float moveCloserBy, int snapAngles = 16)
    //{
    //    // Step 1: Get midpoint
    //    Vector3 center = (a.position + b.position) * 0.5f;
    //
    //    // Step 2: Get direction from a to b (on horizontal plane)
    //    Vector3 rawDir = b.position - a.position;
    //    rawDir.y = 0f;
    //
    //    // Step 3: Snap the direction to the nearest of `snapAngles`
    //    float angle = Mathf.Atan2(rawDir.z, rawDir.x) * Mathf.Rad2Deg;
    //    float snappedAngle = Mathf.Round(angle / (360f / snapAngles)) * (360f / snapAngles);
    //    float rad = snappedAngle * Mathf.Deg2Rad;
    //
    //    Vector3 snappedDir = new Vector3(Mathf.Cos(rad), 0f, Mathf.Sin(rad)).normalized;
    //
    //    // Step 4: Get side-facing direction
    //    Vector3 sideDir = Vector3.Cross(Vector3.up, snappedDir).normalized;
    //
    //    // Step 5: Set rotations so both look side-by-side (opposite directions)
    //    a.rotation = Quaternion.LookRotation(sideDir, Vector3.up);
    //    b.rotation = Quaternion.LookRotation(-sideDir, Vector3.up);
    //
    //    // Step 6: Set positions along snapped direction (same distance from center)
    //    Vector3 offset = snappedDir * (moveCloserBy * 0.5f);
    //    a.position = center - offset;
    //    b.position = center + offset;
    //}

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
            //if (target.playerId == self.playerId) { return; } // Temporary
            const float sideToSideDistance = 0.27f;
            AlignSideBySide(target.transform, self.transform, sideToSideDistance);
            MovableUnit.ShipData.DockAgainstAnotherShip(target, self);
            Debug.Log("Boarded ship?");
        }
        base.OnChangeState(newState);
    }
}
