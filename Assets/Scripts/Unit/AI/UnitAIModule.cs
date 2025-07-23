using UnityEngine;

public class UnitAIModule : MonoBehaviour, IDeterministicUpdate, MapLoader.IMapSaveLoad
{
    public enum AIModule
    {
        None,
        BasicAttackAIModule,
        BasicMovementAIModule,
        TargetFollowingMovementAIModule,
        AttackMoveAIModule,
        BoardshipUnitAIModule,
        DockToShoreUnitAIModule
    }

    public void DeterministicUpdate(float deltaTime, ulong tickID)
    {
        //throw new System.NotImplementedException();
    }

    public void Load(MapLoader.SaveLoadData data)
    {
        //throw new System.NotImplementedException();
    }

    public void PostLoad(MapLoader.SaveLoadData data)
    {
        //throw new System.NotImplementedException();
    }

    public MapLoader.SaveLoadData Save()
    {
        //return new MapLoader.SaveLoadData();
        throw new System.NotImplementedException();
    }
}
