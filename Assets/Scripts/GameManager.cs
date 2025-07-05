using Unity.VisualScripting;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    private void Awake()
    {
        // If there is an instance, and it's not me, delete myself.

        if (Instance != null && Instance != this)
        {
            Destroy(this);
        }
        else
        {
            Instance = this;
        }
    }

    public CameraMovement CameraHandler;

    public int loadAmount = 0;
    int targetAmountToLoad = 3;

    public bool IsLoaded()
    {
        return loadAmount >= targetAmountToLoad;
    }

    public void IncrementLoadCount()
    {
        loadAmount++;

        if (loadAmount == 1)
        {
            GridGeneration.Instance.enabled = true;
        }
        if (loadAmount == 2)
        {
            IncrementLoadCount();
            //MapLoader.Instance.enabled = true;
        }
        if (IsLoaded())
        {
            DeterministicUpdateManager.Instance.enabled = true;
        }
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        IncrementLoadCount();
    }
}
