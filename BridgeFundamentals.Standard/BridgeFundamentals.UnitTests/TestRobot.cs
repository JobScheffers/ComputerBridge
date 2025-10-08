using Microsoft.VisualStudio.TestTools.UnitTesting;
using Bridge.Test.Helpers;
using System.Threading.Tasks;

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
        public async Task TestRobot_Handle1Board()
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
            r.HandleBidDone(Seats.North, await r.FindBid(Bid.C("Pass"), false, false));
            r.HandleBidDone(Seats.East, Bid.C("Pass"));
            r.HandleBidDone(Seats.South, Bid.C("Pass"));
            r.HandleBidDone(Seats.West, Bid.C("Pass"));
            r.HandleAuctionFinished(Seats.North, new Contract("1NT", Seats.North, Vulnerable.Neither));
            r.HandleCardPlayed(Seats.East, Suits.Diamonds, Ranks.Queen);
            r.HandleShowDummy(Seats.South);
            r.HandleCardPlayed(Seats.South, Suits.Diamonds, Ranks.Two);
            r.HandleCardPlayed(Seats.West, Suits.Diamonds, Ranks.Four);
            r.HandleCardNeeded(Seats.North, Seats.North, Suits.Diamonds, Suits.NoTrump, false, 2, 1);
            var card = await r.FindCard(Seats.North, Suits.Diamonds, Suits.NoTrump, false, 2, 1);
        }
    }

    public class SimpleRobot(Seats seat, int actualThinkTime = 0) : AsyncBridgeRobotBase(seat, $"SimpleRobot.{seat}")
    {
        public override async ValueTask<Bid> FindBid(Bid lastRegularBid, bool allowDouble, bool allowRedouble)
        {
            if (actualThinkTime > 0) await Task.Delay(1000 * actualThinkTime);
            var bid = new Bid(SpecialBids.Pass);
            switch (mySeat)
            {
                case Seats.North:
                    break;
                case Seats.East:
                    if (lastRegularBid.IsPass) bid = Bid.C("1NT!pa1517");
                    break;
                case Seats.South:
                    break;
                case Seats.West:
                    bid = Bid.C("3NT");
                    break;
                default:
                    break;
            }

            await Task.CompletedTask;       // only to prevent a warning while this body is synchronous

            Log.Trace(2, $"{NameForLog} finds bid {bid}");
            return bid;
        }

        public override async ValueTask<ExplainedCard> FindCard(Seats whoseTurn, Suits leadSuit, Suits trump, bool trumpAllowed, int leadSuitLength, int trick)
        {
            if (actualThinkTime > 0) await Task.Delay(1000 * actualThinkTime);
            var card = Card.Null;
            if (leadSuit == Suits.NoTrump)
            {
                for (Ranks rank = Ranks.Ace; rank >= Ranks.Two; rank--)
                {
                    for (Suits suit = Suits.Clubs; suit <= Suits.Spades; suit++)
                    {
                        if (Play.PlayedInTrick(suit, rank) == 14 && Distribution.Owns(whoseTurn, suit, rank))
                        {
                            Log.Trace(2, $"{NameForLog} finds card {rank.ToXML()}{suit.ToXML()}");
                            return new ExplainedCard(CardDeck.Instance[suit, rank], "test");
                        }
                    }
                }
            }
            else
            {
                for (Ranks rank = Play.bestRank + 1; rank <= Ranks.Ace; rank++)
                {
                    if (Play.PlayedInTrick(leadSuit, rank) == 14 && Distribution.Owns(whoseTurn, leadSuit, rank))
                    {
                        Log.Trace(2, $"{NameForLog} finds card {rank.ToXML()}{leadSuit.ToXML()}");
                        return new ExplainedCard(CardDeck.Instance[leadSuit, rank], "test");
                    }
                }
                for (Ranks rank = Ranks.Two; rank < Play.bestRank; rank++)
                {
                    if (Play.PlayedInTrick(leadSuit, rank) == 14 && Distribution.Owns(whoseTurn, leadSuit, rank))
                    {
                        Log.Trace(2, $"{NameForLog} finds card {rank.ToXML()}{leadSuit.ToXML()}");
                        return new ExplainedCard(CardDeck.Instance[leadSuit, rank], "test");
                    }
                }
                if (trump != Suits.NoTrump)
                {
                    for (Ranks rank = Ranks.Two; rank < Ranks.Ace; rank++)
                    {
                        if (Play.PlayedInTrick(trump, rank) == 14 && Distribution.Owns(whoseTurn, trump, rank))
                        {
                            Log.Trace(2, $"{NameForLog} finds card {rank.ToXML()}{trump.ToXML()}");
                            return new ExplainedCard(CardDeck.Instance[trump, rank], "test");
                        }
                    }
                }

                for (Ranks rank = Ranks.Two; rank < Ranks.Ace; rank++)
                {
                    for (Suits suit = Suits.Clubs; suit <= Suits.Spades; suit++)
                    {
                        if (suit != leadSuit && suit != trump)
                        {
                            if (Play.PlayedInTrick(suit, rank) == 14 && Distribution.Owns(whoseTurn, suit, rank))
                            {
                                Log.Trace(2, $"{NameForLog} finds card {rank.ToXML()}{suit.ToXML()}");
                                return new ExplainedCard(CardDeck.Instance[suit, rank], "test");
                            }
                        }
                    }
                }
            }

            throw new System.Exception("no card found");
        }

        public override async ValueTask Finish()
        {
            await ValueTask.CompletedTask;
        }
    }
}
