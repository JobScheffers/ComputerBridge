#define syncTrace   // uncomment to get detailed trace of events and protocol messages

using System;
using Sodes.Bridge.Base;
using System.Threading.Tasks;
using BridgeNetworkProtocol2;
using System.Collections.Concurrent;
using System.IO;
using Sodes.Base;
using System.Threading;

namespace Sodes.Bridge.Networking
{
	public enum HostEvents { Seated, ReadyForTeams, ReadyToStart, ReadyForDeal, ReadyForCards, BoardFinished, Finished }
    public delegate void HandleHostEvent(TableManagerHost sender, HostEvents hostEvent, object eventData);

    /// <summary>
    /// Implementation of the server side of the Bridge Network Protocol
    /// as described in http://www.bluechipbridge.co.uk/protocol.htm
    /// </summary>
    public abstract class TableManagerHost : BridgeEventBusClient
    {
		public event HandleHostEvent OnHostEvent;

		internal SeatCollection<ClientData> clients;
        private string lastRelevantMessage;
        private bool moreBoards;
        private bool allReadyForStartOfBoard;
        private bool allReadyForDeal;
        private TournamentController c;
        private BoardResultRecorder CurrentResult;
        private DirectionDictionary<TimeSpan> boardTime;
        private System.Diagnostics.Stopwatch lagTimer;

        public DirectionDictionary<System.Diagnostics.Stopwatch> ThinkTime { get; private set; }

        public Tournament HostedTournament { get; private set; }

        protected TableManagerHost(BridgeEventBus bus, string name) : base(bus, name)
		{
			this.clients = new SeatCollection<ClientData>();
            this.moreBoards = true;
            this.lagTimer = new System.Diagnostics.Stopwatch();
            this.ThinkTime = new DirectionDictionary<System.Diagnostics.Stopwatch>(new System.Diagnostics.Stopwatch(), new System.Diagnostics.Stopwatch());
            this.boardTime = new DirectionDictionary<TimeSpan>(new TimeSpan(), new TimeSpan());
            Task.Run(async () =>
            {
                await this.ProcessMessages();
            });
        }

        public void HostTournament(string pbnTournament)
        {
            this.HostedTournament = TournamentLoader.LoadAsync(File.OpenRead(pbnTournament)).Result;
            this.c = new TMController(this, this.HostedTournament, new ParticipantInfo() { ConventionCardNS = this.clients[Seats.North].teamName, ConventionCardWE = this.clients[Seats.East].teamName, MaxThinkTime = 120, UserId = Guid.NewGuid(), PlayerNames = new Participant(this.clients[Seats.North].teamName, this.clients[Seats.East].teamName, this.clients[Seats.North].teamName, this.clients[Seats.East].teamName) }, this.EventBus);
            this.allReadyForStartOfBoard = false;
            this.c.StartTournament();
            this.ThinkTime[Directions.NorthSouth].Reset();
            this.ThinkTime[Directions.EastWest].Reset();
        }

        public bool IsProcessing
        {
            get
            {
                for (Seats seat = Seats.North; seat <= Seats.West; seat++)
                {
                    if (this.clients[seat].messages.Count > 0) return true;
                }
                return false;
            }
        }

		private async Task ProcessMessages()
		{
            try
            {
                const int minimumWait = 10;
                var waitForNewMessage = minimumWait;
			    string m = null;
			    do
			    {
#if syncTrace
                    Log.Trace(4, "{0} main message loop", this.Name);
#endif
                    waitForNewMessage = 20;
                    for (Seats seat = Seats.North; seat <= Seats.West; seat++)
                    {
                        if (this.clients[seat] != null && !this.clients[seat].Pause && !this.clients[seat].messages.IsEmpty)
                        {
                            lock (this.clients[seat].messages)
                            {
                                this.clients[seat].messages.TryDequeue(out m);
                            }

                            if (m != null)
                            {
#if syncTrace
                                Log.Trace(3, "{2} dequeued {0}'s '{1}'", seat, m, this.Name);
#endif
                                waitForNewMessage = minimumWait;
                                lock (this.clients)
                                {       // ensure exclusive access to ProcessMessage
                                    this.ProcessMessage(m, seat);
                                }
                            }
                        }
                    }

                    if (waitForNewMessage > minimumWait)
                    {
                        await Task.Delay(waitForNewMessage);
                    }
                } while (this.moreBoards);
            }
            catch (Exception x)
            {
                Log.Trace(0, x.ToString());
                throw;
            }
        }

#if syncTrace
        private void DumpQueue()
        {
            Log.Trace(1, "{0} remaining messages on queue:", this.Name);
            for (Seats seat = Seats.North; seat <= Seats.West; seat++)
            {
                var more = true;
                while (more)
                {
                    string m = null;
                    this.clients[seat].messages.TryDequeue(out m);
                    if (m == null)
                    {
                        more = false;
                    }
                    else
                    { 
                        Log.Trace(1, "{2} queue item {0} '{1}'", seat, m, this.Name);
                    }
                }
            }
        }
#endif

