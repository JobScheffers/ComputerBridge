using System;
using System.Security.Cryptography;
using System.Threading;

namespace Bridge
{
    /// <summary>
    /// RNGCryptoServiceProvider based random generator
    /// </summary>
    public class RandomGenerator : RandomGeneratorBase
    {
        // https://docs.microsoft.com/en-us/dotnet/api/system.security.cryptography.rngcryptoserviceprovider?view=netcore-3.1
        // https://www.i-programmer.info/programming/theory/2744-how-not-to-shuffle-the-kunth-fisher-yates-algorithm.html

        private static RNGCryptoServiceProvider rngCsp = new RNGCryptoServiceProvider();
        private const int bufferSize = 16;

        private static ThreadLocal<int> bufferPosition = new ThreadLocal<int>(() => bufferSize);

        private static ThreadLocal<byte[]> randomNumbers = new ThreadLocal<byte[]>(() => new byte[bufferSize]);

        protected override byte Roll(int maxValue)
        {
#if DEBUG
            if (maxValue <= 0 || maxValue > 254)
                throw new ArgumentOutOfRangeException("numberSides");
#endif
            if (bufferPosition.Value + 1 > bufferSize)
            {
                rngCsp.GetBytes(randomNumbers.Value);
                bufferPosition.Value = 0;
            }

            return randomNumbers.Value[bufferPosition.Value++];
        }

        public static RandomGeneratorBase Instance { get; set; } = new RandomGenerator();
    }


    public abstract class RandomGeneratorBase
    {
        /// <returns>random int x: 0 <= x < maxValue</returns>
        public int Next(int maxValue)
        {
            // There are MaxValue / numSides full sets of numbers that can come up
            // in a single byte.  For instance, if we have a 6 sided die, there are
            // 42 full sets of 1-6 that come up.  The 43rd set is incomplete.
            int fullSetsOfValues = Byte.MaxValue / maxValue;
            byte randomNumber;
            do
            {
                randomNumber = Roll(maxValue);
            } while (!IsFairRoll(randomNumber));        // remove modulo bias
            // Return the random number mod the number of sides.
            return (byte)(randomNumber % maxValue);

            bool IsFairRoll(byte roll)
            {
                // If the roll is within this range of fair values, then we let it continue.
                // In the 6 sided die case, a roll between 0 and 251 is allowed.  (We use
                // < rather than <= since the = portion allows through an extra 0 value).
                // 252 through 255 would provide an extra 0, 1, 2, 3 so they are not fair to use.
                //return true;
                return roll < maxValue * fullSetsOfValues;
            }
        }

        protected abstract byte Roll(int maxValue);

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
            return Percentage() <= p;
        }

        /// <summary>
        /// If there is no need for a specific number but only a decision that has to be taken in a certain percentage of all cases
        /// </summary>
        /// <param name="p">between 0.0 and 1.0</param>
        /// <returns>True in p% of all calls</returns>
        public bool Percentage(double p)
        {
            return Percentage(Math.Round(100.0 * p));
        }

        /// <summary>
        /// Returns a random number between 0.0 and 1.0
        /// </summary>
        /// <returns>A double between 0.0 and 1.0</returns>
        public int Percentage()
        {
            return Next(101);
        }
    }
}
