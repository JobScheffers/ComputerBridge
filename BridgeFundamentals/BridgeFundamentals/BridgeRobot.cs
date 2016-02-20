using System;
using System.Diagnostics;

namespace Sodes.Bridge.Base
{
    /// <summary>
    /// Base for the robot that has to implement bidiing and playing tactics.
    /// </summary>
    public class BridgeRobot : BridgeEventBusClient
    {
        private Seats mySeat;
        private BoardResultRecorder boardResult;

        public BridgeRobot(Seats seat)
        {
            this.mySeat = seat;
        }

        public override void HandleBoardStarted(int boardNumber, Seats dealer, Vulnerable vulnerabilty)
        {
            this.boardResult = new BoardResultRecorder(null);
            this.boardResult.HandleBoardStarted(boardNumber, dealer, vulnerabilty);
        }

        public override void HandleBidNeeded(Seats whoseTurn, Bid lastRegularBid, bool allowDouble, bool allowRedouble)
        {
            if (whoseTurn == this.mySeat)
            {
                var myBid = this.FindBid(lastRegularBid, allowDouble, allowRedouble);
                Debug.WriteLine("{0} BridgeRobot.HandleBidNeeded: {1} bids {2}", DateTime.UtcNow, whoseTurn, myBid);
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
                Debug.WriteLine("{0} BridgeRobot.HandleCardNeeded: {1} plays {2}", DateTime.UtcNow, whoseTurn, myCard);
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

            throw new InvalidOperationException("");
        }
    }
}
