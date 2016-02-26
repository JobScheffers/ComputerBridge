using Sodes.Base;
using System;

namespace Sodes.Bridge.Base
{
    /// <summary>
    /// Base for the robot that has to implement bidiing and playing tactics.
    /// </summary>
    public class BridgeRobot : BridgeEventBusClient
    {
        private Seats mySeat;
        private BoardResultRecorder boardResult;

        public BridgeRobot(Seats seat) : this(seat, null)
        {
        }

        public BridgeRobot(Seats seat, BridgeEventBus bus) : base(bus)
        {
            this.mySeat = seat;
        }

        public override void HandleBoardStarted(int boardNumber, Seats dealer, Vulnerable vulnerabilty)
        {
            Log.Trace("BridgeRobot.HandleBoardStarted {0}", this.mySeat);
            this.boardResult = new BoardResultRecorder("BridgeRobot." + this.mySeat, null, this.EventBus);
            this.boardResult.HandleBoardStarted(boardNumber, dealer, vulnerabilty);
        }

        public override void HandleBidNeeded(Seats whoseTurn, Bid lastRegularBid, bool allowDouble, bool allowRedouble)
        {
            if (whoseTurn == this.mySeat)
            {
                var myBid = this.FindBid(lastRegularBid, allowDouble, allowRedouble);
                Log.Trace("BridgeRobot.HandleBidNeeded: {0} bids {1}", whoseTurn, myBid);
                this.EventBus.HandleBidDone(this.mySeat, myBid);
            }
        }

        public virtual Bid FindBid(Bid lastRegularBid, bool allowDouble, bool allowRedouble)
        {
            /// this is just some basic logic to enable testing
            /// override this method and implement your own logic
            /// 
            if (lastRegularBid.IsPass) return Bid.C("1NT");
            return Bid.C("Pass");
        }

        public override void HandleCardNeeded(Seats controller, Seats whoseTurn, Suits leadSuit, Suits trump, bool trumpAllowed, int leadSuitLength, int trick)
        {
            if (controller == this.mySeat)
            {
                var myCard = this.FindCard(whoseTurn, leadSuit, trump, trumpAllowed, leadSuitLength, trick);
                //Log.Trace("BridgeRobot.HandleCardNeeded: {0} plays {1}", whoseTurn, myCard);
                this.EventBus.HandleCardPlayed(whoseTurn, myCard.Suit, myCard.Rank);
            }
        }
        public virtual Card FindCard(Seats whoseTurn, Suits leadSuit, Suits trump, bool trumpAllowed, int leadSuitLength, int trick)
        {
            if (leadSuit == Suits.NoTrump || leadSuitLength == 0)
            {   // 1st man or void in lead suit
                for (Suits s = Suits.Clubs; s <= Suits.Spades; s++)
                {
                    for (Ranks r = Ranks.Two; r <= Ranks.Ace; r++)
                    {
                        if (this.boardResult.Distribution.Owns(whoseTurn, s, r))
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
                    if (this.boardResult.Distribution.Owns(whoseTurn, leadSuit, r))
                    {
                        return new Card(leadSuit, r);
                    }
                }
            }

            throw new InvalidOperationException("BridgeRobot.FindCard: no card found");
        }
    }
}
