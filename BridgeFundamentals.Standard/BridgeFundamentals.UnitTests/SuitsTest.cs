using Bridge.Test.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Diagnostics;
using System.Globalization;
using System.Threading;

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace Bridge.Test
#pragma warning restore IDE0130 // Namespace does not match folder structure
{
    [TestClass]
    public class SuitsTest
    {
        [TestMethod]
        public void Suits_ToString()
        {
            Thread.CurrentThread.CurrentUICulture = new CultureInfo("en");
            Assert.AreEqual<string>("Hearts", (Suits.Hearts).ToLocalizedString());
            Thread.CurrentThread.CurrentUICulture = new CultureInfo("nl");
            Assert.AreEqual<string>("Harten", Suits.Hearts.ToLocalizedString());
        }

        [TestMethod]
        public void SuitHelper_Next()
        {
            Assert.AreEqual(Suits.Diamonds, SuitHelper.Next(Suits.Clubs));
            Assert.AreEqual(Suits.Clubs, SuitHelper.Next(Suits.Spades));
        }

        [TestMethod]
        public void SuitHelper_TrumpSuitsAscending()
        {
            int count = 0;
            foreach (var suit in SuitHelper.TrumpSuitsAscending)
            {
                count++;
            }
            Assert.AreEqual(5, count);
        }

        [TestMethod]
        public void SuitHelper_TrumpSuitsDescending()
        {
            int count = 0;
            foreach (var suit in SuitHelper.TrumpSuitsDescending)
            {
                count++;
            }
            Assert.AreEqual(5, count);
        }

        //[TestMethod]
        /// research how much faster generic collections are compared to specific collections and how much faster array access is using int as indexer compared to suits and ranks as indexer
        /// 2017: 
        /// release build is 3x faster than debug build
        /// in release build byte is 2% faster than int
        public void SuitRankCollection_Performs()
        {
            int loopSize = 10000000;
            int newValue = 13;

            var target3 = new SuitRankCollection<byte>();
            byte newValue3 = 13;
            // warm-up
            for (int i = 0; i < 100; i++)
            {
                foreach (Suits s in SuitHelper.StandardSuitsAscending)
                {
                    foreach (Ranks r in RankHelper.RanksAscending)
                    {
                        target3[s, r] = newValue3;
                    }
                }
            }

            var t7 = ElapsedTime.Do(() =>
            {
                foreach (Suits s in SuitHelper.StandardSuitsAscending)
                {
                    foreach (Ranks r in RankHelper.RanksAscending)
                    {
                        target3[s, r] = newValue3;
                    }
                }
                foreach (Suits s in SuitHelper.StandardSuitsAscending)
                {
                    foreach (Ranks r in RankHelper.RanksAscending)
                    {
                        var y = target3[s, r];
                    }
                }
            }, loopSize);
            Trace2("SuitRankCollection<byte>: read/write[suit,rank] : {0}", t7);

            var t8 = ElapsedTime.Do(() =>
            {
                for (int s = 0; s <= 3; s++)
                {
                    for (int r = 0; r <= 12; r++)
                    {
                        target3[s, r] = newValue3;
                    }
                }
                for (int s = 0; s <= 3; s++)
                {
                    for (int r = 0; r <= 12; r++)
                    {
                        var y = target3[s, r];
                    }
                }
            }, loopSize);
            Trace2("SuitRankCollection<byte>: read/write[int ,int ] : {0}", t8);

            var t9 = ElapsedTime.Do(() =>
            {
                var x = target3.Clone();
            }, loopSize);
            Trace2("SuitRankCollection<byte>: Clone                 : {0}", t9);

            var target2 = new SuitRankCollection<int>();
            // warm-up
            for (int i = 0; i < 100; i++)
            {
                foreach (Suits s in SuitHelper.StandardSuitsAscending)
                {
                    foreach (Ranks r in RankHelper.RanksAscending)
                    {
                        target2[s, r] = newValue;
                    }
                }
            }

            var t4 = ElapsedTime.Do(() =>
            {
                foreach (Suits s in SuitHelper.StandardSuitsAscending)
                {
                    foreach (Ranks r in RankHelper.RanksAscending)
                    {
                        target2[s, r] = newValue;
                    }
                }
                foreach (Suits s in SuitHelper.StandardSuitsAscending)
                {
                    foreach (Ranks r in RankHelper.RanksAscending)
                    {
                        var y = target2[s, r];
                    }
                }
            }, loopSize);
            Trace2("SuitRankCollection<int> : read/write[suit,rank] : {0}", t4);

            var t5 = ElapsedTime.Do(() =>
            {
                for (int s = 0; s <= 3; s++)
                {
                    for (int r = 0; r <= 12; r++)
                    {
                        target2[s, r] = newValue;
                    }
                }
                for (int s = 0; s <= 3; s++)
                {
                    for (int r = 0; r <= 12; r++)
                    {
                        var y = target2[s, r];
                    }
                }
            }, loopSize);
            Trace2("SuitRankCollection<int> : read/write[int ,int ] : {0}", t5);

            var t10 = ElapsedTime.Do(() =>
            {
                for (int s = 0; s <= 3; s++)
                {
                    int s13 = 13 * s;
                    for (int r = 0; r <= 12; r++)
                    {
                        target2[s13 + r] = newValue;
                    }
                }
                for (int s = 0; s <= 3; s++)
                {
                    int s13 = 13 * s;
                    for (int r = 0; r <= 12; r++)
                    {
                        var y = target2[s13 + r];
                    }
                }
            }, loopSize);
            Trace2("SuitRankCollection<int> : read/write[int      ] : {0}", t10);

            var t6 = ElapsedTime.Do(() =>
            {
                var x = target2.Clone();
            }, loopSize);
            Trace2("SuitRankCollection<int> : Clone                 : {0}", t6);
        }

        //[TestMethod]
        /// research how much faster int calculations are compared to byte calculations
        /// 2017: 
        /// release build is 3x faster than debug build
        /// in release build byte is 2% faster than int
        public void IntCalculations_Performs()
        {
            int loopSize = 100000000;

            var t1 = ElapsedTime.Do(() =>
            {
                int x = 34;
                int y = 5;
                int z = 13;
                int a = x * y + z;
            }, loopSize);
            Log.Trace(0, "int: {0}", t1);

            var t2 = ElapsedTime.Do(() =>
            {
                byte x = 34;
                byte y = 5;
                byte z = 13;
                byte a = (byte)(x * y + z);
            }, loopSize);
            Log.Trace(0, "byte: {0}", t2);
            Assert.IsLessThan(t2, t1);
        }

        private void Trace2(string format, params object[] args)
        {
            Trace.WriteLine(string.Format(format, args));
        }
    }
}
