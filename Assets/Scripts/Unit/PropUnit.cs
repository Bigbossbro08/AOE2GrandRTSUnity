using UnityEngine;

public class PropUnit : Unit, MapLoader.IMapSaveLoad
{
    public string spriteName = "corpse";
    [SerializeField] DeterministicVisualUpdater visualUpdater;

    private void OnEnable()
    {
        SetVisual(spriteName);
        Initialize();
        //DeterministicUpdateManager.Instance.Register(this);
    }

    private void OnDisable()
    {
        //DeterministicUpdateManager.Instance.Unregister(this);
    }

    private void SetVisual(string sprite)
    {
        visualUpdater.SetSpriteName(sprite, true);
        visualUpdater.PlayOrResume(false);
        visualUpdater.playerId = playerId;
        visualUpdater.RefreshVisuals();
    }
}
