using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

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

            var card4  = Card.Get(card.Index);
            Assert.IsTrue(card == card4);

            card = Card.Null;
            Assert.IsTrue(Card.IsNull(card));
            Assert.IsFalse(Card.IsNotNull(card));
            Assert.AreEqual(255, card.Index);

            card = Card.Get(255);
            Assert.IsTrue(Card.IsNull(card));
            Assert.IsFalse(Card.IsNotNull(card));
            Assert.AreEqual(255, card.Index);
            Assert.AreEqual("null", card.ToString());
            Assert.AreEqual(255, (int)card.Suit);
        }

        [TestMethod, ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void CardDeck_TestValiation1()
        {
            var card = Card.Get(52);
        }

        [TestMethod, ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void CardDeck_TestValiation2()
        {
            var card = Card.Get(-1);
        }
    }
}
