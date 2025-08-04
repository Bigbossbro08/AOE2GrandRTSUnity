using Mono.Cecil.Cil;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using static PlasticPipe.PlasticProtocol.Messages.Serialization.ItemHandlerMessagesSerialization;

public class MissionCollisionTriggerCheckerEditorWindow : EditorWindow
{
    private const string HistoryKey = "SpawnShapeCommandHistory";
    private Vector2 scrollPos;
    private static List<string> history = new List<string>();

    // User Options
    bool includePosition = true;
    bool includeRotation = true;
    bool includeYAngle = false;
    bool snapToGround = false;
    bool generateMissionCollisions = false;
    bool includeUnitAddRelease = false;

    [MenuItem("Tools/Generate Mission SpawnBox Command")]
    static void ShowWindow()
    {
        LoadHistory();
        GetWindow<MissionCollisionTriggerCheckerEditorWindow>("Mission SpawnBox Generator");
    }

    void OnGUI()
    {
        if (Selection.gameObjects.Length == 0)
        {
            EditorGUILayout.HelpBox("Select GameObjects", MessageType.Info);
        }
        else
        {
            if (GUILayout.Button("Use raycast to ground"))
            {
                foreach (var o in Selection.gameObjects)
                {
                    Ray ray = new Ray(o.transform.position, Vector3.down);
                    if (Physics.Raycast(ray, out RaycastHit hit, 1000, 1))
                    {
                        o.transform.position = hit.point;
                    }
                }
            }
            //for (int i = 0; i < Selection.gameObjects.Length; i++)
            //{
            //    GameObject go = Selection.gameObjects[i];
            //    HandleGameObject(go, i);
            //}

            EditorGUILayout.LabelField("Options", EditorStyles.boldLabel);
            includePosition = EditorGUILayout.Toggle("Include Position", includePosition);
            includeRotation = EditorGUILayout.Toggle("Include Rotation", includeRotation);
            includeYAngle = EditorGUILayout.Toggle("Include Y Angle", includeYAngle);
            snapToGround = EditorGUILayout.Toggle("Snap to Ground", snapToGround);
            includeUnitAddRelease = EditorGUILayout.Toggle("Make unit spawn friendly codes", includeUnitAddRelease);
            EditorGUILayout.Space(10);
            if (GUILayout.Button("Generate Copy Commands"))
            {
                StringBuilder code = new();
                if (includeUnitAddRelease)
                {
                    code.AppendLine($@"List<MovableUnit> copyCommandUnits = new();");
                }
                for (int i = 0; i < Selection.gameObjects.Length; i++)
                {
                    GameObject go = Selection.gameObjects[i];
                    string finalCode = HandleGameObject(go, i);
                    code.AppendLine(finalCode);
                }
                StoreCommand(code.ToString(), "Merged Codes");
            }
            if (GUILayout.Button("New Position Rotation Copy Command"))
            {
                StringBuilder code = new();
                code.AppendLine($@"List<(Vector3, float)> copyPosRot = new()");
                code.Append(" {");
                for (int i = 0; i < Selection.gameObjects.Length; i++)
                {
                    GameObject go = Selection.gameObjects[i];
                    Transform t = go.transform;
                    string line = $"new (new Vector3({V3(t.position)}), {t.eulerAngles.y}f),";
                    code.AppendLine(line);
                }
                code.AppendLine("};");
                StoreCommand(code.ToString(), "Merged Codes");
            }
            //for (int i = 0; i < Selection.gameObjects.Length; i++)
            //{
            //    GameObject go = Selection.gameObjects[i];
            //    EditorGUILayout.BeginVertical("box");
            //    EditorGUILayout.LabelField(go.name, EditorStyles.boldLabel);
            //    if (GUILayout.Button("Generate Full Command")) {
            //        string finalCode = HandleGameObject(go, i);
            //        StoreCommand(finalCode, go.name);
            //    }
            //    EditorGUILayout.EndVertical();
            //}
        }

        EditorGUILayout.Space(10);
        GUILayout.Label("Command History", EditorStyles.boldLabel);

        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
        for (int i = 0; i < history.Count; i++)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.SelectableLabel(history[i], GUILayout.Height(40));
            if (GUILayout.Button("Copy", GUILayout.Width(60)))
            {
                EditorGUIUtility.systemCopyBuffer = history[i];
                Debug.Log("Copied command to clipboard:\n" + history[i]);
            }
            EditorGUILayout.EndHorizontal();
        }
        EditorGUILayout.EndScrollView();

