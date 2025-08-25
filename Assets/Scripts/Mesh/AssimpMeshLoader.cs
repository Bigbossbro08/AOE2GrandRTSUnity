using Assimp;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using static CustomSpriteLoader;

public class AssimpMeshLoader : MonoBehaviour
{
    public static AssimpMeshLoader Instance;

    public class MeshReturnData
    {
        public UnityEngine.Mesh mesh = null;
    }

    Dictionary<string, MeshReturnData> meshDictionary = new Dictionary<string, MeshReturnData>();
    //AssimpContext importer = null;

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
        //importer = new AssimpContext();
    }

    private void OnDestroy()
    {
        //if (importer != null)
        //{
        //    importer.Dispose();
        //}
    }

    public static UnityEngine.Mesh ScaleMesh(UnityEngine.Mesh mesh, float scale)
    {
        UnityEngine.Mesh ret = new UnityEngine.Mesh();

        Vector3[] vertices = mesh.vertices;
        for (int i = 0; i < vertices.Length; i++)
            vertices[i] *= scale;

        ret.vertices = vertices;
        ret.triangles = mesh.triangles;

        ret.RecalculateNormals();
        ret.RecalculateBounds();
        return ret;
    }

    public MeshReturnData LoadMeshFromAssimp(string fileName)
    {
        if (meshDictionary.ContainsKey(fileName)) { 
            return meshDictionary[fileName]; 
        }

        string path = Path.Combine(MapLoader.GetDataPath());
        string filePath = Path.Combine(path, $"{fileName}.obj");

        NativeLogger.Log($"Trying to load from: {filePath}");
        AssimpContext importer = new AssimpContext();
        Scene scene = importer.ImportFile(filePath, PostProcessSteps.Triangulate | PostProcessSteps.JoinIdenticalVertices);

        if (scene == null || scene.MeshCount == 0)
        {
            NativeLogger.Error($"Failed to load mesh from: {filePath} from name {fileName}");
            return null;
        }

        Assimp.Mesh assimpMesh = scene.Meshes[0]; // First mesh only
        
        Vector3[] vertices = new Vector3[assimpMesh.VertexCount];
        for (int i = 0; i < assimpMesh.VertexCount; i++)
        {
            var v = assimpMesh.Vertices[i];
            vertices[i] = new Vector3(v.X, v.Y, v.Z);
        }

        int[] triangles = new int[assimpMesh.FaceCount * 3];
        for (int i = 0; i < assimpMesh.FaceCount; i++)
        {
            Face face = assimpMesh.Faces[i];
            if (face.IndexCount == 3)
            {
                triangles[i * 3 + 0] = face.Indices[0];
                triangles[i * 3 + 1] = face.Indices[1];
                triangles[i * 3 + 2] = face.Indices[2];
            }
        }

        UnityEngine.Mesh mesh = new UnityEngine.Mesh();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        MeshReturnData meshReturnData = new MeshReturnData
        {
            mesh = mesh,
        };

        meshDictionary.Add(fileName, meshReturnData);

        return meshReturnData;
    }
}
