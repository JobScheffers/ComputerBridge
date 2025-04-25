using System;
using System.Threading.Tasks;

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

        public BridgeRobot(Seats seat, BridgeEventBus bus) : base($"{seat}.BridgeRobot", bus)
        {
            this.mySeat = seat;
        }

        private Seats mySeat;

        public abstract Task<Bid> FindBid(Bid lastRegularBid, bool allowDouble, bool allowRedouble);

        public abstract Task<ExplainedCard> FindCard(Seats whoseTurn, Suits leadSuit, Suits trump, bool trumpAllowed, int leadSuitLength, int trick);

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

        public override async void HandleBidNeeded(Seats whoseTurn, Bid lastRegularBid, bool allowDouble, bool allowRedouble)
        {
            if (whoseTurn == this.mySeat && this.EventBus != null)
            {
                var myBid = await this.FindBid(lastRegularBid, allowDouble, allowRedouble).ConfigureAwait(false);
                Log.Trace(3, "BridgeRobot.{0}.HandleBidNeeded: bids {1}", this.mySeat.ToXML(), myBid);
                this.EventBus.HandleBidDone(this.mySeat, myBid);
            }
        }

        public override async void HandleCardNeeded(Seats controller, Seats whoseTurn, Suits leadSuit, Suits trump, bool trumpAllowed, int leadSuitLength, int trick)
        {
            if (whoseTurn != this.CurrentResult.Play.whoseTurn)
                throw new ArgumentOutOfRangeException("whoseTurn", "Expected a needcard from " + this.CurrentResult.Play.whoseTurn);

            if (controller == this.mySeat && this.EventBus != null)
            {
                var myCard = await this.FindCard(whoseTurn, leadSuit, trump, trumpAllowed, leadSuitLength, trick).ConfigureAwait(false);
                Log.Trace(3, "BridgeRobot.{2}.HandleCardNeeded: {0} plays {3}{1}", whoseTurn.ToString(), myCard.Card.Suit.ToXML().ToLower(), this.mySeat.ToXML(), myCard.Card.Rank.ToXML());
                this.EventBus.HandleCardPlayed(whoseTurn, myCard.Card.Suit, myCard.Card.Rank, myCard.Explanation);
            }
        }

        #endregion
    }

#if NET6_0_OR_GREATER

    public abstract class BridgeRobotBase(Seats _seat, string nameForLog) : AsyncBridgeEventRecorder(nameForLog)
    {
        protected readonly Seats mySeat = _seat;

        public abstract ValueTask<Bid> FindBid(Bid lastRegularBid, bool allowDouble, bool allowRedouble);

        public abstract ValueTask<ExplainedCard> FindCard(Seats whoseTurn, Suits leadSuit, Suits trump, bool trumpAllowed, int leadSuitLength, int trick);

        public override async ValueTask HandleBidNeeded(Seats whoseTurn, Bid lastRegularBid, bool allowDouble, bool allowRedouble)
        {
            if (whoseTurn == this.mySeat)
            {
                var myBid = await this.FindBid(lastRegularBid, allowDouble, allowRedouble).ConfigureAwait(false);
                //Log.Trace(3, "BridgeRobot.{0}.HandleBidNeeded: bids {1}", this.mySeat.ToXML(), myBid);
                await HandleBidDone(this.mySeat, myBid, DateTimeOffset.UtcNow);
            }
        }

        public override async ValueTask HandleCardNeeded(Seats controller, Seats whoseTurn, Suits leadSuit, Suits trump, bool trumpAllowed, int leadSuitLength, int trick)
        {
            if (controller == this.mySeat)
            {
                var myCard = await this.FindCard(whoseTurn, leadSuit, trump, trumpAllowed, leadSuitLength, trick).ConfigureAwait(false);
                //Log.Trace(3, "BridgeRobot.{2}.HandleCardNeeded: {0} plays {3}{1}", whoseTurn.ToString(), myCard.Suit.ToXML().ToLower(), this.mySeat.ToXML(), myCard.Rank.ToXML());
                await HandleCardPlayed(whoseTurn, myCard.Card.Suit, myCard.Card.Rank, myCard.Explanation, DateTimeOffset.UtcNow);
            }
        }
    }

#endif
}
