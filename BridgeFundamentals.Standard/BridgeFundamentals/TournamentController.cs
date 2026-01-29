using System;
using System.Threading;
using System.Threading.Tasks;

namespace Bridge
{
    public class TournamentController(Tournament t, ParticipantInfo p, BridgeEventBus bus) : BoardResultOwner("TournamentController", bus)
    {
        public Board2 currentBoard;
        private int boardNumber;
        private readonly Tournament currentTournament = t;
        protected ParticipantInfo participant = p;
        //private Action onTournamentFinished;
        private SemaphoreSlim waiter;

        public TournamentController(Tournament t, ParticipantInfo p) : this(t, p, BridgeEventBus.MainEventBus)
        {
        }

        public async Task StartTournamentAsync(int firstBoard)
        {
            Log.Trace(2, "TournamentController.StartTournamentAsync begin");
            this.boardNumber = firstBoard - 1;
            this.EventBus.HandleTournamentStarted(this.currentTournament.ScoringMethod, 120, this.participant.MaxThinkTime, this.currentTournament.EventName);
            this.EventBus.HandleRoundStarted(this.participant.PlayerNames.Names, new DirectionDictionary<string>(this.participant.ConventionCardNS, this.participant.ConventionCardWE));
            this.waiter = new SemaphoreSlim(initialCount: 0);
            await this.NextBoard().ConfigureAwait(false);
            await this.waiter.WaitAsync().ConfigureAwait(false);
            Log.Trace(4, "TournamentController.StartTournamentAsync end");
        }

        public void StartTournament(int firstBoard)
        {
            Log.Trace(2, "TournamentController.StartTournament");
            this.waiter = new SemaphoreSlim(initialCount: 0);
            this.boardNumber = firstBoard - 1;
            this.EventBus.HandleTournamentStarted(this.currentTournament.ScoringMethod, 120, this.participant.MaxThinkTime, this.currentTournament.EventName);
            this.EventBus.HandleRoundStarted(this.participant.PlayerNames.Names, new DirectionDictionary<string>(this.participant.ConventionCardNS, this.participant.ConventionCardWE));
        }

        public async Task StartNextBoard()
        {
            Log.Trace(2, "TournamentController2.StartNextBoard");
            await this.NextBoard().ConfigureAwait(false);
        }

        public override async void HandlePlayFinished(BoardResultRecorder currentResult)
        {
            Log.Trace(2, "TournamentController.HandlePlayFinished start");
            await this.currentTournament.SaveAsync(this.CurrentResult as BoardResult).ConfigureAwait(false);
            Log.Trace(3, "TournamentController.HandlePlayFinished after SaveAsync");
            await this.NextBoard().ConfigureAwait(false);
            Log.Trace(3, "TournamentController.HandlePlayFinished finished");
        }

        public override void HandleTournamentStopped()
        {
            base.HandleTournamentStopped();
        }

        private async Task NextBoard()
        {
            if (this.waiter == null) throw new ArgumentNullException("waiter");
            Log.Trace(3, "TournamentController.NextBoard start");
            await this.GetNextBoard().ConfigureAwait(false);
            if (this.currentBoard == null)
            {
                Log.Trace(2, "TournamentController.NextBoard no next board");
                this.EventBus.HandleTournamentStopped();
                //this.EventBus.Unlink(this);
                Log.Trace(5, "TournamentController.NextBoard after HandleTournamentStopped and before waiter.Release");
                this.waiter.Release();
                Log.Trace(5, "TournamentController.NextBoard after waiter.Release");
            }
            else
            {
                Log.Trace(1, $"TournamentController.NextBoard board={this.currentBoard.BoardNumber}");
                this.EventBus.HandleBoardStarted(this.currentBoard.BoardNumber, this.currentBoard.Dealer, this.currentBoard.Vulnerable);
                for (int card = 0; card < currentBoard.Distribution.Deal.Count; card++)
                {
                    DistributionCard item = currentBoard.Distribution.Deal[card];
                    this.EventBus.HandleCardPosition(item.Seat, item.Suit, item.Rank);
                }

                this.EventBus.HandleCardDealingEnded();
            }
        }

        protected virtual async Task GetNextBoard()
        {
            this.boardNumber++;
            this.currentBoard = await this.currentTournament.GetNextBoardAsync(this.boardNumber, this.participant.UserId).ConfigureAwait(false);
        }

        protected override BoardResultRecorder NewBoardResult(int boardNumber)
        {
            return new BoardResultEventPublisher($"TournamentController.Result.{currentBoard.BoardNumber}", currentBoard, this.participant.PlayerNames.Names, this.EventBus, this.currentTournament);
        }
    }

    public class BoardResultEventPublisher(string _owner, Board2 board, SeatCollection<string> newParticipants, BridgeEventBus bus, Tournament t) : BoardResult(_owner, board, newParticipants)
    {
        private bool dummyVisible = false;
        protected BridgeEventBus EventBus = bus;
        private readonly Tournament currentTournament = t;

