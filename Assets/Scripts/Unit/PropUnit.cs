using UnityEngine;
using UnityEngine.AI;

public class PropUnit : Unit, MapLoader.IMapSaveLoad
{
    public string spriteName = "corpse";
    [SerializeField] DeterministicVisualUpdater visualUpdater;
    GameObject collisionHolder = null;
    CapsuleCollider capsuleCollider = null;
    BoxCollider boxCollider = null;
    NavMeshObstacle navMeshObstacle = null;

    void EnsureCollisionHolder()
    {
        if (collisionHolder == null)
        {
            collisionHolder = new GameObject("Collision");
            collisionHolder.transform.SetParent(transform, false);
        }
    }

    void SetupCapsuleCollision(float radius, float height, Vector3 offset)
    {
        if (capsuleCollider == null)
        {
            GameObject capsuleGO = new GameObject("Capsule Collsion");
            capsuleGO.transform.SetParent(collisionHolder.transform, false);
            capsuleCollider = capsuleGO.AddComponent<CapsuleCollider>();
        }

        capsuleCollider.height = height;
        capsuleCollider.radius = radius;

        capsuleCollider.center = offset;
    }

    void SetupCapsuleNavmesh(float radius, float height, Vector3 offset)
    {
        if (navMeshObstacle == null)
        {
            GameObject navmeshGO = new GameObject("Navmesh");
            navmeshGO.transform.SetParent(transform, false);
            navMeshObstacle = navmeshGO.AddComponent<NavMeshObstacle>();
        }

        navMeshObstacle.shape = NavMeshObstacleShape.Capsule;
        navMeshObstacle.center = offset;

        navMeshObstacle.height = height;
        navMeshObstacle.radius = radius;
        navMeshObstacle.carving = true;
    }

    void SetupBoxCollision(Vector3 size, Vector3 offset)
    {
        if (boxCollider == null)
        {
            GameObject boxGO = new GameObject("Box Collision");
            boxGO.transform.SetParent(collisionHolder.transform, false);
            boxCollider = boxGO.AddComponent<BoxCollider>();
        }

        boxCollider.size = size;
        boxCollider.center = offset;
    }

    void SetupBoxNavmesh(Vector3 size, Vector3 offset)
    {
        if (navMeshObstacle == null)
        {
            GameObject navmeshGO = new GameObject("Navmesh");
            navmeshGO.transform.SetParent(transform, false);
            navMeshObstacle = navmeshGO.AddComponent<NavMeshObstacle>();
        }

        navMeshObstacle.shape = NavMeshObstacleShape.Box;
        navMeshObstacle.center = offset;
        navMeshObstacle.size = size;
        navMeshObstacle.carving = true;
    }

    void SetupPropCollision(UnitManager.UnitJsonData.Prop propData)
    {
        if (propData.collisionData != null)
        {
            if (propData.collisionData.radius.HasValue && propData.collisionData.height.HasValue)
            {
                if (boxCollider) boxCollider.gameObject.SetActive(false);

                EnsureCollisionHolder();
                SetupCapsuleCollision(
                    propData.collisionData.radius.Value,
                    propData.collisionData.height.Value,
                    propData.collisionData.offset.HasValue ? (Vector3)propData.collisionData.offset.Value : Vector3.zero);
            }

            if (propData.collisionData.size3d.HasValue)
            {
                if (capsuleCollider) capsuleCollider.gameObject.SetActive(false);

                EnsureCollisionHolder();
                SetupBoxCollision(
                    (Vector3)propData.collisionData.size3d.Value,
                    propData.collisionData.offset.HasValue ? (Vector3)propData.collisionData.offset.Value : Vector3.zero);
            }
        }
        else
        {
            if (collisionHolder)
            {
                collisionHolder.SetActive(false);
            }
        }

    }

    void SetupPropNavmesh(UnitManager.UnitJsonData.Prop propData)
    {
        if (propData.navMeshObstacleData != null)
        {
            if (propData.navMeshObstacleData.height.HasValue && propData.navMeshObstacleData.radius.HasValue)
            {
                EnsureCollisionHolder();
                SetupCapsuleNavmesh(
                    propData.navMeshObstacleData.radius.Value,
                    propData.navMeshObstacleData.height.Value,
                    propData.navMeshObstacleData.offset.HasValue ? (Vector3)propData.navMeshObstacleData.offset.Value : Vector3.zero);
            }


            if (propData.navMeshObstacleData.size3d.HasValue)
            {
                EnsureCollisionHolder();
                SetupBoxNavmesh(
                    (Vector3)propData.navMeshObstacleData.size3d.Value,
                    propData.navMeshObstacleData.offset.HasValue ? (Vector3)propData.navMeshObstacleData.offset.Value : Vector3.zero);
            }
        }
        else
        {
            if (navMeshObstacle)
            {
                navMeshObstacle.gameObject.SetActive(false);
            }
        }
    }
        
    private void OnEnable()
    {
        UnitManager.UnitJsonData.Prop propData = UnitManager.Instance.LoadPropJsonData(unitDataName);
        if (propData != null)
        {
            spriteName = propData.graphics;
            SetVisual(spriteName);

            SetupPropCollision(propData);
            SetupPropNavmesh(propData);
        }
        else
        {
        }
        Initialize();
        //DeterministicUpdateManager.Instance.Register(this);
    }

    private void OnDisable()
    {
        //DeterministicUpdateManager.Instance.Unregister(this);
    }

    private void SetVisual(string sprite)
    {
        visualUpdater.CustomDeterministicVisualUpdate = null;
        visualUpdater.SetSpriteName(sprite, true);
        visualUpdater.PlayOrResume(false);
        visualUpdater.playerId = playerId;
        visualUpdater.RefreshVisuals();
    }
}
