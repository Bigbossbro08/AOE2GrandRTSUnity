using UnityEngine;
using System.Collections.Generic;
using Unity.AI.Navigation;

public class WallBuilder : MonoBehaviour
{
    public GameObject wallPiecePrefab;
    public GameObject wallTowerPrefab;

    private Vector3 startPosition;
    private bool isBuilding = false;

    private bool hasFirstHitWall = false;
    private bool hasLastHitWall = false;

    // TODO make identifier to make sure when to start wall build
    //private bool startWallBuildingIdentifier = false;

    private GameObject wallParent = null;
    private GameObject nextWallParent = null;

    private GameObject currentWallTower = null;
    private GameObject nextWallTower = null;
    private GameObject inbetWeenWallPiece = null;

    private ulong wallCounter = 0;
    
    const float distanceToMakeNewTower = 6;

    private void Awake()
    {
        wallCounter = 0;
    }

    private void OnDisable()
    {
        EndWallDragging(true);
    }

    GameObject ValidateWallTower(GameObject wallTower, bool active)
    {
        if (wallTower == null)
        {
            wallTower = Instantiate(wallTowerPrefab);
            CapsuleCollider wallTowerCapsuleCollider = wallTower.GetComponentInChildren<CapsuleCollider>();
            if (wallTowerCapsuleCollider)
            {
                wallTowerCapsuleCollider.enabled = false;
            }
            wallTower.SetActive(active);
            return wallTower;
        }

        if (wallTower.activeSelf != active)
        {
            CapsuleCollider wallTowerCapsuleCollider = wallTower.GetComponentInChildren<CapsuleCollider>();
            if (wallTowerCapsuleCollider)
            {
                wallTowerCapsuleCollider.enabled = false;
            }
            wallTower.SetActive(active);
        }
        return wallTower;
    }

    void HandleWallDragging()
    {
        if (Input.GetMouseButtonDown(1))
        {
            EndWallDragging(true);
            return;
        }
        if (!isBuilding) return;
        Vector3? hitPoint = GetMouseHitPoint(out bool hasHitWallFromHit, out nextWallParent);
        if (hitPoint.HasValue)
        {
            hasLastHitWall = hasHitWallFromHit;
            Vector3 nextPosition = hitPoint.Value;
            Vector3 direction = (nextPosition - startPosition);
            float yAngle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
            Quaternion rotation = Quaternion.Euler(0f, yAngle, 0f);
            currentWallTower.transform.rotation = rotation;
            if (direction.sqrMagnitude > distanceToMakeNewTower * distanceToMakeNewTower)
            {
                nextWallTower = ValidateWallTower(nextWallTower, true);

                if (inbetWeenWallPiece == null)
                {
                    inbetWeenWallPiece = Instantiate(wallPiecePrefab);
                }
                inbetWeenWallPiece.SetActive(true);

                nextWallTower.transform.SetPositionAndRotation(nextPosition, rotation);
                Vector3 midPoint = (nextPosition + startPosition) / 2;
                inbetWeenWallPiece.transform.SetPositionAndRotation(midPoint, rotation);
                inbetWeenWallPiece.transform.localScale = new Vector3(2, 1, (startPosition - nextPosition).magnitude);
            }
            else
            {
                nextWallTower = ValidateWallTower(nextWallTower, false);

                if (inbetWeenWallPiece)
                {
                    inbetWeenWallPiece.SetActive(false);
                }
            }
        }
    }

    GameObject MakeMergeParent()
    {
        GameObject newWallParent = new GameObject("Wall Parent: " + wallCounter++);
        newWallParent.tag = "Wall Parent";
        newWallParent.transform.SetParent(null);
        NavMeshSurface navMeshSurface = newWallParent.AddComponent<NavMeshSurface>();
        if (navMeshSurface)
        {
            navMeshSurface.collectObjects = CollectObjects.Children;
        }
        return newWallParent;
    }
    
