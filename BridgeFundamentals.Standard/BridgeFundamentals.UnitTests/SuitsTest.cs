using Microsoft.VisualStudio.TestTools.UnitTesting;
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

            // test if it is possible to iterate over the trump suits in reverse order
            for (Suits suit = Suits.NoTrump; suit >= Suits.Clubs; suit--)
            {
                Assert.AreEqual(suit, SuitHelper.TrumpSuitsDescending[^count]);
                count--;
            }
        }
    }
}
