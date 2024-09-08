
#define DEBUG
// trick for Trace in PCL

using System;
using System.Diagnostics;

namespace Bridge
{
    public static partial class Log
    {
        private static Logger TheLogger;
        public static int Level;

        [Conditional("DEBUG")]
        public static void Debug(string message, params object[] args)
        {
            Trace(0, message, args);
        }

        public static void Trace(int level, string message, params object[] args)
        {
            if (Level >= 10)
            {   // for debugging problems with logger
                System.Diagnostics.Debug.WriteLine($"Logger.Trace {message}");
            }

            if (level <= Log.Level && TheLogger != null)
            {
                var msg = args == null || args.Length == 0 ? message : string.Format(message, args);
                if (!string.IsNullOrWhiteSpace(msg))
                {
                    TheLogger.Trace($"{DateTime.Now:HH:mm:ss.fff} {level} {msg}");
                    TheLogger.Flush();
                }
            }
        }

        public static void Initialize(int _level, Logger _logger)
        {
            Level = _level;
            TheLogger = _logger;
        }
    }

    public abstract class Logger
    {
        public abstract void Trace(string msg);
        public abstract void Flush();
    }
}
