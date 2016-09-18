using System.Diagnostics;
using System.IO;
using System.Runtime.Serialization;
using System.Xml;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using Sodes.Base;
using System.Threading.Tasks;

namespace Sodes.Bridge.Base.Test
{
	[TestClass]
	public class TraceTest
	{
		[TestMethod]
		public void LogTraceTest()
		{
            Log.Level = 2;
            Parallel.For(1, 12, (p) =>
            {
                CustomTrace.StartTrace();
                Parallel.For(1, 11, (t) =>
                {
                    for (int i = 0; i < 1000; i++)
                    {
                        Log.Trace(0, "test {2:0000} from process {0:00} thread {1:00}", p, t, i);
                    }
                });
                CustomTrace.StopTrace();
            });
        }
    }

    public static class CustomTrace
    {
        //private static Stack listenersStack = new Stack();

        public static void StartTrace()
        {
            //#if DEBUG
            //                while (Trace.Listeners.Count > 0)
            //                {
            //                    listenersStack.Push(Trace.Listeners[Trace.Listeners.Count - 1]);      // save default listener
            //                    Trace.Listeners.RemoveAt(Trace.Listeners.Count - 1);      // remove default listener
            //                }
            //#endif

#if TRACE
            bool fileCreated = false;
            int appInstance = 1;

            do
            {
                /* Create a new text writer using the output stream, and add it to the trace listeners. */
                try
                {
                    foreach (var file in System.IO.Directory.GetFiles(".", "Trace*.txt"))
                    {
                        if (DateTime.Now.Subtract(System.IO.File.GetLastAccessTime(file)).TotalDays > 2)
                        {
                            System.IO.File.Delete(file);
                        }
                    }

                    string fileName = "Trace " + DateTime.Now.ToString("yyyy-MM-dd HH-mm-ss") + "." + appInstance + ".txt";
                    Stream myFile = File.Create(fileName);
                    //myFile.Close();
                    //myFile = File.Open(fileName, FileMode.Open, FileAccess.Write, FileShare.Read);
                    var myTextListener = new TextWriterTraceListener(myFile);
                    Trace.Listeners.Add(myTextListener);
                    fileCreated = true;
                    Debug.Flush();
                    Trace.Flush();
                    myFile.Flush();
                }
                catch (IOException ex)
                {
                    appInstance++;
                    if (appInstance >= 9)
                    {
                        throw new InvalidOperationException("Unable to create trace file", ex);
                    }
                }
            } while (!fileCreated);
#endif
        }

        public static void StopTrace()
        {
#if TRACE
            try
            {
                Debug.Flush();
                Trace.Flush();
            }
            catch (System.Text.EncoderFallbackException)
            {
            }
            Trace.Close();
            Trace.Listeners.RemoveAt(Trace.Listeners.Count - 1);      // remove my listener
#endif
            //#if DEBUG
            //                while (listenersStack.Count > 0)
            //                    Trace.Listeners.Add((TraceListener)listenersStack.Pop());
            //#endif
        }
    }

}