        public void Seat(ClientData client, string message)
        {
            if (message.ToLowerInvariant().Contains("connecting") && message.ToLowerInvariant().Contains("using protocol version"))
            {
#if syncTrace
                Log.Trace(2, "{1} processing '{0}'", message, this.Name);
#endif
                var hand = message.Substring(message.IndexOf(" as ") + 4, 5).Trim().ToLowerInvariant();
                if (hand == "north" || hand == "east" || hand == "south" || hand == "west")
                {
                    client.seat = SeatsExtensions.FromXML(hand.Substring(0, 1).ToUpperInvariant());
                    if (this.clients[client.seat] == null)
                    {
                        int p = message.IndexOf("\"");
                        var teamName = message.Substring(p + 1, message.IndexOf("\"", p + 1) - (p + 1));
                        client.teamName = teamName;
                        client.hand = client.seat.ToString();      //.Substring(0, 1)
                        var protocolVersion = int.Parse(message.Substring(message.IndexOf(" version ") + 9));
                        switch (protocolVersion)
                        {
                            case 18:
                                client.PauseBeforeSending = true;
                                client.CanAskForExplanation = false;
                                break;
                            case 19:
                                client.PauseBeforeSending = false;
                                client.CanAskForExplanation = true;
                                break;
                            default:
                                throw new ArgumentOutOfRangeException("protocolVersion", protocolVersion + " not supported");
                        }

                        var partner = client.seat.Partner();
                        var partnerTeamName = teamName;
                        if (this.clients[partner] != null)
                        {
                            if (this.clients[partner].teamName == null)
                            {
                                this.clients[partner].teamName = teamName;
                            }
                            else
                            {
                                partnerTeamName = this.clients[partner].teamName;
                            }
                        }

                        if (teamName == partnerTeamName)
                        {
                            client.state = TableManagerProtocolState.WaitForSeated;
                            client.seatTaken = true;
                            this.clients[client.seat] = client;
                            client.WriteData("{1} (\"{0}\") seated", client.teamName, client.hand);
                            this.OnHostEvent(this, HostEvents.Seated, client.seat + "|" + teamName);
                        }
                        else
                        {
                            client.Refuse("Expected team name '{0}'", partnerTeamName);
                        }
                    }
                    else
                    {
                        client.Refuse("Seat already has been taken");
                    }
                }
                else
                {
                    client.Refuse("Illegal hand specified");
                }
            }
            else
            {
                client.Refuse("Expected 'Connecting ....'");
            }
        }

