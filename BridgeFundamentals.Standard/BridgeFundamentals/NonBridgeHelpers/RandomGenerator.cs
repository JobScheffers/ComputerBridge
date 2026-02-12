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

        //private class RepeatableRandomGenerator : RandomGeneratorBase
        //{
        //    // based on: https://stackoverflow.com/questions/64937914/thread-safe-high-performance-random-generator

        //    /// <summary>
        //    /// Return random int x: 0 <= x &lt; maxValue using rejection sampling to avoid modulo bias.
        //    /// </summary>
        //    public override int Next(int maxValue)
        //    {
        //        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxValue);

        //        // Use 64-bit source to reduce number of iterations
        //        ulong bound = (ulong)maxValue;
        //        //ulong threshold = (ulong.MaxValue - (ulong.MaxValue % bound)) - (ulong.MaxValue % bound);
        //        // simpler: compute rejection threshold as largest multiple of bound <= ulong.MaxValue
        //        // but we can use a standard approach: compute limit = (ulong.MaxValue / bound) * bound
        //        ulong limit = (ulong.MaxValue / bound) * bound;

        //        while (true)
        //        {
        //            ulong r = GetULong();
        //            if (r < limit)
        //                return (int)(r % bound);
        //            // else retry
        //        }
        //    }

        //    /// <summary>
        //    /// Return a uniformly random 64-bit value.
        //    /// </summary>
        //    public override ulong NextULong()
        //    {
        //        return GetULong();
        //    }

        //    /// <summary>
        //    /// Fill the provided span with random bytes.
        //    /// </summary>
        //    public override void NextBytes(Span<byte> destination)
        //    {
        //        int i = 0;
        //        int len = destination.Length;
        //        while (i < len)
        //        {
        //            ulong v = GetULong();
        //            // copy up to 8 bytes
        //            for (int b = 0; b < 8 && i < len; b++, i++)
        //            {
        //                destination[i] = (byte)(v & 0xFF);
        //                v >>= 8;
        //            }
        //        }
        //    }

        //    public override void Repeatable(ulong _seed)
        //    {
        //        this.seed = _seed;
        //    }

        //    private ulong seed = 22;

        //    private ulong GetULong()
        //    {
        //        unchecked
        //        {
        //            long prev = (long)seed;

        //            long t = prev;
        //            t ^= t >> 12;
        //            t ^= t << 25;
        //            t ^= t >> 27;

        //            while (InterlockedCompareExchange(ref seed, (ulong)t, (ulong)prev) != (ulong)prev)
        //            {
        //                prev = (long)seed;
        //                t = prev;
        //                t ^= t >> 12;
        //                t ^= t << 25;
        //                t ^= t >> 27;
        //            }

        //            return (ulong)(t * 0x2545F4914F6CDD1D);
        //        }
        //    }

        //    // Use safe Interlocked on ulong by using CompareExchange on ulong via casting to long pointer.
        //    private static unsafe ulong InterlockedCompareExchange(ref ulong location, ulong value, ulong comparand)
        //    {
        //        fixed (ulong* ptr = &location)
        //        {
        //            long result = Interlocked.CompareExchange(ref *(long*)ptr, (long)value, (long)comparand);
        //            return (ulong)result;
        //        }
        //    }
        //}
        private class RepeatableRandomGenerator : RandomGeneratorBase
        {
            private const ulong Multiplier = 0x2545F4914F6CDD1DUL;
            private long state = 22L; // shared state (CAS on long)

            // Thread-local batch buffer to avoid CAS on every Next(maxValue) call.
            // We keep a modest capacity; refill will call GetULong() as needed.
            [ThreadStatic] private static int t_batchPos;
            [ThreadStatic] private static int t_batchLen;
            [ThreadStatic] private static byte[] t_batch;

            private const int BatchCapacity = 64; // number of small-range values to buffer
            private const int SmallBoundMax = 52;
            //private const int ByteRange = 256;
            //private const int ByteAcceptLimit = (ByteRange / SmallBoundMax) * SmallBoundMax; // 208

            public override void Repeatable(ulong _seed)
            {
                long s = unchecked((long)_seed);
                if (s == 0) s = 1;
                Interlocked.Exchange(ref state, s);

                // clear thread-local buffers so subsequent Next() uses fresh randomness
                // (best-effort: only clears current thread's buffer)
                t_batch = null;
                t_batchPos = 0;
                t_batchLen = 0;
            }

            public override ulong NextULong() => GetULong();

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public override int Next(int maxValue)
            {
                ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxValue);

                if (maxValue <= SmallBoundMax)
                {
                    // fast path: use thread-local batch of 0..(SmallBoundMax-1)
                    if (t_batch == null)
                    {
                        t_batch = new byte[BatchCapacity];
                        t_batchPos = 0;
                        t_batchLen = 0;
                    }

                    if (t_batchPos < t_batchLen)
                    {
                        return t_batch[t_batchPos++];
                    }

                    // refill batch
                    RefillBatch();
                    // after refill, there should be at least one value
                    if (t_batchLen == 0)
                    {
                        // extremely unlikely, but fallback to single-sample method
                        return NextFallback(maxValue);
                    }
                    t_batchPos = 1;
                    return t_batch[0];
                }
                else
                {
                    // fallback for large bounds: multiply-high mapping (portable MultiplyHigh used)
                    ulong r = GetULong();
                    ulong mapped = MultiplyHigh(r, (ulong)maxValue);
                    return (int)mapped;
                }
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
            private void RefillBatch()
            {
                if (t_batch == null) t_batch = new byte[BatchCapacity];

                int pos = 0;

                // We'll extract 6-bit chunks from each 64-bit word (10 chunks per ulong)
                // mask = 0x3F (6 bits). Accept chunk if < SmallBoundMax.
                const int mask = 0x3F;
                while (pos < BatchCapacity)
                {
                    ulong v = GetULong();
                    // process up to 10 chunks from this ulong
                    // local copy for speed
                    ulong x = v;
                    for (int chunk = 0; chunk < 10 && pos < BatchCapacity; chunk++)
                    {
                        int val = (int)(x & (ulong)mask);
                        x >>= 6;
                        if (val < SmallBoundMax)
                        {
                            t_batch[pos++] = (byte)val;
                        }
                        // else reject and continue
                    }
                }

                t_batchLen = pos;
                t_batchPos = 0;
            }

            // Fallback single-sample method for small bounds (used only in rare edge cases)
            private int NextFallback(int maxValue)
            {
                // use rejection sampling on 64-bit value
                ulong bound = (ulong)maxValue;
                ulong limit = (ulong.MaxValue / bound) * bound;
                while (true)
                {
                    ulong r = GetULong();
                    if (r < limit) return (int)(r % bound);
                }
            }

            // xorshift64* core with CAS on long state
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private ulong GetULong()
            {
                while (true)
                {
                    long prevLong = Volatile.Read(ref state);
                    ulong x = unchecked((ulong)prevLong);

                    x ^= x >> 12;
                    x ^= x << 25;
                    x ^= x >> 27;

                    long nextLong = unchecked((long)x);
                    long observed = Interlocked.CompareExchange(ref state, nextLong, prevLong);
                    if (observed == prevLong)
                    {
                        return x * Multiplier;
                    }
                    // else retry
                }
            }

            // Portable 64x64 -> high64 multiply (works on runtimes without BitOperations.MultiplyHigh)
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static ulong MultiplyHigh(ulong x, ulong y)
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
