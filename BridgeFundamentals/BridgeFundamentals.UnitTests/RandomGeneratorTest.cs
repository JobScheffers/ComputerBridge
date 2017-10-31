using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Bridge.Test
{
    [TestClass]
    public class RandomGeneratorTest
    {
        private static readonly object lockObject = new object();

        [TestMethod, TestCategory("CI"), TestCategory("Other")]
        public void RandomTest()
        {
            RandomGenerator.RandomSeed();
            int[] dobbelsteen = new int[6];
            const int loopSize = 20000000;

            Parallel.For(0, loopSize, (i, loop) =>
            {
                var draw = RandomGenerator.Next(6);
                lock (lockObject)
                {
                    dobbelsteen[draw]++;
                }
            });

            for (int i = 0; i < 6; i++)
            {
                var part = 1.0 * dobbelsteen[i] / loopSize;
                Debug.WriteLine("{0} {1:F4}", i, part);
                Assert.IsTrue(part > 0.166, "dobbelsteen");
            }

            int[] percentage = new int[2];
            for (int i = 0; i < loopSize; i++)
            {
                percentage[RandomGenerator.Percentage(2) ? 0 : 1]++;
            }

            var part0 = 1.0 * percentage[0] / loopSize;
            Assert.IsTrue(Math.Abs(part0 - 0.02) < 0.0028, "Too little or too much 2% : " + part0);

            percentage = new int[2];
            for (int i = 0; i < loopSize; i++)
            {
                percentage[RandomGenerator.Percentage(95) ? 0 : 1]++;
            }

            part0 = 1.0 * percentage[0] / loopSize;
            Assert.IsTrue(Math.Abs(part0 - 0.95) < 0.0028, "Too little or too much 95% : " + part0);
        }

        [TestMethod, TestCategory("CI"), TestCategory("Other")]
        public void RandomSpeedTest()
        {
            // Checking the speed of the random generator
            RandomGenerator.RandomSeed();
            const int loopSize = 1000000;
            double averageTime;
            var timer = System.Diagnostics.Stopwatch.StartNew();
            for (int i = 0; i < loopSize; i++)
            {
                RandomGenerator.Next(6);
            }

            timer.Stop();
            averageTime = 1.0 * timer.ElapsedMilliseconds / loopSize;
            Trace.TraceInformation("Elapsed: {0}ms", timer.ElapsedMilliseconds);
            Trace.TraceInformation("Bridge.Random.Next(6): {0}ms", averageTime);
            Assert.IsTrue(averageTime < 0.02, "Bridge.Random.Next(6)");

            timer.Reset();
            timer.Start();
            for (int i = 0; i < loopSize; i++)
            {
                RandomGenerator.Percentage();
            }

            timer.Stop();
            averageTime = 1.0 * timer.ElapsedMilliseconds / loopSize;
            Trace.TraceInformation("Elapsed: {0}ms", timer.ElapsedMilliseconds);
            Trace.TraceInformation("Bridge.Random.Percentage(): {0}ms", averageTime);
            Assert.IsTrue(averageTime < 0.02, "Bridge.Random.Percentage()");
        }
    }
}
