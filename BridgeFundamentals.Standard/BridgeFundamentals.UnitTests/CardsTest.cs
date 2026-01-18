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
            var card = Card.Get(Suits.Hearts, Ranks.King);
            Assert.AreEqual(Suits.Hearts, card.Suit);
            Assert.AreEqual(Ranks.King, card.Rank);
            Assert.AreEqual(3, card.HighCardPoints);
            Assert.AreEqual("hK", card.ToString());
            Assert.IsTrue(Card.IsNotNull(card));
            Assert.IsFalse(Card.IsNull(card));

            var card2 = Card.Get(Suits.Hearts, Ranks.King);
            var card3 = Card.Get(Suits.Hearts, Ranks.Queen);
            Assert.IsTrue(card == card2);
            Assert.IsFalse(card == card3);
            Assert.IsFalse(card != card2);

            card = Card.Null;
            Assert.IsTrue(Card.IsNull(card));
            Assert.IsFalse(Card.IsNotNull(card));

        }

        //[TestMethod]
        //public void SimpleMove_ToString()
        //{
        //    var target1 = new SimpleMove(Suits.Spades, Ranks.King);
        //    Assert.AreEqual<string>("SK", target1.ToString());
        //}
    }
}
