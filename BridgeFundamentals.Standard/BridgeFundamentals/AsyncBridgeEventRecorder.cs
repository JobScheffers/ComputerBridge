using System;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Bridge
{
    public abstract class AsyncBridgeEventRecorder : AsyncBridgeEventHandler
    {
        private Auction theAuction;
        private PlaySequence thePlay;
        private Seats _dealer;
        private Vulnerable _vulnerability;

        public AsyncBridgeEventRecorder(string _owner) : base(_owner)
        {
            this.Created = DateTime.Now;
            this.Distribution = new Distribution();
        }

        #region Public Properties

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
                    this.Auction = new Auction(this.Vulnerability, this.Dealer);
                }
                this.Auction.FinalContract = value;
            }
        }

        [DataMember]
        protected Seats Dealer
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

        protected Seats Dummy
        {
            get
            {
                if (this.theAuction == null) throw new Exception();
                if (!this.theAuction.Ended) throw new Exception();
                if (thePlay == null) throw new Exception();
                if (thePlay.currentTrick == 1 && thePlay.man == 1) throw new Exception();
                return theAuction.FinalContract.Declarer.Partner();
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
                ArgumentNullException.ThrowIfNull(value, nameof(value));
                this.theAuction = new Auction(this.Vulnerability, this.Dealer);
                for (int i = 0; i < value.Bids.Count; i++)
                {
                    var bid = value.Bids[i];
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
        }

        [IgnoreDataMember]
        public int NorthSouthScore
        {
            get
            {
                if (this.theAuction.Ended && this.theAuction.FinalContract.Bid.IsPass) return 0;
                if (this.thePlay == null && !(!this.theAuction.Ended && this.Contract != null)) return -100000;
                return this.Contract.Score * (this.Contract.Declarer == Seats.North || this.Contract.Declarer == Seats.South ? 1 : -1);
            }
        }

#endregion

        #region Public Methods

        public override string ToString()
        {
            var result = new StringBuilder();
            result.Append(this.Auction.ToString());
            if (this.thePlay != null)
            {
                result.Append(this.thePlay.ToString());
            }

            return result.ToString();
        }

        public override bool Equals(object obj)
        {
            var otherResult = obj as AsyncBridgeEventRecorder;
            if (otherResult == null) return false;
            if (this.Auction.AantalBiedingen != otherResult.Auction.AantalBiedingen) return false;
            if (this.Auction.Dealer != otherResult.Auction.Dealer) return false;
            if (this.Auction.Declarer != otherResult.Auction.Declarer) return false;
            if (this.Auction.WhoseTurn != otherResult.Auction.WhoseTurn) return false;
            if (this.Auction.Ended != otherResult.Auction.Ended) return false;
            if ((this.Play == null) != (otherResult.Play == null)) return false;
            if (this.Play != null && this.Play.CompletedTricks != otherResult.Play.CompletedTricks) return false;
            if (this.Play != null && this.Play.currentTrick != otherResult.Play.currentTrick) return false;
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

        public override ValueTask HandleBoardStarted(int boardNumber, Seats dealer, Vulnerable vulnerabilty)
        {
            this.Distribution.Clear();
            this.Dealer = dealer;
            this.Vulnerability = vulnerabilty;
            this.theAuction = new Auction(this.Vulnerability, this.Dealer);
            return base.HandleBoardStarted(boardNumber, dealer, vulnerabilty);
        }

        public override ValueTask HandleCardPosition(Seats seat, Suits suit, Ranks rank)
        {
            if (this.Distribution.Incomplete)
            {       // this should only happen in a hosted tournament
                this.Distribution.Give(seat, suit, rank);
            }

            return base.HandleCardPosition(seat, suit, rank);
        }

        public override ValueTask HandleBidDone(Seats source, AuctionBid bid, DateTimeOffset when)
        {
#if NET6_0_OR_GREATER
            ArgumentNullException.ThrowIfNull(bid);
#else
#endif
            Log.Trace(4, $"{this.NameForLog}.HandleBidDone: {source.ToXML()} bid {bid.ToXML()}");
            if (this.theAuction.WhoseTurn != source) throw new FatalBridgeException($"Expected a bid from {this.theAuction.WhoseTurn.ToLocalizedString()}");
            if (!bid.Hint)
            {
                this.theAuction.Record(bid.Clone());
                if (this.theAuction.Ended)
                {
                    this.thePlay = new PlaySequence(this.theAuction.FinalContract, 13);
                    //Log.Trace(4, $"{this.NameForLog}.HandleBidDone: auction ended; whoseturn={this.Play.whoseTurn}");
                }
            }

            return base.HandleBidDone(source, bid, when);
        }

        public override ValueTask HandleCardPlayed(Seats source, Suits suit, Ranks rank, string signal, DateTimeOffset when)
        {
            Log.Trace(4, $"{this.NameForLog}.HandleCardPlayed: {source.ToXML()} played {rank.ToXML()}{suit.ToXML().ToLower()}");
            if (this.thePlay != null && this.Distribution != null)
            {
                var playedInTrick = this.thePlay.PlayedInTrick(suit, rank);
                if (playedInTrick < 14) throw new FatalBridgeException($"{rank.ToXML()}{suit.ToXML()} was already played in trick {playedInTrick}");
                this.thePlay.Record(suit, rank, signal);
                if (!this.Distribution.Owns(source, suit, rank))
                {
                    //  throw new FatalBridgeException(string.Format("{0} does not own {1}", source, card));
                    /// 18-03-08: cannot check here: hosted tournaments get a card at the moment the card is played
                    this.Distribution.Give(source, suit, rank);
                }

                this.Distribution.Played(source, suit, rank);
            }

            return base.HandleCardPlayed(source, suit, rank, signal, when);
        }

#endregion
    }
}
