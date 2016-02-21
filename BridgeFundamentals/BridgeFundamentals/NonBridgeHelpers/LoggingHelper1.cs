
#define DEBUG
// trick for Trace in PCL

using System;

namespace Sodes.Base
{
    public static partial class Log
    {
        public static void Trace(string message, params object[] args)
        {
            var msg = string.Format(message, args);
            msg = string.Format("{0:HH:mm:ss.fff} {1}", DateTime.UtcNow, msg);
            System.Diagnostics.Debug.WriteLine(msg);
        }
    }
}