        private void ProcessMessage(string message, Seats seat)
		{
#if syncTrace
			Log.Trace(2, "{1} processing '{0}'", message, this.Name);
#endif
            switch (this.clients[seat].state)
			{
				case TableManagerProtocolState.WaitForSeated:
					ChangeState(message, this.clients[seat].hand + " ready for teams", TableManagerProtocolState.WaitForTeams, seat);
					break;

				case TableManagerProtocolState.WaitForTeams:
                    this.UpdateCommunicationLag(seat, this.lagTimer.ElapsedTicks);
					ChangeState(message, this.clients[seat].hand + " ready to start", TableManagerProtocolState.WaitForStartOfBoard, seat);
					break;

				case TableManagerProtocolState.WaitForStartOfBoard:
                    this.UpdateCommunicationLag(seat, this.lagTimer.ElapsedTicks);
                    ChangeState(message, this.clients[seat].hand + " ready for deal", TableManagerProtocolState.WaitForBoardInfo, seat);
					break;

				case TableManagerProtocolState.WaitForBoardInfo:
                    this.UpdateCommunicationLag(seat, this.lagTimer.ElapsedTicks);
                    ChangeState(message, this.clients[seat].hand + " ready for cards", TableManagerProtocolState.WaitForMyCards, seat);
					this.OnHostEvent(this, HostEvents.ReadyForCards, seat);
					break;

				case TableManagerProtocolState.WaitForMyCards:
                    lock (this.clients) this.clients[seat].Pause = true;
                    if (seat == this.CurrentResult.Auction.WhoseTurn)
                    {
                        if (message.Contains(" ready for "))
                        {
#if syncTrace
                            Log.Trace(0, "{1} expected '... bids ..' from {0}", seat, this.Name);
                            this.DumpQueue();
#endif
                            throw new InvalidOperationException();
                        }

                        this.lastRelevantMessage = message;
#if syncTrace
                        //Log.Trace("Host lastRelevantMessage={0}", message);
#endif
                        ChangeState(message, this.clients[seat].hand + " ", TableManagerProtocolState.WaitForOtherBid, seat);
                    }
                    else
                    {
                        ChangeState(message, string.Format("{0} ready for {1}'s bid", this.clients[seat].hand, this.CurrentResult.Auction.WhoseTurn), TableManagerProtocolState.WaitForOtherBid, seat);
                    }
                    break;

                case TableManagerProtocolState.WaitForCardPlay:
                    lock (this.clients) this.clients[seat].Pause = true;
                    // ready for dummy's card mag ook ready for xx's card
                    if (this.CurrentResult.Play.whoseTurn == this.CurrentResult.Play.Dummy && seat == this.CurrentResult.Play.Dummy)
					{
						ChangeState(message, string.Format("{0} ready for dummy's card to trick {2}", this.clients[seat].hand, message.Contains("dummy") ? "dummy" : this.CurrentResult.Play.whoseTurn.ToString(), this.CurrentResult.Play.currentTrick), TableManagerProtocolState.WaitForOtherCardPlay, seat);
					}
					else
					{
						ChangeState(message, string.Format("{0} ready for {1}'s card to trick {2}", this.clients[seat].hand, this.CurrentResult.Play.whoseTurn.ToString(), this.CurrentResult.Play.currentTrick), TableManagerProtocolState.WaitForOtherCardPlay, seat);
					}
					break;

				case TableManagerProtocolState.WaitForOwnCardPlay:
                    lock (this.clients) this.clients[seat].Pause = true;
                    this.lastRelevantMessage = message;
#if syncTrace
                    //Log.Trace("{1} lastRelevantMessage={0}", message, this.hostName);
#endif
                    ChangeState(message, string.Format("{0} plays ", this.CurrentResult.Play.whoseTurn), TableManagerProtocolState.WaitForOtherCardPlay, seat);
                    break;

				case TableManagerProtocolState.WaitForDummiesCardPlay:
					ChangeState(message, string.Format("{0} plays ", this.CurrentResult.Play.whoseTurn), TableManagerProtocolState.WaitForOtherCardPlay, seat);
					break;

				case TableManagerProtocolState.WaitForDummiesCards:
					ChangeState(message, string.Format("{0} ready for dummy", this.clients[seat].hand), TableManagerProtocolState.GiveDummiesCards, seat);
					break;

				default:
#if syncTrace
                    Log.Trace(0, "{3} unexpected '{0}' from {1} in state {2}", message, seat, this.clients[seat].state, this.Name);
                    this.DumpQueue();
#endif
                    this.clients[seat].Refuse("Unexpected '{0}' in state {1}", message, this.clients[seat].state);
                    throw new InvalidOperationException(string.Format("Unexpected '{0}' in state {1}", message, this.clients[seat].state));
			}
		}

