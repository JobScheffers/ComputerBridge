using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Globalization;
using System.Threading;

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace Bridge.Test
#pragma warning restore IDE0130 // Namespace does not match folder structure
{
    [TestClass]
    public class RanksTest
    {
        [TestMethod]
        public void SuitHelper_TrumpSuitsDescending()
        {
            int count = 0;
            // test if it is possible to iterate in descending order
            for (Ranks rank = Ranks.Ace; rank >= Ranks.Two; rank--)
            {
                count++;
            }

            Assert.AreEqual(13, count);
        }
    }
}
