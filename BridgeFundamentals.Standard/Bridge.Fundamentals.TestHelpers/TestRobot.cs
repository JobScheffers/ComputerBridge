using System;
using System.Threading.Tasks;

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

        //public override void HandleMyCardPosition(Seats seat, Suits suit, Ranks rank)
        //{
        //}

        public override void HandleBidNeeded(Seats whoseTurn, Bid lastRegularBid, bool allowDouble, bool allowRedouble)
        {
            if (this.CardCount < 13) throw new InvalidOperationException("not 13 cards");
            base.HandleBidNeeded(whoseTurn, lastRegularBid, allowDouble, allowRedouble);
        }

        /// <summary>
        /// this is just some basic logic to enable testing
        /// (always produces 1C 1S x 2S p p p
        /// override this method and implement your own logic
        /// </summary>
        /// <param name="lastRegularBid"></param>
        /// <param name="allowDouble"></param>
        /// <param name="allowRedouble"></param>
        /// <returns></returns>
        public override async Task<Bid> FindBid(Bid lastRegularBid, bool allowDouble, bool allowRedouble)
        {
            /// 
            await Task.CompletedTask;       // only to prevent a warning while this body is synchronous
            if (lastRegularBid.IsPass) return Bid.C("1C");
            if (lastRegularBid.Equals(1, Suits.Clubs)) return Bid.C("1S");
            if (lastRegularBid.Equals(1, Suits.Spades) && allowDouble) return Bid.C("x");
            if (lastRegularBid.Equals(1, Suits.Spades) && !allowDouble) return Bid.C("2S");
            return Bid.C("Pass");
        }

        /// <summary>
        /// finds the first valid card
        /// override this method and implement your own logic
        /// </summary>
        /// <param name="whoseTurn"></param>
        /// <param name="leadSuit"></param>
        /// <param name="trump"></param>
        /// <param name="trumpAllowed"></param>
        /// <param name="leadSuitLength"></param>
        /// <param name="trick"></param>
        /// <returns></returns>
        public override async Task<Card> FindCard(Seats whoseTurn, Suits leadSuit, Suits trump, bool trumpAllowed, int leadSuitLength, int trick)
        {
            await Task.CompletedTask;       // only to prevent a warning while this body is synchronous
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
