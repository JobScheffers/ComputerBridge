using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;

namespace Bridge.Fundamentals.Benchmark
{

    [MemoryDiagnoser]
    [DisassemblyDiagnoser(printSource: true)]
    public unsafe class SuitsRanksArrayBenchmark
    {
        private SuitsRanksArrayOfSeats oldArray;
        private SuitsRanksArrayOfRanks newArray;

        private const int Iterations = 1_000_000;

        [GlobalSetup]
        public void Setup()
        {
            // warm-up initialization
            for (int r = 0; r < 13; r++)
                for (int s = 0; s < 4; s++)
                {
                    oldArray[(Suits)s, (Ranks)r] = Seats.East;
                    newArray[(Suits)s, (Ranks)r] = Ranks.King;
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
        public Ranks New_Get()
        {
            Ranks result = 0;
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
                newArray[Suits.Spades, Ranks.Ace] = Ranks.Jack;
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
            BenchmarkRunner.Run<SuitsRanksArrayBenchmark>();
        }
    }
}
