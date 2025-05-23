using System;
using System.Runtime.Serialization;

namespace Bridge
{
    [DataContract(Namespace = "http://schemas.datacontract.org/2004/07/Sodes.Bridge.Base")]     // namespace is needed to be backward compatible for old RoboBridge client
    public abstract class BridgeEventHandlers
    {
        protected virtual bool AllReadyForNextStep()
        {
            return true;
        }

        #region Empty event handlers

        public virtual void HandleTournamentStarted(Scorings scoring, int maxTimePerBoard, int maxTimePerCard, string tournamentName)
        {
        }

        public virtual void HandleRoundStarted(SeatCollection<string> participantNames, DirectionDictionary<string> conventionCards)
        {
        }

        public virtual void HandleBoardStarted(int boardNumber, Seats dealer, Vulnerable vulnerabilty)
        {
        }

        public virtual void HandleCardPosition(Seats seat, Suits suit, Ranks rank)
        {
        }

        public virtual void HandleBidNeeded(Seats whoseTurn, Bid lastRegularBid, bool allowDouble, bool allowRedouble)
        {
        }

        public virtual void HandleBidDone(Seats source, Bid bid)
        {
        }

        public virtual void HandleExplanationNeeded(Seats source, Bid bid)
        {
        }

        public virtual void HandleExplanationDone(Seats source, Bid bid)
        {
        }

        public virtual void HandleAuctionFinished(Seats declarer, Contract finalContract)
        {
        }

        public virtual void HandleCardNeeded(Seats controller, Seats whoseTurn, Suits leadSuit, Suits trump, bool trumpAllowed, int leadSuitLength, int trick)
        {
        }

        public virtual void HandleCardPlayed(Seats source, Suits suit, Ranks rank, string signal)
        {
        }

        public virtual void HandleTrickFinished(Seats trickWinner, int tricksForDeclarer, int tricksForDefense)
        {
        }

        public virtual void HandlePlayFinished(BoardResultRecorder currentResult)
        {
        }

        public virtual void HandleTimeUsed(TimeSpan boardByNS, TimeSpan totalByNS, TimeSpan boardByEW, TimeSpan totalByEW)
        {
        }

        public virtual void HandleTournamentStopped()
        {
        }

        public virtual void HandleCardDealingEnded()
        {
        }

        public virtual void HandleNeedDummiesCards(Seats dummy)
        {
        }

        public virtual void HandleShowDummy(Seats dummy)
        {
        }

        #endregion
    }
}