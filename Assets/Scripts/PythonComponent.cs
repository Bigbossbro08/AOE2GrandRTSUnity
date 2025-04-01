using System.Collections.Generic;
using System.Diagnostics;
using Newtonsoft.Json;
using System.IO;
using UnityEngine;

public class PythonComponent : MonoBehaviour
{
    public List<UnitData> unitDataList = new List<UnitData>();

    public void RunPythonScript(string arguments)
    {
        UnityEngine.Debug.Log(arguments);
        string workingDirectory = @"E:\repos\aoe2_campaign\";
        string venvPython = Path.Combine(workingDirectory, "env/Scripts/python.exe");
        string scriptPath = Path.Combine(workingDirectory, "map_chunk_gen.py");
        string unit_input = Path.Combine(workingDirectory, "unit_input.json");
        //string arguments = "0 0 120";

        if (File.Exists(unit_input))
        {
            string unitDataListJson = JsonConvert.SerializeObject(unitDataList, Formatting.Indented); // JsonUtility.ToJson(unitDataList, true);
            File.WriteAllText(unit_input, unitDataListJson);
        }

        // Check if paths exist
        if (!File.Exists(venvPython))
        {
            UnityEngine.Debug.LogError("Python executable not found at: " + venvPython);
            return;
        }
        if (!File.Exists(scriptPath))
        {
            UnityEngine.Debug.LogError("Python script not found at: " + scriptPath);
            return;
        }

        ProcessStartInfo psi = new ProcessStartInfo
        {
            FileName = venvPython,
            Arguments = $"\"{scriptPath}\" \"{arguments}\"",  // Corrected argument format
            RedirectStandardOutput = true,
            RedirectStandardError = true,  // Capture errors
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = workingDirectory,
            StandardOutputEncoding = System.Text.Encoding.UTF8,
            StandardErrorEncoding = System.Text.Encoding.UTF8
        };

        Process process = new Process { StartInfo = psi };
        process.Start();

        string output = process.StandardOutput.ReadToEnd();
        string errors = process.StandardError.ReadToEnd();
        process.WaitForExit();

        if (!string.IsNullOrEmpty(errors))
        {
            UnityEngine.Debug.LogError("Python Errors: " + errors);
        }

        UnityEngine.Debug.Log("Python Output: " + output);
    }
}
