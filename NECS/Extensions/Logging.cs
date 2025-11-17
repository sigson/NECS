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
        public static ConcurrentQueue<(string, ConsoleColor, string)> logsBag = new ConcurrentQueue<(string, ConsoleColor, string)>();

        private static void Write(string type, ConsoleColor color, object content, string logstack = "")
        {
            var message = "";
            if(content is Exception)
                message = $"[{DateTime.UtcNow}, {type}] {(content as Exception).Message}\n=======EXCEPTION======\n{new System.Diagnostics.StackTrace(content as Exception, true).ToString()}\n======================";
            else
            {
                if (ConsoleColor.Red.Equals(color))
                {
                    message = $"[{DateTime.UtcNow}, {type}] {content}\n=======LOGGED======\n{new System.Diagnostics.StackTrace(true).ToString()}\n======================";
                }
                else
                {
                    message = $"[{DateTime.UtcNow}, {type}] {content}";
                }
            }

            if(Defines.OneThreadMode && !Defines.RedirectAllLogsToExeFile)
            {
                PrintErrorBase(type, color, message, logstack);
            }
            else
            {
                logsBag.Enqueue((type, color, message));
            }
        }

        public static void PrintErrorBase(string type, ConsoleColor color, string message, string logstack = "")
        {
            if (logstack != "")
            {
                if (!logs_stack.ContainsKey(logstack))
                    logs_stack.TryAdd(logstack, new List<string>());
                logs_stack[logstack].Add(message);
            }

#if UNITY_5_3_OR_NEWER
            Console.ForegroundColor = color;
            if(ConsoleColor.Red.Equals(color))
                UnityEngine.Debug.LogError(message);
            else if(ConsoleColor.DarkYellow.Equals(color))
                UnityEngine.Debug.LogWarning(message);
            else
                UnityEngine.Debug.Log(message);
#elif GODOT && !GODOT_4_0_OR_GREATER
            if (ConsoleColor.Red.Equals(color))
            {
                Godot.GD.PrintErr(message);
                //Godot.GD.PrintStack();
            }
            else if (ConsoleColor.DarkYellow.Equals(color))
                Godot.GD.Print(message);
            else
                Godot.GD.Print(message);
#else
            Console.ForegroundColor = color;
            Console.WriteLine(message);
#endif
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
        public static void ErrorThread(object content) => Write("ERRORTHREAD", ConsoleColor.Red, content);
        public static void LogError(object content) => Error(content);
        public static void LogErrorLocking(object content) => Write("ERRORLOCK", ConsoleColor.Red, content);
    }
}
