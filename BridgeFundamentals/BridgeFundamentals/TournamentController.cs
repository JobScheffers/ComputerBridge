using Sodes.Base;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Sodes.Bridge.Base
{
    public class TournamentController : BridgeEventBusClient
    {
        public Board2 currentBoard;
        private int boardNumber;
        private Tournament currentTournament;
        private ParticipantInfo participant;
        private Action onTournamentFinished;
        private BoardResultEventPublisher currentResult;

        public TournamentController(Tournament t, ParticipantInfo p) : base()
        {
            this.currentTournament = t;
            this.participant = p;
        }

        public async Task StartTournament(Action onTournamentFinish)
        {
            //Log.Trace("TournamentController2.StartTournament");
            this.boardNumber = 0;
            this.onTournamentFinished = onTournamentFinish;
            this.EventBus.HandleTournamentStarted(this.currentTournament.ScoringMethod, 120, this.participant.MaxThinkTime, this.currentTournament.EventName);
            this.EventBus.HandleRoundStarted(this.participant.PlayerNames.Names, new DirectionDictionary<string>(this.participant.ConventionCardNS, this.participant.ConventionCardWE));
            await this.NextBoard();
        }

        public override async void HandlePlayFinished(BoardResultRecorder currentResult)
        {
            Log.Trace("TournamentController2.HandlePlayFinished start");
            await this.currentTournament.SaveAsync(this.currentResult);
            //Log.Trace("TournamentController2.HandlePlayFinished after SaveAsync");
            await this.NextBoard();
            //Log.Trace("TournamentController2.HandlePlayFinished finished");
        }

        private async Task NextBoard()
        {
            Log.Trace("TournamentController.NextBoard start");
            this.boardNumber++;
            this.currentBoard = await this.currentTournament.GetNextBoardAsync(this.boardNumber, this.participant.UserId);
            if (this.currentBoard == null)
            {
                Log.Trace("TournamentController.NextBoard no next board");
                this.EventBus.Unlink(this);
                //Log.Trace("TournamentController2.NextBoard after BridgeEventBus.MainEventBus.Unlink");
                this.onTournamentFinished();
                //Log.Trace("TournamentController2.NextBoard after onTournamentFinished");
            }
            else
            {
                //System.Diagnostics.Tracing. Trace.Wr("{0} TournamentController2.NextBoard b={1}", DateTime.UtcNow, this.currentBoard.BoardNumber);
                this.EventBus.HandleBoardStarted(this.currentBoard.BoardNumber, this.currentBoard.Dealer, this.currentBoard.Vulnerable);
                foreach (var item in currentBoard.Distribution.Deal)
                {
                    this.EventBus.HandleCardPosition(item.Seat, item.Suit, item.Rank);
                }

                this.currentResult = new BoardResultEventPublisher("TournamentController", this.currentBoard, this.participant.PlayerNames.Names, this.EventBus);
                this.EventBus.HandleCardDealingEnded();
                //Debug.WriteLine("{0} BoardResult3.Start: 1st bid needed", DateTime.UtcNow);
                //this.EventBus.HandleBidNeeded(this.theAuction.WhoseTurn, this.theAuction.LastRegularBid, this.theAuction.AllowDouble, this.theAuction.AllowRedouble, null);
            }
        }
    }

    public class BoardResultEventPublisher : BoardResult
    {
        private bool dummyVisible;

        public BoardResultEventPublisher(string _owner, Board2 board, SeatCollection<string> newParticipants, BridgeEventBus bus)
            : base(_owner, board, newParticipants, bus)
        {
        }

        #region Bridge Event Handlers

        public override void HandleCardDealingEnded()
        {
            //Log.Trace("BoardResultEventPublisher.HandleCardDealingEnded: 1st bid needed from {0}", this.Auction.WhoseTurn);
            this.EventBus.HandleBidNeeded(this.Auction.WhoseTurn, this.Auction.LastRegularBid, this.Auction.AllowDouble, this.Auction.AllowRedouble);
        }

        public override void HandleBidDone(Seats source, Bid bid)
        {
            //Log.Trace("BoardResultEventPublisher.HandleBidDone: {0} bids {12}", source, bid);

            base.HandleBidDone(source, bid);
            if (this.Auction.Ended)
            {
                //Log.Trace("BoardResultEventPublisher.HandleBidDone: auction finished");
                if (this.Contract.Bid.IsRegular)
                {
                    this.EventBus.HandleAuctionFinished(this.Auction.Declarer, this.Play.Contract);
                    this.NeedCard();
                }
                else
                {
                    Log.Trace("BoardResultEventPublisher.HandleBidDone: all passed");
                    this.EventBus.HandlePlayFinished(this);
                }
            }
            else
            {
                //Log.Trace("BoardResultEventPublisher.HandleBidDone: next bid needed from {0}", this.Auction.WhoseTurn);
                this.EventBus.HandleBidNeeded(this.Auction.WhoseTurn, this.Auction.LastRegularBid, this.Auction.AllowDouble, this.Auction.AllowRedouble);
            }
        }

        public override void HandleCardPlayed(Seats source, Suits suit, Ranks rank)
        {
            //if (!this.theDistribution.Owns(source, card))
            //  throw new FatalBridgeException(string.Format("{0} does not own {1}", source, card));
            /// 18-03-08: cannot check here: hosted tournaments get a card at the moment the card is played
            /// 
            base.HandleCardPlayed(source, suit, rank);
            if (this.Play.PlayEnded)
            {
                this.EventBus.HandlePlayFinished(this);
            }
            else
            {
                if (!this.dummyVisible)
                {
                    this.dummyVisible = true;
                    this.EventBus.HandleNeedDummiesCards(this.Play.whoseTurn);
                }
                else if (this.Play.TrickEnded)
                {
                    this.EventBus.HandleTrickFinished(this.Play.whoseTurn, this.Play.Contract.tricksForDeclarer, this.Play.Contract.tricksForDefense);
                    this.NeedCard();
                }
                else
                {
                    this.NeedCard();
                }
            }
        }

        //public override void HandlePlayFinished(BoardResultRecorder currentResult)
        //{
        //    this.EventBus.Unlink(this);
        //}

        private void NeedCard()
        {
            if (this.Auction == null) throw new ObjectDisposedException("this.theAuction");
            if (this.Play == null) throw new ObjectDisposedException("this.thePlay");

            Seats controller = this.Play.whoseTurn;
            if (this.Play.whoseTurn == this.Auction.Declarer.Partner())
            {
                controller = this.Auction.Declarer;
            }

            int leadSuitLength = this.Distribution.Length(this.Play.whoseTurn, this.Play.leadSuit);
            Log.Trace("BoardResultEventPublisher.NeedCard from {0}", this.Play.whoseTurn);
            this.EventBus.HandleCardNeeded(
                controller
                , this.Play.whoseTurn
                , this.Play.leadSuit
                , this.Play.Trump
                , leadSuitLength == 0 && this.Play.Trump != Suits.NoTrump
                , leadSuitLength
                , this.Play.currentTrick
            );
        }

        public override void HandleShowDummy(Seats dummy)
        {
            this.NeedCard();
        }

        public override void HandleReadyForNextStep(Seats source, NextSteps readyForStep)
        {
            switch (readyForStep)
            {
                case NextSteps.NextStartPlay:
                    this.NeedCard();
                    break;
                case NextSteps.NextTrick:
                    this.NeedCard();
                    break;
                case NextSteps.NextShowScore:
                    break;
                case NextSteps.NextBoard:
                    break;
                default:
                    break;
            }
        }

        #endregion

    }

    public class ParticipantInfo
    {
        public Guid UserId { get; set; }
        public string ConventionCardNS { get; set; }
        public string ConventionCardWE { get; set; }
        public int MaxThinkTime { get; set; }
        public Participant PlayerNames { get; set; }
    }

    public delegate void TournamentFinishedHandler();

}
