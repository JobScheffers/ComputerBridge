#define syncTrace   // uncomment to get detailed trace of events and protocol messages

using System;
using System.Threading.Tasks;

namespace Bridge.Networking
{
    /// <summary>
    /// 
    /// </summary>
    public class TableManagerEventsClient : BoardResultOwner
    {
        public Tournament Tournament;
        public Board2 currentBoard;
        private string teamNS;
        private string teamEW;

        public TableManagerEventsClient(BridgeEventBus bus) : base("South", bus)
        {
            this.Tournament = new RandomBoardsTournament("?");
            this.Tournament.ScoringMethod = Scorings.scIMP;
        }

        public TableManagerEventsClient() : this(BridgeEventBus.MainEventBus)
        {
        }

        public async Task ProcessEvent(string eventMessage)
        {
            eventMessage = eventMessage.ToLower();
#if syncTrace
            Log.Trace(3, "ProcessEvent {0}", eventMessage);
#endif
            if (eventMessage.StartsWith("event "))
            {
                var eventName = eventMessage.Substring(6);
                this.Tournament.EventName = eventName;
                this.EventBus.HandleTournamentStarted(Scorings.scIMP, 0, 0, eventName);
            }
            else
            if (eventMessage.StartsWith("teams : n/s : "))
            {
                teamNS = eventMessage.Substring(eventMessage.IndexOf("n/s : \"") + 7);
                teamNS = teamNS.Substring(0, teamNS.IndexOf("\""));
                teamEW = eventMessage.Substring(eventMessage.IndexOf("e/w : \"") + 7);
                teamEW = teamEW.Substring(0, teamEW.IndexOf("\""));
            }
            else
            if (eventMessage.StartsWith("board number "))
            {
                // "Board number 1. Dealer North. Neither vulnerable."
                string[] dealInfoParts = eventMessage.Split('.');
                int boardNumber = Convert.ToInt32(dealInfoParts[0].Substring(13));
                var theDealer = SeatsExtensions.FromXML(dealInfoParts[1].Substring(8));
                Vulnerable vulnerability = Vulnerable.Neither;
                switch (dealInfoParts[2].Substring(1))
                {
                    case "both vulnerable":
                        vulnerability = Vulnerable.Both; break;
                    case "n/s vulnerable":
                        vulnerability = Vulnerable.NS; break;
                    case "e/w vulnerable":
                        vulnerability = Vulnerable.EW; break;
                }

                if (this.Tournament.Boards.Count >= boardNumber && this.Tournament.Boards[boardNumber - 1].BoardNumber == boardNumber)
                {
                    this.currentBoard = this.Tournament.Boards[boardNumber - 1];
                }
                else
                {
                    this.currentBoard = new Board2(theDealer, vulnerability, new Distribution());
                    this.currentBoard.BoardNumber = boardNumber;
                    this.Tournament.Boards.Add(this.currentBoard);
                }

                this.EventBus.HandleBoardStarted(boardNumber, theDealer, vulnerability);
            }
            else
            if (eventMessage.Contains("'s cards "))
            {
                // "North's cards : S J 8 5.H A K T 8.D 7 6.C A K T 3."
                // "North's cards : S J 8 5.H A K T 8.D.C A K T 7 6 3."
                // "North's cards : S -.H A K T 8 4 3 2.D.C A K T 7 6 3."
                var seat = SeatsExtensions.FromXML(eventMessage.Substring(0, eventMessage.IndexOf("'")));
                string cardInfo = eventMessage.Substring(2 + eventMessage.IndexOf(":"));
                string[] suitInfo = cardInfo.Split('.');
                for (int s1 = 0; s1 < 4; s1++)
                {
                    suitInfo[s1] = suitInfo[s1].Trim();
                    Suits s = SuitHelper.FromXML(suitInfo[s1][0]);
                    if (suitInfo[s1].Length > 2)
                    {
                        string cardsInSuit = suitInfo[s1].Substring(2) + " ";
                        if (cardsInSuit[0] != '-')
                        {
                            while (cardsInSuit.Length > 1)
                            {
                                Ranks rank = RankHelper.From(cardsInSuit[0]);
                                this.EventBus.HandleCardPosition(seat, s, rank);
                                cardsInSuit = cardsInSuit.Substring(2);
                                if (this.currentBoard.Distribution.Incomplete)
                                {
                                    this.currentBoard.Distribution.Give(seat, s, rank);
                                }
                            }
                        }
                    }
                }
            }
            else
            if (eventMessage.Contains(" bids") || eventMessage.Contains(" passes") || eventMessage.Contains(" doubles") || eventMessage.Contains(" redoubles"))
            {
                ProtocolHelper.HandleProtocolBid(eventMessage, this.EventBus);
            }
            else
            if (eventMessage.Contains(" plays "))
            {
                string[] signalParts = eventMessage.Split('.');
                var signal = signalParts.Length >= 2 ? signalParts[1] : "";
                string[] cardPlay = signalParts[0].Split(' ');
                Seats player = SeatsExtensions.FromXML(cardPlay[0]);
                Card card = Card.Get(SuitHelper.FromXML(cardPlay[2][1]), RankHelper.From(cardPlay[2][0]));
                this.EventBus.HandleCardPlayed(player, card.Suit, card.Rank, signal);
            }
            else
            {
            }

            await this.EventBus.WaitForEventCompletionAsync().ConfigureAwait(false);
            //Log.Trace(3, $"TableManagerEventsClient.ProcessEvent: EventBus finished after '{eventMessage}'");
        }

