using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;

//public class NativeLogger : MonoBehaviour
//{
//    // Start is called once before the first execution of Update after the MonoBehaviour is created
//    void Start()
//    {
//        
//    }
//
//    // Update is called once per frame
//    void Update()
//    {
//        
//    }
//}

public static class NativeLogger
{
    [DllImport("NativeLogger")]
    private static extern void init_logger(string filename);

    [DllImport("NativeLogger")]
    private static extern void log_info_ext(string message, string file, int line, string method);

    [DllImport("NativeLogger")]
    private static extern void log_error_ext(string message, string file, int line, string method);

    [DllImport("NativeLogger")]
    private static extern void log_debug_ext(string message, string file, int line, string method);

    [DllImport("NativeLogger")]
    private static extern void log_warn_ext(string message, string file, int line, string method);

    public static void Init(string filename = "Logs/unity_native_log.txt") => init_logger(filename);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void InitLogging()
    {
        //string path = Path.Combine(Application.dataPath, "Logs", "native_log.txt");
        //Directory.CreateDirectory(Path.GetDirectoryName(path));
        string path = "Logs/unity_native.log";
        Init(path);
        //Info("Logger initialized");
    }

    public static string GetCallStack()
    {
        // Skip 1 frame to remove GetCallStack itself
        var stackTrace = new StackTrace(1, true);  // true = get file info
        return stackTrace.ToString();
    }

    public static string GetFullCallStack()
    {
        var trace = new StackTrace(3, true); // skip GetFullCallStack itself
        var frames = trace.GetFrames();
        if (frames == null) return "";

        string stackString = "";
        //var sb = new StringBuilder();
        for (int i = frames.Length - 1; i >= 0; i--)
        {
            var frame = frames[i];
            var method = frame.GetMethod();
            var file = frame.GetFileName();
            var line = frame.GetFileLineNumber();
            //string currentFrameString = $"{method.DeclaringType?.FullName}.{method.Name}::({System.IO.Path.GetFileName(file)}:{line}) => ";
            string currentFrameString = $"[{System.IO.Path.GetFileName(file)}:{line}::{method.DeclaringType?.FullName}.{method.Name}] => ";
            stackString += currentFrameString;
        }

        return stackString;
    }

    public static void Info(string message,
        bool doFullTrace = false,
        [CallerFilePath] string file = "",
        [CallerLineNumber] int line = 0,
        [CallerMemberName] string method = "")
    {
        if (!doFullTrace)
        {
            log_warn_ext(message, System.IO.Path.GetFileName(file), line, method);
            return;
        }

        string trace = GetFullCallStack();
        string shortFileName = System.IO.Path.GetFileName(file);
        log_info_ext(message, shortFileName, line, $"[{method}] [Stack trace: {trace}{shortFileName}:{line}::[{method}]]");
    }

    public static void Error(string message,
        bool doFullTrace = false,
        [CallerFilePath] string file = "",
        [CallerLineNumber] int line = 0,
        [CallerMemberName] string method = "")
    {
        if (!doFullTrace)
        {
            log_error_ext(message, System.IO.Path.GetFileName(file), line, method);
            return;
        }
        string trace = GetFullCallStack();
        string shortFileName = System.IO.Path.GetFileName(file);
        log_error_ext(message, shortFileName, line, $"[{method}] [Stack trace: {trace}{shortFileName}:{line}::[{method}]]");
    }

    public static void Log(string message,
        bool doFullTrace = false,
        [CallerFilePath] string file = "",
        [CallerLineNumber] int line = 0,
        [CallerMemberName] string method = "")
    {
        if (!doFullTrace)
        {
            log_debug_ext(message, System.IO.Path.GetFileName(file), line, method);
            return;
        }
        string trace = GetFullCallStack();
        string shortFileName = System.IO.Path.GetFileName(file);
        log_debug_ext(message, shortFileName, line, $"[{method}] [Stack trace: {trace}{shortFileName}:{line}::[{method}]]");
    }
    public static void Warning(string message,
        bool doFullTrace = false,
        [CallerFilePath] string file = "",
        [CallerLineNumber] int line = 0,
        [CallerMemberName] string method = "")
    {
        if (!doFullTrace)
        {
            log_warn_ext(message, System.IO.Path.GetFileName(file), line, method);
            return;
        }
        string trace = GetFullCallStack();
        string shortFileName = System.IO.Path.GetFileName(file);
        log_warn_ext(message, shortFileName, line, $"[{method}] [Stack trace: {trace}{shortFileName}:{line}::[{method}]]");
    }
}
