﻿#if !CHAMPIONSHIP
//#define Olympus
#endif

using System;
using System.Collections.Generic;
using System.Diagnostics;       // Trace
using System.Threading;
using Sodes.Bridge.Base;
using System.Threading.Tasks;
using Sodes.Base;
using BridgeNetworkProtocol2;

namespace Sodes.Bridge.Networking
{
    public abstract class TableManagerClient : BridgeEventBusClient
    {
        public TableManagerProtocolState state;
        public event BridgeNetworkEventHandler OnBridgeNetworkEvent;
        private string teamNS;
        private string teamEW;
        public Seats seat;
        private string seatName;
        private Seats dummy;
        private Seats theDealer;
        private bool isDeclarer;
        private string team;
        private int maxTimePerBoard;
        private int maxTimePerCard;
        //private bool sendAlerts;
        //private NextSteps nextStep;     // next step that has to be started by TD after waiting for a '.... to lead' message
        //public bool busyProcessing;
        private Queue<string> messages;
        private string[] tableManagerExpectedResponse;
        //private bool waitForResume;
        private object locker = new object();
        private BoardResultRecorder boardResult;
        private Card leadCard;
        private Queue<StateChange> stateChanges;

        protected TableManagerClient(BridgeEventBus bus)
            : base(bus)
        {
            this.messages = new Queue<string>();
            this.stateChanges = new Queue<StateChange>();
        }

        protected TableManagerClient()
            : this(null)
        {
        }

        public bool WaitForProtocolSync { get; set; }

        public async Task Connect(Seats _seat, int _maxTimePerBoard, int _maxTimePerCard, string teamName, int botCount, bool _sendAlerts)
        {
            this.seat = _seat;
            this.seatName = seat.ToString();		// Seat.ToXML(seat);
            this.team = teamName;
            this.maxTimePerBoard = _maxTimePerBoard;
            this.maxTimePerCard = _maxTimePerCard;
            //this.sendAlerts = _sendAlerts;
            //this.waitForResume = false;
            this.WaitForProtocolSync = false;

            await this.ChangeState(TableManagerProtocolState.WaitForSeated
                , false
                , new string[] { string.Format("{0} (\"{1}\") seated", seatName, teamName)
                                ,string.Format("{0} {1} seated", seatName, teamName)
                                }
                , "Connecting \"{0}\" as {1} using protocol version 18", this.team, this.seat);
        }

        protected abstract Task WriteProtocolMessageToRemoteMachine(string message);

