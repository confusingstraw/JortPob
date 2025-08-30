using System;
using System.Collections.Concurrent;

namespace JortPob.Common
{
    public class Lort
    {
        public static ConcurrentBag<string> mainOutput;
        public static ConcurrentBag<string> debugOutput;
        public static string progressOutput;
        public static int total, current;
        public static bool update;

        public static void Initialize()
        {
            mainOutput = new();
            debugOutput = new();
            progressOutput = string.Empty;
            total = 0;
            current = 0;
            update = false;
        }

        public enum Type
        {
            Main,
            Debug
        }

        public static void Log(string message, Lort.Type type)
        {
            switch(type)
            {
                case Type.Main:
                    mainOutput.Add(message); break;
                case Type.Debug:
                    debugOutput.Add(message); break;
            }
            update = true;
        }

        public static void NewTask(string task, int max)
        {
            progressOutput = $"{task}";
            current = 0;
            total = max;
            update = true;
        }

        public static void TaskIterate()
        {
            current = Math.Min(current+1, total);
            update = true;
        }
    }
}
