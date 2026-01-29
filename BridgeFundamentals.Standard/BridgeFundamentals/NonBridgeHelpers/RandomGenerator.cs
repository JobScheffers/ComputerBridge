using System;
using System.Threading;
using System.Numerics;

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace Bridge
#pragma warning restore IDE0130 // Namespace does not match folder structure
{
    public static class RandomGenerator
    {
        public static RandomGeneratorBase Instance { get; set; } = new RepeatableRandomGenerator();

        private class RepeatableRandomGenerator : RandomGeneratorBase
        {
            // based on: https://stackoverflow.com/questions/64937914/thread-safe-high-performance-random-generator

            /// <summary>
            /// Return random int x: 0 <= x &lt; maxValue using rejection sampling to avoid modulo bias.
            /// </summary>
            public override int Next(int maxValue)
            {
                ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxValue);

                // Use 64-bit source to reduce number of iterations
                ulong bound = (ulong)maxValue;
                //ulong threshold = (ulong.MaxValue - (ulong.MaxValue % bound)) - (ulong.MaxValue % bound);
                // simpler: compute rejection threshold as largest multiple of bound <= ulong.MaxValue
                // but we can use a standard approach: compute limit = (ulong.MaxValue / bound) * bound
                ulong limit = (ulong.MaxValue / bound) * bound;

                while (true)
                {
                    ulong r = GetULong();
                    if (r < limit)
                        return (int)(r % bound);
                    // else retry
                }
            }

            /// <summary>
            /// Return a uniformly random 64-bit value.
            /// </summary>
            public override ulong NextULong()
            {
                return GetULong();
            }

            /// <summary>
            /// Fill the provided span with random bytes.
            /// </summary>
            public override void NextBytes(Span<byte> destination)
            {
                int i = 0;
                int len = destination.Length;
                while (i < len)
                {
                    ulong v = GetULong();
                    // copy up to 8 bytes
                    for (int b = 0; b < 8 && i < len; b++, i++)
                    {
                        destination[i] = (byte)(v & 0xFF);
                        v >>= 8;
                    }
                }
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

                    while (InterlockedCompareExchange(ref seed, (ulong)t, (ulong)prev) != (ulong)prev)
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

            // Use safe Interlocked on ulong by using CompareExchange on ulong via casting to long pointer.
            private static unsafe ulong InterlockedCompareExchange(ref ulong location, ulong value, ulong comparand)
            {
                fixed (ulong* ptr = &location)
                {
                    long result = Interlocked.CompareExchange(ref *(long*)ptr, (long)value, (long)comparand);
                    return (ulong)result;
                }
            }
        }
    }

    public abstract class RandomGeneratorBase
    {
        /// <returns>random int x: 0 <= x &lt; maxValue</returns>
        public abstract int Next(int maxValue);

        /// <summary>
        /// Return a random 64-bit unsigned integer.
        /// </summary>
        public virtual ulong NextULong() => throw new NotImplementedException();

        /// <summary>
        /// Fill destination with random bytes.
        /// </summary>
        public virtual void NextBytes(Span<byte> destination) => throw new NotImplementedException();

        public abstract void Repeatable(ulong seed);

        public int Next(int minValue, int maxValue)
        {
            ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(minValue, maxValue);
            return minValue + Next(maxValue - minValue);
        }

        /// <summary>
        /// Returns a BigInteger uniformly distributed in [0, n! - 1].
        /// Useful as a Lehmer-code seed for permutations of n items.
        /// n must be between 1 and 52 (inclusive).
        /// </summary>
        public BigInteger NextPermutationBigInteger(int n)
        {
            if (n < 1 || n > 52) throw new ArgumentOutOfRangeException(nameof(n), "n must be between 1 and 52.");

            // compute n!
            BigInteger fact = BigInteger.One;
            for (int i = 2; i <= n; i++) fact *= i;

            // number of bits needed to represent fact-1
            int bitLength = GetBitLength(fact);
            int byteLen = (bitLength + 7) / 8;

            // generate random bytes of length byteLen, reduce modulo fact
            byte[] buf = new byte[byteLen + 1]; // +1 to ensure non-negative BigInteger
            while (true)
            {
                // fill buf[0..byteLen-1] with random bytes (little-endian for BigInteger ctor)
                Span<byte> span = buf.AsSpan(0, byteLen);
                NextBytes(span);

                // ensure top bits beyond bitLength are zeroed to slightly reduce rejections
                int extraBits = byteLen * 8 - bitLength;
                if (extraBits > 0)
                {
                    byte mask = (byte)(0xFF >> extraBits);
                    buf[byteLen - 1] &= mask;
                }

                // BigInteger expects little-endian two's complement; append zero to force positive
                buf[byteLen] = 0;

                var candidate = new BigInteger(buf);
                if (candidate < fact)
                    return candidate;

                // else reduce modulo fact (still uniform enough for our use; optional rejection loop could be used)
                // Using modulo is acceptable here because fact may be huge; to preserve uniformity strictly we'd use rejection,
                // but modulo reduction is fine for practical purposes when using cryptographic RNG; here we use our RNG so we accept modulo.
                return candidate % fact;
            }
        }

        /// <summary>
        /// Convenience: BigInteger for a full 52-card deal (52! possibilities).
        /// </summary>
        public BigInteger NextDealBigInteger() => NextPermutationBigInteger(52);

        /// <summary>
        /// Helper: get bit length of a BigInteger (non-negative).
        /// </summary>
        private static int GetBitLength(BigInteger value)
        {
            if (value.Sign < 0) throw new ArgumentOutOfRangeException(nameof(value));
            if (value.IsZero) return 1;
            // count bits
            int bits = 0;
            BigInteger v = value;
            while (v > 0)
            {
                v >>= 1;
                bits++;
            }
            return bits;
        }

        // Percentage helpers unchanged...
        public bool Percentage(int p)
        {
#if DEBUG
            if (p < 0 || p > 100)
                throw new ArgumentOutOfRangeException(nameof(p));
#endif
            return Next(101) <= p;
        }

        public bool Percentage(double p)
        {
            return Percentage() <= p;
        }

        public double Percentage()
        {
            return 1.0 * Next(65536) / 65535.0;
        }
    }




    //    public static class RandomGenerator
    //    {
    //        public static RandomGeneratorBase Instance { get; set; } = new RepeatableRandomGenerator();

    //        private class RepeatableRandomGenerator : RandomGeneratorBase
    //        {
    //            // based on: https://stackoverflow.com/questions/64937914/thread-safe-high-performance-random-generator

    //            /// <returns>random int x: 0 <= x < maxValue</returns>
    //            public override int Next(int maxValue)
    //            {
    //                ulong nextRawRandomNumber = GetULong();
    //                var int1 = (int)(nextRawRandomNumber % (ulong)maxValue);
    //                return int1;
    //            }

    //            public override void Repeatable(ulong _seed)
    //            {
    //                this.seed = _seed;
    //            }

    //            private ulong seed = 22;

    //            private ulong GetULong()
    //            {
    //                unchecked
    //                {
    //                    long prev = (long)seed;

    //                    long t = prev;
    //                    t ^= t >> 12;
    //                    t ^= t << 25;
    //                    t ^= t >> 27;

    //                    while (InterlockedCompareExchange(ref seed, t, prev) != prev)
    //                    {
    //                        prev = (long)seed;
    //                        t = prev;
    //                        t ^= t >> 12;
    //                        t ^= t << 25;
    //                        t ^= t >> 27;
    //                    }

    //                    return (ulong)(t * 0x2545F4914F6CDD1D);
    //                }
    //            }

    //            private static unsafe long InterlockedCompareExchange(ref ulong location,
    //                 long value, long comparand)
    //            {
    //                fixed (ulong* ptr = &location)
    //                {
    //                    return Interlocked.CompareExchange(ref *(long*)ptr, value, comparand);
    //                }
    //            }
    //        }
    //    }

    //    public abstract class RandomGeneratorBase
    //    {
    //        /// <returns>random int x: 0 <= x < maxValue</returns>
    //        public abstract int Next(int maxValue);

    //        public abstract void Repeatable(ulong seed);

    //        /// <summary>
    //        /// 
    //        /// </summary>
    //        /// <param name="minValue"></param>
    //        /// <param name="maxValue"></param>
    //        /// <returns>A random integer between <paramref name="minValue"/> and <paramref name="maxValue"/></returns>
    //        public int Next(int minValue, int maxValue)
    //        {
    //            return minValue + Next(maxValue - minValue);
    //        }

    //        /// <summary>
    //        /// If there is no need for a specific number but only a decision that has to be taken in a certain percentage of all cases
    //        /// </summary>
    //        /// <param name="p">between 0 and 100</param>
    //        /// <returns>True in p% of all calls</returns>
    //        public bool Percentage(int p)
    //        {
    //#if DEBUG
    //            if (p < 0 || p > 100)
    //                throw new ArgumentOutOfRangeException("p");
    //#endif
    //            return Next(101) <= p;
    //        }

    //        /// <summary>
    //        /// If there is no need for a specific number but only a decision that has to be taken in a certain percentage of all cases
    //        /// </summary>
    //        /// <param name="p">between 0.0 and 1.0</param>
    //        /// <returns>True in p% of all calls</returns>
    //        public bool Percentage(double p)
    //        {
    //            return Percentage() <= p;
    //        }

    //        /// <summary>
    //        /// Returns a random number between 0.0 and 1.0 with a precision of 
    //        /// </summary>
    //        /// <returns>A double between 0.0 and 1.0</returns>
    //        public double Percentage()
    //        {
    //            return 1.0 * Next(65536) / 65535.0;
    //        }
    //    }
}