        private async Task ProcessMessage(string message)
        {
            Log.Trace("Client {1} processing '{0}'", message, seat);

            if (message.StartsWith("NS:"))		// something new from Bridge Moniteur: session ends with 
            {
                this.EventBus.HandleTournamentStopped();
                this.state = TableManagerProtocolState.Finished;
                if (this.OnBridgeNetworkEvent != null) this.OnBridgeNetworkEvent(this, BridgeNetworkEvents.SessionEnd, new BridgeNetworkEventData());
                return;
            }

            if (message == "End of session")
            {
                this.EventBus.HandleTournamentStopped();
                await this.ChangeState(TableManagerProtocolState.WaitForDisconnect, false, new string[] { "Disconnect" }, "");
                return;
            }

            if (Expected(message))
            {
                try
                {
                    switch (this.state)
                    {
                        case TableManagerProtocolState.WaitForSeated:
                            if (this.OnBridgeNetworkEvent != null) this.OnBridgeNetworkEvent(this, BridgeNetworkEvents.Seated, new BridgeNetworkEventData());
                            await this.ChangeState(TableManagerProtocolState.WaitForTeams, false, new string[] { "Teams" }, "{0} ready for teams", this.seat);
                            break;

                        case TableManagerProtocolState.WaitForTeams:
                            this.teamNS = message.Substring(message.IndexOf("N/S : \"") + 7);
                            this.teamNS = teamNS.Substring(0, teamNS.IndexOf("\""));
                            this.teamEW = message.Substring(message.IndexOf("E/W : \"") + 7);
                            this.teamEW = teamEW.Substring(0, teamEW.IndexOf("\""));

                            if (this.OnBridgeNetworkEvent != null) this.OnBridgeNetworkEvent(this, BridgeNetworkEvents.Teams, new BridgeNetworkTeamsEventData(teamNS, teamEW));

                            await this.ChangeState(TableManagerProtocolState.WaitForStartOfBoard, false, new string[] { "Start of board", "End of session" }, "{0} ready to start", this.seat);
                            break;

                        case TableManagerProtocolState.WaitForStartOfBoard:
                            if (message.StartsWith("Teams"))
                            {		// bug in BridgeMoniteur when tournament is restarted
                            }
                            else if (message.StartsWith("Timing"))
                            {
                                // Timing - N/S : this board [minutes:seconds], total [hours:minutes:seconds]. E/W : this board [minutes:seconds], total [hours:minutes:seconds]".
                                // Bridge Moniteur does not send the '.' at the end of the message
                                // Timing - N/S : this board  01:36,  total  0:01:36.  E/W : this board  01:34,  total  0:01:34
                                if (!message.EndsWith(".")) message += ".";
                                string[] timing = message.Split('.');
                                string[] parts = timing[0].Split(',');
                                string boardNS = "00:" + parts[0].Substring(parts[0].IndexOf("board") + 6).Trim();
                                string totalNS = parts[1].Substring(parts[1].IndexOf("total") + 6).Trim();
                                parts = timing[1].Split(',');
                                string boardEW = "00:" + parts[0].Substring(parts[0].IndexOf("board") + 6).Trim();
                                string totalEW = parts[1].Substring(parts[1].IndexOf("total") + 6).Trim();

                                TimeSpan _boardNS = ParseTimeUsed(boardNS);
                                TimeSpan _totalNS = ParseTimeUsed(totalNS);
                                TimeSpan _boardEW = ParseTimeUsed(boardEW);
                                TimeSpan _totalEW = ParseTimeUsed(totalEW);
                                this.EventBus.HandleTimeUsed(_boardNS, _totalNS, _boardEW, _totalEW);
                            }
                            else
                            {
                                await this.ChangeState(TableManagerProtocolState.WaitForBoardInfo, false, new string[] { "Board number" }, "{0} ready for deal", this.seat);
                            }
                            break;

                        case TableManagerProtocolState.WaitForBoardInfo:
                            // "Board number 1. Dealer North. Neither vulnerable."
                            string[] dealInfoParts = message.Split('.');
                            int boardNumber = Convert.ToInt32(dealInfoParts[0].Substring(13));
                            this.theDealer = SeatsExtensions.FromXML(dealInfoParts[1].Substring(8));
                            Vulnerable vulnerability = Vulnerable.Neither;
                            switch (dealInfoParts[2].Substring(1))
                            {
                                case "Both vulnerable":
                                    vulnerability = Vulnerable.Both; break;
                                case "N/S vulnerable":
                                    vulnerability = Vulnerable.NS; break;
                                case "E/W vulnerable":
                                    vulnerability = Vulnerable.EW; break;
                            }

                            var board = new Board2(this.theDealer, vulnerability, new Distribution());
                            this.boardResult = new BoardResultEventPublisher("TableManagerClient", board, new SeatCollection<string>(new string[] { this.teamNS, this.teamEW, this.teamNS, this.teamEW }), this.EventBus);
                            //this.boardResult = new BoardResultRecorder("TableManagerClient." + this.seat, board, this.EventBus);

                            this.EventBus.HandleBoardStarted(boardNumber, this.theDealer, vulnerability);
                            await this.ChangeState(TableManagerProtocolState.WaitForMyCards, false, new string[] { this.seat + "'s cards : " }, "{0} ready for cards", this.seat);
                            break;

                        case TableManagerProtocolState.WaitForMyCards:
                            // "North's cards : S J 8 5.H A K T 8.D 7 6.C A K T 3."
                            // "North's cards : S J 8 5.H A K T 8.D.C A K T 7 6 3."
                            // "North's cards : S -.H A K T 8 4 3 2.D.C A K T 7 6 3."
                            string cardInfo = message.Substring(2 + message.IndexOf(":"));
                            string[] suitInfo = cardInfo.Split('.');
                            for (int s1 = 0; s1 < 4; s1++)
                            {
                                suitInfo[s1] = suitInfo[s1].Trim();
                                Suits s = SuitHelper.FromXML(suitInfo[s1].Substring(0, 1));
                                if (suitInfo[s1].Length > 2)
                                {
                                    string cardsInSuit = suitInfo[s1].Substring(2) + " ";
                                    if (cardsInSuit.Substring(0, 1) != "-")
                                    {
                                        while (cardsInSuit.Length > 1)
                                        {
                                            Ranks rank = Rank.From(cardsInSuit.Substring(0, 1));
                                            this.EventBus.HandleCardPosition(this.seat, s, rank);
                                            cardsInSuit = cardsInSuit.Substring(2);
                                        }
                                    }
                                }
                            }

                            // TM is now expecting a response: either a bid or a 'ready for bid'
                            //Trace.WriteLine(string.Format("TrafficManager4TM.ProcessIncomingMessage: call FireCardDealingEnded"));
                            this.EventBus.HandleCardDealingEnded();

                            break;

                        case TableManagerProtocolState.WaitForOtherBid:
                            ProtocolHelper.HandleProtocolBid(message, this.EventBus);
                            break;

                        case TableManagerProtocolState.WaitForDummiesCards:
                            Log.Trace("Client {1} processing dummies cards", message, seat);
                            string dummiesCards = message.Substring(2 + message.IndexOf(":"));
                            string[] suitInfo2 = dummiesCards.Split('.');
                            for (Suits s = Suits.Spades; s >= Suits.Clubs; s--)
                            {
                                int suit = 3 - (int)s;
                                suitInfo2[suit] = suitInfo2[suit].Trim();
                                if (suitInfo2[suit].Length > 2)
                                {
                                    string cardsInSuit = suitInfo2[suit].Substring(2) + " ";
                                    if (cardsInSuit.Substring(0, 1) != "-")
                                    {
                                        while (cardsInSuit.Length > 1)
                                        {
                                            Ranks rank = Rank.From(cardsInSuit.Substring(0, 1));
                                            this.EventBus.HandleCardPosition(this.boardResult.Play.Dummy, s, rank);
                                            cardsInSuit = cardsInSuit.Substring(2);
                                        }
                                    }
                                }
                            }

                            this.EventBus.HandleShowDummy(this.dummy);
                            this.WaitForProtocolSync = false;
                            await this.PerformStateChanges();
                            break;

                        case TableManagerProtocolState.WaitForLead:
                            //await SendPlayedCard(this.seat, leadCard.Suit, leadCard.Rank);
                            //this.ForceNextStep(this.nextStep);
                            this.WaitForProtocolSync = false;
                            await this.ChangeState(TableManagerProtocolState.WaitForCardPlay
                                , false
                                , new string[] { "" }
                                , "{0} ready for {1}'s card to trick {2}", this.seat, (this.seat == this.dummy && this.seat == this.boardResult.Play.whoseTurn ? "dummy" : this.boardResult.Play.whoseTurn.ToString()), this.boardResult.Play.currentTrick);
                            break;

                        case TableManagerProtocolState.WaitForCardPlay:
                            if (message.Contains("to lead"))
                            {
                                /// This indicates a timing issue: TM sent a '... to lead' message before TD sent its HandleTrickFinished event
                                /// Wait until I receveive the HandleTrickFinished event
                                Log.Trace("TrafficManager4TM.ProcessIncomingMessage: received 'to lead' before TD raised HandleTrickFinished");
                                //lock (this)
                                //{
                                //    this.busyProcessing = false;
                                //}
                                //do
                                //{
                                //    //Trace.WriteLine(string.Format("TrafficManager4TM.ProcessIncomingMessage: wait for HandleTrickFinished"));
                                //    Task.Delay(100).Wait();
                                //} while (this.state != TableManagerProtocolState.WaitForLead);

                                //lock (this)
                                //{
                                //    this.busyProcessing = true;
                                //}
                                Log.Trace("TrafficManager4TM.ProcessIncomingMessage: ForceNextStep");
                                //this.ForceNextStep(this.nextStep);
                            }
                            else
                            {
                                string[] cardPlay = message.Split(' ');
                                Seats player = SeatsExtensions.FromXML(cardPlay[0]);
                                Card card = new Card(SuitHelper.FromXML(cardPlay[2].Substring(1, 1)), Rank.From(cardPlay[2].Substring(0, 1)));
                                this.EventBus.HandleCardPosition(player, card.Suit, card.Rank);
                                this.EventBus.HandleCardPlayed(player, card.Suit, card.Rank);
                            }

                            break;

                        case TableManagerProtocolState.WaitForDisconnect:
                            //this.stream.Close();
                            //this.client.Close();
                            this.state = TableManagerProtocolState.Finished;
                            break;
                    }
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException(string.Format("Error while processing message '{0}' in state {1}", message, state), ex);
                }
            }
            else
            {		// unexpected message

            }
        }