    // Function to move only first-level children
    void MoveFirstLevelChildren(GameObject parent, GameObject newParent)
    {
        List<Transform> firstLevelChildren = new List<Transform>();

        // Store the first-level children (direct children only)
        foreach (Transform child in parent.transform)
        {
            firstLevelChildren.Add(child);
        }

        // Move first-level children to newParent
        foreach (Transform child in firstLevelChildren)
        {
            child.SetParent(newParent.transform, true); // Keep world position
        }

        Destroy(parent);
    }

    void EndWallDragging(bool forceEnd = false)
    {
        // Use first tower and last tower as a mean to make wall permanently on the map...
        bool canPlaceWall = (currentWallTower.activeInHierarchy && nextWallTower.activeInHierarchy && inbetWeenWallPiece);
        if (canPlaceWall && !forceEnd)
        {
            GameObject newFirstWallTower = null;
            GameObject newLastWallTower = null;
            GameObject newWallPiece = null;

            // Cleanup
            if (currentWallTower.activeSelf)
            {
                if (!hasFirstHitWall)
                {
                    newFirstWallTower = Instantiate(wallTowerPrefab, currentWallTower.transform);
                    newFirstWallTower.transform.SetParent(null, true);
                    newFirstWallTower.name = "Wall Tower: " + wallCounter++;
                    CapsuleCollider wallTowerCapsuleCollider = newFirstWallTower.GetComponentInChildren<CapsuleCollider>();
                    if (wallTowerCapsuleCollider)
                    {
                        wallTowerCapsuleCollider.enabled = true;
                    }
                }
            }
            if (nextWallTower.activeSelf)
            {
                if (!hasLastHitWall)
                {
                    newLastWallTower = Instantiate(wallTowerPrefab, nextWallTower.transform);
                    newLastWallTower.name = "Wall Tower: " + wallCounter++;
                    newLastWallTower.transform.SetParent(null, true);
                    CapsuleCollider wallTowerCapsuleCollider = newLastWallTower.GetComponentInChildren<CapsuleCollider>();
                    if (wallTowerCapsuleCollider)
                    {
                        wallTowerCapsuleCollider.enabled = true;
                    }
                }
            }

            if (inbetWeenWallPiece.activeSelf)
            {
                newWallPiece = Instantiate(wallPiecePrefab, inbetWeenWallPiece.transform);
                newWallPiece.transform.SetParent(null, true);
                newWallPiece.name = "Wall Piece: " + wallCounter++;
                newWallPiece.tag = "Wall";
            }

            GameObject newWallParent = MakeMergeParent();

            // Merge the parent
            if (newWallParent)
            {
                // Move children from both parents
                if (wallParent)
                {
                    MoveFirstLevelChildren(wallParent, newWallParent);
                }

                if (nextWallParent)
                {
                    MoveFirstLevelChildren(nextWallParent, newWallParent);
                }

                if (newFirstWallTower)
                {
                    newFirstWallTower.transform.SetParent(newWallParent.transform, true);
                }

                if (newLastWallTower)
                {
                    newLastWallTower.transform.SetParent(newWallParent.transform, true);
                }

                if (newWallPiece)
                {
                    newWallPiece.transform.SetParent(newWallParent.transform, true);
                }
            }

            // We make all mesh being navmesh only thing
            foreach (Transform child in newWallParent.transform)
            {
                Transform trigger = child.Find("Trigger");
                if (trigger)
                {
                    trigger.gameObject.SetActive(false);
                }

                Transform visual = child.Find("Visual");
                if (visual)
                {
                    visual.gameObject.SetActive(false);
                }
                Transform navMesh = child.Find("Navmesh");
                if (navMesh)
                {
                    navMesh.gameObject.SetActive(true);
                }
            }

            // Finally surface build it
            if (newWallParent.TryGetComponent(out NavMeshSurface navMeshSurface))
            {
                navMeshSurface.BuildNavMesh();
            }

            // We bring back the visual
            foreach (Transform child in newWallParent.transform)
            {
                Transform trigger = child.Find("Trigger");
                if (trigger)
                {
                    trigger.gameObject.SetActive(true);
                }

                Transform visual = child.Find("Visual");
                if (visual)
                {
                    visual.gameObject.SetActive(true);
                }
                Transform navMesh = child.Find("Navmesh");
                if (navMesh)
                {
                    navMesh.gameObject.SetActive(false);
                }
            }

            //void TurnUpVisual(GameObject obj)
            //{
            //    Transform visual = obj.transform.Find("Visual");
            //    if (visual)
            //    {
            //        visual.gameObject.SetActive(true);
            //    }
            //
            //    Transform navMesh = obj.transform.Find("Navmesh");
            //    if (navMesh)
            //    {
            //        navMesh.gameObject.SetActive(false);
            //    }
            //}
            //
            //// Turn on visual
            //if (newFirstWallTower)
            //{
            //    TurnUpVisual(newFirstWallTower);
            //}
            //if (newLastWallTower)
            //{
            //    TurnUpVisual(newLastWallTower);
            //}
            //if (newWallPiece)
            //{
            //    TurnUpVisual(newWallPiece);
            //}
        }

        if (currentWallTower)
        {
            ValidateWallTower(currentWallTower, false);
        }

        if (nextWallTower)
        {
            ValidateWallTower(nextWallTower, false);
        }

        if (inbetWeenWallPiece)
        {
            inbetWeenWallPiece.SetActive(false);
        }

        wallParent = null;
        nextWallParent = null;
        
        //currentWallTower = null;
        //nextWallTower = null;
        //inbetWeenWallPiece = null;

        isBuilding = false;
        hasFirstHitWall = false;
        hasLastHitWall = false;
    }

