namespace Bridge.Test
{
    public class TestLogger : Logger
    {
        public override void Trace(string msg)
        {
            System.Diagnostics.Trace.WriteLine(msg);
        }

        public static void Initialize()
        {
            Log.Initialize(Log.Level, new TestLogger());
        }
    }
}
