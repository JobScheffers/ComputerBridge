using System;

namespace Bridge
{
    /// <summary>
    /// Base for the robot that has to implement bidding and playing tactics.
    /// </summary>
    public abstract class BridgeRobot : BoardResultOwner
    {
        public BridgeRobot(Seats seat) : this(seat, null)
        {
        }

        public BridgeRobot(Seats seat, BridgeEventBus bus) : base("BridgeRobot." + seat.ToXML(), bus)
        {
            this.mySeat = seat;
        }

        private Seats mySeat;

        public abstract Bid FindBid(Bid lastRegularBid, bool allowDouble, bool allowRedouble);

        public abstract Card FindCard(Seats whoseTurn, Suits leadSuit, Suits trump, bool trumpAllowed, int leadSuitLength, int trick);

        #region Bridge Event Handlers

        public override void HandleCardPosition(Seats seat, Suits suit, Ranks rank)
        {
            // no cheating: only look at the card when it is meant for you
            //if (seat != this.mySeat
            //    && !this.CurrentResult.Auction.Ended
            //    && (this.CurrentResult.Play == null
            //        || seat != this.CurrentResult.Play.Dummy
            //        )
            //    )
            //{
            //    return;
            //}

            //Log.Trace(3, "BridgeRobot.{3}.HandleCardPosition: {0} gets {1}{2}", seat.ToString(), rank.ToXML(), suit.ToXML().ToLower(), this.mySeat.ToXML());
            base.HandleCardPosition(seat, suit, rank);
            //this.HandleMyCardPosition(seat, suit, rank);
        }

        public override void HandleBidNeeded(Seats whoseTurn, Bid lastRegularBid, bool allowDouble, bool allowRedouble)
        {
            if (whoseTurn == this.mySeat && this.EventBus != null)
            {
                var myBid = this.FindBid(lastRegularBid, allowDouble, allowRedouble);
                Log.Trace(3, "BridgeRobot.{0}.HandleBidNeeded: bids {1}", this.mySeat.ToXML(), myBid);
                this.EventBus.HandleBidDone(this.mySeat, myBid);
            }
        }

        public override void HandleCardNeeded(Seats controller, Seats whoseTurn, Suits leadSuit, Suits trump, bool trumpAllowed, int leadSuitLength, int trick)
        {
            if (whoseTurn != this.CurrentResult.Play.whoseTurn)
                throw new ArgumentOutOfRangeException("whoseTurn", "Expected a needcard from " + this.CurrentResult.Play.whoseTurn);

            if (controller == this.mySeat && this.EventBus != null)
            {
                var myCard = this.FindCard(whoseTurn, leadSuit, trump, trumpAllowed, leadSuitLength, trick);
                Log.Trace(3, "BridgeRobot.{2}.HandleCardNeeded: {0} plays {3}{1}", whoseTurn.ToString(), myCard.Suit.ToXML().ToLower(), this.mySeat.ToXML(), myCard.Rank.ToXML());
                this.EventBus.HandleCardPlayed(whoseTurn, myCard.Suit, myCard.Rank);
            }
        }

        #endregion
    }
}
