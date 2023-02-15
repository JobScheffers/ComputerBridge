using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bridge.Test
{
	[TestClass]
	public class CollectionTest
	{
        [TestMethod, TestCategory("CI"), TestCategory("Bid")]
        public void SeatsSuitsArrayOfByte_Test1()
        {
            var x = new SeatsSuitsArrayOfByte();
            SeatsExtensions.ForEachSeat(seat =>
            {
                x[seat, Suits.Hearts] = 6;
                x[seat, Suits.Clubs] = 5;
                x[seat, Suits.Diamonds] = (byte)(seat + 1);
                Assert.AreEqual(6, x[seat, Suits.Hearts]);
                Assert.AreEqual(5, x[seat, Suits.Clubs]);
                Assert.AreEqual(0, x[seat, Suits.Spades]);
            });


            var y = x.DisplayValue;
            Assert.IsFalse(string.IsNullOrWhiteSpace(y));
            Assert.AreEqual("North: 0 6 1 5 East: 0 6 2 5 South: 0 6 3 5 West: 0 6 4 5", y);
        }

        [TestMethod]
        public void Suits_SuitsRanksArrayOfRanks_Debug()
        {
            var target = new SuitsRanksArrayOfRanks();
            //target[Suits.Clubs, Ranks.Two] = Ranks.Ace;
            target[Suits.Diamonds, Ranks.Three] = Ranks.Three;
        }
    }
}
