using Bridge;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bridge.Test
{
    [TestClass]
    public class BidTest
    {
        [TestMethod, TestCategory("CI"), TestCategory("Bid")]
        public void Bid_CompareTest()
        {
            Bid b1 = new Bid(0, Suits.Diamonds);
            Bid b2 = Bid.C("Pass");
            Assert.IsTrue(b1 < b2);
            Assert.IsFalse(Bid.C("Pass") < "Pass");
            Assert.IsTrue(Bid.C("Pass") <= "Pass");
            Assert.IsFalse(Bid.C("Pass") > "Pass");
            Assert.IsTrue(Bid.C("Pass") >= "Pass");
            Assert.IsTrue(Bid.C("Pass") == Bid.C("Pass"));
            Assert.IsFalse(Bid.C("1C") < Bid.C("1C"));
            Assert.IsTrue(Bid.C("1C") < Bid.C("1D"));
            Assert.IsTrue(Bid.C("Pass") < Bid.C("1C"));
        }

        [TestMethod, TestCategory("CI"), TestCategory("Bid")]
        public void Bid_ToXml_BackwardCompatible()
        {
            Bid b2;
            b2 = Bid.C("p");
            Assert.AreEqual("Pass", b2.ToXML());
            b2 = Bid.C("x");
            Assert.AreEqual("X", b2.ToXML());
            b2 = Bid.C("xx");
            Assert.AreEqual("XX", b2.ToXML());
            b2 = Bid.C("1C");
            Assert.AreEqual("1C", b2.ToXML());
            b2 = Bid.C("1D");
            Assert.AreEqual("1D", b2.ToXML());
            b2 = Bid.C("1H");
            Assert.AreEqual("1H", b2.ToXML());
            b2 = Bid.C("1S!S5");
            Assert.AreEqual("1S!", b2.ToXML());
            Assert.AreEqual("S5", b2.Explanation);
            b2 = Bid.C("1NT;;");
            Assert.AreEqual("1NT", b2.ToXML());
        }

        [TestMethod, TestCategory("CI"), TestCategory("Bid")]
        public void Bid_ToSymbol()
        {
            System.Threading.Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("en-US");
            System.Threading.Thread.CurrentThread.CurrentUICulture = new System.Globalization.CultureInfo("en-US");
            Bid b2;
            b2 = Bid.C("p");
            Assert.AreEqual("Pass", b2.ToSymbol());
            b2 = Bid.C("x");
            Assert.AreEqual("x", b2.ToSymbol());
            b2 = Bid.C("xx");
            Assert.AreEqual("xx", b2.ToSymbol());
            b2 = Bid.C("1C");
            Assert.AreEqual("1♣", b2.ToSymbol());
            b2 = Bid.C("1D");
            Assert.AreEqual("1♦", b2.ToSymbol());
            b2 = Bid.C("1H");
            Assert.AreEqual("1♥", b2.ToSymbol());
            b2 = Bid.C("1S");
            Assert.AreEqual("1♠", b2.ToSymbol());
            b2 = Bid.C("1NT");
            Assert.AreEqual("1NT", b2.ToSymbol());
        }

        [TestMethod, TestCategory("CI"), TestCategory("Bid")]
        public void Bid_ToString()
        {
            Bid b2;
            b2 = Bid.C("p");
            Assert.AreEqual("Pass", b2.ToString());
            b2 = Bid.C("x");
            Assert.AreEqual("x", b2.ToString());
            b2 = Bid.C("xx");
            Assert.AreEqual("xx", b2.ToString());
            b2 = Bid.C("1C");
            Assert.AreEqual("1C", b2.ToString());
            b2 = Bid.C("1D");
            Assert.AreEqual("1D", b2.ToString());
            b2 = Bid.C("1H");
            Assert.AreEqual("1H", b2.ToString());
            b2 = Bid.C("1S");
            Assert.AreEqual("1S", b2.ToString());
            b2 = Bid.C("1NT");
            Assert.AreEqual("1NT", b2.ToString());
        }
    }
}
