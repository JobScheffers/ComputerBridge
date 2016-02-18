using System;
using System.Runtime.Serialization;

namespace Sodes.Bridge.Base
{
    #region All bridge event delegates

    /// <summary>
    /// Handler for TournamentStarted event
    /// </summary>
    /// <param name="scoring">Scoring method for this tournament</param>
    /// <param name="maxTimePerBoard">Maximum time a robot may use for one board</param>
    /// <param name="maxTimePerCard">Maximum time a robot may use thinking about one card</param>
    /// <param name="tournamentName">Name of the tournament</param>
    /// <param name="participants">All contenders in the match. Certain names ('computer') have special meaning</param>
    public delegate void TournamentStartedHandler(Scorings Scoring, int maxTimePerBoard, int maxTimePerCard, string tournamentName, SeatCollection<string> participantNames, SeatCollection<SeatType> participantTypes, DirectionDictionary<string> conventionCards, bool allowReplay, bool showOnlineScore, bool allowOvercalls);
    public delegate void TournamentStartedHandler2(Scorings Scoring, int maxTimePerBoard, int maxTimePerCard, string tournamentName);

    /// <summary>
    /// Handler for RoundStarted event
    /// </summary>
    /// <param name="participants">All contenders in the match. Certain names ('computer') have special meaning</param>
    public delegate void RoundStartedHandler(SeatCollection<string> participantNames, DirectionDictionary<string> conventionCards);

    /// <summary>
    /// Handler for BoardStarted event
    /// </summary>
    /// <param name="boardNumber">The number of the board that just started</param>
    /// <param name="dealer">The dealer for this board</param>
    /// <param name="vulnerabilty">The vulnerability for this board</param>
    public delegate void BoardStartedHandler(int boardNumber, Seats dealer, Vulnerable vulnerabilty);

    /// <summary>
    /// Handler for CardPosition event
    /// This event fires for every card that the TournamentDirector publishes
    /// </summary>
    /// <param name="source">The owner of the card</param>
    /// <param name="suit">The suit of the card</param>
    /// <param name="rank">The rank of the card</param>
    public delegate void CardPositionHandler(Seats source, Suits suit, Ranks rank);

    /// <summary>
    /// Handler for CardPosition event
    /// This event fires for every card that the TournamentDirector publishes
    /// </summary>
    /// <param name="source">The owner of the card</param>
    /// <param name="suit">The suit of the card</param>
    /// <param name="rank">The rank of the card</param>
    public delegate void DummiesCardPositionHandler(Suits suit, Ranks rank);

    /// <summary>
    /// Handler for BidNeeded event
    /// </summary>
    /// <param name="whoseTurn">The player that must make the bid</param>
    public delegate void BidNeededHandler(Seats whoseTurn, Bid lastRegularBid, bool allowDouble, bool allowRedouble, Bid givenBid);

    /// <summary>
    /// Handler for BidDone event
    /// </summary>
    /// <param name="source">The player that made the bid</param>
    /// <param name="bid">The bid that was made</param>
    public delegate void BidDoneHandler(Seats source, Bid bid);

    /// <summary>
    /// Handler for AuctionFinished event
    /// </summary>
    /// <param name="declarer">The declarer of the contract</param>
    /// <param name="finalContract">The bid that won the auction</param>
    /// <param name="humanActivelyInvolved">Indicator for human participation during play.
    /// Although a human is involved during bidding, he will not be involved when becoming dummy.</param>
    public delegate void AuctionFinishedHandler(Seats declarer, Contract finalContract);

    /// <summary>
    /// Handler for ShowDummy event
    /// </summary>
    /// <param name="dummy">The player that has become dummy</param>
    public delegate void ShowDummyHandler(Seats dummy);

    public delegate void CardNeededHandler(Seats controller, Seats whoseTurn, Suits leadSuit, Suits trump, bool trumpAllowed, int leadSuitLength, int trick, Card allowedCard);

    /// <summary>
    /// Handler for CardPlayed event
    /// </summary>
    /// <param name="source">The player that played the card</param>
    /// <param name="card">The card being played</param>
    public delegate void CardPlayedHandler(Seats source, Card card);
    public delegate void CardPlayedHandler2(Seats source, Suits suit, Ranks rank);

