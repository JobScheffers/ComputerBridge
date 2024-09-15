using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;

namespace Bridge.Fundamentals.Benchmark
{
    [MemoryDiagnoser]
    public class Experiment
    {
        private SeatsSuitsRanksArrayOfByte x1 = new SeatsSuitsRanksArrayOfByte();

        public Experiment()
        {
            x1[Seats.East, Suits.Hearts, Ranks.King] = 14;
            x1[Seats.East, Suits.Hearts, Ranks.Jack] = 14;
            x1[Seats.East, Suits.Hearts, Ranks.Five] = 14;
        }

        [Benchmark(Baseline = true)]
		public object E1()
		{
            var h = x1.Highest(Seats.East, Suits.Hearts, 0);
            return h;
        }

        [Benchmark]
		public object E2()
		{
            var h = x1.Highest(Seats.East, Suits.Hearts, 0);
            return h;
        }
    }

	public class Program
    {
        public static void Main(string[] args)
        {
            var summary = BenchmarkRunner.Run<Experiment>();
        }
    }
}
