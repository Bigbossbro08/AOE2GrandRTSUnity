using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ObjectivePanel : MonoBehaviour
{
    public static ObjectivePanel Instance;

    // TODO: Temporary. Make some of universal mission loader or sort
    public Material missionDebugMaterial;

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

    [SerializeField] private Transform Context;
    [SerializeField] private GameObject TextMeshPrefab;

    int counter = 0;

    private void Start()
    {
        counter = 0;
    }

    public TextMeshProUGUI AddObjectiveText()
    {
        counter++;
        GameObject go = new GameObject($"Objective Text {counter}", typeof(RectTransform));
        //GameObject go = Instantiate(TextMeshPrefab);
        //go.SetActive(true);
        var textMeshGui = go.AddComponent<TextMeshProUGUI>();
        textMeshGui.fontSize = 11;
        textMeshGui.rectTransform.sizeDelta = new Vector2(180, 30);
        go.transform.SetParent( Context.transform, false );
        
        return textMeshGui; 
    }
}