		private void ChangeState(string message, string expected, TableManagerProtocolState newState, Seats seat)
		{
			if (message.ToLowerInvariant().StartsWith(expected.ToLowerInvariant()))
			{
				this.clients[seat].state = newState;
                var allReady = true;
                for (Seats s = Seats.North; s <= Seats.West; s++) if (this.clients[s] == null || this.clients[s].state != newState) { allReady = false; break; }
                if (allReady)
                {
#if syncTrace
                    Log.Trace(2, "{1} ChangeState {0}", newState, this.Name);
#endif
                    switch (newState)
                    {
                        case TableManagerProtocolState.Initial:
                            break;
                        case TableManagerProtocolState.WaitForSeated:
                            break;
                        case TableManagerProtocolState.WaitForTeams:
                            this.BroadCast("Teams : N/S : \"" + this.clients[Seats.North].teamName + "\". E/W : \"" + this.clients[Seats.East].teamName + "\"");
                            this.OnHostEvent(this, HostEvents.ReadyForTeams, null);
                            break;
                        case TableManagerProtocolState.WaitForStartOfBoard:
                            this.c.StartNextBoard().Wait();
                            break;
                        case TableManagerProtocolState.WaitForBoardInfo:
                            this.BroadCast("Board number {0}. Dealer {1}. {2} vulnerable.", this.c.currentBoard.BoardNumber, this.c.currentBoard.Dealer.ToXMLFull(), ProtocolHelper.Translate(this.c.currentBoard.Vulnerable));
                            break;
                        case TableManagerProtocolState.WaitForMyCards:
                            this.clients[Seats.North].WriteData(ProtocolHelper.Translate(Seats.North, this.c.currentBoard.Distribution));
                            this.clients[Seats.East].WriteData(ProtocolHelper.Translate(Seats.East, this.c.currentBoard.Distribution));
                            this.clients[Seats.South].WriteData(ProtocolHelper.Translate(Seats.South, this.c.currentBoard.Distribution));
                            this.clients[Seats.West].WriteData(ProtocolHelper.Translate(Seats.West, this.c.currentBoard.Distribution));
                            break;
                        case TableManagerProtocolState.WaitForCardPlay:
                            break;
                        case TableManagerProtocolState.WaitForOtherBid:
                            for (Seats s = Seats.North; s <= Seats.West; s++) this.clients[s].state = TableManagerProtocolState.WaitForCardPlay;
                            ProtocolHelper.HandleProtocolBid(this.lastRelevantMessage, this.EventBus);
                            break;
                        case TableManagerProtocolState.WaitForOtherCardPlay:
                            ProtocolHelper.HandleProtocolPlay(this.lastRelevantMessage, this.EventBus);
                            break;
                        case TableManagerProtocolState.WaitForOwnCardPlay:
                            break;
                        case TableManagerProtocolState.WaitForDummiesCardPlay:
                            break;
                        case TableManagerProtocolState.GiveDummiesCards:
                            var cards = ProtocolHelper.Translate(this.CurrentResult.Play.Dummy, this.c.currentBoard.Distribution).Replace(this.CurrentResult.Play.Dummy.ToXMLFull(), "Dummy");
                            for (Seats s = Seats.North; s <= Seats.West; s++)
                            {
                                if (s != this.CurrentResult.Play.Dummy)
                                {
                                    this.clients[s].WriteData(cards);
                                }
                            }
                            for (Seats s = Seats.North; s <= Seats.West; s++)
                            {
                                this.clients[s].state = (s == this.CurrentResult.Auction.Declarer ? TableManagerProtocolState.WaitForOwnCardPlay : TableManagerProtocolState.WaitForCardPlay);
                                lock (this.clients) this.clients[s].Pause = false;
                            }
                            break;
                        case TableManagerProtocolState.WaitForDisconnect:
                            break;
                        case TableManagerProtocolState.WaitForLead:
                            break;
                        case TableManagerProtocolState.Finished:
                            break;
                        default:
                            break;
                    }
                }
            }
			else
            {
#if syncTrace
                Log.Trace(0, "{1} expected '{0}'", expected, this.Name);
                this.DumpQueue();
#endif
                this.clients[seat].Refuse("Expected '{0}'", expected);
                throw new InvalidOperationException(string.Format("Expected '{0}'", expected));
            }
        }