        private static TimeSpan ParseTimeUsed(string boardTime)
        {
            TimeSpan result;
            try
            {
                result = TimeSpan.Parse(boardTime);
                // bug in TableManager: boards played over midnight get 24h complement of spent time
                if (result.TotalHours >= 22) result = new TimeSpan(0, 2, 0);
            }
            catch (ArgumentNullException)
            {
                result = new TimeSpan(0, 1, 50);
            }
            catch (FormatException)
            {
                result = new TimeSpan(0, 1, 50);
            }
            catch (OverflowException)
            {
                result = new TimeSpan(0, 1, 50);
            }

            return result;
        }

        private async Task ChangeState(TableManagerProtocolState newState, bool waitForSync, string[] expectedAnswers, string message, params object[] args)
        {
            var stateChange = new StateChange() { Message = string.Format(message, args), NewState = newState, ExpectedResponses = expectedAnswers, WaitForSync = waitForSync };
            this.stateChanges.Enqueue(stateChange);
            if (!this.WaitForProtocolSync)
            {
                await this.PerformStateChanges();
            }
        }

        private async Task PerformStateChanges()
        {
            while (this.stateChanges.Count > 0 && !this.WaitForProtocolSync)
            {
                var stateChange = this.stateChanges.Dequeue();
                this.state = stateChange.NewState;
                Log.Trace("Client {0} new state {1}", this.seat, this.state);
                this.tableManagerExpectedResponse = stateChange.ExpectedResponses;
                if (stateChange.Message.Length > 0)
                {
                    await this.WriteProtocolMessageToRemoteMachine(stateChange.Message);
                }

                if (stateChange.WaitForSync) this.WaitForProtocolSync = true;
            }
        }

