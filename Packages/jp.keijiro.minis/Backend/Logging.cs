using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;

using Debug = UnityEngine.Debug;

// Borrowed from https://github.com/TheNathannator/HIDrogen

namespace Minis
{
    internal static class Logging
    {
        public static void Message(string message)
            => Debug.Log($"[Minis] {message}");

        public static void Warning(string message)
            => Debug.LogWarning($"[Minis] {message}");

        public static void Error(
            string message,
            [CallerFilePath] string sourceFilePath = "",
            [CallerLineNumber] int sourceLineNumber = 0
        )
            => Debug.LogError($"[Minis {Path.GetFileName(sourceFilePath)}({sourceLineNumber})] {message}");

        public static void Exception(string message, Exception ex)
        {
            Debug.LogError($"[Minis] {message}");
            Debug.LogException(ex);
        }

        [Conditional("MINIS_VERBOSE_LOGGING")]
        public static void Verbose(string message)
            => Message(message);
    }
}