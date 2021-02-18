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
		[Benchmark]
		public bool E1()
		{
			int x = 2;
			return SuitHelper.AnySuit(s => x == (int)s);
		}

		[Benchmark]
		public bool E2()
		{
			int x = 2;
			return x == (int)Suits.Clubs || x == (int)Suits.Diamonds || x == (int)Suits.Hearts || x == (int)Suits.Spades;
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
