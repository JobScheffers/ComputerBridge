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
        public void Random_Repeatable()
        {
            const int loopSize = 1000000;
            var history1 = new int[loopSize];
            var history2 = new int[loopSize];
            RandomGenerator.Instance.Repeatable(42);
            for (int i = 0; i < loopSize; i++)
            {
                history1[i] = RandomGenerator.Instance.Next(52);
            }
            RandomGenerator.Instance.Repeatable(42);
            for (int i = 0; i < loopSize; i++)
            {
                history2[i] = RandomGenerator.Instance.Next(52);
            }

            for (int i = 0; i < loopSize; i++)
            {
                Assert.AreEqual(history1[i], history2[i], i.ToString());
            }

            //RandomGenerator.Instance.Repeatable(13372861712568832);
            //Assert.AreEqual(966060258, RandomGenerator.Instance.Next(int.MaxValue));
            //Assert.AreEqual(797361684, RandomGenerator.Instance.Next(int.MaxValue));
        }

        [TestMethod, TestCategory("CI"), TestCategory("Other")]
        public void Random_52_CardDealing()
        {
            RandomGenerator.Instance.Repeatable(1);
            var draw52 = RandomGenerator.Instance.Next(52);
            for (int i = 0; i < 100; i++)
            {
                var draw6 = RandomGenerator.Instance.Next(6);
                Assert.IsGreaterThanOrEqualTo(0, draw6);
                Assert.IsLessThan(6, draw6);
            }
        }

        [TestMethod, TestCategory("CI"), TestCategory("Other")]
        public void Random_6()
        {
            int possibilities = 6;
            var count = new int[possibilities];
            var frequency = new double[possibilities];
            const int loopSize = 20_000_000;

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
                Trace.WriteLine($"{i:00} {100.0 * frequency[i]:F5}%");
            }

            var mathematicalAverage = 1.0 / possibilities;
            int highest = 0;
            int lowest = 0;
            for (int i = 0; i < possibilities; i++)
            {
                var deltaToAverage = Math.Abs(frequency[i] - mathematicalAverage);
                var relativeDistance = deltaToAverage / mathematicalAverage;
                Assert.IsLessThan(0.03, relativeDistance, $"delta too large for {i} {frequency[i]:F5} {mathematicalAverage:F5} {relativeDistance:F3}");
                if (frequency[i] > frequency[highest]) highest = i;
                if (frequency[i] < frequency[lowest]) lowest = i;
            }
            var absoluteDifference = frequency[highest] - frequency[lowest];
            var relativeDifference = 100.0 * absoluteDifference / frequency[highest];
            Trace.WriteLine($"highest - lowest: {100 * absoluteDifference:F5}% {relativeDifference:F4}%");
            Assert.IsLessThan(0.03, absoluteDifference, "absolute difference");
            Assert.IsLessThan(0.25, relativeDifference, "relative difference");
        }

        [TestMethod, TestCategory("CI"), TestCategory("Other")]
        public void Random_52()
        {
            int possibilities = 52;
            var count = new int[possibilities];
            var frequency = new double[possibilities];
            const int loopSize = 20_000_000;

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
                Trace.WriteLine($"{i:00} {100.0 * frequency[i]:F5}%");
            }

            var mathematicalAverage = 1.0 / possibilities;
            int highest = 0;
            int lowest = 0;
            for (int i = 0; i < possibilities; i++)
            {
                var deltaToAverage = Math.Abs(frequency[i] - mathematicalAverage);
                var relativeDistance = deltaToAverage / mathematicalAverage;
                Assert.IsLessThan(0.007, relativeDistance, $"delta too large for {i} {frequency[i]:F5} {mathematicalAverage:F5} {relativeDistance:F3}");
                if (frequency[i] > frequency[highest]) highest = i;
                if (frequency[i] < frequency[lowest]) lowest = i;
            }
            var absoluteDifference = frequency[highest] - frequency[lowest];
            var relativeDifference = 100.0 * absoluteDifference / frequency[highest];
            Trace.WriteLine($"highest - lowest: {100*absoluteDifference:F5}% {relativeDifference:F4}%");
            Assert.IsLessThan(0.019, absoluteDifference, "absolute difference");
            Assert.IsLessThan(1.6, relativeDifference, "relative difference");
        }

        [TestMethod, TestCategory("CI"), TestCategory("Other")]
        public void Random_52_SingleThread()
        {
            int possibilities = 52;
            var count = new int[possibilities];
            var frequency = new double[possibilities];
            const int loopSize = 20_000_000;

            for (int i = 0; i < loopSize; i++)
            {
                var draw = RandomGenerator.Instance.Next(possibilities);
                count[draw]++;
            }

            for (int i = 0; i < possibilities; i++)
            {
                frequency[i] = 1.0 * count[i] / loopSize;
                Trace.WriteLine($"{i:00} {100.0 * frequency[i]:F5}%");
            }

            var mathematicalAverage = 1.0 / possibilities;
            int highest = 0;
            int lowest = 0;
            for (int i = 0; i < possibilities; i++)
            {
                var deltaToAverage = Math.Abs(frequency[i] - mathematicalAverage);
                var relativeDistance = deltaToAverage / mathematicalAverage;
                Assert.IsLessThan(0.007, relativeDistance, $"delta too large for {i} {frequency[i]:F5} {mathematicalAverage:F5} {relativeDistance:F3}");
                if (frequency[i] > frequency[highest]) highest = i;
                if (frequency[i] < frequency[lowest]) lowest = i;
            }
            var absoluteDifference = frequency[highest] - frequency[lowest];
            var relativeDifference = 100.0 * absoluteDifference / frequency[highest];
            Trace.WriteLine($"highest - lowest: {100 * absoluteDifference:F5}% {relativeDifference:F4}%");
            Assert.IsLessThan(0.019, absoluteDifference, "absolute difference");
            Assert.IsLessThan(1.6, relativeDifference, "relative difference");
        }

        [TestMethod, TestCategory("CI"), TestCategory("Other")]
        public void Random_Percentage()
        {
            int possibilities = 101;
            var count = new int[possibilities];
            var frequency = new double[possibilities];
            const int loopSize = 1_000_000;

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
                Trace.WriteLine($"{i:00} {100.0 * frequency[i]:F5}%");
            }

            for (int i = 0; i < possibilities; i++)
            {
                var mathematicalAverage = 0.01 * i;
                var deltaToAverage = Math.Abs(frequency[i] - mathematicalAverage);
                Assert.IsLessThan(0.015, deltaToAverage, $"delta too large for {i} {frequency[i]:F5} {mathematicalAverage:F5} {deltaToAverage:F3}");
            }
        }

        [TestMethod, TestCategory("CI"), TestCategory("Other")]
        public void Random_Speed()
        {
            // Checking the speed of the random generator
            const int loopSize = 10_000_000;
            double averageTime;
            var timer = System.Diagnostics.Stopwatch.StartNew();

            for (int i = 0; i < loopSize; i++)
            {
                RandomGenerator.Instance.Next(52);
            }
            timer.Stop();
            averageTime = 1.0 * timer.ElapsedMilliseconds / loopSize;
            Trace.TraceInformation("Elapsed: {0:F6}ms", timer.ElapsedMilliseconds);
            Trace.TraceInformation("Bridge.Random.Next(52): {0:F6}ms", averageTime);
            Assert.IsLessThan(0.02, averageTime, "Bridge.Random.Next(52)");

            timer.Reset();
            timer.Start();
            for (int i = 0; i < loopSize; i++)
            {
                RandomGenerator.Instance.Percentage(50);
            }

            timer.Stop();
            averageTime = 1.0 * timer.ElapsedMilliseconds / loopSize;
            Trace.TraceInformation("Elapsed: {0:F6}ms", timer.ElapsedMilliseconds);
            Trace.TraceInformation("Bridge.Random.Percentage(): {0:F6}ms", averageTime);
            Assert.IsLessThan(0.02, averageTime, "Bridge.Random.Percentage()");
        }
    }
}
