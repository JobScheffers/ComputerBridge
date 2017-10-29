using Bridge;
using System;

namespace Bridge.Test.Helpers
{
    public class TestRobot : BridgeRobot
    {
        public TestRobot(Seats seat) : this(seat, null)
        {
        }

        public TestRobot(Seats seat, BridgeEventBus bus) : base(seat, bus)
        {
            this.seat = seat;
        }

        private Seats seat;

        public override void HandleBidNeeded(Seats whoseTurn, Bid lastRegularBid, bool allowDouble, bool allowRedouble)
        {
            if (this.CardCount < 13) throw new InvalidOperationException("not 13 cards");
            base.HandleBidNeeded(whoseTurn, lastRegularBid, allowDouble, allowRedouble);
        }
        public override Bid FindBid(Bid lastRegularBid, bool allowDouble, bool allowRedouble)
        {
            /// this is just some basic logic to enable testing
            /// override this method and implement your own logic
            /// 
            if (lastRegularBid.IsPass) return Bid.C("1C");
            if (lastRegularBid.Equals(1, Suits.Clubs)) return Bid.C("1S");
            if (lastRegularBid.Equals(1, Suits.Spades) && allowDouble) return Bid.C("x");
            if (lastRegularBid.Equals(1, Suits.Spades) && !allowDouble) return Bid.C("2C");
            return Bid.C("Pass");
        }

        public override Card FindCard(Seats whoseTurn, Suits leadSuit, Suits trump, bool trumpAllowed, int leadSuitLength, int trick)
        {
            if (leadSuit == Suits.NoTrump || leadSuitLength == 0)
            {   // 1st man or void in lead suit
                for (Suits s = Suits.Clubs; s <= Suits.Spades; s++)
                {
                    for (Ranks r = Ranks.Two; r <= Ranks.Ace; r++)
                    {
                        if (this.CurrentResult.Distribution.Owns(whoseTurn, s, r))
                        {
                            return new Card(s, r);
                        }
                    }
                }
            }
            else
            {
                for (Ranks r = Ranks.Two; r <= Ranks.Ace; r++)
                {
                    if (this.CurrentResult.Distribution.Owns(whoseTurn, leadSuit, r))
                    {
                        return new Card(leadSuit, r);
                    }
                }
            }

            throw new InvalidOperationException("BridgeRobot.FindCard: no card found");
        }

        private int CardCount
        {
            get
            {
                int count = 0;
                for (Suits s = Suits.Clubs; s <= Suits.Spades; s++)
                {
                    for (Ranks r = Ranks.Two; r <= Ranks.Ace; r++)
                    {
                        if (this.CurrentResult.Distribution.Owns(this.seat, s, r))
                        {
                            count++; ;
                        }
                    }
                }

                return count;
            }
        }
    }
}
