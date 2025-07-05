using UnityEngine;
using UnityEngine.LowLevelPhysics;

public class MissionCollisionTriggerChecker : MonoBehaviour
{
    public System.Func<bool> CheckerCallback;
    public System.Action<Collider> OnTriggerEnterCallback;
    public System.Action<Collider> OnTriggerExitCallback;

    private void OnTriggerEnter(Collider other)
    {
        OnTriggerEnterCallback?.Invoke(other);
    }

    private void OnTriggerExit(Collider other)
    {
        OnTriggerExitCallback?.Invoke(other);
    }

    public static MissionCollisionTriggerChecker SpawnSphere(Vector3 position, Quaternion rotation, Vector3 scale,
        float radius, Vector3 center,
        System.Func<bool> CheckerCallback = null,
        System.Action<Collider> OnTriggerEnterCallback = null,
        System.Action<Collider> OnTriggerExitCallback = null)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.layer = 2; // Ignore Raycast
        go.transform.position = position;
        go.transform.rotation = rotation;
        go.transform.localScale = scale;

        MeshRenderer meshRenderer = go.GetComponent<MeshRenderer>();
        meshRenderer.material = ObjectivePanel.Instance.missionDebugMaterial;
        meshRenderer.material.color = new Color(1, 1, 1, 0.15f);

        SphereCollider sphereCollider = go.GetComponent<SphereCollider>();
        sphereCollider.center = center;
        sphereCollider.radius = radius;
        sphereCollider.isTrigger = true;

        Rigidbody rb = go.AddComponent<Rigidbody>();
        rb.useGravity = false;

        MissionCollisionTriggerChecker missionCollisionTriggerChecker = go.AddComponent<MissionCollisionTriggerChecker>();
        missionCollisionTriggerChecker.CheckerCallback = CheckerCallback;
        missionCollisionTriggerChecker.OnTriggerEnterCallback = OnTriggerEnterCallback;
        missionCollisionTriggerChecker.OnTriggerExitCallback = OnTriggerExitCallback;
        return missionCollisionTriggerChecker;
    }

    public static MissionCollisionTriggerChecker SpawnCylinder(Vector3 position, Quaternion rotation, Vector3 scale,
        float radius, float height, int direction, Vector3 center,
        System.Func<bool> CheckerCallback = null,
        System.Action<Collider> OnTriggerEnterCallback = null,
        System.Action<Collider> OnTriggerExitCallback = null)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        go.layer = 2; // Ignore Raycast
        go.transform.position = position;
        go.transform.rotation = rotation;
        go.transform.localScale = scale;

        CapsuleCollider capsuleCollider = go.GetComponent<CapsuleCollider>();
        capsuleCollider.center = center;
        capsuleCollider.radius = radius;
        capsuleCollider.direction = direction;
        capsuleCollider.height = height;
        capsuleCollider.isTrigger = true;

        MeshRenderer meshRenderer = go.GetComponent<MeshRenderer>();
        meshRenderer.material = ObjectivePanel.Instance.missionDebugMaterial;
        meshRenderer.material.color = new Color(1, 1, 1, 0.15f);

        MissionCollisionTriggerChecker missionCollisionTriggerChecker = go.AddComponent<MissionCollisionTriggerChecker>();
        missionCollisionTriggerChecker.CheckerCallback = CheckerCallback;
        missionCollisionTriggerChecker.OnTriggerEnterCallback = OnTriggerEnterCallback;
        missionCollisionTriggerChecker.OnTriggerExitCallback = OnTriggerExitCallback;

        return missionCollisionTriggerChecker;
    }

    public static MissionCollisionTriggerChecker SpawnBox(Vector3 position, Quaternion rotation, Vector3 scale, 
        Vector3 size, Vector3 center,
        System.Func<bool> CheckerCallback = null, 
        System.Action<Collider> OnTriggerEnterCallback = null,
        System.Action<Collider> OnTriggerExitCallback = null)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.layer = 2; // Ignore Raycast
        go.transform.position = position;
        go.transform.rotation = rotation;
        go.transform.localScale = scale;

        MeshRenderer meshRenderer = go.GetComponent<MeshRenderer>();
        meshRenderer.material = ObjectivePanel.Instance.missionDebugMaterial;
        meshRenderer.material.color = new Color(1, 1, 1, 0.15f);

        BoxCollider boxCollider = go.GetComponent<BoxCollider>();
        boxCollider.size = size;
        boxCollider.center = center;
        boxCollider.isTrigger = true;
        Rigidbody rb = go.AddComponent<Rigidbody>();
        rb.useGravity = false;

        MissionCollisionTriggerChecker missionCollisionTriggerChecker = go.AddComponent<MissionCollisionTriggerChecker>();
        missionCollisionTriggerChecker.CheckerCallback = CheckerCallback;
        missionCollisionTriggerChecker.OnTriggerEnterCallback = OnTriggerEnterCallback;
        missionCollisionTriggerChecker.OnTriggerExitCallback = OnTriggerExitCallback;
        return missionCollisionTriggerChecker;
    }
}
