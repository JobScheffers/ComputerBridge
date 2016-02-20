using System;
using System.Diagnostics;
using System.Runtime.Serialization;
using System.Text;

namespace Sodes.Bridge.Base
{
    [DataContract]
    public class BoardResult : BoardResultRecorder
    {
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

        public BoardResult(Board2 board, Participant newParticipant)
            : this(board, newParticipant.Names, null)
        {
        }

        public BoardResult(Board2 board, SeatCollection<string> newParticipants, BridgeEventBus bus) : base(board, bus)
        {
            if (board == null) throw new ArgumentNullException("board");
            this.Participants = new Participant(newParticipants);
        }

        /// <summary>
        /// Only for deserialization
        /// </summary>
        private BoardResult()
        {
        }

        #region Public Properties

        [IgnoreDataMember]
        public Participant Participants { get; set; }

        [DataMember]
        public Guid UserId { get; set; }

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

        [DataMember]
        public int TournamentId { get; set; }

        #endregion

        #region Public Methods

        public override string ToString()
        {
            StringBuilder result = new StringBuilder();
            result.AppendLine("Result for " + this.TeamName);
            result.Append(base.ToString());
            return result.ToString();
        }

        public override bool Equals(object obj)
        {
            var otherResult = obj as BoardResult;
            if (otherResult == null) return false;
            if (!base.Equals(otherResult)) return false;
            if (this.Participants.Names[Seats.South] != otherResult.Participants.Names[Seats.South]) return false;
            if (this.TeamName != otherResult.TeamName) return false;
            return true;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        #endregion

        #region Bridge Event Handlers

        //public override void HandleCardDealingEnded()
        //{
        //    Debug.WriteLine("{0} BoaardResult3.HandleCardDealingEnded: 1st bid needed", DateTime.UtcNow);
        //    this.EventBus.HandleBidNeeded(this.Auction.WhoseTurn, this.Auction.LastRegularBid, this.Auction.AllowDouble, this.Auction.AllowRedouble);
        //}

        //public override void HandleBidDone(Seats source, Bid bid)
        //{
        //    base.HandleBidDone(source, bid);
        //    if (this.Auction.Ended)
        //    {
        //        if (this.Contract.Bid.IsRegular)
        //        {
        //            this.EventBus.HandleAuctionFinished(this.Auction.Declarer, this.Play.Contract);
        //            this.NeedCard();
        //        }
        //        else
        //        {
        //            this.EventBus.HandlePlayFinished(this);
        //        }
        //    }
        //    else
        //    {
        //        this.EventBus.HandleBidNeeded(this.Auction.WhoseTurn, this.Auction.LastRegularBid, this.Auction.AllowDouble, this.Auction.AllowRedouble);
        //    }
        //}

        public override void HandlePlayFinished(BoardResultRecorder currentResult)
        {
            this.EventBus.Unlink(this);
        }

        //private void NeedCard()
        //{
        //    //Trace.WriteLine(string.Format("BoardResult.NeedCard"));
        //    if (this.Auction == null) throw new ObjectDisposedException("this.theAuction");
        //    if (this.Play == null) throw new ObjectDisposedException("this.thePlay");

        //    Seats controller = this.Play.whoseTurn;
        //    if (this.Play.whoseTurn == this.Auction.Declarer.Partner())
        //    {
        //        controller = this.Auction.Declarer;
        //    }

        //    int leadSuitLength = this.Distribution.Length(this.Play.whoseTurn, this.Play.leadSuit);
        //    this.EventBus.HandleCardNeeded(
        //        controller
        //        , this.Play.whoseTurn
        //        , this.Play.leadSuit
        //        , this.Play.Trump
        //        , leadSuitLength == 0 && this.Play.Trump != Suits.NoTrump
        //        , leadSuitLength
        //        , this.Play.currentTrick
        //    );
        //}

        //public override void HandleShowDummy(Seats dummy)
        //{
        //    this.NeedCard();
        //}

        //public override void HandleReadyForNextStep(Seats source, NextSteps readyForStep)
        //{
        //    switch (readyForStep)
        //    {
        //        case NextSteps.NextStartPlay:
        //            this.NeedCard();
        //            break;
        //        case NextSteps.NextTrick:
        //            this.NeedCard();
        //            break;
        //        case NextSteps.NextShowScore:
        //            break;
        //        case NextSteps.NextBoard:
        //            break;
        //        default:
        //            break;
        //    }
        //}

        #endregion
    }
}
