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
            Assert.AreEqual(Ranks.King, card.Rank);
            Assert.AreEqual(3, card.HighCardPoints);
            Assert.AreEqual("hK", card.ToString());
            Assert.AreEqual(1, Marshal.SizeOf(card));

            var card2 = CardDeck.Instance[Suits.Hearts, Ranks.King];
            var card3 = CardDeck.Instance[Suits.Hearts, Ranks.Queen];
            Assert.IsTrue(card == card2);
            Assert.IsFalse(card == card3);
            Assert.IsFalse(card != card2);

            card = Card.Null;
            Assert.AreEqual("null", card.ToString());
        }

        //[TestMethod]
        //public void SimpleMove_ToString()
        //{
        //    var target1 = new SimpleMove(Suits.Spades, Ranks.King);
        //    Assert.AreEqual<string>("SK", target1.ToString());
        //}
    }
}
