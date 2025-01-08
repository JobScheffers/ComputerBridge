using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Runtime.InteropServices;

namespace Bridge.Test
{
    [TestClass]
    public class CardsTest
    {
        [TestMethod]
        public void CardDeck_Test()
        {
            var card = CardDeck.Instance[Suits.Hearts, Ranks.King];
            Assert.AreEqual(Suits.Hearts, card.Suit);
            Assert.AreEqual(1, Marshal.SizeOf(card));
        }

        [TestMethod]
        public void SimpleMove_ToString()
        {
            var target1 = new SimpleMove(Suits.Spades, Ranks.King);
            Assert.AreEqual<string>("SK", target1.ToString());
        }
    }
}
