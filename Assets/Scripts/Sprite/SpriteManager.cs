using System.Collections.Generic;
using UnityEngine;
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
    Mesh quadMesh = new Mesh();
    [SerializeField] Material material = null;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
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
}