        public void BroadCast(string message, params object[] args)
        {
            for (Seats s = Seats.North; s <= Seats.West; s++)
            {
                if (this.clients[s].PauseBeforeSending) Threading.Sleep(300);
                this.clients[s].WriteData(message, args);
            }

            this.lagTimer.Restart();
        }

        private void UpdateCommunicationLag(Seats source, long lag)
        {
            //Log.Trace("Host UpdateCommunicationLag for {0} old lag={1} lag={2}", source, this.clients[source].communicationLag, lag);
            this.clients[source].communicationLag += lag;
            this.clients[source].communicationLag /= 2;
            //Log.Trace("Host UpdateCommunicationLag for {0} new lag={1}", source, this.clients[source].communicationLag);
        }

        protected virtual void ExplainBid(Seats source, Bid bid)
        {
            // opportunity to implement manual alerting
        }

        protected virtual void Stop()
        {

        }

        #region Bridge Events

        public override void HandleBoardStarted(int boardNumber, Seats dealer, Vulnerable vulnerabilty)
        {
#if syncTrace
            //Log.Trace(4, "TableManagerHost.HandleBoardStarted");
#endif
            base.HandleBoardStarted(boardNumber, dealer, vulnerabilty);
            for (Seats s = Seats.North; s <= Seats.West; s++)
            {
                this.clients[s].Pause = true;
                this.clients[s].state = TableManagerProtocolState.WaitForStartOfBoard;
            }

            Threading.Sleep(20);
            this.BroadCast("Start of board");
            for (Seats s = Seats.North; s <= Seats.West; s++)
            {
                this.clients[s].Pause = false;
            }
        }

        public override void HandlePlayFinished(BoardResultRecorder currentResult)
        {
#if syncTrace
            Log.Trace(4, "HostBoardResult.HandlePlayFinished");
#endif
            base.HandlePlayFinished(currentResult);
            this.boardTime[Directions.NorthSouth] = this.ThinkTime[Directions.NorthSouth].Elapsed.Subtract(this.boardTime[Directions.NorthSouth]);
            this.boardTime[Directions.EastWest] = this.ThinkTime[Directions.EastWest].Elapsed.Subtract(this.boardTime[Directions.EastWest]);
            Threading.Sleep(20);
            this.BroadCast("Timing - N/S : this board  {0:mm\\:ss},  total  {1:h\\:mm\\:ss}.  E/W : this board  {2:mm\\:ss},  total  {3:h\\:mm\\:ss}."
                , this.boardTime[Directions.NorthSouth].RoundToSeconds()
                , this.ThinkTime[Directions.NorthSouth].Elapsed.RoundToSeconds()
                , this.boardTime[Directions.EastWest].RoundToSeconds()
                , this.ThinkTime[Directions.EastWest].Elapsed.RoundToSeconds()
                );
            this.boardTime[Directions.NorthSouth] = this.ThinkTime[Directions.NorthSouth].Elapsed;
            this.boardTime[Directions.EastWest] = this.ThinkTime[Directions.EastWest].Elapsed;
            for (Seats s = Seats.North; s <= Seats.West; s++)
            {
                this.clients[s].state = TableManagerProtocolState.WaitForStartOfBoard;
                this.clients[s].Pause = false;
            }

            this.OnHostEvent(this, HostEvents.BoardFinished, currentResult);
        }

        public override void HandleTournamentStopped()
        {
#if syncTrace
            //Log.Trace(4, "TableManagerHost.HandleTournamentStopped");
#endif
            Threading.Sleep(20);
            this.BroadCast("End of session");
            this.moreBoards = false;
            this.OnHostEvent(this, HostEvents.Finished, null);
            this.Stop();
        }

        #endregion

        private class TMController : TournamentController
        {
            private TableManagerHost host;

            public TMController(TableManagerHost h, Tournament t, ParticipantInfo p, BridgeEventBus bus) : base(t, p, bus)
            {
                this.host = h;
            }

            protected override BoardResultRecorder NewBoardResult(int boardNumber)
            {
                this.host.CurrentResult = new HostBoardResult(this.host, this.currentBoard, this.participant.PlayerNames.Names, this.EventBus);
                return this.host.CurrentResult;
            }
        }