    /// <summary>
    /// Handler for CardHint event
    /// </summary>
    /// <param name="source">The player that played the card</param>
    /// <param name="card">The card being played</param>
    public delegate void CardHintHandler(Seats source, CardPlayedHandler callback);

    /// <summary>
    /// Handler for TrickFinished event
    /// </summary>
    /// <param name="trickWinner">The player that won this trick</param>
    /// <param name="tricksForDeclarer">Number of tricks for the declarer</param>
    /// <param name="tricksForDefense">Number of tricks for the defense</param>
    public delegate void TrickFinishedHandler(Seats trickWinner, int tricksForDeclarer, int tricksForDefense);

    /// <summary>
    /// Handler for PlayFinished event
    /// </summary>
    /// <param name="finalContract">String representation of the final result (3NT+1 +430)</param>
    /// <param name="resultCount">Number of results that exist for this board</param>
    public delegate void PlayFinishedHandler(BoardResult2 currentResult);
    public delegate void PlayFinishedHandler2(BoardResult3 currentResult);

    /// <summary>
    /// Handler for ReadyForNextStep event
    /// TournamentDirector signals all participants (UI and robots) that they must answer when ready for the next step in board play
    /// </summary>
    /// <param name="source">The player/robot who signals he is ready for the next step</param>
    /// <param name="readyForStep">Confirmation of the step he is ready for</param>
    public delegate void ReadyForNextStepHandler(Seats source, NextSteps readyForStep);

    /// <summary>
    /// Handler for ReadyForBoardScore event
    /// </summary>
    /// <param name="resultCount">Number of results that exist for this board</param>
    public delegate void ReadyForBoardScoreHandler(int resultCount, Board2 currentBoard);

    /// <summary>
    /// Handler for TournamentStopped event
    /// </summary>
    public delegate void TournamentStoppedHandler();

    /// <summary>
    /// Handler for CardDealingEnded event
    /// </summary>
    public delegate void CardDealingEndedHandler();

    public delegate void StatusChangedHandler(string status);

    public delegate void TimeUsedHandler(TimeSpan boardByNS, TimeSpan totalByNS, TimeSpan boardByEW, TimeSpan totalByEW);

    /// <summary>
    /// Handler for LongTrace event
    /// </summary>
    /// <param name="trace">Trace data</param>
    public delegate void LongTraceHandler(string trace);
    #endregion

    public enum SeatType
    {
        Human,
        Robot,
        Remote
    }

    /// <summary>
    /// Possible steps that occur during play of a board
    /// </summary>
    public enum NextSteps
    {
        /// <summary>
        /// Prepare for play of the board
        /// </summary>
        NextStartPlay,

        /// <summary>
        /// Prepare for the next trick
        /// </summary>
        NextTrick,

        /// <summary>
        /// Prepare for showing the result of this board
        /// </summary>
        NextShowScore,

        /// <summary>
        /// Prepare for the next board
        /// </summary>
        NextBoard

        /// <summary>
            /// Prepare for the same board
            /// </summary>
        , SameBoard
    }

    [DataContract]
    public abstract class BridgeEventHandlers
    {
        public static object OneAtATime = new object();
        public event TournamentStartedHandler OnTournamentStarted;
        public event BoardStartedHandler OnBoardStarted;
        public event BidNeededHandler OnBidNeeded;
        public event BidDoneHandler OnBidDone;
        public event AuctionFinishedHandler OnAuctionFinished;

        private CardNeededHandler onCardNeeded;
        public event CardNeededHandler OnCardNeeded
        {
            add
            {
                if (value == null) throw new ArgumentNullException("value");
                if (value.Target is StatelessBridgeRobot)
                {
                    this.onCardNeeded = (CardNeededHandler)Delegate.Combine(value, this.onCardNeeded);
                }
                else
                {
                    this.onCardNeeded = (CardNeededHandler)Delegate.Combine(this.onCardNeeded, value);
                }
            }
            remove
            {
                this.onCardNeeded -= value;
            }
        }

