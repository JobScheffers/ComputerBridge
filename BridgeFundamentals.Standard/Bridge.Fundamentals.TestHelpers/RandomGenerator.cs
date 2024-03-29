using System;
using System.Threading;

namespace Bridge.Test.Helpers
{
    /// <summary>
    /// Repeatable sequences for test purposes
    /// </summary>
    public class TestRandomGenerator : RandomGeneratorBase
    {
        private static ThreadLocal<Random> instance = new ThreadLocal<Random>(() => new Random(0));

        protected override int Roll(int maxValue)
        {
            return instance.Value.Next(maxValue);
        }

        public void ResetSeed()
        {
            instance.Value = new Random(0);
        }

        protected override int TypeMaximun(int maxValue)
        {
            return int.MaxValue;
        }
    }
}
