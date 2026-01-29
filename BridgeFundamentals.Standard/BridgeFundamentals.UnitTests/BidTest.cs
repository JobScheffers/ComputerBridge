using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace Bridge.Test
#pragma warning restore IDE0130 // Namespace does not match folder structure
{
    [TestClass]
    public class BidTest
    {
        [TestMethod, TestCategory("CI"), TestCategory("Bid")]
        public void Bid_NullCompare()
        {
            var target1 = Bid.Get(1, Suits.Clubs);
            Assert.IsFalse(target1 == null);
            Bid target2 = null;
            Assert.IsTrue(target2 == null);
            var target3 = Bid.Get(1, Suits.Clubs);
            Assert.IsTrue(target1 == target3);
            var target4 = Bid.Get(2, Suits.Clubs);
            Assert.IsFalse(target1 == target4);
        }

        [TestMethod, TestCategory("CI"), TestCategory("Bid")]
        public void BidSet_Test()
        {
            var target = new BiedingenSet();
            Assert.IsFalse(target.BevatDoublet("a"));
            Assert.IsTrue(target.BevatDoublet("a"));
            Assert.IsFalse(target.BevatHK(1, Suits.Spades, "b"));
            Assert.IsTrue(target.BevatHK(1, Suits.Spades, "b"));
            Assert.AreEqual("[1S, b] [x, a] ", target.ToString());
        }

        [TestMethod, TestCategory("CI"), TestCategory("Bid")]
        public void BidSet_InvalidBid()
        {
            var target = new BiedingenSet();
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                target.Bevat(38, "a");
            });
        }

        [TestMethod, TestCategory("CI"), TestCategory("Bid")]
        public void Bid_CompareTest()
        {
            var bid1C1 = Bid.Parse("1C");
            var bid1C2 = Bid.Parse("1C");
            var bid1D = Bid.Parse("1D");
            var bidPass1 = Bid.Parse("Pass");
            var bidPass2 = Bid.Parse("Pass");
            Assert.IsFalse(bidPass1 < bidPass2);
            Assert.IsTrue(bidPass1 <= bidPass2);
            Assert.IsFalse(bidPass1 > bidPass2);
            Assert.IsTrue(bidPass1 >= bidPass2);
            Assert.IsTrue(bidPass1 == bidPass2);
            Assert.IsTrue(bidPass2 == bidPass1);
            Assert.IsFalse(bidPass1 == bid1C1);
            Assert.IsFalse(bid1C1 == bidPass1);
            Assert.IsFalse(bid1C1 < bid1C2);
            Assert.IsFalse(bid1D < bid1C1);
            Assert.IsTrue(bid1C1 <= bid1C2);
            Assert.IsTrue(bid1C1 >= bid1C2);
            Assert.IsTrue(bid1D > bid1C1);
            Assert.IsFalse(bid1D < bid1C1);
            Assert.IsTrue(bidPass1 < bid1C1);
        }

        [TestMethod, TestCategory("CI"), TestCategory("Bid")]
        public void Bid_GetTest()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                Bid b1 = Bid.Get(0, Suits.Diamonds);
            });
        }

        [TestMethod, TestCategory("CI"), TestCategory("Bid")]
        public void Bid_ToXml_BackwardCompatible()
        {
            AuctionBid b2;
            b2 = AuctionBid.Parse("p");
            Assert.AreEqual("Pass", b2.ToXML());
            b2 = AuctionBid.Parse("x");
            Assert.AreEqual("X", b2.ToXML());
            b2 = AuctionBid.Parse("xx");
            Assert.AreEqual("XX", b2.ToXML());
            b2 = AuctionBid.Parse("1C");
            Assert.AreEqual("1C", b2.ToXML());
            b2 = AuctionBid.Parse("1D");
            Assert.AreEqual("1D", b2.ToXML());
            b2 = AuctionBid.Parse("1H");
            Assert.AreEqual("1H", b2.ToXML());
            Assert.IsFalse(b2.Alert);
            b2 = AuctionBid.Parse("1S!S5");
            Assert.AreEqual("1S", b2.ToXML());
            Assert.IsTrue(b2.Alert);
            Assert.AreEqual("S5", b2.Explanation);
            b2 = AuctionBid.Parse("1NT;;");
            Assert.AreEqual("1NT", b2.ToXML());

            b2 = AuctionBid.Parse("1S!(!(ps0911*pa0012*D8*o1D*!H4*!S4*r23D5))*(!(ps0911*pa0012*C8*o1C*!H4*!S4*r23C5))*(!(pS1019*pa0612*S7*o7S3*r23S4))*(!(pH1019*pa0612*H7*o7H3*r23H4))*(!(pD1019*pa0712*D7*o7D1*!H4*!S4*r23D4))*(!(pC1019*pa0712*C7*o7C1*!H4*!S4*r23C4))*(!(pS0814*pa0510*(pa0509+th3)*S7*o7S1*!H4*r23S3))*(!(pH0814*pa0512*(pa0509+th3)*H7*o7H0*!S4*r23H3))*(!(pD0714*pa0011*D6*(D7+o7D6*(!S3+!H3))*o7D1*!H4*!S4*r23D3))*(!(pC0714*pa0011*C6*(C7+o7C6*(!S3+!H3))*o7C1*!H4*!S4*r23C3))*(!(S5*!S8*!>HS*o7S1*(!u+S6*(o7S3+th3+pS1011+pa0009))*r23S2*pa0011*pS0413*(pa0010+th3)*!(pS1213*pa1021*(H5+D5+C5+S7))*!(pa1115*=S5*H3*(D4+C4))*!(pa1019*S6*(H4+D4+C4))*!(vS6*pa1240)))*(!(H5*!H8*>HS*o7H1*(!u+H6*(o7H3+th3+pH1011+pa0009))*r23H2*pa0011*pH0413*(pa0010+th3)*!(pH1213*pa1021*(S5+D5+C5+H7))*!(pa1019*H6*(S4+D4+C4))*!(vH6*pa1240)))*(!(D5*!S5*!H5*(!u+D6*o7D1*(o7D3+th3))*r23D2*pa0311*pD0413*(pa0010+th3)))*(!(pN2022*S2*!S6*H2*!H6*D1*(D2+o7D3*C3*!C6*H3*!H5*S3*!S5)*!D7*C1*(C2+o7C3*D3*!D6*H3*!H5*S3*!S5)*!C7*!(S5*(H4*!t07+D5+C5))*!(H5*(S4*!t07+D5+C5))*!(pa2240*(S5+H5))))*(pN1517*pa1418*t07*(t01+th4+th3))");
            Assert.AreEqual("1S", b2.ToXML());
            Assert.IsTrue(b2.Alert);
            Assert.AreEqual("(!(ps0911*pa0012*D8*o1D*!H4*!S4*r23D5))*(!(ps0911*pa0012*C8*o1C*!H4*!S4*r23C5))*(!(pS1019*pa0612*S7*o7S3*r23S4))*(!(pH1019*pa0612*H7*o7H3*r23H4))*(!(pD1019*pa0712*D7*o7D1*!H4*!S4*r23D4))*(!(pC1019*pa0712*C7*o7C1*!H4*!S4*r23C4))*(!(pS0814*pa0510*(pa0509+th3)*S7*o7S1*!H4*r23S3))*(!(pH0814*pa0512*(pa0509+th3)*H7*o7H0*!S4*r23H3))*(!(pD0714*pa0011*D6*(D7+o7D6*(!S3+!H3))*o7D1*!H4*!S4*r23D3))*(!(pC0714*pa0011*C6*(C7+o7C6*(!S3+!H3))*o7C1*!H4*!S4*r23C3))*(!(S5*!S8*!>HS*o7S1*(!u+S6*(o7S3+th3+pS1011+pa0009))*r23S2*pa0011*pS0413*(pa0010+th3)*!(pS1213*pa1021*(H5+D5+C5+S7))*!(pa1115*=S5*H3*(D4+C4))*!(pa1019*S6*(H4+D4+C4))*!(vS6*pa1240)))*(!(H5*!H8*>HS*o7H1*(!u+H6*(o7H3+th3+pH1011+pa0009))*r23H2*pa0011*pH0413*(pa0010+th3)*!(pH1213*pa1021*(S5+D5+C5+H7))*!(pa1019*H6*(S4+D4+C4))*!(vH6*pa1240)))*(!(D5*!S5*!H5*(!u+D6*o7D1*(o7D3+th3))*r23D2*pa0311*pD0413*(pa0010+th3)))*(!(pN2022*S2*!S6*H2*!H6*D1*(D2+o7D3*C3*!C6*H3*!H5*S3*!S5)*!D7*C1*(C2+o7C3*D3*!D6*H3*!H5*S3*!S5)*!C7*!(S5*(H4*!t07+D5+C5))*!(H5*(S4*!t07+D5+C5))*!(pa2240*(S5+H5))))*(pN1517*pa1418*t07*(t01+th4+th3))", b2.Explanation);

            b2 = AuctionBid.Parse("1S?(!(ps0911*pa0012*D8*o1D*!H4*!S4*r23D5))*(!(ps0911*pa0012*C8*o1C*!H4*!S4*r23C5))*(!(pS1019*pa0612*S7*o7S3*r23S4))*(!(pH1019*pa0612*H7*o7H3*r23H4))*(!(pD1019*pa0712*D7*o7D1*!H4*!S4*r23D4))*(!(pC1019*pa0712*C7*o7C1*!H4*!S4*r23C4))*(!(pS0814*pa0510*(pa0509+th3)*S7*o7S1*!H4*r23S3))*(!(pH0814*pa0512*(pa0509+th3)*H7*o7H0*!S4*r23H3))*(!(pD0714*pa0011*D6*(D7+o7D6*(!S3+!H3))*o7D1*!H4*!S4*r23D3))*(!(pC0714*pa0011*C6*(C7+o7C6*(!S3+!H3))*o7C1*!H4*!S4*r23C3))*(!(S5*!S8*!>HS*o7S1*(!u+S6*(o7S3+th3+pS1011+pa0009))*r23S2*pa0011*pS0413*(pa0010+th3)*!(pS1213*pa1021*(H5+D5+C5+S7))*!(pa1115*=S5*H3*(D4+C4))*!(pa1019*S6*(H4+D4+C4))*!(vS6*pa1240)))*(!(H5*!H8*>HS*o7H1*(!u+H6*(o7H3+th3+pH1011+pa0009))*r23H2*pa0011*pH0413*(pa0010+th3)*!(pH1213*pa1021*(S5+D5+C5+H7))*!(pa1019*H6*(S4+D4+C4))*!(vH6*pa1240)))*(!(D5*!S5*!H5*(!u+D6*o7D1*(o7D3+th3))*r23D2*pa0311*pD0413*(pa0010+th3)))*(!(pN2022*S2*!S6*H2*!H6*D1*(D2+o7D3*C3*!C6*H3*!H5*S3*!S5)*!D7*C1*(C2+o7C3*D3*!D6*H3*!H5*S3*!S5)*!C7*!(S5*(H4*!t07+D5+C5))*!(H5*(S4*!t07+D5+C5))*!(pa2240*(S5+H5))))*(pN1517*pa1418*t07*(t01+th4+th3))");
            Assert.AreEqual("1S", b2.ToXML());
            Assert.IsFalse(b2.Alert);
            Assert.AreEqual("(!(ps0911*pa0012*D8*o1D*!H4*!S4*r23D5))*(!(ps0911*pa0012*C8*o1C*!H4*!S4*r23C5))*(!(pS1019*pa0612*S7*o7S3*r23S4))*(!(pH1019*pa0612*H7*o7H3*r23H4))*(!(pD1019*pa0712*D7*o7D1*!H4*!S4*r23D4))*(!(pC1019*pa0712*C7*o7C1*!H4*!S4*r23C4))*(!(pS0814*pa0510*(pa0509+th3)*S7*o7S1*!H4*r23S3))*(!(pH0814*pa0512*(pa0509+th3)*H7*o7H0*!S4*r23H3))*(!(pD0714*pa0011*D6*(D7+o7D6*(!S3+!H3))*o7D1*!H4*!S4*r23D3))*(!(pC0714*pa0011*C6*(C7+o7C6*(!S3+!H3))*o7C1*!H4*!S4*r23C3))*(!(S5*!S8*!>HS*o7S1*(!u+S6*(o7S3+th3+pS1011+pa0009))*r23S2*pa0011*pS0413*(pa0010+th3)*!(pS1213*pa1021*(H5+D5+C5+S7))*!(pa1115*=S5*H3*(D4+C4))*!(pa1019*S6*(H4+D4+C4))*!(vS6*pa1240)))*(!(H5*!H8*>HS*o7H1*(!u+H6*(o7H3+th3+pH1011+pa0009))*r23H2*pa0011*pH0413*(pa0010+th3)*!(pH1213*pa1021*(S5+D5+C5+H7))*!(pa1019*H6*(S4+D4+C4))*!(vH6*pa1240)))*(!(D5*!S5*!H5*(!u+D6*o7D1*(o7D3+th3))*r23D2*pa0311*pD0413*(pa0010+th3)))*(!(pN2022*S2*!S6*H2*!H6*D1*(D2+o7D3*C3*!C6*H3*!H5*S3*!S5)*!D7*C1*(C2+o7C3*D3*!D6*H3*!H5*S3*!S5)*!C7*!(S5*(H4*!t07+D5+C5))*!(H5*(S4*!t07+D5+C5))*!(pa2240*(S5+H5))))*(pN1517*pa1418*t07*(t01+th4+th3))", b2.Explanation);
        }

        [TestMethod, TestCategory("CI"), TestCategory("Bid")]
        public void Bid_ToSymbol()
        {
            System.Threading.Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("en-US");
            System.Threading.Thread.CurrentThread.CurrentUICulture = new System.Globalization.CultureInfo("en-US");
            var b2 = AuctionBid.Parse("p");
            Assert.AreEqual("Pass", b2.ToSymbol());
            b2 = AuctionBid.Parse("x");
            Assert.AreEqual("x", b2.ToSymbol());
            b2 = AuctionBid.Parse("xx");
            Assert.AreEqual("xx", b2.ToSymbol());
            b2 = AuctionBid.Parse("1C");
            Assert.AreEqual("1♣", b2.ToSymbol());
            b2 = AuctionBid.Parse("1D");
            Assert.AreEqual("1♦", b2.ToSymbol());
            b2 = AuctionBid.Parse("1H");
            Assert.AreEqual("1♥", b2.ToSymbol());
            b2 = AuctionBid.Parse("1S");
            Assert.AreEqual("1♠", b2.ToSymbol());
            b2 = AuctionBid.Parse("1NT");
            Assert.AreEqual("1NT", b2.ToSymbol());
        }

        [TestMethod, TestCategory("CI"), TestCategory("Bid")]
        public void Bid_ToString()
        {
            Bid b2;
            b2 = Bid.Parse("p");
            Assert.AreEqual("Pass", b2.ToString());
            b2 = Bid.Parse("x");
            Assert.AreEqual("x", b2.ToString());
            b2 = Bid.Parse("xx");
            Assert.AreEqual("xx", b2.ToString());
            b2 = Bid.Parse("1C");
            Assert.AreEqual("1C", b2.ToString());
            b2 = Bid.Parse("1D");
            Assert.AreEqual("1D", b2.ToString());
            b2 = Bid.Parse("1H");
            Assert.AreEqual("1H", b2.ToString());
            b2 = Bid.Parse("1S");
            Assert.AreEqual("1S", b2.ToString());
            b2 = Bid.Parse("1NT");
            Assert.AreEqual("1NT", b2.ToString());
        }

        [TestMethod, TestCategory("CI"), TestCategory("Bid")]
        public void Bid_ToText()
        {
            System.Threading.Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("nl-NL");
            System.Threading.Thread.CurrentThread.CurrentUICulture = new System.Globalization.CultureInfo("nl-NL");
            Assert.AreEqual("Pas", AuctionBid.Parse("p").ToText());
            Assert.AreEqual("x", AuctionBid.Parse("x!").ToText());
            Assert.AreEqual("xx", AuctionBid.Parse("xx").ToText());
            Assert.AreEqual("1K", AuctionBid.Parse("1C").ToText());
            Assert.AreEqual("1R", AuctionBid.Parse("1D").ToText());
            Assert.AreEqual("1H", AuctionBid.Parse("1H").ToText());
            Assert.AreEqual("1S", AuctionBid.Parse("1S").ToText());
            Assert.AreEqual("1SA", AuctionBid.Parse("1NT").ToText());

            System.Threading.Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("en-US");
            System.Threading.Thread.CurrentThread.CurrentUICulture = new System.Globalization.CultureInfo("en-US");
            Assert.AreEqual("Pass", AuctionBid.Parse("p").ToText());
            Assert.AreEqual("x", AuctionBid.Parse("x").ToText());
            Assert.AreEqual("xx", AuctionBid.Parse("xx").ToText());
            Assert.AreEqual("1C", AuctionBid.Parse("1C").ToText());
            Assert.AreEqual("1D", AuctionBid.Parse("1D").ToText());
            Assert.AreEqual("1H", AuctionBid.Parse("1H").ToText());
            Assert.AreEqual("1S", AuctionBid.Parse("1S").ToText());
            Assert.AreEqual("1NT", AuctionBid.Parse("1NT").ToText());
        }
    }
}
