using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using System;
using System.Collections.Generic;
using System.Text;

namespace Bridge.Fundamentals.Benchmark
{
    [MemoryDiagnoser]
    public class Experiment
    {
		[Benchmark]
		public int Closure()
		{
			int x = 2;
			return Execute(() => x * 2);
		}

		[Benchmark]
		public int Action()
		{
			int x = 2;
			return Execute<PollyAction<int, int>, int>(new PollyAction<int, int>(n => n * 2, x));
		}

		private static T Execute<T>(Func<T> action) => action();

		private static TResult Execute<TAction, TResult>(TAction action)
			where TAction : IPollyAction<TResult>
			=> action.Execute();
	}

	public struct PollyAction<T1, TResult> : IPollyAction<TResult>
	{
		private readonly Func<T1, TResult> _action;
		private readonly T1 _arg1;

		public PollyAction(Func<T1, TResult> action, T1 arg1)
		{
			_action = action;
			_arg1 = arg1;
		}

		public TResult Execute() => _action(_arg1);
	}

	public interface IPollyAction<TResult>
	{
		TResult Execute();
	}

	public class Program
    {
        public static void Main(string[] args)
        {
            var summary = BenchmarkRunner.Run<Experiment>();
        }
    }
}