        private bool Expected(string message)
        {
            string loweredMessage = message.ToLowerInvariant();
            if (loweredMessage.StartsWith("disconnect"))
            {
                this.state = TableManagerProtocolState.WaitForDisconnect;
                return true;
            }

            for (int a = 0; a < this.tableManagerExpectedResponse.Length; a++)
            {
                if (loweredMessage.StartsWith(this.tableManagerExpectedResponse[a].ToLowerInvariant()))
                {
                    return true;
                }
            }

            if (message.StartsWith("Teams")) return true;		// bug in BridgeMoniteur

            Log.Trace("Unexpected response '{0}'; expected '{1}'", message, this.tableManagerExpectedResponse[0]);
#if DEBUG
            //System.Diagnostics.Debugger.Break();
#endif
            //if (this.OnBridgeNetworkEvent == null)
            {
                throw new InvalidOperationException(string.Format("Unexpected response '{0}'; expected '{1}'", message, this.tableManagerExpectedResponse[0]));
            }
            //else
            //{
            //    this.OnBridgeNetworkEvent(this, BridgeNetworkEvents.Error, new BridgeNetworkErrorEventData(string.Format("Unexpected response '{0}'; expected '{1}'", message, this.tableManagerExpectedResponse[0])));
            //    return false;
            //}
        }

        //private void ForceNextStep(NextSteps step)
        //{
        //    for (Seats seat = Seats.North; seat <= Seats.West; seat++)
        //    {
        //        if (seat != this.seat)
        //        {
        //            this.EventBus.HandleReadyForNextStep(seat, step);
        //        }
        //    }
        //}

        protected void ProcessIncomingMessage(string message)
        {
            lock (this.messages)
            {
                this.messages.Enqueue(message);
            }

            this.ProcessMessages(null);
        }

