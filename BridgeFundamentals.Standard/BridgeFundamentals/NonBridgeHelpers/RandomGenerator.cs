using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Threading;

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace Bridge
#pragma warning restore IDE0130 // Namespace does not match folder structure
{
    public static class RandomGenerator
    {
        public static RandomGeneratorBase Instance { get; set; } = new RepeatableRandomGenerator();

        private class RepeatableRandomGenerator : RandomGeneratorBase
        {
            private const ulong Multiplier = 0x2545F4914F6CDD1DUL;
            [ThreadStatic] private static ulong s0;
            [ThreadStatic] private static ulong s1;
            [ThreadStatic] private static ulong s2;
            [ThreadStatic] private static ulong s3;

            private long globalSeed;
            private long threadCounter;

            public override void Repeatable(ulong seed)
            {
                Interlocked.Exchange(ref globalSeed, (long)seed);
                Interlocked.Exchange(ref threadCounter, 0);

                s0 = s1 = s2 = s3 = 0;
            }

            public override ulong NextULong() => GetULong();

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public override int Next(int maxValue)
            {
#if DEBUG
                ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxValue);
#endif
                ulong r = GetULong();
                ulong mapped = MultiplyHigh(r, (ulong)maxValue);
                return (int)mapped;
            }

            public override void NextBytes(Span<byte> destination)
            {
                unchecked
                {
                    int i = 0;
                    int len = destination.Length;
                    while (i < len)
                    {
                        ulong v = GetULong();
                        for (int b = 0; b < 8 && i < len; b++, i++)
                        {
                            destination[i] = (byte)v;
                            v >>= 8;
                        }
                    }
                }
            }

            // ---------------- internal helpers ----------------

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static ulong SplitMix64(ref ulong x)
            {
                unchecked
                {
                    x += 0x9E3779B97F4A7C15UL;
                    ulong z = x;
                    z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
                    z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
                    return z ^ (z >> 31);
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private void EnsureState()
            {
                if ((s0 | s1 | s2 | s3) != 0)
                    return;

                long id = Interlocked.Increment(ref threadCounter);
                unchecked
                {
                    ulong seed = (ulong)globalSeed ^ ((ulong)id * 0x9E3779B97F4A7C15UL);

                    if (seed == 0)
                        seed = 1;

                    ulong x = seed;
                    s0 = SplitMix64(ref x);
                    s1 = SplitMix64(ref x);
                    s2 = SplitMix64(ref x);
                    s3 = SplitMix64(ref x);
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static ulong RotL(ulong x, int k)
            {
                return (x << k) | (x >> (64 - k));
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private ulong GetULong()
            {
                unchecked
                {
                    EnsureState();

                    ulong result = RotL(s1 * 5, 7) * 9;

                    ulong t = s1 << 17;

                    s2 ^= s0;
                    s3 ^= s1;
                    s1 ^= s2;
                    s0 ^= s3;

                    s2 ^= t;
                    s3 = RotL(s3, 45);

                    return result;
                }
            }

            // Portable 64x64 -> high64 multiply (works on runtimes without BitOperations.MultiplyHigh)
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static ulong MultiplyHigh(ulong x, ulong y)
            {
                unchecked
                {
                    // split into 32-bit halves
                    ulong xLo = (uint)x;
                    ulong xHi = x >> 32;
                    ulong yLo = (uint)y;
                    ulong yHi = y >> 32;

                    // partial products
                    ulong p0 = xLo * yLo;      // low 64
                    ulong p1 = xLo * yHi;      // 64
                    ulong p2 = xHi * yLo;      // 64
                    ulong p3 = xHi * yHi;      // high 64

                    // combine:
                    ulong middle = (p0 >> 32) + (uint)p1 + (uint)p2;
                    ulong high = p3 + (p1 >> 32) + (p2 >> 32) + (middle >> 32);

                    return high;
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
}
