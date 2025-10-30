using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using NECS.Extensions;

namespace NECS.Core.Logging
{
    public static class NLogger
    {
        private static readonly object _lock = new object();
        private static DictionaryWrapper<string, List<string>> logs_stack = new DictionaryWrapper<string, List<string>>();

        private static void Write(string type, ConsoleColor color, object content, string logstack = "")
        {
            lock (_lock)
            {
                if(logstack != "")
                {
                    if(!logs_stack.ContainsKey(logstack))
                        logs_stack.TryAdd(logstack, new List<string>());
                    logs_stack[logstack].Add($"{DateTime.UtcNow}##{content}");
                }
                
#if UNITY_5_3_OR_NEWER
                Console.ForegroundColor = color;
                if(ConsoleColor.Red.Equals(color))
                    UnityEngine.Debug.LogError($"[{DateTime.UtcNow}, {type}] {content}");
                else if(ConsoleColor.DarkYellow.Equals(color))
                    UnityEngine.Debug.LogWarning($"[{DateTime.UtcNow}, {type}] {content}");
                else
                    UnityEngine.Debug.Log($"[{DateTime.UtcNow}, {type}] {content}");
#elif GODOT && !GODOT_4_0_OR_GREATER
                if (ConsoleColor.Red.Equals(color))
                {
                    Godot.GD.PrintErr($"[{DateTime.UtcNow}, {type}] {content}");
                    Godot.GD.PrintStack();
                }
                else if(ConsoleColor.DarkYellow.Equals(color))
                    Godot.GD.Print($"[{DateTime.UtcNow}, {type}] {content}");
                else
                    Godot.GD.Print($"[{DateTime.UtcNow}, {type}] {content}");
#else
                Console.ForegroundColor = color;
                if(content is Exception)
                    Console.WriteLine($"[{DateTime.UtcNow}, {type}] {(content as Exception).Message}\n {(content as Exception).StackTrace}");
                else
                {
                    if (ConsoleColor.Red.Equals(color))
                    {
                        Console.WriteLine($"[{DateTime.UtcNow}, {type}] {content}\n=======/\\/\\/\\/\\/\\======\n{new System.Diagnostics.StackTrace().ToString()}\n======================");
                    }
                    else
                    {
                        Console.WriteLine($"[{DateTime.UtcNow}, {type}] {content}");
                    }
                }
                    
#endif
            }
        }

        public static void DumpLogStack(string logstack, string file, bool clear = true)
        {
            if(FileAdapter.Exists(file))
            {
                FileAdapter.Delete(file);
                FileAdapter.WriteAllText(file, "");
            }
            if(logs_stack.TryGetValue(logstack, out var logs))
            {
                foreach (var log in logs)
                {
                    var previousLogs = FileAdapter.ReadAllText(file);
                    FileAdapter.WriteAllText(file, previousLogs + log + "\n");
                }
            }
        }

        private static void DebugWrite(string type, ConsoleColor color, object content)
        {
#if DEBUG
            Write(type, color, content);
#endif
        }

        public static void LogStack(object content, string stackName) => Write("INFO", ConsoleColor.Gray, content, stackName);
        public static void Log(object content) => Write("INFO", ConsoleColor.Gray, content);
        public static void LogNetwork(object content) => Write("Network", ConsoleColor.Blue, content);
        public static void LogDB(object content) => Write("DataBase", ConsoleColor.Yellow, content);
        public static void LogErrorDB(object content) => Write("ERRORDB", ConsoleColor.Red, content);
        public static void LogService(object content) => Write("Service", ConsoleColor.Magenta, content);
        public static void LogSuccess(object content) => Write("Success", ConsoleColor.Green, content);

        public static void Debug(object content) => DebugWrite("DEBUG", ConsoleColor.DarkGreen, content);
        public static void Trace(object content)
        {
            //if (Server.Instance.Settings.EnableTracing)
            //    DebugWrite("TRACE", ConsoleColor.DarkGray, content);
        }

        public static void Warn(object content) => Write("WARN", ConsoleColor.DarkYellow, content);

        public static void Error(object content) => Write("ERROR", ConsoleColor.Red, content);
        public static void LogError(object content) => Error(content);
    }
}
