using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using System;
using System.Collections.Generic;
using System.Text;
using Bridge;

namespace Bridge.Fundamentals.Benchmark
{
    [MemoryDiagnoser]
    public class Experiment
    {
        private SuitsRanksArrayOfRanks x1 = new SuitsRanksArrayOfRanks();

        [Benchmark(Baseline = true)]
		public object E1()
		{
            x1.Fill(Ranks.Queen);
            return x1;
        }

        [Benchmark]
		public object E2()
		{
            x1.Fill2(Ranks.Ten);
            return x1;
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
