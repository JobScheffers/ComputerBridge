
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

        [DebuggerStepThrough]
        public static void Trace(int level, string message, params object[] args)
        {
            if (string.IsNullOrWhiteSpace(message)) return;
            var msg = args == null || args.Length == 0 ? message : string.Format(message, args);
            Trace(level, msg);
        }

        [DebuggerStepThrough]
        public static void Trace(int level, string message)
        {
            if (string.IsNullOrWhiteSpace(message)) return;
            if (Level >= 10)
            {   // for debugging problems with logger
                System.Diagnostics.Debug.WriteLine($"Logger.Trace {message}");
            }

            if (level <= Log.Level && TheLogger != null)
            {
                var msg = $"{DateTime.Now:HH:mm:ss.fff} {level} {message}";
                TheLogger.Trace(in msg);
                TheLogger.Flush();
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
#if NET6_0_OR_GREATER
        public abstract void Trace(ref readonly string msg);
#else
        public abstract void Trace(in string msg);
#endif
        public abstract void Flush();
    }
}
