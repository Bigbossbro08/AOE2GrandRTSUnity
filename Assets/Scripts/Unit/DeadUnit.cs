using UnityEngine;

public class DeadUnit : Unit, MapLoader.IMapSaveLoad, IDeterministicUpdate
{
    private float currentTime = 0.0f;
    public float timeToDestroy = 300.0f;

    public string spriteName = "corpse";
    [SerializeField] DeterministicVisualUpdater visualUpdater;

    private void OnEnable()
    {
        SetVisual(spriteName);
        Initialize();
        DeterministicUpdateManager.Instance.Register(this);
    }

    private void OnDisable()
    {
        DeterministicUpdateManager.Instance.Unregister(this);
    }

    private void SetVisual(string sprite)
    {
        visualUpdater.SetSpriteName(sprite, true);
        visualUpdater.PlayOrResume(false);
        visualUpdater.playerId = playerId;
        visualUpdater.RefreshVisuals();
    }

    public void DeterministicUpdate(float deltaTime, ulong tickID)
    {
        if (currentTime > timeToDestroy)
        {
            UnitManager.Instance.ReleaseDeadUnitFromPool(this);;
        }
        currentTime += deltaTime;
    }
}
