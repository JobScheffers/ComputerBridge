using System;
using System.Runtime.Serialization;
using System.Text;

namespace Bridge
{
    [DataContract(Namespace = "http://schemas.datacontract.org/2004/07/Sodes.Bridge.Base")]     // namespace is needed to be backward compatible for old RoboBridge client
    public class BoardResultRecorder : BridgeEventHandlers
    {
        private Auction theAuction;
        private PlaySequence thePlay;
        private Board2 parent;
        private int frequencyScore;
        private int frequencyCount;
        private Seats _dealer;
        private Vulnerable _vulnerability;

        public BoardResultRecorder(string _owner, Board2 board) : base()
        {
            //if (board == null) throw new ArgumentNullException("board");
            this.Board = board;
            this.Owner = _owner;
            this.Created = DateTime.Now;
            if (board == null)
            {
                this.Distribution = new Distribution();
            }
            else
            {
                this.Distribution = board.Distribution.Clone();
            }
        }

        public BoardResultRecorder(string _owner) : this(_owner, null)
        {
        }

        /// <summary>
        /// Only for deserialization
        /// </summary>
        protected BoardResultRecorder()
        {
        }

        protected string Owner;

        #region Public Properties

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

        [IgnoreDataMember]
        public Contract Contract
        {
            get
            {
                if (this.thePlay != null && this.thePlay.Contract != null) return this.thePlay.Contract;
                if (this.Auction == null || (!this.Auction.Ended && this.Auction.Bids.Count > 0)) return null;
                return this.Auction.FinalContract;
            }
            set
            {
                if (this.Auction == null)
                {
                    if (this.Board == null)
                    {
                        this.Auction = new Auction(this.Vulnerability, this.Dealer);
                    }
                    else
                    {
                        this.Auction = new Auction(this.Board.Vulnerable, this.Board.Dealer);
                    }
                }
                this.Auction.FinalContract = value;
            }
        }

        [DataMember]
        public double TournamentScore { get; set; }

        [DataMember]
        internal Seats Dealer
        {
            get { return this._dealer; }
            set
            {
                if (value != this._dealer)
                {
                    this._dealer = value;
                    this.theAuction?.Dealer = value;
                }
            }
        }

        [DataMember]
        internal Vulnerable Vulnerability
        {
            get { return this._vulnerability; }
            set
            {
                if (value != this._vulnerability)this._vulnerability = value;

                if (this.theAuction != null)
                {
                    this.theAuction.Vulnerability = value;
                    if (this.theAuction.Ended)
                    {
                        if (this.Contract != null && this.Contract.Vulnerability != value) this.Contract.Vulnerability = value;
                        if (this.theAuction.FinalContract != null && this.theAuction.FinalContract.Vulnerability != value) this.theAuction.FinalContract.Vulnerability = value;
                    }
                }

                if (this.thePlay != null && this.thePlay.Contract != null && this.thePlay.Contract.Vulnerability != value) this.thePlay.Contract.Vulnerability = value;
            }
        }

        [DataMember]
        public int BoardId { get; set; }

        [DataMember]
        public DateTime Created { get; set; }

        [IgnoreDataMember]
        public Distribution Distribution { get; private set; }

        [DataMember]
        public Auction Auction
        {
            get
            {
                return theAuction;
            }
            set
            {
                ArgumentNullException.ThrowIfNull(value);
                if (this.Board == null)
                {
                    this.theAuction = new Auction(this.Vulnerability, this.Dealer);
                }
                else
                {
                    this.theAuction = new Auction(this.Board.Vulnerable, this.Board.Dealer);
                }
                for (int i = 0; i < value.Bids.Count; i++)
                {
                    Bid bid = value.Bids[i];
                    this.theAuction.Record(bid);
                }
            }
        }

