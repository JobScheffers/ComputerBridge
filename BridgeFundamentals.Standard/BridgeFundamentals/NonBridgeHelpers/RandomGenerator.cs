using System;
using System.Security.Cryptography;
using System.Threading;

namespace Bridge
{
    public static class RandomGenerator
    {
        public static RandomGeneratorBase Instance { get; set; } = new RandomGenerator2();
    }

    /// <summary>
    /// RNGCryptoServiceProvider based random generator
    /// </summary>
    public class RandomGenerator1 : RandomGeneratorBase
    {
        // https://docs.microsoft.com/en-us/dotnet/api/system.security.cryptography.rngcryptoserviceprovider?view=netcore-3.1
        // https://www.i-programmer.info/programming/theory/2744-how-not-to-shuffle-the-kunth-fisher-yates-algorithm.html

        private static RandomNumberGenerator rngCsp = RandomNumberGenerator.Create();
        private const int bufferSize = 16;

        private static ThreadLocal<int> bufferPosition = new ThreadLocal<int>(() => bufferSize);

        private static ThreadLocal<byte[]> randomNumbers = new ThreadLocal<byte[]>(() => new byte[bufferSize]);

        protected override int Roll(int maxValue)
        {
            var byte1 = GetRandomByte();
            if (maxValue - 1 <= Byte.MaxValue) return byte1;
            var byte2 = GetRandomByte();
            return (Byte.MaxValue + 1) * byte2 + byte1;

            byte GetRandomByte()
            {
                if (bufferPosition.Value + 1 > bufferSize)
                {
                    rngCsp.GetBytes(randomNumbers.Value);
                    bufferPosition.Value = 0;
                }

                return randomNumbers.Value[bufferPosition.Value++];
            }
        }

        protected override int TypeMaximun(int maxValue)
        {
            if (maxValue <= (Byte.MaxValue + 1)) return Byte.MaxValue + 1;
            if (maxValue <= (Byte.MaxValue + 1) * (Byte.MaxValue + 1)) return (Byte.MaxValue + 1) * (Byte.MaxValue + 1);
            throw new ArgumentOutOfRangeException("maxValue", "this random generator can handle a maximum maxValue of 256 * 256");
        }

        protected override void Repeatable(ulong seed)
        {
        }
    }

    /// <summary>
    /// RNGCryptoServiceProvider based random generator
    /// </summary>
    public class RandomGenerator2 : RandomGeneratorBase
    {

        protected override int Roll(int maxValue)
        {
            var byte1 = GetULong();
            var int1 =  (int)(byte1 % int.MaxValue);
            return int1;
        }

        protected override int TypeMaximun(int maxValue)
        {
            return int.MaxValue;
        }

        protected override void Repeatable(ulong _seed)
        {
            this.seed = _seed;
        }

        private ulong seed = 22;

        private ulong GetULong()
        {
            unchecked
            {
                ulong prev = seed;

                ulong t = prev;
                t ^= t >> 12;
                t ^= t << 25;
                t ^= t >> 27;

                while (InterlockedCompareExchange(ref seed, t, prev) != prev)
                {
                    prev = seed;
                    t = prev;
                    t ^= t >> 12;
                    t ^= t << 25;
                    t ^= t >> 27;
                }

                return t * 0x2545F4914F6CDD1D;
            }
        }

        private static unsafe ulong InterlockedCompareExchange(ref ulong location,
             ulong value, ulong comparand)
        {
            fixed (ulong* ptr = &location)
            {
                return (ulong)Interlocked.CompareExchange(ref *(long*)ptr, (long)value, (long)comparand);
            }
        }
    }


    public abstract class RandomGeneratorBase
    {
        /// <returns>random int x: 0 <= x < maxValue</returns>
        public int Next(int maxValue)
        {
            // There are MaxValue / numSides full sets of numbers that can come up
            // in a single byte.  For instance, if we have a 6 sided dice, there are
            // 42 full sets of 1-6 that come up.  The 43rd set is incomplete.
            int fullSetsOfValues = TypeMaximun(maxValue) / maxValue;
            int randomNumber;
            do
            {
                randomNumber = Roll(maxValue);
            } while (!IsFairRoll(randomNumber));        // remove modulo bias
            // Return the random number mod the number of sides.
            return (randomNumber % maxValue);

            bool IsFairRoll(int roll)
            {
                // If the roll is within this range of fair values, then we let it continue.
                // In the 6 sided die case, a roll between 0 and 251 is allowed.  (We use
                // < rather than <= since the = portion allows through an extra 0 value).
                // 252 through 255 would provide an extra 0, 1, 2, 3 so they are not fair to use.
                //return true;
                return roll < maxValue * fullSetsOfValues;
            }
        }

        protected abstract void Repeatable(ulong seed);

        protected abstract int Roll(int maxValue);

        protected abstract int TypeMaximun(int maxValue);

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
            return Percentage(0.01 * p);
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