        private class HostBoardResult : BoardResultEventPublisher
        {
            private TableManagerHost host;

            public HostBoardResult(TableManagerHost h, Board2 board, SeatCollection<string> newParticipants, BridgeEventBus bus)
                : base("HostBoardResult", board, newParticipants, bus)
            {
                this.host = h;
            }

            public override void HandleBidNeeded(Seats whoseTurn, Bid lastRegularBid, bool allowDouble, bool allowRedouble)
            {
                base.HandleBidNeeded(whoseTurn, lastRegularBid, allowDouble, allowRedouble);
                this.host.ThinkTime[whoseTurn.Direction()].Start();
            }

            public override void HandleBidDone(Seats source, Bid bid)
            {
                this.host.ThinkTime[source.Direction()].Stop();
#if syncTrace
                Log.Trace(4, "HostBoardResult.HandleBidDone");
#endif
                if (this.BidMayBeAlerted(bid))
                {
                    //if (!bid.Alert || string.IsNullOrWhiteSpace(bid.Explanation))
                    {
#if syncTrace
                        Log.Trace(2, "HostBoardResult.HandleBidDone explain opponents bid");
#endif
                        this.host.ExplainBid(source, bid);
                        if (bid.Alert 
                            //&& string.IsNullOrWhiteSpace(bid.Explanation)
                            )
                        {   // the operator has indicated this bid needs an explanation
                            Log.Trace(2, "HostBoardResult.HandleBidDone host operator wants an alert");
                            if (this.host.clients[source].CanAskForExplanation)
                            {   // client implements this new part of the protocol
                                var answer = this.host.clients[source.Next()].WriteAndWait("Explain {0}'s {1}", source, ProtocolHelper.Translate(bid));
                                bid.Explanation = answer;
                            }
                        }
                        else
                        {
                            Log.Trace(2, "HostBoardResult.HandleBidDone host operator does not want an alert");
                        }
                    }
                }

                base.HandleBidDone(source, bid);
                for (Seats s = Seats.North; s <= Seats.West; s++)
                {
                    this.host.clients[s].state = TableManagerProtocolState.WaitForMyCards;
                    if (s != source)
                    {
                        if (bid.Alert && s.IsSameDirection(source))
                        {   // remove alert info for his partner
                            var unalerted = new Bid(bid.Index, "", false, "");
                            this.host.clients[s].WriteData(ProtocolHelper.Translate(unalerted, source));
                        }
                        else
                        {
                            this.host.clients[s].WriteData(ProtocolHelper.Translate(bid, source));
                        }
                    }
                }

                lock (this.host.clients) for (Seats s = Seats.North; s <= Seats.West; s++)
                {
                    this.host.clients[s].Pause = this.Auction.Ended;
                }
            }

            private bool BidMayBeAlerted(Bid bid)
            {
                if (bid.IsPass) return false;
                if (this.Auction.LastRegularBid.IsPass) return false;
                return true;
            }

            public override void HandleCardNeeded(Seats controller, Seats whoseTurn, Suits leadSuit, Suits trump, bool trumpAllowed, int leadSuitLength, int trick)
            {
#if syncTrace
                //Log.Trace(4, "HostBoardResult.HandleCardNeeded");
#endif
                if (leadSuit == Suits.NoTrump)
                {
                    if (this.host.clients[controller].PauseBeforeSending) Threading.Sleep(200);
                    this.host.clients[controller].WriteData("{0} to lead", whoseTurn == this.Play.Dummy ? "Dummy" : whoseTurn.ToXMLFull());
                }

                for (Seats s = Seats.North; s <= Seats.West; s++)
                {
                    this.host.clients[s].state = (s == controller ? TableManagerProtocolState.WaitForOwnCardPlay : TableManagerProtocolState.WaitForCardPlay);
                    lock (this.host.clients) this.host.clients[s].Pause = false;
                }

                this.host.ThinkTime[whoseTurn.Direction()].Start();
            }

