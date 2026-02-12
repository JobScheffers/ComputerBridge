using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using System;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Bridge.Fundamentals.Benchmark
{

    public class RepeatableRandomGenerator : RandomGeneratorBase
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

    // Batched RNG optimized for small bounds (<= 52) common in card games.
    // Uses a thread-local refill buffer of precomputed 0..51 values extracted
    // from 64-bit random words with per-byte rejection sampling (256 -> 208).
    // Falls back to multiply-high mapping for larger bounds.
    public class RepeatableRandomGeneratorBatched : RandomGeneratorBase
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
        private const int ByteRange = 256;
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

    [MemoryDiagnoser]
    public class RepeatableRandomGeneratorBenchmarks
    {
        private RepeatableRandomGenerator original;
        private RepeatableRandomGeneratorBatched optimized;

        [Params(52, 100)]
        public int MaxValue;

        [GlobalSetup]
        public void Setup()
        {
            original = new RepeatableRandomGenerator();
            optimized = new RepeatableRandomGeneratorBatched();

            // use same seed for both
            ulong seed = 0xDEADBEEFCAFEBABEUL;
            original.Repeatable(seed);
            optimized.Repeatable(seed);
        }

        [Benchmark(Baseline = true)]
        public int Original_Next()
        {
            // call Next repeatedly to amortize overhead in the benchmark harness
            int sum = 0;
            for (int i = 0; i < 1000; i++)
                sum += original.Next(MaxValue);
            return sum;
        }

        [Benchmark]
        public int Optimized_Next()
        {
            int sum = 0;
            for (int i = 0; i < 1000; i++)
                sum += optimized.Next(MaxValue);
            return sum;
        }
    }

    public static class Program
    {
        public static void Main()
        {
            BenchmarkRunner.Run<RepeatableRandomGeneratorBenchmarks>();
        }
    }
}
