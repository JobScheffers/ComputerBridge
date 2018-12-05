
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
            var msg = string.Format(message, args);
            if (!string.IsNullOrWhiteSpace(msg))
            {
                msg = string.Format("{0:HH:mm:ss.fff} {1}", DateTime.UtcNow, msg);
                if (level <= Log.Level && TheLogger != null)
                {
                    TheLogger.Trace(msg);
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
    }
}