    void OnWallDragStart()
    {
        Vector3? hitPoint = GetMouseHitPoint(out bool hasHitWallFromHit, out wallParent);
        if (hitPoint.HasValue)
        {
            startPosition = hitPoint.Value;

            currentWallTower = ValidateWallTower(currentWallTower, true);
            currentWallTower.transform.position = startPosition;

            hasFirstHitWall = hasHitWallFromHit;
            hasLastHitWall = false;
            isBuilding = true;
        }
    }

    void Update()
    {
        // Start building on left mouse button press.
        if (Input.GetMouseButtonDown(0))
        {
            OnWallDragStart();
        }

        // Continue building while dragging.
        HandleWallDragging();

        // End building on mouse button release.
        if (Input.GetMouseButtonUp(0))
        {
            EndWallDragging();
        }
    }

    // Helper method to perform a raycast from the mouse position.
    private Vector3? GetMouseHitPoint(out bool hasHitWall, out GameObject wallParentObj)
    {
        wallParentObj = null;
        hasHitWall = false;
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        //RaycastHit hit;
        RaycastHit[] hits = Physics.SphereCastAll(ray, 0.1f, Mathf.Infinity);
        Vector3? hitPoint = null;
        foreach (var hit in hits)
        {
            if (hit.collider.CompareTag("Wall Connector"))
            {
                Debug.Log("Wall Connector found");
                hasHitWall = true;
                if (hit.transform.root.CompareTag("Wall Parent"))
                {
                    wallParentObj = hit.transform.root.gameObject;
                    Debug.Log("Wall parent exist");
                }
                return hit.collider.transform.position;
            }
            if (hit.collider.CompareTag("Wall")) continue;
            if (hit.collider.CompareTag("Wall Untagged")) continue;
            
            hitPoint = hit.point;
        }

        // Assuming your terrain or placement plane is on a specific layer or has a collider.
        //if (Physics.Raycast(ray, out hit, Mathf.Infinity))
        //{
        //    return hit.point;
       // }
        return hitPoint;
    }
}