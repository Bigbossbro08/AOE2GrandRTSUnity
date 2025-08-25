using System.IO;
using UnityEngine;

public class CustomMapLoader : MonoBehaviour
{
    public static CustomMapLoader Instance;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
        }
        else
        {
            Instance = this;
        }
    }

    public void LoadMap(string path)
    {
        string mainpath = Path.Combine(MapLoader.GetDataPath(), path);
    }
}
