using System;
using System.Globalization;
using System.IO;
using UnityEngine;

namespace ODMatrix.Diagnostics
{
    internal static class ModLogger
    {
        private static readonly object SyncRoot = new object();

        private static string s_logFilePath;
        private static bool s_initialized;

        internal static string LogDirectoryPath
        {
            get
            {
                return Path.Combine(
                    Path.Combine(
                        Path.Combine(
                            Path.Combine(
                                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                                "Colossal Order"),
                            "Cities_Skylines"),
                        "ODMatrix"),
                    "Logs");
            }
        }

        internal static void Initialize()
        {
            lock (SyncRoot)
            {
                if (s_initialized)
                {
                    return;
                }

                Directory.CreateDirectory(LogDirectoryPath);
                s_logFilePath = Path.Combine(
                    LogDirectoryPath,
                    "odmatrix_" + DateTime.Now.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture) + ".log");
                s_initialized = true;

                WriteCore("INFO", "Log session started.");
            }
        }

        internal static void Shutdown()
        {
            lock (SyncRoot)
            {
                if (!s_initialized)
                {
                    return;
                }

                WriteCore("INFO", "Log session ended.");
                s_initialized = false;
                s_logFilePath = null;
            }
        }

        internal static void Lifecycle(string message)
        {
            Debug.Log("[ODMatrix] " + message);
            Write("LIFECYCLE", message);
        }

        internal static void Info(string message)
        {
            Write("INFO", message);
        }

        internal static void Warn(string message)
        {
            Write("WARN", message);
        }

        internal static void Error(string message, Exception ex)
        {
            string fullMessage = message;
            if (ex != null)
            {
                fullMessage = fullMessage + Environment.NewLine + ex;
            }

            Write("ERROR", fullMessage);
        }

        private static void Write(string level, string message)
        {
            lock (SyncRoot)
            {
                if (!s_initialized)
                {
                    Initialize();
                }

                WriteCore(level, message);
            }
        }

        private static void WriteCore(string level, string message)
        {
            string line = string.Format(
                CultureInfo.InvariantCulture,
                "{0} [{1}] {2}",
                DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture),
                level,
                message);

            File.AppendAllText(s_logFilePath, line + Environment.NewLine);
        }
    }
}
