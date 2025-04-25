using UnityEngine;

public class ActionComponent : MonoBehaviour, IDeterministicUpdate, MapLoader.IMapSaveLoad
{
    public void DeterministicUpdate(float deltaTime, ulong tickID)
    {
        if (currentTime < duration)
        {
            currentTime += deltaTime;
        } else
        {
            OnEndAction();
            StopAction();
        }
    }

    public void OnEndAction()
    {
        UnitEventHandler.Instance.CallEventByID(UnitEventHandler.EventID.OnActionEnd, endActionType, movableUnit.id);
    }

    public float GetCurrentTime() { return currentTime; }

    public void StopAction()
    {
        if (!enabled) return;
        
        currentTime = 0;
        if (movableUnit)
        {
            DeterministicVisualUpdater deterministicVisualUpdater = movableUnit.GetDeterministicVisualUpdater();
            string sprite = movableUnit.standSprite;
            if (movableUnit.movementComponent.movementState == MovementComponent.State.Moving)
                sprite = movableUnit.walkSprite;
            if (endActionType == "DeathEndAction")
            {
                Debug.Log($"On post anim is: {sprite}");
            }

            deterministicVisualUpdater.SetSpriteName(sprite, true);
            deterministicVisualUpdater.PlayOrResume(false);
            movableUnit.DecrementActionBlock();
        }
        enabled = false;
    }

    public void Load(MapLoader.SaveLoadData data)
    {
        throw new System.NotImplementedException();
    }

    public void PostLoad(MapLoader.SaveLoadData data)
    {
        throw new System.NotImplementedException();
    }

    public MapLoader.SaveLoadData Save()
    {
        throw new System.NotImplementedException();
    }

    private void OnEnable()
    {
        DeterministicUpdateManager.Instance.Register(this);
    }

    private void OnDisable()
    {
        DeterministicUpdateManager.Instance.Unregister(this);
    }

    [SerializeField]
    MovableUnit movableUnit;

    public string spriteName = "attacking";
    float currentTime = 0.0f;
    float duration = 1.0f;
    public string endActionType = "";

    private void Awake()
    {
        enabled = false;
    }

    public void SetActionSprite(string spriteName, string endActionType = "")
    {
        this.spriteName = spriteName;
        this.endActionType = endActionType;
    }

    public bool IsPlayingAction()
    {
        return enabled;
    }

    public void StartAction()
    {
        if (enabled) { return; }
        OpenageSpriteLoader.ReturnMinimalisticData minimalisticData = OpenageSpriteLoader.Instance.RequestMinimalSpriteData(spriteName);
        if (minimalisticData != null)
        {
            duration = minimalisticData.duration;
            enabled = true;
            if (movableUnit)
            {
                movableUnit.GetDeterministicVisualUpdater().SetSpriteName(spriteName, true);
                movableUnit.GetDeterministicVisualUpdater().PlayOrResume(false);
                currentTime = 0;
                movableUnit.IncrementActionBlock();
            }
        }
    }
}
