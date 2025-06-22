using BepInEx.Logging;
using UnityEngine;

namespace Utilla.Tools
{
    internal class Logging
    {
        public static ManualLogSource Logger;

        public static void Info(object data) => Log(data, LogLevel.Info);

        public static void Warning(object data) => Log(data, LogLevel.Warning);

        public static void Error(object data) => Log(data, LogLevel.Error);

        private static LogType ToLogType(LogLevel level) => level switch
        {
            LogLevel.Fatal or LogLevel.Error => LogType.Error,
            LogLevel.Warning => LogType.Warning,
            _ => LogType.Log
        };

        public static void Log(object data, LogLevel level = LogLevel.Info)
        {
#if DEBUG
            if (Logger == null) Debug.unityLogger.Log(ToLogType(level), $"Utilla: {data}");
            else Logger.Log(level, data);
#endif
        }
    }
}