        [DataMember]
        public PlaySequence Play
        {
            get
            {
                //Log.Trace(5, $"{this.Owner}.BoardResultRecorder.Play: whoseTurn={thePlay?.whoseTurn}");
                return thePlay;
            }
            set
            {
                // Play can be null when Auction has not finished yet (happens when sending a bug report)
                //if (value == null) throw new ArgumentNullException("value");
                if (value != null && this.theAuction != null && this.theAuction.Ended)
                {
                    this.thePlay = new PlaySequence(this.theAuction.FinalContract ?? value.Contract, 13);
                    if (this.thePlay.Contract == null) throw new NullReferenceException("this.thePlay.Contract");
                    this.thePlay.Contract.tricksForDeclarer = 0;
                    this.thePlay.Contract.tricksForDefense = 0;
                    var newPlay = value.play;
                    for (int card = 0; card < newPlay.Count; card++)
                    {
                        PlayRecord item = newPlay[card];
                        this.thePlay.Record(item.Suit, item.Rank, item.Comment);
                    }
                }
                else
                {
                    this.thePlay = value;
                }
            }
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
                    if (this.theAuction.Ended && this.theAuction.FinalContract.Bid.IsPass) return 0;
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

        #endregion

        #region Public Methods

        public void CopyBoardData(Vulnerable vulnerability, Distribution boardDistribution)
        {
            ArgumentNullException.ThrowIfNull(boardDistribution);
            if (this.theAuction == null)
            {
                this.theAuction = new Auction(this.Board.Vulnerable, this.Board.Dealer);
            }
            else
            {
                if (this.theAuction.Ended)
                {
                    this.Contract.Vulnerability = vulnerability;
                    this.theAuction.FinalContract.Vulnerability = vulnerability;
                }
            }

            this.Distribution = boardDistribution.Clone();
        }

        public override string ToString()
        {
            StringBuilder result = new();
            result.Append(this.Auction.ToString());
            if (this.thePlay != null)
            {
                result.Append(this.thePlay.ToString());
            }

            return result.ToString();
        }

        public override bool Equals(object obj)
        {
            if (obj is not BoardResultRecorder otherResult) return false;
            if (this.Auction.AantalBiedingen != otherResult.Auction.AantalBiedingen) return false;
            if (this.Auction.Dealer != otherResult.Auction.Dealer) return false;
            if (this.Auction.Declarer != otherResult.Auction.Declarer) return false;
            if (this.Auction.WhoseTurn != otherResult.Auction.WhoseTurn) return false;
            if (this.Auction.Ended != otherResult.Auction.Ended) return false;
            if ((this.Play == null) != (otherResult.Play == null)) return false;
            if (this.Play != null && this.Play.CompletedTricks != otherResult.Play.CompletedTricks) return false;
            if (this.Play != null && this.Play.currentTrick != otherResult.Play.currentTrick) return false;
            if (this.TournamentScore != otherResult.TournamentScore) return false;
            if (this.Auction.Ended && this.Contract.Bid != otherResult.Contract.Bid) return false;
            if (this.Auction.Ended && this.Contract.Vulnerability != otherResult.Contract.Vulnerability) return false;
            if (this.Auction.Ended && this.NorthSouthScore != otherResult.NorthSouthScore) return false;
            return true;
        }

        /// <summary>
        /// Required since Equals has an override
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        #endregion

        #region Bridge Event Handlers

        public override void HandleBoardStarted(int boardNumber, Seats dealer, Vulnerable vulnerabilty)
        {
            if (this.Board == null)
            {
                this.Dealer = dealer;
                this.Vulnerability = vulnerabilty;
                this.theAuction = new Auction(this.Vulnerability, this.Dealer);
            }
        }

        public override void HandleCardPosition(Seats seat, Suits suit, Ranks rank)
        {
            if (this.Distribution.Incomplete)
            {       // this should only happen in a hosted tournament
                Log.Trace(4, $"{this.Owner}.BoardResultRecorder.HandleCardPosition: {seat.ToXML()} gets {rank.ToXML()}{suit.ToXML().ToLower()}");
                this.Distribution.Give(seat, suit, rank);
            }
        }

        public override void HandleBidDone(Seats source, Bid bid)
        {
            ArgumentNullException.ThrowIfNull(bid);
            Log.Trace(4, $"{this.Owner}.BoardResultRecorder.HandleBidDone: {source.ToXML()} bid {bid.ToXML()}");
            if (this.theAuction.WhoseTurn != source) throw new FatalBridgeException($"Expected a bid from {this.theAuction.WhoseTurn.ToString2()}");
            if (!bid.Hint)
            {
                this.theAuction.Record(bid.Clone());
                if (this.theAuction.Ended)
                {
                    this.thePlay = new PlaySequence(this.theAuction.FinalContract, 13);
                    //Log.Trace(4, $"{this.Owner}.BoardResultRecorder.HandleBidDone: auction ended; whoseturn={this.Play.whoseTurn}");
                }
            }
        }

        public override void HandleCardPlayed(Seats source, Suits suit, Ranks rank, string signal)
        {
            Log.Trace(4, $"{this.Owner}.BoardResultRecorder.HandleCardPlayed: {source.ToXML()} played {rank.ToXML()}{suit.ToXML().ToLower()} signalling '{signal}'");
            if (this.thePlay != null && this.Distribution != null)
            {
                this.thePlay.Record(suit, rank, signal);
                if (!this.Distribution.Owns(source, suit, rank))
                {
                    //  throw new FatalBridgeException(string.Format("{0} does not own {1}", source, card));
                    /// 18-03-08: cannot check here: hosted tournaments get a card at the moment the card is played
                    this.Distribution.Give(source, suit, rank);
                }

                this.Distribution.Played(source, suit, rank);
            }
        }

        #endregion
    }
}
