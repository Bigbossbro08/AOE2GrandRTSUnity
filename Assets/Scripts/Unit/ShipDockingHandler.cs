using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.UIElements;

public class ShipDockingHandler : MonoBehaviour, IDeterministicUpdate, MapLoader.IMapSaveLoad
{
    public class ShipDockingHandlerData : MapLoader.SaveLoadData
    {
        [JsonProperty] public bool enabled;
        [JsonProperty] public float thresholdForDocking = 5.0f;
        [JsonProperty] public CommonStructures.SerializableVector3 targetPointToDock;

        public ShipDockingHandlerData() { type = "ShipDockingHandlerData"; }
    }

    public float thresholdForDocking = 5.0f;
    Vector3 targetPointToDock = Vector3.zero;

    [SerializeField]
    MovementComponent m_MovementComponent;

    [SerializeField]
    ShipSurfaceController shipSurfaceController;

    private void OnEnable()
    {
        DeterministicUpdateManager.Instance.Register(this);
        m_MovementComponent.OnStopMoving += M_MovementComponent_OnStopMoving;
        //m_MovementComponent.controlRotation = false;
        m_MovementComponent.RemoveState(MovementComponent.MovementFlag.ControlRotation);
    }

    private void M_MovementComponent_OnStopMoving()
    {
        m_MovementComponent.SetState(MovementComponent.MovementFlag.ControlRotation);
        if (shipSurfaceController)
        {
            shipSurfaceController.SetShipState(true);
        }
    }

    private void OnDisable()
    {
        DeterministicUpdateManager.Instance.Unregister(this);
        m_MovementComponent.SetState(MovementComponent.MovementFlag.ControlRotation);
        m_MovementComponent.OnStopMoving -= M_MovementComponent_OnStopMoving;
    }

    public void SetTargetPointToDock(Vector3 targetPointToDock)
    {
        this.targetPointToDock = targetPointToDock;
    }

    public void DeterministicUpdate(float deltaTime, ulong tickID)
    {
        if (!m_MovementComponent) return;
        var pathPositions = m_MovementComponent.GetPathPositions();
        if (pathPositions.Count == 0) { return; }
        Vector3 lastPosition = pathPositions[pathPositions.Count - 1];
        Vector3 diff = targetPointToDock - lastPosition;
        Debug.DrawLine(targetPointToDock, lastPosition);
        Vector3 diffToTransform = lastPosition - transform.localPosition;
        if (diffToTransform.sqrMagnitude < thresholdForDocking * thresholdForDocking)
        {
            m_MovementComponent.RemoveState(MovementComponent.MovementFlag.ControlRotation);
            float yAngle = transform.localEulerAngles.y;
            if (diff != Vector3.zero)
            {
                diff = diff.normalized;
                float firstyAngle = Mathf.Atan2(diff.z, diff.x) * Mathf.Rad2Deg;
                float secondyAngle = Mathf.Atan2(diff.z, diff.x) * Mathf.Rad2Deg + 180;
                float firstDeltaAngle = Mathf.DeltaAngle(yAngle, firstyAngle);
                float secondDeltaAngle = Mathf.DeltaAngle(yAngle, secondyAngle);
                yAngle = firstyAngle;
                if (Mathf.Abs(secondDeltaAngle) < Mathf.Abs(firstDeltaAngle))
                {
                    yAngle = secondyAngle;
                }
                float rotationDelta = m_MovementComponent.GetRotationSpeed() * deltaTime;
                transform.eulerAngles = new Vector3(0, Mathf.LerpAngle(transform.eulerAngles.y, yAngle, rotationDelta), 0);
            }
        }
        else
        {
            m_MovementComponent.SetState(MovementComponent.MovementFlag.ControlRotation);
        }
    }

    public void Load(MapLoader.SaveLoadData data)
    {
        ShipDockingHandlerData shipDockingHandlerData = data as ShipDockingHandlerData;
        thresholdForDocking = shipDockingHandlerData.thresholdForDocking;
        targetPointToDock = (Vector3)shipDockingHandlerData.targetPointToDock;
        enabled = shipDockingHandlerData.enabled;
    }

    public void PostLoad(MapLoader.SaveLoadData data)
    {
        //throw new System.NotImplementedException();
    }

    public MapLoader.SaveLoadData Save()
    {
        ShipDockingHandlerData shipDockingHandlerData = new ShipDockingHandlerData()
        {
            enabled = this.enabled,
            targetPointToDock = (CommonStructures.SerializableVector3)targetPointToDock,
            thresholdForDocking = this.thresholdForDocking,
        };

        return shipDockingHandlerData;
    }
}
