using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bridge.Test
{
    [TestClass]
    public class CardsTest
    {
        [TestMethod]
        public void SimpleMove_ToString()
        {
            var target1 = new SimpleMove(Suits.Spades, Ranks.King);
            Assert.AreEqual<string>("SK", target1.ToString());
        }
    }
}
