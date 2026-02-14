using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using System;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Bridge.Fundamentals.Benchmark
{

    public class RgXoshiro : RandomGeneratorBase
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ulong SplitMix64(ref ulong x)
        {
            x += 0x9E3779B97F4A7C15UL;
            ulong z = x;
            z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
            z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
            return z ^ (z >> 31);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void EnsureState()
        {
            if ((s0 | s1 | s2 | s3) != 0)
                return;

            long id = Interlocked.Increment(ref threadCounter);
            ulong seed = (ulong)globalSeed ^ ((ulong)id * 0x9E3779B97F4A7C15UL);

            if (seed == 0)
                seed = 1;

            ulong x = seed;
            s0 = SplitMix64(ref x);
            s1 = SplitMix64(ref x);
            s2 = SplitMix64(ref x);
            s3 = SplitMix64(ref x);
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

    public class RepeatableRandomGenerator : RandomGeneratorBase
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

    [MemoryDiagnoser]
    public class RepeatableRandomGeneratorBenchmarks
    {
        private RepeatableRandomGenerator original;
        private RgXoshiro optimized;

        [Params(52, 1000)]
        public int MaxValue;

        [GlobalSetup]
        public void Setup()
        {
            original = new RepeatableRandomGenerator();
            optimized = new RgXoshiro();

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
