using Microsoft.VisualStudio.TestTools.UnitTesting;
using Bridge.Test.Helpers;

namespace Bridge.Test
{
    [TestClass]
    public class TestRobotTest : BridgeTestBase
    {
        [ClassInitialize]
        public static void MyClassInitialize(TestContext testContext)
        {
            BridgeTestBase.ClassInitialize(testContext);
        }

        [TestMethod]
        public void TestRobot_Handle1Board()
        {
            var r = new TestRobot(Seats.North);
            r.HandleTournamentStarted(Scorings.scPairs, 120, 1, "");
            r.HandleRoundStarted(null, new DirectionDictionary<string>("", ""));
            r.HandleBoardStarted(1, Seats.North, Vulnerable.Neither);
            r.HandleCardPosition(Seats.North, Suits.Clubs, Ranks.Ace);
            r.HandleCardPosition(Seats.North, Suits.Clubs, Ranks.King);
            r.HandleCardPosition(Seats.North, Suits.Clubs, Ranks.Nine);
            r.HandleCardPosition(Seats.North, Suits.Clubs, Ranks.Seven);
            r.HandleCardPosition(Seats.North, Suits.Diamonds, Ranks.King);
            r.HandleCardPosition(Seats.North, Suits.Diamonds, Ranks.Three);
            r.HandleCardPosition(Seats.North, Suits.Hearts, Ranks.Ace);
            r.HandleCardPosition(Seats.North, Suits.Hearts, Ranks.Six);
            r.HandleCardPosition(Seats.North, Suits.Hearts, Ranks.Five);
            r.HandleCardPosition(Seats.North, Suits.Hearts, Ranks.Three);
            r.HandleCardPosition(Seats.North, Suits.Spades, Ranks.Ace);
            r.HandleCardPosition(Seats.North, Suits.Spades, Ranks.Queen);
            r.HandleCardPosition(Seats.North, Suits.Spades, Ranks.Eight);
            r.HandleBidDone(Seats.North, r.FindBid(Bid.C("Pass"), false, false));
            r.HandleBidDone(Seats.East, Bid.C("Pass"));
            r.HandleBidDone(Seats.South, Bid.C("Pass"));
            r.HandleBidDone(Seats.West, Bid.C("Pass"));
            r.HandleAuctionFinished(Seats.North, new Contract("1NT", Seats.North, Vulnerable.Neither));
            r.HandleCardPlayed(Seats.East, Suits.Diamonds, Ranks.Queen);
            r.HandleShowDummy(Seats.South);
            r.HandleCardPlayed(Seats.South, Suits.Diamonds, Ranks.Two);
            r.HandleCardPlayed(Seats.West, Suits.Diamonds, Ranks.Four);
            r.HandleCardNeeded(Seats.North, Seats.North, Suits.Diamonds, Suits.NoTrump, false, 2, 1);
            var card = r.FindCard(Seats.North, Suits.Diamonds, Suits.NoTrump, false, 2, 1);
        }
    }
}