        protected override BoardResultRecorder NewBoardResult(int boardNumber)
        {
            Log.Trace(4, $"TableManagerEventsClient.NewBoardResult: board {boardNumber}");
            // remove result if already played
            int resultSameTeam = -1;
            for (int result = 0; result < this.currentBoard.Results.Count; result++)
            {
                if (this.currentBoard.Results[result].Participants != null
                    && this.currentBoard.Results[result].Participants.Names != null
                    && this.currentBoard.Results[result].Participants.Names[Seats.South] == this.teamNS
                    ) resultSameTeam = result;
            }

            if (resultSameTeam >= 0) this.currentBoard.Results.RemoveAt(resultSameTeam);
            Log.Trace(4, $"TableManagerEventsClient.NewBoardResult: removed old result");
            this.CurrentResult = this.currentBoard.CurrentResult(new Participant { Names = new SeatCollection<string>(new string[] { this.teamNS, this.teamEW, this.teamNS, this.teamEW }) }, false);
            Log.Trace(4, $"TableManagerEventsClient.NewBoardResult: created new result");
            return this.CurrentResult;
        }

        public override void HandleBidDone(Seats source, AuctionBid bid)
        {
            Log.Trace(3, "TableManagerEventsClient.HandleBidDone: {0} bids {1}", source, bid);

            base.HandleBidDone(source, bid);
            if (this.CurrentResult.Auction.Ended)
            {
                //Log.Trace("BoardResultEventPublisher.HandleBidDone: auction finished");
                if (this.CurrentResult.Contract.Bid.IsRegular
                    )
                {
                    this.EventBus.HandleAuctionFinished(this.CurrentResult.Auction.Declarer, this.CurrentResult.Play.Contract);
                }
                else
                {
                    //Log.Trace("BoardResultEventPublisher.HandleBidDone: all passed");
                    this.EventBus.HandlePlayFinished(this.CurrentResult);
                }
            }
        }

        public override void HandleCardPlayed(Seats source, Suits suit, Ranks rank, string signal)
        {
            Log.Trace(3, "TableManagerEventsClient.HandleCardPlayed: {0} played {2}{1}, whoseTurn={3}", source, suit.ToXML(), rank.ToXML(), this.CurrentResult.Play.whoseTurn);

            if (this.CurrentResult.Play == null)      // this is an event that is meant for the previous boardResult
                throw new ArgumentNullException("this.CurrentResult.Play");

            if (source != this.CurrentResult.Play.whoseTurn)
                throw new ArgumentOutOfRangeException("source", "Expected a card from " + this.CurrentResult.Play.whoseTurn);

            Log.Trace(4, "TableManagerEventsClient.HandleCardPlayed: before base.HandleCardPlayed");
            base.HandleCardPlayed(source, suit, rank, signal);
            Log.Trace(4, "TableManagerEventsClient.HandleCardPlayed: before CurrentResult.Play.TrickEnded");
            if (this.CurrentResult.Play.TrickEnded)
            {
                Log.Trace(4, "TableManagerEventsClient.HandleCardPlayed: before EventBus.HandleTrickFinished");
                this.EventBus.HandleTrickFinished(this.CurrentResult.Play.whoseTurn, this.CurrentResult.Play.Contract.tricksForDeclarer, this.CurrentResult.Play.Contract.tricksForDefense);
                Log.Trace(4, "TableManagerEventsClient.HandleCardPlayed: before CurrentResult.Play.PlayEnded");
                if (this.CurrentResult.Play.PlayEnded)
                {
                    //Log.Trace("BoardResultEventPublisher({0}).HandleCardPlayed: play finished", this.Owner);
                    this.EventBus.HandlePlayFinished(this.CurrentResult);
                }
            }
        }
    }
}
