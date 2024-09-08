using System.Diagnostics;

namespace Bridge.Test
{
    public class TestLogger : Logger
    {
        public override void Trace(string msg)
        {
            System.Diagnostics.Trace.WriteLine(msg);
        }

        public override void Flush()
        {
            System.Diagnostics.Trace.Flush();
        }

        public static void Initialize()
        {
            Log.Initialize(Log.Level, new TestLogger());
            //System.Diagnostics.Trace.Listeners.Add(new TextWriterTraceListener("TextWriterOutput.log", "myListener"));
        }
    }
}
