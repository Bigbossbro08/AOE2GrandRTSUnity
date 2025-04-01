using System.IO;
using UnityEngine;

public class SaveComponent : MonoBehaviour
{
    class SaveObject
    {
        public Vector2 playerPosition;
    }

    public GameObject player;
    public GameObject camera;
    string saveFilePath = Application.dataPath + "/save.json";

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        string loadText = File.ReadAllText(saveFilePath);
        SaveObject saveObject = JsonUtility.FromJson<SaveObject>(loadText);
        if (player && saveObject != null)
        {
            Vector3 playerPosition = new Vector3(saveObject.playerPosition.x, 0, saveObject.playerPosition.y);
            player.transform.position = playerPosition;
            camera.transform.position = playerPosition;
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnApplicationQuit()
    {
        return;
        SaveObject saveObject = new SaveObject
        {
            playerPosition = new Vector2(player.transform.position.x, player.transform.position.z)
        };
        string saveObjectJson = JsonUtility.ToJson(saveObject);
        File.WriteAllText(saveFilePath, saveObjectJson);
    }
}
