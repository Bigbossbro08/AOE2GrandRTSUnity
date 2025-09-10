using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LobbyManager : MonoBehaviour
{
    public class DropdownUniqueSelection
    {
        [SerializeField] private TMP_Dropdown dropdown;
        private LobbyManager manager;

        private int currentSelection = -1; // -1 = None

        public DropdownUniqueSelection(LobbyManager mgr, TMP_Dropdown dropdown)
        {
            this.manager = mgr;
            this.dropdown = dropdown;
            dropdown.onValueChanged.AddListener(OnOptionSelected);
        }

        ~DropdownUniqueSelection()
        {
            dropdown.onValueChanged.RemoveListener(OnOptionSelected);
        }

        public void RefreshOptions()
        {
            var ids = manager.GetAvailableIds(this);

            // Keep current selection if it’s valid
            if (currentSelection != -1 && !ids.Contains(currentSelection))
                ids.Add(currentSelection);

            ids.Sort();

            // Build options list with "None" at top
            dropdown.ClearOptions();
            List<string> options = new List<string> { "None" };
            foreach (int id in ids)
                options.Add(id.ToString());
            dropdown.AddOptions(options);

            // Restore current selection
            if (currentSelection == -1)
            {
                dropdown.value = 0; // "None"
            }
            else
            {
                int index = ids.IndexOf(currentSelection) + 1; // +1 for "None"
                dropdown.value = index;
            }

            dropdown.RefreshShownValue();
        }

        private void OnOptionSelected(int index)
        {
            if (index == 0)
            {
                // None selected
                currentSelection = -1;
                manager.OnIdSelected(-1, this);
            }
            else
            {
                int selectedId = int.Parse(dropdown.options[index].text);
                currentSelection = selectedId;
                manager.OnIdSelected(selectedId, this);
            }
        }
    }

    private List<DropdownUniqueSelection> uniqueDropdowns = new List<DropdownUniqueSelection>();

    [SerializeField] private List<TMPro.TMP_Dropdown> dropdowns = new List<TMP_Dropdown>();

    // Full set of IDs
    private List<int> allIds = new List<int> { 1, 2 };

    // Current assignments: dropdown → picked ID (or -1 if None)
    private Dictionary<DropdownUniqueSelection, int> assignedIds = new Dictionary<DropdownUniqueSelection, int>();

    [SerializeField] private Button button;
    //[SerializeField] private TMPro.TextMeshProUGUI textMeshPro;
    [SerializeField] private TMPro.TMP_InputField playerIdField;
    [SerializeField] private TMPro.TMP_InputField ipField;
    [SerializeField] private TMPro.TMP_InputField portField;

    private void Start()
    {
        //for (int i = 0; i < allIds.Count; i++)
        //{
        //    DropdownUniqueSelection dropdownUniqueSelection = new DropdownUniqueSelection(this, dropdowns[i]);
        //    assignedIds[uniqueDropdowns[i]] = -1; // none initially
        //}
        //
        //RefreshAll();
    }

    public List<int> GetAvailableIds(DropdownUniqueSelection requestingDropdown)
    {
        List<int> available = new List<int>(allIds);

        // Remove IDs already taken by other dropdowns
        foreach (var kvp in assignedIds)
        {
            if (kvp.Key != requestingDropdown && kvp.Value != -1)
                available.Remove(kvp.Value);
        }

        return available;
    }

    public void OnIdSelected(int newId, DropdownUniqueSelection sourceDropdown)
    {
        assignedIds[sourceDropdown] = newId;
        RefreshAll();
    }

    private void RefreshAll()
    {
        foreach (var dd in uniqueDropdowns)
            dd.RefreshOptions();
    }

    protected static IEnumerator LoadGameScene()
    {
        // Load your scene by name (must be in build settings!)
        SceneManager.LoadScene("Scenes/SampleScene");

        bool sceneLoaded = false;
        void OnSceneLoaded(Scene s, LoadSceneMode m) => sceneLoaded = true;
        SceneManager.sceneLoaded += OnSceneLoaded;

        while (!sceneLoaded)
        {
            // Wait until scene is loaded
            yield return null;
        }

        SceneManager.sceneLoaded -= OnSceneLoaded; // clean up

        while (!GameManager.Instance.IsLoaded())
        {
            yield return null;
        }
    }

    public void StartGame()
    {
        bool playerCheck = false;
        bool ipCheck = false;
        bool portCheck = false;
        if (ulong.TryParse(playerIdField.text, out ulong id))
        {
            UnitManager.localPlayerId = id;
            Debug.Log($"Add start game logic as player {id}");
            playerCheck = true;
        }

        {
            DeterministicUpdateManager.ENetMultiplayerInputManager.ip = ipField.text;
            ipCheck = true;
        }

        if (ushort.TryParse(portField.text, out ushort port))
        {
            DeterministicUpdateManager.ENetMultiplayerInputManager.port = port;
            portCheck = true;
        }

        if (playerCheck && ipCheck && portCheck)
        {
            StartCoroutine(LoadGameScene()); 
        }
        Debug.Log($"{playerIdField.text}");
    }
}
