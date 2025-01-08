using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;

namespace Bridge.Fundamentals.Benchmark
{
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
            target.Record(Suits.Clubs, Ranks.King);
            target.Undo();
            return target;
        }

        [Benchmark]
		public object E2()
		{
            target.Record(Suits.Clubs, Ranks.King);
            target.Undo();
            return target;
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
