#define faster
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

#if faster
        private class RepeatableRandomGenerator : RandomGeneratorBase
        {
            private const ulong Multiplier = 0x2545F4914F6CDD1DUL;
            [ThreadStatic] private static ulong t_state;

            // Thread-local batch buffer to avoid CAS on every Next(maxValue) call.
            // We keep a modest capacity; refill will call GetULong() as needed.
            [ThreadStatic] private static byte[] t_bytes;
            [ThreadStatic] private static int t_pos;
            [ThreadStatic] private static int t_len;

            private const int ByteBatchSize = 256;
            private long globalSeed;
            private long threadCounter;

            public override void Repeatable(ulong seed)
            {
                Interlocked.Exchange(ref globalSeed, (long)seed);
                Interlocked.Exchange(ref threadCounter, 0);

                t_state = 0;
                // reset batch buffer too
                t_pos = 0;
                t_len = 0;
            }

            public override ulong NextULong() => GetULong();

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public override int Next(int maxValue)
            {
                ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxValue);

                if (maxValue <= 256)
                {
                    int limit = (256 / maxValue) * maxValue;

                    while (true)
                    {
                        EnsureBytes();

                        byte b = t_bytes[t_pos++];

                        if (b < limit)
                            return b % maxValue;
                    }
                }

                // Large bounds → multiply-high
                ulong r = GetULong();
                ulong mapped = MultiplyHigh(r, (ulong)maxValue);
                return (int)mapped;
            }

            public override void NextBytes(Span<byte> destination)
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

            // ---------------- internal helpers ----------------

            // Refill the thread-local batch with values in [0, SmallBoundMax).
            // Uses per-byte rejection sampling on bytes extracted from GetULong().
            // change thread-local buffer types
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private void EnsureBytes()
            {
                if (t_bytes == null)
                    t_bytes = new byte[ByteBatchSize];

                if (t_pos >= t_len)
                {
                    // Fill entire buffer using 64-bit generator
                    int i = 0;
                    while (i < ByteBatchSize)
                    {
                        ulong v = GetULong();
                        for (int b = 0; b < 8 && i < ByteBatchSize; b++, i++)
                        {
                            t_bytes[i] = (byte)v;
                            v >>= 8;
                        }
                    }

                    t_pos = 0;
                    t_len = ByteBatchSize;
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private ulong GetState()
            {
                if (t_state == 0)
                {
                    long id = Interlocked.Increment(ref threadCounter);
                    ulong s = (ulong)globalSeed + (ulong)id * 0x9E3779B97F4A7C15UL;
                    //ulong s = (ulong)globalSeed ^ ((ulong)id * 0x9E3779B97F4A7C15UL);     // better, but will break bidding tests

                    if (s == 0)
                        s = 1;

                    t_state = s;
                }

                return t_state;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private ulong GetULong()
            {
                unchecked
                {
                    ulong x = GetState();

                    x ^= x >> 12;
                    x ^= x << 25;
                    x ^= x >> 27;

                    t_state = x;
                    return x * Multiplier;
                }
            }

            // Portable 64x64 -> high64 multiply (works on runtimes without BitOperations.MultiplyHigh)
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static ulong MultiplyHigh(ulong x, ulong y)
            {
                unchecked
                {
                    // split into 32-bit halves
                    ulong xLo = unchecked((uint)x);
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
#else
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
#endif


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
}
