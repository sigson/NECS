using System;

namespace NECS.Core.Logging
{
    public static class NLogger
    {
        private static readonly object _lock = new object();

        private static void Write(string type, ConsoleColor color, object content)
        {
            lock (_lock)
            {
#if UNITY_5_3_OR_NEWER
                Console.ForegroundColor = color;
                if(ConsoleColor.Red.Equals(color))
                    UnityEngine.Debug.LogError($"[{DateTime.UtcNow}, {type}] {content}");
                else if(ConsoleColor.DarkYellow.Equals(color))
                    UnityEngine.Debug.LogWarning($"[{DateTime.UtcNow}, {type}] {content}");
                else
                    UnityEngine.Debug.Log($"[{DateTime.UtcNow}, {type}] {content}");

#else
                Console.ForegroundColor = color;
                if(content is Exception)
                    Console.WriteLine($"[{DateTime.UtcNow}, {type}] {(content as Exception).Message}\n {(content as Exception).StackTrace}");
                else
                    Console.WriteLine($"[{DateTime.UtcNow}, {type}] {content}");
#endif
            }
        }

        private static void DebugWrite(string type, ConsoleColor color, object content)
        {
#if DEBUG
            Write(type, color, content);
#endif
        }

        public static void Log(object content) => Write("INFO", ConsoleColor.Gray, content);
        public static void LogNetwork(object content) => Write("Network", ConsoleColor.Blue, content);
        public static void LogDB(object content) => Write("DataBase", ConsoleColor.Yellow, content);
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
