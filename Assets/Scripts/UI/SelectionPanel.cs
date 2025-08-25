using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SelectionPanel : MonoBehaviour
{
    public RectTransform panelRect;             // The visible panel area
    public GridLayoutGroup gridLayoutGroup;     // GridLayoutGroup on content
    public Transform content;                   // Grid content
    private int activeButtonCount = 0;

    private List<CommandButton> selectionButtons = new List<CommandButton>(61);

    private List<Unit> selectedUnits = new List<Unit>();
    private List<MovableUnit> movableUnits = new List<MovableUnit>();

    [SerializeField] private Material circleMaterial;

    public static SelectionPanel Instance;

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

    private Mesh circleQuadMesh;

    public Mesh GetCircleQuadMesh()
    {
        Mesh quad = new Mesh();
        quad.vertices = new Vector3[]
        {
            new Vector3(-0.5f, 0, -0.5f),
            new Vector3(0.5f, 0, -0.5f),
            new Vector3(0.5f, 0, 0.5f),
            new Vector3(-0.5f, 0, 0.5f),
        };
        quad.uv = new Vector2[]
        {
            new Vector2(0, 0),
            new Vector2(1, 0),
            new Vector2(1, 1),
            new Vector2(0, 1),
        };
        quad.triangles = new int[] { 0, 1, 2, 0, 2, 3 };
        quad.RecalculateNormals();

        return quad;
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        InitializeSelectionButtons();
        UpdateGridLayout();
        circleQuadMesh = GetCircleQuadMesh();
    }

    public float circleSize = 2f;
    public float height = 0.01f;

    void OnPreRender()
    {
        if (circleMaterial == null || selectedUnits == null) return;
        if (selectedUnits.Count == 0) return;
        circleMaterial.SetPass(0); // Use the material for this draw call
        foreach (var unit in selectedUnits)
        {
            if (unit == null) continue;

            Matrix4x4 matrix = Matrix4x4.TRS(
                unit.transform.position + Vector3.up * height, // Slightly above ground
                Quaternion.Euler(0f, 0f, 0f),      // Face upward
                Vector3.one * circleSize
            );

            Graphics.DrawMeshNow(circleQuadMesh, matrix);
        }
    }

    void InitializeSelectionButtons()
    {
        int counter = 0;
        int activeButtonCount = 0;
        foreach (Transform child in content)
        {
            child.gameObject.name = $"Selection Button: {counter}";
            CommandButton button = child.GetComponent<CommandButton>();
            selectionButtons.Add(button);
            if (button.gameObject.activeSelf)
            {
                activeButtonCount++;
            }
            counter++;
        }
        this.activeButtonCount = activeButtonCount;
    }

    void DeactivateButton(CommandButton button, bool checkSelectionCount = true)
    {
        button.gameObject.SetActive(false);
        button.onClick.RemoveAllListeners();

        if (checkSelectionCount)
        {
            UpdateGridLayout();
        }
    }

    void ClearSelectionButtons()
    {
        selectedUnits.Clear();
        for (int i = 0; i < selectionButtons.Count; i++)
        {
            DeactivateButton(selectionButtons[i], false);
        }
        UpdateGridLayout();
    }

    public List<Unit> GetSelectedUnits()
    {
        return selectedUnits;
    }

    public void SetupUnitSelection(List<Unit> units)
    {
        ClearSelectionButtons();

        for (int i = 0; i < units.Count && i < 60; i++)
        {
            Unit unit = units[i];
            
            // TODO: make unit to have sprite
            if (unit.GetType() == typeof(MovableUnit))
            {
                MovableUnit movableUnit = (MovableUnit)unit;
                CustomSpriteLoader.IconReturnData iconReturnData = movableUnit.GetSpriteIcon();
                var btn = selectionButtons[i];
                btn.gameObject.SetActive(true);
                if (iconReturnData != null)
                {
                    if (btn.image != null)
                    {
                        btn.image.sprite = iconReturnData?.sprite;
                    }
                }

                // Capture variables into local scope
                Unit capturedUnit = unit;
                CommandButton capturedBtn = btn;

                // Define the death callback
                Action<ulong> deathCallback = null;

                System.Action RemoveSelectedUnit = () =>
                {
                    if (movableUnit)
                    {
                        movableUnit.statComponent.OnDeathCallback -= deathCallback;
                    }
                    DeactivateButton(capturedBtn);
                    selectedUnits.Remove(capturedUnit);
                };

                // Assign deathCallback AFTER RemoveSelectedUnit is defined
                deathCallback = (id) => RemoveSelectedUnit?.Invoke();

                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() =>
                {
                    RemoveSelectedUnit?.Invoke();
                });

                if (movableUnit)
                    movableUnit.statComponent.OnDeathCallback += deathCallback;

                selectedUnits.Add(unit);
                NativeLogger.Log("Setting up id: " + btn.gameObject.name);
                
            }
        }
        UpdateGridLayout();
    }

    int GetActiveSelectedCount()
    {
        int activeButtonCount = 0;
        foreach (Transform child in content)
        {
            if (child.gameObject.activeSelf)
            {
                activeButtonCount++;
            }
        }
        return activeButtonCount;
    }

    void UpdateGridLayout()
    {
        int activeButtonCount = GetActiveSelectedCount();

        AdjustGridLayout(activeButtonCount);

        this.activeButtonCount = activeButtonCount;
    }

    void AdjustGridLayout(int unitCount)
    {
        int rows = 4;
        float panelWidth = panelRect.rect.width;
        float spacingX = gridLayoutGroup.spacing.x;

        // Compute how many columns are needed
        int columns = Mathf.CeilToInt(unitCount / (float)rows);

        // Compute max available width per column
        float totalSpacing = spacingX * (columns - 1);
        float availableWidth = panelWidth - totalSpacing;
        float maxCellWidth = availableWidth / columns;

        // Clamp the cell size
        float cellSize = Mathf.Clamp(maxCellWidth, 10f, 30f); // Shrink between 30 and 20 px

        // Apply size
        //gridLayoutGroup.constraint = GridLayoutGroup.Constraint.FixedRowCount;
        //gridLayoutGroup.constraintCount = rows;
        gridLayoutGroup.cellSize = new Vector2(cellSize, gridLayoutGroup.cellSize.y);
    }
}