        private CardPlayedHandler onCardPlayed;
        public event CardPlayedHandler OnCardPlayed
        {
            add
            {
                if (value == null) throw new ArgumentNullException("value");
                if (value.Target is StatelessBridgeRobot)
                {
                    this.onCardPlayed = (CardPlayedHandler)Delegate.Combine(value, this.onCardPlayed);
                }
                else
                {
                    this.onCardPlayed = (CardPlayedHandler)Delegate.Combine(this.onCardPlayed, value);
                }
            }
            remove
            {
                this.onCardPlayed -= value;
            }
        }

        public event TrickFinishedHandler TrickFinished;
        public event PlayFinishedHandler PlayFinished;
        public event ReadyForNextStepHandler OnReadyForNextStep;
        public event TournamentStoppedHandler OnTournamentStopped;
        public event ReadyForBoardScoreHandler OnOriginalDistributionRestoreFinished;
        public event TimeUsedHandler OnTimeUsed;

        #region Empty event handlers
        public virtual void HandleTournamentStarted(Scorings Scoring, int maxTimePerBoard, int maxTimePerCard, string tournamentName, SeatCollection<string> participantNames, SeatCollection<SeatType> participantTypes, DirectionDictionary<string> conventionCards, bool allowReplay, bool showOnlineScore, bool allowOvercalls) { }
        public virtual void HandleBoardStarted(int boardNumber, Seats dealer, Vulnerable vulnerabilty) { }
        public virtual void HandleBidNeeded(Seats whoseTurn, Bid lastRegularBid, bool allowDouble, bool allowRedouble, Bid givenBid) { }
        public virtual void HandleBidDone(Seats source, Bid bid) { }
        public virtual void HandleAuctionFinished(Seats declarer, Contract finalContract) { }
        public virtual void HandleCardNeeded(Seats controller, Seats whoseTurn, Suits leadSuit, Suits trump, bool trumpAllowed, int leadSuitLength, int trick, Card allowedCard) { }
        public virtual void HandleCardPlayed(Seats source, Card card) { }
        public virtual void HandleTrickFinished(Seats trickWinner, int tricksForDeclarer, int tricksForDefense) { }
        protected virtual void HandlePlayFinished(BoardResult2 currentResult) { }
        protected virtual void HandleReadyForNextStep(Seats source, NextSteps readyForStep) { }
        protected virtual void HandleReadyForBoardScore(int resultCount, Board2 currentBoard) { }
        //protected virtual void HandleTimeUsed(TimeSpan boardByNS, TimeSpan totalByNS, TimeSpan boardByEW, TimeSpan totalByEW) {}
        protected virtual void HandleTimeUsed(TimeSpan boardByNS, TimeSpan totalByNS, TimeSpan boardByEW, TimeSpan totalByEW) { }
        protected virtual void HandleTournamentStopped() { }
        #endregion

        #region Fire event

        protected void FireTournamentStarted(Scorings scoring, int maxTimePerBoard, int maxTimePerCard, string tournamentName, SeatCollection<string> participantNames, SeatCollection<SeatType> participantTypes, DirectionDictionary<string> conventionCards, bool allowReplay, bool onlineScore, bool allowOvercalls)
        {
            if (this.OnTournamentStarted != null)
            {
                this.OnTournamentStarted(scoring, maxTimePerBoard, maxTimePerCard, tournamentName, participantNames, participantTypes, conventionCards, allowReplay, onlineScore, allowOvercalls);
            }
        }

        protected void FireBoardStarted(int boardNumber, Seats dealer, Vulnerable vulnerabilty)
        {
            if (this.OnBoardStarted != null)
            {
                this.OnBoardStarted(boardNumber, dealer, vulnerabilty);
            }
        }

        protected void FireBidNeeded(Seats whoseTurn, Bid lastRegularBid, bool allowDouble, bool allowRedouble, Bid givenBid)
        {
            if (this.OnBidNeeded != null)
            {
                this.OnBidNeeded(whoseTurn, lastRegularBid, allowDouble, allowRedouble, givenBid);
            }
        }

        protected void FireBidDone(Seats source, Bid bid)
        {
            if (this.OnBidDone != null)
            {
                this.OnBidDone(source, bid);
            }
        }

        protected void FireAuctionFinished(Seats declarer, Contract finalContract)
        {
            if (this.OnAuctionFinished != null)
            {
                this.OnAuctionFinished(declarer, finalContract);
            }
        }

