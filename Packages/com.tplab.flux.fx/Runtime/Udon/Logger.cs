using UnityEngine;

namespace TpLab.Flux.FX.Udon
{
    internal static class Logger
    {
        const string Prefix = "[<color=#4EC9B0>FluxFX</color>]";

        public static void Log(object message)
        {
            Debug.Log($"{Prefix} {message}");
        }

        public static void Log(object message, Object context)
        {
            Debug.Log($"{Prefix} {message}", context);
        }

        public static void LogWarning(object message)
        {
            Debug.LogWarning($"{Prefix} {message}");
        }

        public static void LogWarning(object message, Object context)
        {
            Debug.LogWarning($"{Prefix} {message}", context);
        }

        public static void LogError(object message)
        {
            Debug.LogError($"{Prefix} {message}");
        }

        public static void LogError(object message, Object context)
        {
            Debug.LogError($"{Prefix} {message}", context);
        }
    }
}