        if (history.Count > 0 && GUILayout.Button("Clear History"))
        {
            if (EditorUtility.DisplayDialog("Clear Command History?", "Are you sure?", "Yes", "Cancel"))
            {
                history.Clear();
                SaveHistory();
            }
        }
    }

    string HandleGameObject(GameObject go, int id)
    {
        string finalCode = "";
        //if (GUILayout.Button("Generate Full Command"))
        {
            StringBuilder code = new();

            Transform t = go.transform;

            // Optional: snap to ground
            if (Physics.Raycast(t.position + Vector3.up * 1000, Vector3.down, out RaycastHit hit))
            {
                if (snapToGround)
                {
                    t.position = hit.point;
                    Debug.Log($"{go.name} snapped to ground at {hit.point}");
                }
            }

            if (generateMissionCollisions)
            {
                // Generate spawn command
                if (go.TryGetComponent<BoxCollider>(out var box))
                {
                    code.AppendLine($"SpawnBox(new Vector3({V3(t.position)}),");
                    code.AppendLine($"         Quaternion.Euler({V3(t.eulerAngles)}),");
                    code.AppendLine($"         new Vector3({V3(t.localScale)}),");
                    code.AppendLine($"         new Vector3({V3(box.size)}),");
                    code.AppendLine($"         new Vector3({V3(box.center)}));");
                }
                else if (go.TryGetComponent<SphereCollider>(out var sphere))
                {
                    code.AppendLine($"SpawnSphere(new Vector3({V3(t.position)}),");
                    code.AppendLine($"            Quaternion.Euler({V3(t.eulerAngles)}),");
                    code.AppendLine($"            new Vector3({V3(t.localScale)}),");
                    code.AppendLine($"            {sphere.radius}f,");
                    code.AppendLine($"            new Vector3({V3(sphere.center)}));");
                }
                else if (go.TryGetComponent<CapsuleCollider>(out var capsule))
                {
                    code.AppendLine($"SpawnCapsule(new Vector3({V3(t.position)}),");
                    code.AppendLine($"              Quaternion.Euler({V3(t.eulerAngles)}),");
                    code.AppendLine($"              new Vector3({V3(t.localScale)}),");
                    code.AppendLine($"              {capsule.radius}f,");
                    code.AppendLine($"              {capsule.height}f,");
                    code.AppendLine($"              {capsule.direction}, // 0=X, 1=Y, 2=Z");
                    code.AppendLine($"              new Vector3({V3(capsule.center)}));");
                }
            }

            // Optional extra info
            if (includePosition)
                code.AppendLine($"Vector3 newCopiedPosition_{id} = new Vector3({V3(t.position)});");

            if (includeRotation)
                code.AppendLine($"Vector3 newCopiedEulerAngles_{id} = new Vector3({V3(t.eulerAngles)});");

            if (includeYAngle)
                code.AppendLine($"float newCopiedYAngle_{id} = {t.eulerAngles.y}f;");

            if (includeUnitAddRelease)
            {
                //MovableUnit movableUnit_{id} = UnitManager.Instance.movableUnitPool.Get();
                //if (movableUnit_{id}) {
                //    movableUnit_{id}.transform.position = newCopiedPosition_{ id};
                //    movableUnit_{id}.transform.eulerAngles = new Vector3(0, newCopiedEulerAngles_{ id }.y, 0);
                //
                //    CleanUp += () =>
                //    {
                //        UnitManager.Instance.movableUnitPool.Release(movableUnit_{id});
                //    };
                //}
                code.AppendLine($@"
                MovableUnit movableUnit_{id} = UnitManager.Instance.movableUnitPool.Get();
                if (movableUnit_{id} != null)
                {{
                    movableUnit_{id}.transform.position = newCopiedPosition_{id};
                    movableUnit_{id}.transform.eulerAngles = new Vector3(0, newCopiedEulerAngles_{id}.y, 0);
                
                    CleanUp += () =>
                    {{
                        UnitManager.Instance.movableUnitPool.Release(movableUnit_{id});
                    }};
                    copyCommandUnits.Add(movableUnit_{id});
                }}");
            }

            code.AppendLine();
            // Done
            finalCode = code.ToString();
            
        }
        return finalCode;
    }

    //static void HandleGameObject(GameObject go, int id)
    //{
    //    if (go.GetComponent<BoxCollider>())
    //    {
    //        if (GUILayout.Button("Generate SpawnBox Command"))
    //            GenerateBoxCommand(go);
    //    }
    //
    //    if (go.GetComponent<SphereCollider>())
    //    {
    //        if (GUILayout.Button("Generate SpawnSphere Command"))
    //            GenerateSphereCommand(go);
    //    }
    //
    //    if (go.GetComponent<CapsuleCollider>())
    //    {
    //        if (GUILayout.Button("Generate SpawnCylinder Command"))
    //            GenerateCapsuleCommand(go);
    //    }
    //
    //    if (GUILayout.Button("Copy Position and Rotation"))
    //    {
    //        Vector3 position = go.transform.position;
    //        Vector3 eulerAngles = go.transform.eulerAngles;
    //        string code = $"Vector3 newCopiedPosition_{id} = new Vector3({V3(position)});\n" +
    //            $"Vector3 newCopiedEulerAngles_{id} = new Vector3({V3(eulerAngles)});";
    //        StoreCommand(code, "CopyPosition");
    //    }
    //
    //    if (GUILayout.Button("Copy Position"))
    //    {
    //        Vector3 position = go.transform.position;
    //        string code = $"Vector3 newCopiedPosition_{id} = new Vector3({V3(position)});";
    //        StoreCommand(code, "CopyPosition");
    //    }
    //
    //    if (GUILayout.Button("Copy Euler Angles"))
    //    {
    //        Vector3 eulerAngles = go.transform.eulerAngles;
    //        string code = $"Vector3 newCopiedEulerAngles_{id} = new Vector3({V3(eulerAngles)});";
    //        StoreCommand(code, "CopyRotation");
    //    }
    //
    //    if (GUILayout.Button("Copy YAngle"))
    //    {
    //        float yAngle = go.transform.eulerAngles.y;
    //        string code = $"float newCopiedyAngle_{id} = {yAngle}f;";
    //        StoreCommand(code, "SpawnBox");
    //    }
    //
    //    if (GUILayout.Button("Make gameobject hit ground"))
    //    {
    //        if (Physics.Raycast(go.transform.position + Vector3.up * 1000, Vector3.up * -1000, out RaycastHit hit))
    //        {
    //            go.transform.position = hit.point;
    //        }
    //    }
    //}

    static void GenerateBoxCommand(GameObject go)
    {
        Transform t = go.transform;
        BoxCollider c = go.GetComponent<BoxCollider>();

        string code = $"SpawnBox(new Vector3({V3(t.position)}),\n" +
                      $"         Quaternion.Euler({V3(t.eulerAngles)}),\n" +
                      $"         new Vector3({V3(t.localScale)}),\n" +
                      $"         new Vector3({V3(c.size)}),\n" +
                      $"         new Vector3({V3(c.center)}));";

        StoreCommand(code, "SpawnBox");
    }

    static void GenerateSphereCommand(GameObject go)
    {
        Transform t = go.transform;
        SphereCollider c = go.GetComponent<SphereCollider>();

        string code = $"SpawnSphere(new Vector3({V3(t.position)}),\n" +
                      $"            Quaternion.Euler({V3(t.eulerAngles)}),\n" +
                      $"            new Vector3({V3(t.localScale)}),\n" +
                      $"            {c.radius}f,\n" +
                      $"            new Vector3({V3(c.center)}));";

        StoreCommand(code, "SpawnSphere");
    }

    static void GenerateCapsuleCommand(GameObject go)
    {
        Transform t = go.transform;
        CapsuleCollider c = go.GetComponent<CapsuleCollider>();

        string code = $"SpawnCylinder(new Vector3({V3(t.position)}),\n" +
                      $"              Quaternion.Euler({V3(t.eulerAngles)}),\n" +
                      $"              new Vector3({V3(t.localScale)}),\n" +
                      $"              {c.radius}f,\n" +
                      $"              {c.height}f,\n" +
                      $"              {c.direction}, // 0=X, 1=Y, 2=Z\n" +
                      $"              new Vector3({V3(c.center)}));";

        StoreCommand(code, "SpawnCylinder");
    }

    static void StoreCommand(string command, string label)
    {
        //Debug.Log($"<b>{label} Command:</b>\n" + code);
        //EditorGUIUtility.systemCopyBuffer = code;
        //Debug.Log($"{label} command copied to clipboard.");

        Debug.Log($"<b>{label} Command:</b>\n" + command);
        EditorGUIUtility.systemCopyBuffer = command;

        history.Insert(0, command);
        while (history.Count > 50) history.RemoveAt(history.Count - 1); // Keep history size manageable
        SaveHistory();
    }

    static void SaveHistory()
    {
        string json = JsonUtility.ToJson(new CommandListWrapper { commands = history });
        EditorPrefs.SetString(HistoryKey, json);
    }

    static void LoadHistory()
    {
        if (EditorPrefs.HasKey(HistoryKey))
        {
            string json = EditorPrefs.GetString(HistoryKey);
            history = JsonUtility.FromJson<CommandListWrapper>(json)?.commands ?? new List<string>();
        }
    }

    [System.Serializable]
    private class CommandListWrapper
    {
        public List<string> commands;
    }

    static string V3(Vector3 v)
    {
        return $"{v.x}f, {v.y}f, {v.z}f";
    }
}
