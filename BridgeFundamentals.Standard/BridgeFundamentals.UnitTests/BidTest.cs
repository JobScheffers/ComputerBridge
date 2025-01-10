using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bridge.Test
{
    [TestClass]
    public class BidTest
    {
        [TestMethod, TestCategory("CI"), TestCategory("Bid")]
        public void BidSet_Test()
        {
            var target = new BiedingenSet();
            Assert.IsFalse(target.BevatDoublet("a"));
            Assert.IsTrue(target.BevatDoublet("a"));
            Assert.IsFalse(target.BevatHK(1, Suits.Spades, "b"));
            Assert.IsTrue(target.BevatHK(1, Suits.Spades, "b"));
            Assert.AreEqual("[x, a] [1S, b] ", target.ToString());
        }

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
            Assert.AreEqual(false, b2.Alert);
            b2 = Bid.C("1S!S5");
            Assert.AreEqual("1S", b2.ToXML());
            Assert.AreEqual(true, b2.Alert);
            Assert.AreEqual("S5", b2.Explanation);
            b2 = Bid.C("1NT;;");
            Assert.AreEqual("1NT", b2.ToXML());

            b2 = Bid.C("1S!(!(ps0911*pa0012*D8*o1D*!H4*!S4*r23D5))*(!(ps0911*pa0012*C8*o1C*!H4*!S4*r23C5))*(!(pS1019*pa0612*S7*o7S3*r23S4))*(!(pH1019*pa0612*H7*o7H3*r23H4))*(!(pD1019*pa0712*D7*o7D1*!H4*!S4*r23D4))*(!(pC1019*pa0712*C7*o7C1*!H4*!S4*r23C4))*(!(pS0814*pa0510*(pa0509+th3)*S7*o7S1*!H4*r23S3))*(!(pH0814*pa0512*(pa0509+th3)*H7*o7H0*!S4*r23H3))*(!(pD0714*pa0011*D6*(D7+o7D6*(!S3+!H3))*o7D1*!H4*!S4*r23D3))*(!(pC0714*pa0011*C6*(C7+o7C6*(!S3+!H3))*o7C1*!H4*!S4*r23C3))*(!(S5*!S8*!>HS*o7S1*(!u+S6*(o7S3+th3+pS1011+pa0009))*r23S2*pa0011*pS0413*(pa0010+th3)*!(pS1213*pa1021*(H5+D5+C5+S7))*!(pa1115*=S5*H3*(D4+C4))*!(pa1019*S6*(H4+D4+C4))*!(vS6*pa1240)))*(!(H5*!H8*>HS*o7H1*(!u+H6*(o7H3+th3+pH1011+pa0009))*r23H2*pa0011*pH0413*(pa0010+th3)*!(pH1213*pa1021*(S5+D5+C5+H7))*!(pa1019*H6*(S4+D4+C4))*!(vH6*pa1240)))*(!(D5*!S5*!H5*(!u+D6*o7D1*(o7D3+th3))*r23D2*pa0311*pD0413*(pa0010+th3)))*(!(pN2022*S2*!S6*H2*!H6*D1*(D2+o7D3*C3*!C6*H3*!H5*S3*!S5)*!D7*C1*(C2+o7C3*D3*!D6*H3*!H5*S3*!S5)*!C7*!(S5*(H4*!t07+D5+C5))*!(H5*(S4*!t07+D5+C5))*!(pa2240*(S5+H5))))*(pN1517*pa1418*t07*(t01+th4+th3))");
            Assert.AreEqual("1S", b2.ToXML());
            Assert.AreEqual(true, b2.Alert);
            Assert.AreEqual("(!(ps0911*pa0012*D8*o1D*!H4*!S4*r23D5))*(!(ps0911*pa0012*C8*o1C*!H4*!S4*r23C5))*(!(pS1019*pa0612*S7*o7S3*r23S4))*(!(pH1019*pa0612*H7*o7H3*r23H4))*(!(pD1019*pa0712*D7*o7D1*!H4*!S4*r23D4))*(!(pC1019*pa0712*C7*o7C1*!H4*!S4*r23C4))*(!(pS0814*pa0510*(pa0509+th3)*S7*o7S1*!H4*r23S3))*(!(pH0814*pa0512*(pa0509+th3)*H7*o7H0*!S4*r23H3))*(!(pD0714*pa0011*D6*(D7+o7D6*(!S3+!H3))*o7D1*!H4*!S4*r23D3))*(!(pC0714*pa0011*C6*(C7+o7C6*(!S3+!H3))*o7C1*!H4*!S4*r23C3))*(!(S5*!S8*!>HS*o7S1*(!u+S6*(o7S3+th3+pS1011+pa0009))*r23S2*pa0011*pS0413*(pa0010+th3)*!(pS1213*pa1021*(H5+D5+C5+S7))*!(pa1115*=S5*H3*(D4+C4))*!(pa1019*S6*(H4+D4+C4))*!(vS6*pa1240)))*(!(H5*!H8*>HS*o7H1*(!u+H6*(o7H3+th3+pH1011+pa0009))*r23H2*pa0011*pH0413*(pa0010+th3)*!(pH1213*pa1021*(S5+D5+C5+H7))*!(pa1019*H6*(S4+D4+C4))*!(vH6*pa1240)))*(!(D5*!S5*!H5*(!u+D6*o7D1*(o7D3+th3))*r23D2*pa0311*pD0413*(pa0010+th3)))*(!(pN2022*S2*!S6*H2*!H6*D1*(D2+o7D3*C3*!C6*H3*!H5*S3*!S5)*!D7*C1*(C2+o7C3*D3*!D6*H3*!H5*S3*!S5)*!C7*!(S5*(H4*!t07+D5+C5))*!(H5*(S4*!t07+D5+C5))*!(pa2240*(S5+H5))))*(pN1517*pa1418*t07*(t01+th4+th3))", b2.Explanation);

            b2 = Bid.C("1S?(!(ps0911*pa0012*D8*o1D*!H4*!S4*r23D5))*(!(ps0911*pa0012*C8*o1C*!H4*!S4*r23C5))*(!(pS1019*pa0612*S7*o7S3*r23S4))*(!(pH1019*pa0612*H7*o7H3*r23H4))*(!(pD1019*pa0712*D7*o7D1*!H4*!S4*r23D4))*(!(pC1019*pa0712*C7*o7C1*!H4*!S4*r23C4))*(!(pS0814*pa0510*(pa0509+th3)*S7*o7S1*!H4*r23S3))*(!(pH0814*pa0512*(pa0509+th3)*H7*o7H0*!S4*r23H3))*(!(pD0714*pa0011*D6*(D7+o7D6*(!S3+!H3))*o7D1*!H4*!S4*r23D3))*(!(pC0714*pa0011*C6*(C7+o7C6*(!S3+!H3))*o7C1*!H4*!S4*r23C3))*(!(S5*!S8*!>HS*o7S1*(!u+S6*(o7S3+th3+pS1011+pa0009))*r23S2*pa0011*pS0413*(pa0010+th3)*!(pS1213*pa1021*(H5+D5+C5+S7))*!(pa1115*=S5*H3*(D4+C4))*!(pa1019*S6*(H4+D4+C4))*!(vS6*pa1240)))*(!(H5*!H8*>HS*o7H1*(!u+H6*(o7H3+th3+pH1011+pa0009))*r23H2*pa0011*pH0413*(pa0010+th3)*!(pH1213*pa1021*(S5+D5+C5+H7))*!(pa1019*H6*(S4+D4+C4))*!(vH6*pa1240)))*(!(D5*!S5*!H5*(!u+D6*o7D1*(o7D3+th3))*r23D2*pa0311*pD0413*(pa0010+th3)))*(!(pN2022*S2*!S6*H2*!H6*D1*(D2+o7D3*C3*!C6*H3*!H5*S3*!S5)*!D7*C1*(C2+o7C3*D3*!D6*H3*!H5*S3*!S5)*!C7*!(S5*(H4*!t07+D5+C5))*!(H5*(S4*!t07+D5+C5))*!(pa2240*(S5+H5))))*(pN1517*pa1418*t07*(t01+th4+th3))");
            Assert.AreEqual("1S", b2.ToXML());
            Assert.AreEqual(false, b2.Alert);
            Assert.AreEqual("(!(ps0911*pa0012*D8*o1D*!H4*!S4*r23D5))*(!(ps0911*pa0012*C8*o1C*!H4*!S4*r23C5))*(!(pS1019*pa0612*S7*o7S3*r23S4))*(!(pH1019*pa0612*H7*o7H3*r23H4))*(!(pD1019*pa0712*D7*o7D1*!H4*!S4*r23D4))*(!(pC1019*pa0712*C7*o7C1*!H4*!S4*r23C4))*(!(pS0814*pa0510*(pa0509+th3)*S7*o7S1*!H4*r23S3))*(!(pH0814*pa0512*(pa0509+th3)*H7*o7H0*!S4*r23H3))*(!(pD0714*pa0011*D6*(D7+o7D6*(!S3+!H3))*o7D1*!H4*!S4*r23D3))*(!(pC0714*pa0011*C6*(C7+o7C6*(!S3+!H3))*o7C1*!H4*!S4*r23C3))*(!(S5*!S8*!>HS*o7S1*(!u+S6*(o7S3+th3+pS1011+pa0009))*r23S2*pa0011*pS0413*(pa0010+th3)*!(pS1213*pa1021*(H5+D5+C5+S7))*!(pa1115*=S5*H3*(D4+C4))*!(pa1019*S6*(H4+D4+C4))*!(vS6*pa1240)))*(!(H5*!H8*>HS*o7H1*(!u+H6*(o7H3+th3+pH1011+pa0009))*r23H2*pa0011*pH0413*(pa0010+th3)*!(pH1213*pa1021*(S5+D5+C5+H7))*!(pa1019*H6*(S4+D4+C4))*!(vH6*pa1240)))*(!(D5*!S5*!H5*(!u+D6*o7D1*(o7D3+th3))*r23D2*pa0311*pD0413*(pa0010+th3)))*(!(pN2022*S2*!S6*H2*!H6*D1*(D2+o7D3*C3*!C6*H3*!H5*S3*!S5)*!D7*C1*(C2+o7C3*D3*!D6*H3*!H5*S3*!S5)*!C7*!(S5*(H4*!t07+D5+C5))*!(H5*(S4*!t07+D5+C5))*!(pa2240*(S5+H5))))*(pN1517*pa1418*t07*(t01+th4+th3))", b2.Explanation);
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

        [TestMethod, TestCategory("CI"), TestCategory("Bid")]
        public void Bid_ToText()
        {
            System.Threading.Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("nl-NL");
            System.Threading.Thread.CurrentThread.CurrentUICulture = new System.Globalization.CultureInfo("nl-NL");
            Assert.AreEqual("Pas", Bid.C("p").ToText());
            Assert.AreEqual("x", Bid.C("x!").ToText());
            Assert.AreEqual("xx", Bid.C("xx").ToText());
            Assert.AreEqual("1K", Bid.C("1C").ToText());
            Assert.AreEqual("1R", Bid.C("1D").ToText());
            Assert.AreEqual("1H", Bid.C("1H").ToText());
            Assert.AreEqual("1S", Bid.C("1S").ToText());
            Assert.AreEqual("1SA", Bid.C("1NT").ToText());

            System.Threading.Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("en-US");
            System.Threading.Thread.CurrentThread.CurrentUICulture = new System.Globalization.CultureInfo("en-US");
            Assert.AreEqual("Pass", Bid.C("p").ToText());
            Assert.AreEqual("x", Bid.C("x").ToText());
            Assert.AreEqual("xx", Bid.C("xx").ToText());
            Assert.AreEqual("1C", Bid.C("1C").ToText());
            Assert.AreEqual("1D", Bid.C("1D").ToText());
            Assert.AreEqual("1H", Bid.C("1H").ToText());
            Assert.AreEqual("1S", Bid.C("1S").ToText());
            Assert.AreEqual("1NT", Bid.C("1NT").ToText());
        }
    }
}