        protected void FireCardNeeded(Seats controller, Seats whoseTurn, Suits leadSuit, Suits trump, bool trumpAllowed, int leadSuitLength, int trick, Card allowedCard)
        {
            if (this.onCardNeeded != null)
            {
                this.onCardNeeded(controller, whoseTurn, leadSuit, trump, trumpAllowed, leadSuitLength, trick, allowedCard);
            }
        }

        protected void FireCardPlayed(Seats source, Card card)
        {
            if (this.onCardPlayed != null)
            {
                this.onCardPlayed(source, card);
            }
        }

        protected void FireTrickFinished(Seats trickWinner, int tricksForDeclarer, int tricksForDefense)
        {
            if (this.TrickFinished != null)
            {
                this.TrickFinished(trickWinner, tricksForDeclarer, tricksForDefense);
            }
        }

        protected void FirePlayFinished(BoardResult2 currentResult)
        {
            if (this.PlayFinished != null)
            {
                this.PlayFinished(currentResult);
            }
        }

        protected void FireReadyForBoardScore(int resultCount, Board2 currentBoard)
        {
            if (this.OnOriginalDistributionRestoreFinished != null)
            {
                this.OnOriginalDistributionRestoreFinished(resultCount, currentBoard);
            }
        }

        protected void FireReadyForNextStep(Seats source, NextSteps readyForStep)
        {
            if (this.OnReadyForNextStep != null)
            {
                this.OnReadyForNextStep(source, readyForStep);
            }
        }

        protected void FireTournamentStopped()
        {
            if (this.OnTournamentStopped != null)
            {
                this.OnTournamentStopped();
            }
        }

        protected void FireTimeUsed(TimeSpan boardByNS, TimeSpan totalByNS, TimeSpan boardByEW, TimeSpan totalByEW)
        {
            if (this.OnTimeUsed != null)
            {
                this.OnTimeUsed(boardByNS, totalByNS, boardByEW, totalByEW);
            }
        }

        #endregion

        public void CrossLink(BridgeEventHandlers other)
        {
            this.Link(other);
            other.Link(this);
        }

        public void CrossUnlink(BridgeEventHandlers other)
        {
            this.Unlink(other);
            other.Unlink(this);
        }

        protected virtual void Link(BridgeEventHandlers other)
        {
            if (other == null) throw new ArgumentNullException("other");
            this.OnTournamentStarted += new TournamentStartedHandler(other.HandleTournamentStarted);
            this.OnBoardStarted += new BoardStartedHandler(other.HandleBoardStarted);
            this.OnBidNeeded += new BidNeededHandler(other.HandleBidNeeded);
            this.OnBidDone += new BidDoneHandler(other.HandleBidDone);
            this.OnAuctionFinished += new AuctionFinishedHandler(other.HandleAuctionFinished);
            this.OnCardNeeded += new CardNeededHandler(other.HandleCardNeeded);
            this.OnCardPlayed += new CardPlayedHandler(other.HandleCardPlayed);
            this.TrickFinished += new TrickFinishedHandler(other.HandleTrickFinished);
            this.PlayFinished += new PlayFinishedHandler(other.HandlePlayFinished);
            this.OnReadyForNextStep += new ReadyForNextStepHandler(other.HandleReadyForNextStep);
            this.OnOriginalDistributionRestoreFinished += new ReadyForBoardScoreHandler(other.HandleReadyForBoardScore);
            this.OnTimeUsed += new TimeUsedHandler(other.HandleTimeUsed);
            this.OnTournamentStopped += new TournamentStoppedHandler(other.HandleTournamentStopped);
        }

        protected virtual void Unlink(BridgeEventHandlers other)
        {
            this.OnTournamentStarted -= new TournamentStartedHandler(other.HandleTournamentStarted);
            this.OnBoardStarted -= new BoardStartedHandler(other.HandleBoardStarted);
            this.OnBidNeeded -= new BidNeededHandler(other.HandleBidNeeded);
            this.OnBidDone -= new BidDoneHandler(other.HandleBidDone);
            this.OnAuctionFinished -= new AuctionFinishedHandler(other.HandleAuctionFinished);
            this.OnCardNeeded -= new CardNeededHandler(other.HandleCardNeeded);
            this.OnCardPlayed -= new CardPlayedHandler(other.HandleCardPlayed);
            this.TrickFinished -= new TrickFinishedHandler(other.HandleTrickFinished);
            this.PlayFinished -= new PlayFinishedHandler(other.HandlePlayFinished);
            this.OnReadyForNextStep -= new ReadyForNextStepHandler(other.HandleReadyForNextStep);
            this.OnOriginalDistributionRestoreFinished -= new ReadyForBoardScoreHandler(other.HandleReadyForBoardScore);
            this.OnTimeUsed -= new TimeUsedHandler(other.HandleTimeUsed);
            this.OnTournamentStopped -= new TournamentStoppedHandler(other.HandleTournamentStopped);
        }
    }

