using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;

namespace Bridge.Fundamentals.Benchmark
{
    [MemoryDiagnoser]
    [DisassemblyDiagnoser(printSource: true)]
    public unsafe class SuitsRanksArrayIntBenchmark
    {
        private SuitsRanksArrayOfInt oldArray;
        private SuitsRanksArray<sbyte> newArray;

        private const int Iterations = 1_000_000;

        [GlobalSetup]
        public void Setup()
        {
            // warm-up initialization
            for (int r = 0; r < 13; r++)
                for (int s = 0; s < 4; s++)
                {
                    oldArray[(Suits)s, (Ranks)r] = 31;
                    newArray[(Suits)s, (Ranks)r] = 31;
                }
        }

        [Benchmark(Baseline = true)]
        public int Old_Get()
        {
            int result = 0;
            for (int i = 0; i < Iterations; i++)
            {
                result ^= oldArray[Suits.Spades, Ranks.Ace];
            }
            return result;
        }

        [Benchmark]
        public int New_Get()
        {
            int result = 0;
            for (int i = 0; i < Iterations; i++)
            {
                result ^= newArray[Suits.Spades, Ranks.Ace];
            }
            return result;
        }

        [Benchmark]
        public void Old_Set()
        {
            for (int i = 0; i < Iterations; i++)
            {
                oldArray[Suits.Spades, Ranks.Ace] = 50;
            }
        }

        [Benchmark]
        public void New_Set()
        {
            for (int i = 0; i < Iterations; i++)
            {
                newArray[Suits.Spades, Ranks.Ace] = 50;
            }
        }
    }

    [MemoryDiagnoser]
    [DisassemblyDiagnoser(printSource: true)]
    public unsafe class TrickArrayBenchmark
    {
        private TrickArrayOfSeats oldArray;
        private TrickArray<Seats> newArray;

        private const int Iterations = 1_000_000;

        [GlobalSetup]
        public void Setup()
        {
            // warm-up initialization
            for (int trick = 1; trick <= 13; trick++)
                for (int man = 1; man <= 4; man++)
                {
                    oldArray[trick, man] = Seats.East;
                    newArray[trick, man] = Seats.East;
                }
        }

        [Benchmark(Baseline = true)]
        public Seats Old_Get()
        {
            Seats result = 0;
            for (int i = 0; i < Iterations; i++)
            {
                result ^= oldArray[2, 3];
            }
            return result;
        }

        [Benchmark]
        public Seats New_Get()
        {
            Seats result = 0;
            for (int i = 0; i < Iterations; i++)
            {
                result ^= newArray[2, 3];
            }
            return result;
        }

        [Benchmark]
        public void Old_Set()
        {
            for (int i = 0; i < Iterations; i++)
            {
                oldArray[3, 1] = Seats.South;
            }
        }

        [Benchmark]
        public void New_Set()
        {
            for (int i = 0; i < Iterations; i++)
            {
                newArray[3, 1] = Seats.South;
            }
        }
    }

    [MemoryDiagnoser]
    [DisassemblyDiagnoser(printSource: true)]
    public unsafe class SuitsRanksArrayBenchmark
    {
        private SuitsRanksArrayOfSeats oldArray;
        private SuitsRanksArray<Seats> newArray;

        private const int Iterations = 1_000_000;

        [GlobalSetup]
        public void Setup()
        {
            // warm-up initialization
            for (int r = 0; r < 13; r++)
                for (int s = 0; s < 4; s++)
                {
                    oldArray[(Suits)s, (Ranks)r] = Seats.East;
                    newArray[(Suits)s, (Ranks)r] = Seats.East;
                }
        }

        [Benchmark(Baseline = true)]
        public Seats Old_Get()
        {
            Seats result = 0;
            for (int i = 0; i < Iterations; i++)
            {
                result ^= oldArray[Suits.Spades, Ranks.Ace];
            }
            return result;
        }

        [Benchmark]
        public Seats New_Get()
        {
            Seats result = 0;
            for (int i = 0; i < Iterations; i++)
            {
                result ^= newArray[Suits.Spades, Ranks.Ace];
            }
            return result;
        }

        [Benchmark]
        public void Old_Set()
        {
            for (int i = 0; i < Iterations; i++)
            {
                oldArray[Suits.Spades, Ranks.Ace] = Seats.South;
            }
        }

        [Benchmark]
        public void New_Set()
        {
            for (int i = 0; i < Iterations; i++)
            {
                newArray[Suits.Spades, Ranks.Ace] = Seats.South;
            }
        }
    }

    [MemoryDiagnoser]
    [DisassemblyDiagnoser(printSource: true)]
    public unsafe class SeatsSuitsArrayBenchmark
    {
        private SeatsSuitsArrayOfByte oldArray;
        private SeatsSuitsArray<sbyte> newArray;

        private const int Iterations = 1_000_000;

        [GlobalSetup]
        public void Setup()
        {
            // warm-up initialization
            for (Seats seat = Seats.North; seat <= Seats.West; seat++)
                for (Suits suit = Suits.Clubs; suit <= Suits.Spades; suit++)
                {
                    oldArray[seat, suit] = 9;
                    newArray[seat, suit] = 9;
                }
        }

        [Benchmark(Baseline = true)]
        public byte Old_Get()
        {
            byte result = 0;
            for (int i = 0; i < Iterations; i++)
            {
                result ^= oldArray[Seats.East, Suits.Spades];
            }
            return result;
        }

        [Benchmark]
        public sbyte New_Get()
        {
            sbyte result = 0;
            for (int i = 0; i < Iterations; i++)
            {
                result ^= newArray[Seats.East, Suits.Spades];
            }
            return result;
        }

        [Benchmark]
        public void Old_Set()
        {
            for (int i = 0; i < Iterations; i++)
            {
                oldArray[Seats.East, Suits.Spades] = 22;
            }
        }

        [Benchmark]
        public void New_Set()
        {
            for (int i = 0; i < Iterations; i++)
            {
                newArray[Seats.East, Suits.Spades] = 22;
            }
        }
    }

    [MemoryDiagnoser]
    public class Experiment
    {
        private PlaySequence target = new PlaySequence(new Contract("1NT", Seats.South, Vulnerable.Neither), 13, Seats.West);

        public Experiment()
        {
        }

        [Benchmark(Baseline = true)]
		public object E1()
		{
            target.Record(Suits.Clubs, Ranks.King, "");
            target.Undo();
            return target;
        }

        [Benchmark]
		public object E2()
		{
            target.Record(Suits.Clubs, Ranks.King, "");
            target.Undo();
            return target;
        }
    }

    public static class Program
    {
        public static void Main()
        {
            //BenchmarkRunner.Run<SuitsRanksArrayBenchmark>();
            //BenchmarkRunner.Run<SuitsRanksArrayIntBenchmark>();
            //BenchmarkRunner.Run<TrickArrayBenchmark>();
            //BenchmarkRunner.Run<SeatsSuitsArrayBenchmark>();
            BenchmarkRunner.Run<AuctionNextKeyWordBenchmark>();
        }
    }
}
