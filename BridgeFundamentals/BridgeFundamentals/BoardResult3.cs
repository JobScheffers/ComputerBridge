using System;
using System.Diagnostics;
using System.Runtime.Serialization;
using System.Text;

namespace Sodes.Bridge.Base
{
    [DataContract]
    public class BoardResult3 : BridgeEventHandlers2
    {
        private double theTournamentScore;
        private Auction theAuction;
        private PlaySequence thePlay;
        private bool dummyVisible;
        private Distribution theDistribution;
        private Board2 parent;
        private int frequencyScore;
        private int frequencyCount;
        private BridgeEventBus myEventBus;

        [DataMember]
        private string[] theParticipants 
        {
            get
            {
                return new string[] { this.Participants.Names[Seats.North], this.Participants.Names[Seats.East], this.Participants.Names[Seats.South], this.Participants.Names[Seats.West] };
            }
            set
            {
                this.Participants = new Participant(value[0], value[1], value[2], value[3]);
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:Validate arguments of public methods", MessageId = "1")]
        public BoardResult3(Board2 board, Guid userId, Participant newParticipant)
            : this(board, userId, newParticipant.Names)
        {
        }

        public BoardResult3(Board2 board, Guid userId, SeatCollection<string> newParticipants) : this()
        {
            if (board == null) throw new ArgumentNullException("board");
            this.theDistribution = board.Distribution.Clone();
            this.Participants = new Participant(newParticipants);
            this.Board = board;
            this.dummyVisible = false;
            this.UserId = userId;
            this.TournamentId = board.TournamentId;
        }

        private BoardResult3()
        {
        }

        #region Public Properties

        [IgnoreDataMember]
        public Participant Participants { get; set; }

        [IgnoreDataMember]
        public Board2 Board
        {
            get { return this.parent; }
            set
            {
                if (value != this.parent)
                {
                    this.parent = value;
                    this.Dealer = value.Dealer;
                    this.Vulnerability = value.Vulnerable;
                    this.BoardId = value.BoardId;
                    if (this.theAuction == null)
                    {
                        this.theAuction = new Auction(value.Vulnerable, value.Dealer);
                    }
                    else
                    {
                        this.theAuction.BoardChanged(value);
                    }
                }
            }
        }

        [DataMember]
        public Guid UserId { get; set; }

        [IgnoreDataMember]
        public Contract Contract
        {
            get
            {
                if (this.Auction == null || (!this.Auction.Ended && this.Auction.Bids.Count > 0)) return null;
                return this.Auction.FinalContract;
            }
            set
            {
                if (this.Auction == null)
                {
                    if (this.Board == null)
                    {
                        this.Auction = new Base.Auction(this.Vulnerability, this.Dealer);
                    }
                    else
                    {
                        this.Auction = new Base.Auction(this.Board.Vulnerable, this.Board.Dealer);
                    }
                }
                this.Auction.FinalContract = value;
            }
        }

        [DataMember]
        public double TournamentScore
        {
            get
            {
                return theTournamentScore;
            }
            set
            {
                theTournamentScore = value;
            }
        }

        private Seats _dealer;
        [DataMember]
        internal Seats Dealer
        {
            get { return this._dealer; }
            set
            {
                if (value != this._dealer)
                {
                    this._dealer = value;
                    if (this.theAuction != null)
                    {
                        this.theAuction.Dealer = value;
                    }
                }
            }
        }

        private Vulnerable _vulnerability;
        [DataMember]
        internal Vulnerable Vulnerability
        {
            get { return this._vulnerability; }
            set
            {
                if (value != this._vulnerability)
                {
                    this._vulnerability = value;
                    if (this.theAuction != null)
                    {
                        this.theAuction.Vulnerability = value;
                    }
                }
            }
        }

        [DataMember]
        public int BoardId { get; set; }

        [DataMember]
        public Auction Auction
        {
            get
            {
                return theAuction;
            }
            set
            {
                if (value == null) throw new ArgumentNullException("value");
                if (this.Board == null)
                {
                    this.theAuction = new Auction(this.Vulnerability, this.Dealer);
                }
                else
                {
                    this.theAuction = new Auction(this.Board.Vulnerable, this.Board.Dealer);
                }
                foreach (var bid in value.Bids)
                {
                    this.theAuction.Record(bid);
                }
            }
        }

        [DataMember]
        public PlaySequence Play
        {
            get
            {
                return thePlay;
            }
            set
            {
                // Play can be null when Auction has not finished yet (happens when sending a bug report)
                //if (value == null) throw new ArgumentNullException("value");
                if (this.theAuction != null && this.theAuction.Ended)
                {
                    this.thePlay = new Sodes.Bridge.Base.PlaySequence(this.theAuction.FinalContract, 13);
                    this.thePlay.Contract.tricksForDeclarer = 0;
                    this.thePlay.Contract.tricksForDefense = 0;
                    foreach (var item in value.play)
                    {
                        this.thePlay.Record(item.Suit, item.Rank);
                    }
                    //if (this.theAuction != null && value.Contract == null)
                    //{
                    //  this.thePlay.Contract = this.theAuction.FinalContract;
                    //}
                }
                else
                {
                    this.thePlay = value;
                }
            }
        }

        //[DataMember]
        public string TeamName
        {
            get
            {
                return this.Participants.Names[Seats.North] + "/" + this.Participants.Names[Seats.South]
                    //+ " - " + this.theParticipants[Seats.West] + "/" + this.theParticipants[Seats.East]
                    ;
            }
            //internal set		// required for DataContract
            //{
            //}
        }

        [IgnoreDataMember]
        public bool IsFrequencyTable { get; set; }

        [IgnoreDataMember]
        public int NorthSouthScore
        {
            get
            {
                if (this.IsFrequencyTable)
                {
                    return this.frequencyScore;
                }
                else
                {
                    if (this.thePlay == null && !(!this.theAuction.Ended && this.Contract != null)) return -100000;
                    return this.Contract.Score * (this.Contract.Declarer == Seats.North || this.Contract.Declarer == Seats.South ? 1 : -1);
                }
            }
            set
            {
                if (!this.IsFrequencyTable) throw new InvalidOperationException("Cannot set for a regular result");
                this.frequencyScore = value;
            }
        }

        [IgnoreDataMember]
        public int Multiplicity
        {
            get
            {
                if (!this.IsFrequencyTable) throw new InvalidOperationException("Cannot get for a regular result");
                return this.frequencyCount;
            }
            set
            {
                if (!this.IsFrequencyTable) throw new InvalidOperationException("Cannot set for a regular result");
                this.frequencyCount = value;
            }
        }

        [DataMember]
        public int TournamentId { get; set; }

        [IgnoreDataMember]
        public BridgeEventBus EventBus
        {
            get
            {
                if (this.myEventBus == null) this.myEventBus = BridgeEventBus.MainEventBus;
                return this.myEventBus;
            }
            set
            {
                this.myEventBus = value;
            }
        }
        #endregion

        #region Public Methods

        public void Start()
        {
            this.EventBus.Link(this);
            foreach (var item in this.theDistribution.Deal)
            {
                this.EventBus.HandleCardPosition(item.Seat, item.Suit, item.Rank);
            }

            Debug.WriteLine("{0} BoaardResult3.Start: 1st bid needed", DateTime.UtcNow);
            this.EventBus.HandleBidNeeded(this.theAuction.WhoseTurn, this.theAuction.LastRegularBid, this.theAuction.AllowDouble, this.theAuction.AllowRedouble, null);
        }

        public void CopyBoardData(Vulnerable vulnerability, Distribution boardDistribution)
        {
            if (boardDistribution == null) throw new ArgumentNullException("boardDistribution");
            if (this.theAuction == null)
            {
                this.theAuction = new Auction(this.Board.Vulnerable, this.Board.Dealer);
            }
            else
            {
                if (this.theAuction.Ended)
                {
                    this.theAuction.FinalContract.Vulnerability = vulnerability;
                }
            }

            this.theDistribution = boardDistribution.Clone();
        }

        public override string ToString()
        {
            StringBuilder result = new StringBuilder();
            result.AppendLine("Result for " + this.TeamName);
            result.Append(this.Auction.ToString());
            if (this.thePlay != null)
            {
                result.Append(this.thePlay.ToString());
            }

            return result.ToString();
        }

        public override bool Equals(object obj)
        {
            var otherResult = obj as BoardResult2;
            if (otherResult == null) return false;
            if (this.Auction.AantalBiedingen != otherResult.Auction.AantalBiedingen) return false;
            if (this.Auction.Dealer != otherResult.Auction.Dealer) return false;
            if (this.Auction.Declarer != otherResult.Auction.Declarer) return false;
            if (this.Auction.WhoseTurn != otherResult.Auction.WhoseTurn) return false;
            if (this.Participants.Names[Seats.South] != otherResult.Participants.Names[Seats.South]) return false;
            if (this.Play.completedTricks != otherResult.Play.completedTricks) return false;
            if (this.Play.currentTrick != otherResult.Play.currentTrick) return false;
            if (this.TeamName != otherResult.TeamName) return false;
            if (this.TournamentScore != otherResult.TournamentScore) return false;
            if (this.Contract.Bid != otherResult.Contract.Bid) return false;
            if (this.Contract.Vulnerability != otherResult.Contract.Vulnerability) return false;
            if (this.NorthSouthScore != otherResult.NorthSouthScore) return false;
            return true;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        #endregion

        #region Bridge Event Handlers

        public override void HandleBoardStarted(int boardNumber, Seats dealer, Vulnerable vulnerabilty)
        {
        }

        public override void HandleCardPosition(Seats seat, Suits suit, Ranks rank)
        {
            if (this.theDistribution.Incomplete)
            {		// this should only happen in a hosted tournament
                this.theDistribution.Give(seat, suit, rank);
            }
        }

        public override void HandleCardDealingEnded()
        {
            //Debug.WriteLine("{0} BoaardResult3.HandleCardDealingEnded: 1st bid needed", DateTime.UtcNow);
            this.EventBus.HandleBidNeeded(this.theAuction.WhoseTurn, this.theAuction.LastRegularBid, this.theAuction.AllowDouble, this.theAuction.AllowRedouble, null);
        }

        public override void HandleBidDone(Seats source, Bid bid)
        {
            if (bid == null) throw new ArgumentNullException("bid");
            if (!bid.Hint)
            {
                this.theAuction.Record(bid.Clone());
                if (this.theAuction.Ended)
                {
                    this.thePlay = new PlaySequence(this.theAuction.FinalContract, 13);
                    if (this.Contract.Bid.IsRegular)
                    {
                        this.EventBus.HandleAuctionFinished(this.theAuction.Declarer, this.thePlay.Contract);
                        this.NeedCard();
                    }
                    else
                    {
                        this.EventBus.HandlePlayFinished(this);
                    }
                }
                else
                {
                    this.EventBus.HandleBidNeeded(this.theAuction.WhoseTurn, this.theAuction.LastRegularBid, this.theAuction.AllowDouble, this.theAuction.AllowRedouble, null);
                }
            }
        }

        public override void HandleCardPlayed(Seats source, Suits suit, Ranks rank)
        {
            //if (!this.theDistribution.Owns(source, card))
            //  throw new FatalBridgeException(string.Format("{0} does not own {1}", source, card));
            /// 18-03-08: cannot check here: hosted tournaments get a card at the moment the card is played
            /// 
            if (this.thePlay != null && this.theDistribution != null)
            {
                this.thePlay.Record(suit, rank);
                this.theDistribution.Played(source, suit, rank);

                if (this.thePlay.PlayEnded)
                {
                    this.EventBus.HandlePlayFinished(this);
                }
                else
                {
                    if (this.thePlay.TrickEnded)
                    {
                        this.EventBus.HandleTrickFinished(this.thePlay.whoseTurn, this.thePlay.Contract.tricksForDeclarer, this.thePlay.Contract.tricksForDefense);
                    }
                    else if (!this.dummyVisible)
                    {
                        this.dummyVisible = true;
                        //this.EventBus.HandleNeedDummiesCards(this.thePlay.whoseTurn);
                        foreach (var item in this.theDistribution.Deal)
                        {
                            if (item.Seat == this.Play.Dummy)
                            {
                                this.EventBus.HandleCardPosition(item.Seat, item.Suit, item.Rank);
                            }
                        }
                    }

                    this.NeedCard();
                }
            }
        }

        public override void HandlePlayFinished(BoardResult3 currentResult)
        {
            this.EventBus.Unlink(this);
        }

        private void NeedCard()
        {
            //Trace.WriteLine(string.Format("BoardResult.NeedCard"));
            if (this.theAuction == null) throw new ObjectDisposedException("this.theAuction");
            if (this.thePlay == null) throw new ObjectDisposedException("this.thePlay");
            if (this.theDistribution == null) throw new ObjectDisposedException("this.theDistribution");

            Seats controller = this.thePlay.whoseTurn;
            if (this.thePlay.whoseTurn == this.theAuction.Declarer.Partner())
            {
                controller = this.theAuction.Declarer;
            }

            int leadSuitLength = this.theDistribution.Length(this.thePlay.whoseTurn, this.thePlay.leadSuit);
            this.EventBus.HandleCardNeeded(
                controller
                , this.thePlay.whoseTurn
                , this.thePlay.leadSuit
                , this.thePlay.Trump
                , leadSuitLength == 0 && this.thePlay.Trump != Suits.NoTrump
                , leadSuitLength
                , this.thePlay.currentTrick
                , null
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
}
