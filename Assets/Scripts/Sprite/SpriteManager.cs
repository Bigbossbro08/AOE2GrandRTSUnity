using Assimp.Unmanaged;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;
using static UnitManager.UnitJsonData;

public class SpriteManager : MonoBehaviour
{
    public static SpriteManager Instance;

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

    public class SpriteInstanceData {
        public List<Matrix4x4> matrices = new List<Matrix4x4>(1024);
        public MaterialPropertyBlock props = new MaterialPropertyBlock();
    }

    Dictionary<string, SpriteInstanceData> m_SpriteInstances = new Dictionary<string, SpriteInstanceData>();
    Mesh quadMesh;
    [SerializeField] Material material = null;

    ObjectPool<UnitVisual> unitVisualPool;
    [SerializeField] private UnitVisual unitVisualPrefab;
    public Dictionary<ulong, UnitVisual> activeVisuals = new Dictionary<ulong, UnitVisual>();

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        unitVisualPool = new ObjectPool<UnitVisual>(_CreateUnitVisual, _GetUnitVisual, _ReleaseUnitVisual, _DestroyUnitVisual);
        quadMesh = new Mesh();
        quadMesh.vertices = new Vector3[]
        {
            new Vector3(-0.5f, 0, -0.5f),
            new Vector3(0.5f, 0, -0.5f),
            new Vector3(0.5f, 0, 0.5f),
            new Vector3(-0.5f, 0, 0.5f),
        };
        quadMesh.uv = new Vector2[]
        {
            new Vector2(0, 0),
            new Vector2(1, 0),
            new Vector2(0, 1),
            new Vector2(1, 1),
        };
        quadMesh.triangles = new int[] { 0, 2, 1, 2, 3, 1 };
    }

    private UnitVisual _CreateUnitVisual()
    {
        UnitVisual unitVisual = Instantiate(unitVisualPrefab); 
        return unitVisual;
    }

    private void _GetUnitVisual(UnitVisual visual)
    {
        visual.gameObject.SetActive(true);
    }

    private void _ReleaseUnitVisual(UnitVisual visual)
    {
        visual.gameObject.SetActive(false);
    }

    private void _DestroyUnitVisual(UnitVisual visual)
    {
        Destroy(visual.gameObject);
    }

    public void RegisterSprite(string name, Matrix4x4 matrixData, MaterialPropertyBlock prop)
    {
        if (!m_SpriteInstances.ContainsKey(name))
        {
            m_SpriteInstances.Add(name, new SpriteInstanceData());
        }
        m_SpriteInstances[name].matrices.Add(matrixData);
        m_SpriteInstances[name].props = prop;
    }

    // Update is called once per frame
    public void Render()
    {
        foreach (var sprite in m_SpriteInstances.Values)
        {
            int instanceCount = sprite.matrices.Count;
            var matrices = sprite.matrices.ToArray();
            int batchSize = 1023;
            var props = sprite.props;
            for (int i = 0; i < instanceCount; i += batchSize)
            {
                int count = Mathf.Min(batchSize, instanceCount - i);
                Graphics.DrawMeshInstanced(quadMesh, 0, material, matrices, count, props);
            }
        }
    }

    private void Update()
    {
        UpdateUnitVisuals();
    }

    void UpdateUnitVisuals()
    {
        float SafeDivide(float a, float b)
        {
            if (Mathf.Approximately(a, b)) // Unity’s epsilon-based comparison
                return 1f;

            return a / b;
        }

        Camera camera = Camera.main;
        var unitDict = UnitManager.Instance.GetAllUnits();
        foreach (var unit in unitDict)
        {
            if (unit.Value.GetType() != typeof(MovableUnit))
                continue;

            bool visible = Utilities.VisibilityUtility.IsPointVisible(camera, unit.Value.transform.position);
            ulong id = unit.Key;
            if (visible && !activeVisuals.ContainsKey(id))
            {
                var visual = unitVisualPool.Get();
                visual.AttachToCore(unit.Value);
                activeVisuals[id] = visual;
            }
            else if (!visible && activeVisuals.ContainsKey(id))
            {
                var visual = activeVisuals[id];
                visual.DetachFromCore();
                unitVisualPool.Release(visual);
                activeVisuals.Remove(id);
            }

            if (visible && activeVisuals.ContainsKey(id))
            {
                var visual = activeVisuals[id];
                MovableUnit controllableUnit = visual.core as MovableUnit;
                bool isSelected = SelectionPanel.Instance.GetSelectedUnits().Contains(unit.Value);
                if (!StatComponent.IsUnitAliveOrValid(controllableUnit))
                {
                    isSelected = false;
                }

                visual.selectionCircle.enabled = isSelected;
                if (isSelected)
                {
                    visual.selectionCircle.transform.localScale = Vector3.one * controllableUnit.movementComponent.radius * 2;
                    PlayerData playerData = UnitManager.Instance.GetPlayerData(controllableUnit.playerId);
                    visual.selectionCircle.material.color = playerData.color;
                }
                if (visual.hpBarCanvas)
                {
                    bool activeSelf = visual.hpBarCanvas.gameObject.activeSelf;
                    if (activeSelf)
                    {
                        if (visual.hpBarCanvas) {
                            float health = controllableUnit.statComponent.GetHealth();
                            float maxHealth = controllableUnit.statComponent.GetMaxHealth();
                            float value = SafeDivide(health, maxHealth);
                            visual.hpBarSlider.value = value;
                        }
                    }
                    if (activeSelf != isSelected)
                    {
                        visual.hpBarCanvas.gameObject.SetActive(isSelected);
                    }
                }
            }
        }
    }
}
