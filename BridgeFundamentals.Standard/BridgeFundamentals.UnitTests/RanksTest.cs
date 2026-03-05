using Microsoft.VisualStudio.TestTools.UnitTesting;

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace Bridge.Test
#pragma warning restore IDE0130 // Namespace does not match folder structure
{
    [TestClass]
    public class RanksTest
    {
        [TestMethod]
        public void Ranks_LoopDescending()
        {
            int count = 0;
            // test if it is possible to iterate in descending order
            for (Ranks rank = Ranks.Ace; rank >= Ranks.Two; rank--)
            {
                count++;
            }

            Assert.AreEqual(13, count);
        }

        [TestMethod]
        public void Ranks_HCP()
        {
            Assert.AreEqual(4, RankHelper.HCP(Ranks.Ace));
            Assert.AreEqual(3, RankHelper.HCP(Ranks.King));
            Assert.AreEqual(2, RankHelper.HCP(Ranks.Queen));
            Assert.AreEqual(1, RankHelper.HCP(Ranks.Jack));
            Assert.AreEqual(0, RankHelper.HCP(Ranks.Ten));
            Assert.AreEqual(0, RankHelper.HCP(Ranks.Nine));
            Assert.AreEqual(0, RankHelper.HCP(Ranks.Eight));
            Assert.AreEqual(0, RankHelper.HCP(Ranks.Seven));
            Assert.AreEqual(0, RankHelper.HCP(Ranks.Six));
            Assert.AreEqual(0, RankHelper.HCP(Ranks.Five));
            Assert.AreEqual(0, RankHelper.HCP(Ranks.Four));
            Assert.AreEqual(0, RankHelper.HCP(Ranks.Three));
            Assert.AreEqual(0, RankHelper.HCP(Ranks.Two));
        }
    }
}