        private void ProcessMessages(Object stateInfo)
        {
            bool more = true;
            do
            {
                string message = string.Empty;
                lock (this.messages)
                {
                    if (this.messages.Count >= 1 
                        //&& !this.waitForResume
                        )
                    {
                        message = this.messages.Dequeue();
                    }
                    else
                    {
                        more = false;
                    }
                }

                if (more)
                {
                    lock (this.locker)
                    {		// ensure exclusive access to ProcessMessage
                        this.ProcessMessage(message);
                    }
                }
            } while (more);
        }

        /// <summary>
        /// Give the time for tracing purposes
        /// </summary>
        protected static string CurrentTime { get { return DateTime.UtcNow.ToString("HH:mm:ss.ff"); } }		// use UtcNow because it is much faster than DateTime.Now

        #region Bridge Event Handlers

        //public override void HandleCardDealingEnded()
        //{
        //    this.EventBus.HandleBidNeeded(this.boardResult.Auction.WhoseTurn, Bid.C("Pass"), false, false);
        //}

        public override async void HandleBidNeeded(Seats whoseTurn, Bid lastRegularBid, bool allowDouble, bool allowRedouble)
        {
            if (this.seat != whoseTurn)
            {
                await this.ChangeState(TableManagerProtocolState.WaitForOtherBid
                    , false
                    , new string[] { whoseTurn.ToString() }
                    , "{0} ready for {1}'s bid", this.seat, whoseTurn);
            }
        }

        public override async void HandleBidDone(Seats source, Bid bid)
        {
            Log.Trace("TableManagerClient.HandleBidDone {0} : {1} bids {2}", this.seat, source, bid);
            if (source == this.seat)
            {
                await this.WriteProtocolMessageToRemoteMachine(ProtocolHelper.Translate(bid, source));
            }
            //else
            //{
            //    if (!this.boardResult.Auction.Ended)
            //    {
            //        this.EventBus.HandleBidNeeded(this.boardResult.Auction.WhoseTurn, this.boardResult.Auction.LastRegularBid, this.boardResult.Auction.AllowDouble, this.boardResult.Auction.AllowRedouble);
            //    }
            //}
        }

        public override async void HandleAuctionFinished(Seats declarer, Contract finalContract)
        {
            Log.Trace("TableManagerClient.HandleAuctionFinished {0}", this.seat);
            //if (finalContract.Bid.IsPass)
            //{
            //    this.ForceNextStep(NextSteps.NextBoard);
            //    await this.ChangeState("",
            //            TableManagerProtocolState.WaitForStartOfBoard,
            //            new string[] { "Start of board", "End of session", "Timing", "NS" });
            //}
            //else
            {
                //this.nextStep = NextSteps.NextStartPlay;
                this.dummy = declarer.Partner();
                this.isDeclarer = (declarer == this.seat);
                Seats toLead = declarer.Next();
                if (this.seat == toLead)
                {
                    // TM requires that I wait for `... to lead`
                    // I do not send the ReadyForNextStep, so TD will not yet ask trickWinner for a card
                    await this.ChangeState(TableManagerProtocolState.WaitForLead, false, new string[] { string.Format("{0} to lead", toLead) }, "");
                }
                else
                {
                    //this.ForceNextStep(NextSteps.NextStartPlay);
                }
            }

            //this.busyProcessing = false;
        }

        public override async void HandleNeedDummiesCards(Seats dummy)
        {
            Log.Trace("TableManagerClient.HandleNeedDummiesCards {0}", this.seat);
            if (this.seat != this.dummy)
            {
                await this.ChangeState(TableManagerProtocolState.WaitForDummiesCards, true, new string[] { "Dummy's cards :" }, "{0} ready for dummy", this.seat);
            }
            else
            {
                this.EventBus.HandleShowDummy(dummy);
            }
        }