    /// <summary>
    /// Handler for those that should know of all 4 hands (Table, TrafficManager, TournamentController)
    /// </summary>
    [DataContract]
    public abstract class AllHandsEventHandlers : BridgeEventHandlers
    {
        public event CardPositionHandler OnCardPosition;
        public event DummiesCardPositionHandler OnDummiesCardPosition;
        public event CardDealingEndedHandler OnCardDealingEnded;
        public event ShowDummyHandler OnNeedDummiesCards;
        public event ShowDummyHandler OnShowDummy;

        protected override void Link(BridgeEventHandlers other)
        {
            if (other == null) throw new ArgumentNullException("other");
            base.Link(other);
            AllHandsEventHandlers theOther = other as AllHandsEventHandlers;
            if (theOther != null)
            {
                this.OnCardPosition += new CardPositionHandler(theOther.HandleReceiveCardPosition);
                this.OnDummiesCardPosition += new DummiesCardPositionHandler(theOther.HandleDummiesCardPosition);
                this.OnCardDealingEnded += new CardDealingEndedHandler(theOther.HandleCardDealingEnded);
                this.OnNeedDummiesCards += new ShowDummyHandler(theOther.HandleNeedDummiesCards);
                this.OnShowDummy += new ShowDummyHandler(theOther.HandleShowDummy);
            }
        }

        protected override void Unlink(BridgeEventHandlers other)
        {
            base.Unlink(other);
            AllHandsEventHandlers theOther = other as AllHandsEventHandlers;
            if (theOther != null)
            {
                this.OnCardPosition -= new CardPositionHandler(theOther.HandleReceiveCardPosition);
                this.OnDummiesCardPosition -= new DummiesCardPositionHandler(theOther.HandleDummiesCardPosition);
                this.OnCardDealingEnded -= new CardDealingEndedHandler(theOther.HandleCardDealingEnded);
                this.OnNeedDummiesCards += new ShowDummyHandler(theOther.HandleNeedDummiesCards);
                this.OnShowDummy -= new ShowDummyHandler(theOther.HandleShowDummy);
            }
        }

        protected virtual void HandleReceiveCardPosition(Seats seat, Suits suit, Ranks rank) { }
        protected virtual void HandleDummiesCardPosition(Suits suit, Ranks rank) { }
        protected virtual void HandleCardDealingEnded() { }
        protected virtual void HandleNeedDummiesCards(Seats dummy) { }
        protected virtual void HandleShowDummy(Seats dummy) { }
        protected override void HandleTimeUsed(TimeSpan boardByNS, TimeSpan totalByNS, TimeSpan boardByEW, TimeSpan totalByEW) { }

        protected void FireCardPosition(Seats seat, Suits suit, Ranks rank)
        {
            if (this.OnCardPosition != null)
            {
                this.OnCardPosition(seat, suit, rank);
            }
        }

        protected void FireDummiesCardPosition(Suits suit, Ranks rank)
        {
            if (this.OnDummiesCardPosition != null)
            {
                this.OnDummiesCardPosition(suit, rank);
            }
        }

        protected void FireCardDealingEnded()
        {
            if (this.OnCardDealingEnded != null)
            {
                this.OnCardDealingEnded();
            }
        }

        protected void FireNeedDummiesCards(Seats dummy)
        {
            if (this.OnNeedDummiesCards != null)
            {
                this.OnNeedDummiesCards(dummy);
            }
        }

        protected void FireShowDummy(Seats dummy)
        {
            if (this.OnShowDummy != null)
            {
                this.OnShowDummy(dummy);
            }
        }
    }
}