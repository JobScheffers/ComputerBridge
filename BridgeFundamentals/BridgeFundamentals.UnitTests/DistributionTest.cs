using Microsoft.VisualStudio.TestTools.UnitTesting;
using Bridge;

namespace Bridge.Test
{
    [TestClass]
	public class DistributionTest
	{
		[TestMethod, TestCategory("CI"), TestCategory("Other")]
		public void Distribution_Clone_Test()
		{
			var source = new Distribution();
			source.Give(Seats.North, Suits.Spades, Ranks.Seven);
			var copy = source.Clone();
			copy.Played(Seats.North, Suits.Spades, Ranks.Seven);
			Assert.IsFalse(copy.Owns(Seats.North, Suits.Spades, Ranks.Seven), "weg uit copy");
			Assert.IsTrue(source.Owns(Seats.North, Suits.Spades, Ranks.Seven), "niet weg uit source");
            Assert.AreEqual<Seats>(Seats.North, source.Owner(Suits.Spades, Ranks.Seven));
            Assert.IsTrue(source.Owned(Seats.North, Suits.Spades, Ranks.Seven));
            Assert.IsTrue(source.Owned(Suits.Spades, Ranks.Seven));
            Assert.IsFalse(source.Owned(Seats.North, Suits.Spades, Ranks.Eight));
            Assert.IsFalse(source.Owned(Suits.Spades, Ranks.Eight));
            Assert.IsFalse(source.Equals(copy));
            Assert.AreEqual(@"       S 7 ", source.ToString().Substring(0, 11));
        }
	}
}