        public override async void HandleCardNeeded(Seats controller, Seats whoseTurn, Suits leadSuit, Suits trump, bool trumpAllowed, int leadSuitLength, int trick)
        {
            Log.Trace("TableManagerClient.HandleCardNeeded {0}", this.seat);
            if (controller == this.seat)
            {
                if (this.boardResult.Play.man == 1) this.WaitForProtocolSync = true;
            }
            else
            {
                //if (this.boardResult.Play.man == 2 && this.seat != this.boardResult.Play.Dummy && whoseTurn.Direction() != this.seat.Direction())
                //{
                //    // must wait for TM to send me a '.. to lead'
                //}
                //else
                {
                    await this.ChangeState(TableManagerProtocolState.WaitForCardPlay
                        , false
                        , new string[] { "" }
                        , "{0} ready for {1}'s card to trick {2}", this.seat, (this.seat == this.dummy && this.seat == whoseTurn ? "dummy" : whoseTurn.ToString()), trick);
                }
            }
        }

        public override async void HandleCardPlayed(Seats source, Suits suit, Ranks rank)
        {
            Log.Trace("TableManagerClient.HandleCardPlayed {0}: {1} plays {3} of {2}", this.seat, source, suit, rank);
            if ((source == this.seat && this.seat != this.dummy) || (source == this.dummy && this.seat == this.dummy.Partner()))
            {
                //if (this.boardResult.Play.man == 1)
                //{
                //    // remember the card played and send it when TM sends me a '.. to lead'
                //    this.leadCard = new Card(suit, rank);
                //}
                //else
                {
                    await SendPlayedCard(source, suit, rank);
                }
            }
        }

        private async Task SendPlayedCard(Seats source, Suits suit, Ranks rank)
        {
            await this.ChangeState(this.state
                , false
                , this.tableManagerExpectedResponse
                , "{0} plays {2}{1}",
                    // TM bug: TM does not recognize the phrase 'Dummy plays 8C', eventhough the protocol defines it
                    //whoseTurn != this.seat ? "Dummy" : whoseTurn.ToString(),
                    source,
                    SuitHelper.ToXML(suit),
                    rank.ToXML());
        }

        public override async void HandleTrickFinished(Seats trickWinner, int tricksForDeclarer, int tricksForDefense)
        {
            //this.nextStep = NextSteps.NextTrick;
            if ((trickWinner == this.seat && this.seat != this.dummy) || (this.isDeclarer && trickWinner == this.dummy))
            {
                // TM requires that I wait for `... to lead`
                // I do not send the ReadyForNextStep, so TD will not yet ask trickWinner for a card
                await this.ChangeState(TableManagerProtocolState.WaitForLead, false, new string[] { string.Format("{0} to lead", trickWinner == this.dummy ? "Dummy" : trickWinner.ToString()) }, "");
            }
            else
            {
                //this.ForceNextStep(NextSteps.NextTrick);
            }

            //this.busyProcessing = false;
        }

        public override async void HandlePlayFinished(BoardResultRecorder currentResult)
        {
            Log.Trace("TableManagerClient.HandlePlayFinished {0}", this.seat);
            this.EventBus.Unlink(this.boardResult);
            //this.ForceNextStep(NextSteps.NextBoard);
            await this.ChangeState(TableManagerProtocolState.WaitForStartOfBoard, false, new string[] { "Start of board", "End of session", "Timing", "NS" }, "");
        }

        public override async void HandleReadyForBoardScore(int resultCount, Board2 currentBoard)
        {
            Log.Trace("TableManagerClient.HandleReadyForBoardScore {0}", this.seat);
            //this.ForceNextStep(NextSteps.NextBoard);
            await this.ChangeState(TableManagerProtocolState.WaitForStartOfBoard, false, new string[] { "Start of board", "End of session", "Timing", "NS" }, "");
        }

        #endregion

        private struct StateChange
        {
            public string Message { get; set; }
            public TableManagerProtocolState NewState { get; set; }
            public string[] ExpectedResponses { get; set; }
            public bool WaitForSync { get; set; }
        }
    }

    public enum BridgeNetworkEvents
    {
        Seated
        ,
        Teams
        ,
        Error
            ,
        SessionEnd
    }

    public class BridgeNetworkEventData
    {
    }

    public class BridgeNetworkTeamsEventData : BridgeNetworkEventData
    {
        public string NS;
        public string EW;

        public BridgeNetworkTeamsEventData(string _ns, string _ew)
        {
            this.NS = _ns;
            this.EW = _ew;
        }
    }

    public delegate void BridgeNetworkEventHandler(object sender, BridgeNetworkEvents e, BridgeNetworkEventData data);
}