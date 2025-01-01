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
        public void Random_52()
        {
            //RandomGenerator.Instance = new TestRandomGenerator();
            int possibilities = 52;
            var count = new int[possibilities];
            var frequency = new double[possibilities];
            const int loopSize = 20000000;

            Parallel.For(0, loopSize, (i, loop) =>
            {
                var draw = RandomGenerator.Instance.Next(possibilities);
                lock (lockObject)
                {
                    count[draw]++;
                }
            });

            for (int i = 0; i < possibilities; i++)
            {
                frequency[i] = 1.0 * count[i] / loopSize;
                Debug.WriteLine("{0,2} {1:F5}%", i, 100.0 * frequency[i]);
            }

            var mathematicalAverage = 1.0 / possibilities;
            int highest = 0;
            int lowest = 0;
            for (int i = 0; i < possibilities; i++)
            {
                var deltaToAverage = Math.Abs(frequency[i] - mathematicalAverage);
                var relativeDistance = deltaToAverage / mathematicalAverage;
                Assert.IsTrue(relativeDistance < 0.007, $"delta too large for {i} {frequency[i]:F5} {mathematicalAverage:F5} {relativeDistance:F3}");
                if (frequency[i] > frequency[highest]) highest = i;
                if (frequency[i] < frequency[lowest]) lowest = i;
            }
            var absoluteDifference = frequency[highest] - frequency[lowest];
            var relativeDifference = 100.0 * absoluteDifference / frequency[highest];
            Debug.WriteLine($"highest - lowest: {100*absoluteDifference:F5}% {relativeDifference:F4}%");
            Assert.IsTrue(absoluteDifference < 0.019, "absolute difference");
            Assert.IsTrue(relativeDifference < 1.6, "relative difference");
        }

        [TestMethod, TestCategory("CI"), TestCategory("Other")]
        public void Random_Percentage()
        {
            //RandomGenerator.Instance = new TestRandomGenerator();
            int possibilities = 101;
            var count = new int[possibilities];
            var frequency = new double[possibilities];
            const int loopSize = 100000;

            Parallel.For(0, loopSize, new ParallelOptions { MaxDegreeOfParallelism = 1 }, (i, loop) =>
            {
                for (int p = 0; p <= 100; p++)
                {
                    if (RandomGenerator.Instance.Percentage(p))
                    {
                        lock (lockObject)
                        {
                            count[p]++;
                        }
                    }
                }
            });

            for (int i = 0; i < possibilities; i++)
            {
                frequency[i] = 1.0 * count[i] / loopSize;
                Debug.WriteLine("{0,2} {1:F5}%", i, 100.0 * frequency[i]);
            }

            for (int i = 0; i < possibilities; i++)
            {
                var mathematicalAverage = 0.01 * i;
                var deltaToAverage = Math.Abs(frequency[i] - mathematicalAverage);
                Assert.IsTrue(deltaToAverage < 0.015, $"delta too large for {i} {frequency[i]:F5} {mathematicalAverage:F5} {deltaToAverage:F3}");
            }
        }

        [TestMethod, TestCategory("CI"), TestCategory("Other")]
        public void Random_Speed()
        {
            // Checking the speed of the random generator
            const int loopSize = 1000000;
            double averageTime;
            var timer = System.Diagnostics.Stopwatch.StartNew();
            for (int i = 0; i < loopSize; i++)
            {
                RandomGenerator.Instance.Next(6);
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
                RandomGenerator.Instance.Percentage(50);
            }

            timer.Stop();
            averageTime = 1.0 * timer.ElapsedMilliseconds / loopSize;
            Trace.TraceInformation("Elapsed: {0}ms", timer.ElapsedMilliseconds);
            Trace.TraceInformation("Bridge.Random.Percentage(): {0}ms", averageTime);
            Assert.IsTrue(averageTime < 0.02, "Bridge.Random.Percentage()");
        }
    }
}