        #region Bridge Event Handlers

        public override void HandleCardDealingEnded()
        {
            this.dummyVisible = false;
            base.HandleCardDealingEnded();
            //Log.Trace("BoardResultEventPublisher.HandleCardDealingEnded: 1st bid needed from {0}", this.Auction.WhoseTurn);
            this.EventBus.HandleBidNeeded(this.Auction.WhoseTurn, this.Auction.LastRegularBid, this.Auction.AllowDouble, this.Auction.AllowRedouble);
        }

        public override void HandleBidDone(Seats source, AuctionBid bid)
        {
            //Log.Trace("BoardResultEventPublisher.HandleBidDone: {0} bids {1}", source, bid);
            if (this.currentTournament != null && !this.currentTournament.AllowOvercalls && this.Auction.Opened && !source.IsSameDirection(this.Auction.Opener) && !bid.IsPass)
            {
                Log.Trace(1, "TournamentController overcall in bid contest: change to pass");
                bid = AuctionBid.Parse("Pass");
            }

            base.HandleBidDone(source, bid);
            if (this.Auction.Ended)
            {
                //Log.Trace("BoardResultEventPublisher.HandleBidDone: auction finished");
                if (this.Contract.Bid.IsRegular
                    && !(this.currentTournament != null && this.currentTournament.BidContest)
                    )
                {
                    this.EventBus.HandleAuctionFinished(this.Auction.Declarer, this.Play.Contract);
                    this.NeedCard();
                }
                else
                {
                    //Log.Trace("BoardResultEventPublisher.HandleBidDone: all passed");
                    this.EventBus.HandlePlayFinished(this);
                }
            }
            else
            {
                //Log.Trace("BoardResultEventPublisher.HandleBidDone: next bid needed from {0}", this.Auction.WhoseTurn);
                this.EventBus.HandleBidNeeded(this.Auction.WhoseTurn, this.Auction.LastRegularBid, this.Auction.AllowDouble, this.Auction.AllowRedouble);
            }
        }

        public override void HandleCardPlayed(Seats source, Suits suit, Ranks rank, string signal)
        {
            //Log.Trace("BoardResultEventPublisher({3}).HandleCardPlayed: {0} played {2}{1}", source, suit.ToXML(), rank.ToXML(), this.Owner);

            //if (!this.theDistribution.Owns(source, card))
            //  throw new FatalBridgeException(string.Format("{0} does not own {1}", source, card));
            /// 18-03-08: cannot check here: hosted tournaments get a card at the moment the card is played
            /// 

            if (this.Play == null)      // this is an event that is meant for the previous boardResult
                throw new NullReferenceException(nameof(Play));

            if (source != this.Play.whoseTurn)
                throw new ArgumentOutOfRangeException(nameof(source), $"Expected a card from {this.Play.whoseTurn.ToLocalizedString()}");

            base.HandleCardPlayed(source, suit, rank, signal);
            if (this.Play.PlayEnded)
            {
                //Log.Trace("BoardResultEventPublisher({0}).HandleCardPlayed: play finished", this.Owner);
                this.EventBus.HandleTrickFinished(this.Play.whoseTurn, this.Play.Contract.tricksForDeclarer, this.Play.Contract.tricksForDefense);
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

        public override void HandleNeedDummiesCards(Seats dummy)
        {
            //Log.Trace("BoardResultEventPublisher({0}).HandleNeedDummiesCards", this.Name);
            if (this.Distribution.Length(dummy) == 13)
            {
                foreach (var suit in SuitHelper.StandardSuitsDescending)
                {
                    foreach (var rank in RankHelper.RanksDescending)
                    {
                        if (this.Distribution.Owns(dummy, suit, rank)) this.EventBus.HandleCardPosition(dummy, suit, rank);
                    }
                }

                this.EventBus.HandleShowDummy(dummy);
            }
            else
            {
                //Log.Trace("BoardResultEventPublisher({0}).HandleNeedDummiesCards waits for dummies cards", this.Name);
            }
        }

        public override void HandleShowDummy(Seats dummy)
        {
            base.HandleShowDummy(dummy);
            this.NeedCard();
        }

        #endregion

        private void NeedCard()
        {
            ObjectDisposedException.ThrowIf(this.Auction == null, this.Auction);
            ObjectDisposedException.ThrowIf(this.Play == null, this.Play);

            Seats controller = this.Play.whoseTurn;
            if (this.Play.whoseTurn == this.Auction.Declarer.Partner())
            {
                controller = this.Auction.Declarer;
            }

            int leadSuitLength = this.Distribution.Length(this.Play.whoseTurn, this.Play.leadSuit);
            //Log.Trace("BoardResultEventPublisher({2}).NeedCard from {0} by {1}", this.Play.whoseTurn, controller, this.Name);
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
