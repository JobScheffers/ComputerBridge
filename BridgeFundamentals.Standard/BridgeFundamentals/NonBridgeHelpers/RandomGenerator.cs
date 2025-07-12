using System;
using System.Threading;

namespace Bridge
{
    public static class RandomGenerator
    {
        public static RandomGeneratorBase Instance { get; set; } = new RepeatableRandomGenerator();

        private class RepeatableRandomGenerator : RandomGeneratorBase
        {
            // based on: https://stackoverflow.com/questions/64937914/thread-safe-high-performance-random-generator

            /// <returns>random int x: 0 <= x < maxValue</returns>
            public override int Next(int maxValue)
            {
                ulong nextRawRandomNumber = GetULong();
                var int1 = (int)(nextRawRandomNumber % (ulong)maxValue);
                return int1;
            }

            public override void Repeatable(ulong _seed)
            {
                this.seed = _seed;
            }

            private ulong seed = 22;

            private ulong GetULong()
            {
                unchecked
                {
                    long prev = (long)seed;

                    long t = prev;
                    t ^= t >> 12;
                    t ^= t << 25;
                    t ^= t >> 27;

                    while (InterlockedCompareExchange(ref seed, t, prev) != prev)
                    {
                        prev = (long)seed;
                        t = prev;
                        t ^= t >> 12;
                        t ^= t << 25;
                        t ^= t >> 27;
                    }

                    return (ulong)(t * 0x2545F4914F6CDD1D);
                }
            }

            private static unsafe long InterlockedCompareExchange(ref ulong location,
                 long value, long comparand)
            {
                fixed (ulong* ptr = &location)
                {
                    return Interlocked.CompareExchange(ref *(long*)ptr, value, comparand);
                }
            }
        }
    }

    public abstract class RandomGeneratorBase
    {
        /// <returns>random int x: 0 <= x < maxValue</returns>
        public abstract int Next(int maxValue);

        public abstract void Repeatable(ulong seed);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="minValue"></param>
        /// <param name="maxValue"></param>
        /// <returns>A random integer between <paramref name="minValue"/> and <paramref name="maxValue"/></returns>
        public int Next(int minValue, int maxValue)
        {
            return minValue + Next(maxValue - minValue);
        }

        /// <summary>
        /// If there is no need for a specific number but only a decision that has to be taken in a certain percentage of all cases
        /// </summary>
        /// <param name="p">between 0 and 100</param>
        /// <returns>True in p% of all calls</returns>
        public bool Percentage(int p)
        {
#if DEBUG
            if (p < 0 || p > 100)
                throw new ArgumentOutOfRangeException("p");
#endif
            return Next(101) <= p;
        }

        /// <summary>
        /// If there is no need for a specific number but only a decision that has to be taken in a certain percentage of all cases
        /// </summary>
        /// <param name="p">between 0.0 and 1.0</param>
        /// <returns>True in p% of all calls</returns>
        public bool Percentage(double p)
        {
            return Percentage() <= p;
        }

        /// <summary>
        /// Returns a random number between 0.0 and 1.0 with a precision of 
        /// </summary>
        /// <returns>A double between 0.0 and 1.0</returns>
        public double Percentage()
        {
            return 1.0 * Next(65536) / 65535.0;
        }
    }
}