            public override void HandleCardPlayed(Seats source, Suits suit, Ranks rank)
            {
#if syncTrace
                Log.Trace(4, "HostBoardResult.HandleCardPlayed {0} plays {2}{1}", source, suit.ToXML(), rank.ToXML());
#endif
                this.host.ThinkTime[source.Direction()].Stop();
                //this.host.boardTime[source.Direction()] = this.host.boardTime[source.Direction()].Add(timer.Elapsed.Subtract(new TimeSpan(this.host.clients[source].communicationLag)));
                base.HandleCardPlayed(source, suit, rank);
                for (Seats s = Seats.North; s <= Seats.West; s++)
                {
                    if ((s != source && !(s == this.Auction.Declarer && source == this.Play.Dummy))
                        || (s == source && source == this.Play.Dummy)
                        )
                    {
                        this.host.clients[s].WriteData("{0} plays {2}{1}", source, suit.ToXML(), rank.ToXML());
                    }

                    if (this.Play.currentTrick == 1 && this.Play.man == 2)
                    {   // 1st card: need to send dummies cards
#if syncTrace
                        //Log.Trace("HostBoardResult.HandleCardPlayed 1st card to {0}", s);
#endif
                        var mustPause = s == this.Play.Dummy;
                        lock (this.host.clients) this.host.clients[s].Pause = mustPause;
                        this.host.clients[s].state = s == this.Play.Dummy ? TableManagerProtocolState.GiveDummiesCards : TableManagerProtocolState.WaitForDummiesCards;
                    }
                }
            }

            public override void HandleNeedDummiesCards(Seats dummy)
            {
                //base.HandleNeedDummiesCards(dummy);
            }

            public override void HandleShowDummy(Seats dummy)
            {
                //base.HandleShowDummy(dummy);
            }
        }
    }

    public abstract class ClientData
    {
        public ClientData(TableManagerHost h)
        {
            this.state = TableManagerProtocolState.Initial;
            this.Pause = false;
            this.messages = new ConcurrentQueue<string>();
            this.host = h;
            this.waitForAnswer = false;
        }

        public TableManagerProtocolState state;
        public string teamName;
        public string hand;
        public ConcurrentQueue<string> messages;
        public long communicationLag;
        public bool PauseBeforeSending;
        public bool CanAskForExplanation;
        public Seats seat;
        public bool seatTaken;
        protected TableManagerHost host;
        private bool waitForAnswer;
        private readonly ManualResetEvent mre = new ManualResetEvent(false);
        private string answer;

        private bool _pause;
        public bool Pause
        {
            get { return _pause; }
            set
            {
                if (value != _pause)
                {
                    _pause = value;
#if syncTrace
                    Log.Trace(3, "Host {1} {0}", hand, _pause ? "pauses" : "resumes");
#endif
                }
            }
        }

        public void WriteData(string message, params object[] args)
        {
            message = string.Format(message, args);
            Log.Trace(0, "{2} sends {0} '{1}'", seat, message, this.host.Name);
            this.WriteData2(message);
        }

        protected abstract void WriteData2(string message);

        public virtual void Refuse(string reason, params object[] args)
        {
            this.WriteData(reason, args);
        }

        public void ProcessIncomingMessage(string message, params object[] args)
        {
            message = string.Format(message, args);
            if (this.waitForAnswer)
            {
                this.answer = message;
                this.waitForAnswer = false;
                this.mre.Set();
                return;
            }

            lock (this.messages)
            {
                this.messages.Enqueue(message);
#if syncTrace
                Log.Trace(3, "{3} queued {0}'s '{1}' ({2} messages on q)", seat, message, this.messages.Count, this.host.Name);
#endif
            }
        }

        public string WriteAndWait(string message, params object[] args)
        {
            this.waitForAnswer = true;
            this.mre.Reset();
            this.WriteData(message, args);
            this.mre.WaitOne();
#if syncTrace
            Log.Trace(3, "{0} received explanation", this.host.Name);
#endif
            return this.answer;
        }
    }

    public static class x
    {
        public static TimeSpan RoundToSeconds(this TimeSpan timespan, int seconds = 1)
        {
            long offset = (timespan.Ticks >= 0) ? TimeSpan.TicksPerSecond / 2 : TimeSpan.TicksPerSecond / -2;
            return TimeSpan.FromTicks((timespan.Ticks + offset) / TimeSpan.TicksPerSecond * TimeSpan.TicksPerSecond);
        }
    }
}
