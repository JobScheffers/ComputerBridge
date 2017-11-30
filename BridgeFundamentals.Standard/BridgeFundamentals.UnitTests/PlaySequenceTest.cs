using Bridge;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bridge.Test
{
	[TestClass]
	public class PlaySequenceTest
	{
		[TestMethod, TestCategory("CI"), TestCategory("Other")]
		public void PlaySequence_Record()
		{
			var target = new PlaySequence(new Contract("1NT", Seats.South, Vulnerable.Neither), 13, Seats.West);
            var clone = target.Clone();

			Assert.AreEqual<Seats>(Seats.West, target.whoseTurn, "before play");
            Assert.AreEqual<int>(13, target.remainingTricks, "before play");
            Assert.AreEqual<int>(1, target.man, "before play");
            Assert.AreEqual<Seats>(Seats.West, clone.whoseTurn, "clone before play");

            target.Record(Suits.Clubs, Ranks.King);
            Assert.AreEqual<Seats>(Seats.North, target.whoseTurn, "after 1st card");
            Assert.AreEqual<int>(13, target.remainingTricks, "after 1st card");
            Assert.AreEqual<int>(2, target.man, "after 1st card");
            Assert.AreEqual<Ranks>(Ranks.King, target.CardPlayed(1, Seats.West).Rank, "after 1st card");
            Assert.AreEqual<Ranks>(Ranks.King, target.CardPlayed(1, 1).Rank, "after 1st card");
            Assert.IsFalse(target.HasBeenRuffed(Suits.Clubs), "after 1st card");
            Assert.AreEqual<Suits>(Suits.Clubs, target.leadSuit, "after 1st card");
            Assert.AreEqual<Seats>(Seats.West, target.bestMan, "after 1st card");
            Assert.AreEqual<Ranks>(Ranks.King, target.bestRank, "after 1st card");

            target.Record(Suits.Clubs, Ranks.Two);
            Assert.AreEqual<Seats>(Seats.East, target.whoseTurn, "after 2nd card");
            Assert.AreEqual<int>(13, target.remainingTricks, "after 2nd card");
            Assert.AreEqual<int>(3, target.man, "after 2nd card");
            Assert.AreEqual<Ranks>(Ranks.King, target.CardPlayed(1, Seats.West).Rank, "after 2nd card");
            Assert.AreEqual<Ranks>(Ranks.King, target.CardPlayed(1, 1).Rank, "after 2nd card");
            Assert.AreEqual<Ranks>(Ranks.Two, target.CardPlayed(1, Seats.North).Rank, "after 2nd card");
            Assert.AreEqual<Ranks>(Ranks.Two, target.CardPlayed(1, 2).Rank, "after 2nd card");
            Assert.IsFalse(target.HasBeenRuffed(Suits.Clubs), "after 2nd card");
            Assert.AreEqual<Suits>(Suits.Clubs, target.leadSuit, "after 2nd card");
            Assert.AreEqual<Seats>(Seats.West, target.bestMan, "after 2nd card");
            Assert.AreEqual<Ranks>(Ranks.King, target.bestRank, "after 2nd card");

            target.Record(Suits.Clubs, Ranks.Three);
            Assert.AreEqual<Seats>(Seats.South, target.whoseTurn, "after 3rd card");
            Assert.AreEqual<int>(13, target.remainingTricks, "after 3rd card");
            Assert.AreEqual<int>(4, target.man, "after 3rd card");
            Assert.AreEqual<Ranks>(Ranks.King, target.CardPlayed(1, Seats.West).Rank, "after 3rd card");
            Assert.AreEqual<Ranks>(Ranks.King, target.CardPlayed(1, 1).Rank, "after 3rd card");
            Assert.AreEqual<Ranks>(Ranks.Two, target.CardPlayed(1, Seats.North).Rank, "after 3rd card");
            Assert.AreEqual<Ranks>(Ranks.Two, target.CardPlayed(1, 2).Rank, "after 3rd card");
            Assert.IsFalse(target.HasBeenRuffed(Suits.Clubs), "after 3rd card");
            Assert.AreEqual<Suits>(Suits.Clubs, target.leadSuit, "after 3rd card");
            Assert.AreEqual<Seats>(Seats.West, target.bestMan, "after 3rd card");
            Assert.AreEqual<Ranks>(Ranks.King, target.bestRank, "after 3rd card");

            target.Record(Suits.Clubs, Ranks.Ace, "hello");
            Assert.AreEqual<Seats>(Seats.South, target.whoseTurn, "after 4th card");
            Assert.AreEqual<int>(12, target.remainingTricks, "after 4th card");
            Assert.AreEqual<int>(1, target.man, "after 4th card");
            Assert.AreEqual<Ranks>(Ranks.King, target.CardPlayed(1, Seats.West).Rank, "after 4th card");
            Assert.AreEqual<Ranks>(Ranks.King, target.CardPlayed(1, 1).Rank, "after 4th card");
            Assert.AreEqual<Ranks>(Ranks.Two, target.CardPlayed(1, Seats.North).Rank, "after 4th card");
            Assert.AreEqual<Ranks>(Ranks.Two, target.CardPlayed(1, 2).Rank, "after 4th card");
            Assert.IsFalse(target.HasBeenRuffed(Suits.Clubs), "after 4th card");
            Assert.AreEqual<Suits>(Suits.NoTrump, target.leadSuit, "after 4th card");
            Assert.AreEqual<Seats>(Seats.South, target.bestMan, "after 4th card");
            Assert.AreEqual<Ranks>(Ranks.Ace, target.bestRank, "after 4th card");

            Assert.AreEqual<int>(1, target.WhichMan(1, Seats.West));
            Assert.AreEqual<int>(2, target.WhichMan(1, Seats.North));
            Assert.AreEqual<int>(3, target.WhichMan(1, Seats.East));
            Assert.AreEqual<int>(4, target.WhichMan(1, Seats.South));
            Assert.AreEqual<Seats>(Seats.West, target.Player(1, 1));
            Assert.AreEqual<Seats>(Seats.North, target.Player(1, 2));
            Assert.AreEqual<Seats>(Seats.East, target.Player(1, 3));
            Assert.AreEqual<Seats>(Seats.South, target.Player(1, 4));
            Assert.AreEqual<int>(1, target.PlayedInTrick(Suits.Clubs, Ranks.King));
            Assert.AreEqual<int>(1, target.PlayedInTrick(Suits.Clubs, Ranks.Two));
            Assert.AreEqual<int>(1, target.PlayedInTrick(Suits.Clubs, Ranks.Three));
            Assert.AreEqual<int>(1, target.PlayedInTrick(Suits.Clubs, Ranks.Ace));
            Assert.AreEqual<int>(14, target.PlayedInTrick(Suits.Clubs, Ranks.Queen));
            Assert.AreEqual<int>(14, target.PlayedInTrick(new Card(Suits.Clubs, Ranks.Jack)));
            Assert.AreEqual("cK c2 c3 cA ", target.ToString());

            target.Undo();
            Assert.AreEqual<Seats>(Seats.South, target.whoseTurn, "after 1st undo");
            Assert.AreEqual<int>(13, target.remainingTricks, "after 1st undo");
            Assert.AreEqual<int>(4, target.man, "after 1st undo");
            Assert.AreEqual<Ranks>(Ranks.King, target.CardPlayed(1, Seats.West).Rank, "after 1st undo");
            Assert.AreEqual<Ranks>(Ranks.King, target.CardPlayed(1, 1).Rank, "after 1st undo");
            Assert.AreEqual<Ranks>(Ranks.Two, target.CardPlayed(1, Seats.North).Rank, "after 1st undo");
            Assert.AreEqual<Ranks>(Ranks.Two, target.CardPlayed(1, 2).Rank, "after 1st undo");
            Assert.IsFalse(target.HasBeenRuffed(Suits.Clubs), "after 1st undo");
            Assert.AreEqual<Suits>(Suits.Clubs, target.leadSuit, "after 1st undo");
            Assert.AreEqual<Seats>(Seats.West, target.bestMan, "after 1st undo");
            Assert.AreEqual<Ranks>(Ranks.King, target.bestRank, "after 1st undo");

            target.Undo();
            Assert.AreEqual<Seats>(Seats.East, target.whoseTurn, "after 2nd undo");
            Assert.AreEqual<int>(13, target.remainingTricks, "after 2nd undo");
            Assert.AreEqual<int>(3, target.man, "after 2nd undo");
            Assert.AreEqual<Ranks>(Ranks.King, target.CardPlayed(1, Seats.West).Rank, "after 2nd undo");
            Assert.AreEqual<Ranks>(Ranks.King, target.CardPlayed(1, 1).Rank, "after 2nd undo");
            Assert.AreEqual<Ranks>(Ranks.Two, target.CardPlayed(1, Seats.North).Rank, "after 2nd undo");
            Assert.AreEqual<Ranks>(Ranks.Two, target.CardPlayed(1, 2).Rank, "after 2nd undo");
            Assert.IsFalse(target.HasBeenRuffed(Suits.Clubs), "after 2nd undo");
            Assert.AreEqual<Suits>(Suits.Clubs, target.leadSuit, "after 2nd undo");
            Assert.AreEqual<Seats>(Seats.West, target.bestMan, "after 2nd undo");
            Assert.AreEqual<Ranks>(Ranks.King, target.bestRank, "after 2nd undo");

            target.Undo();
            target.Undo();
            Assert.AreEqual<Seats>(Seats.West, target.whoseTurn, "after 4th undo");
            Assert.AreEqual<Suits>(Suits.NoTrump, target.leadSuit, "after 4th undo");
            Assert.AreEqual<int>(13, target.remainingTricks, "after 4th undo");

        }
    }
}
