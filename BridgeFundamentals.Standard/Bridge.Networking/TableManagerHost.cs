﻿#define syncTrace   // uncomment to get detailed trace of events and protocol messages

using System;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using System.Collections.Generic;

namespace Bridge.Networking
{
	public enum HostEvents { Seated, ReadyForTeams, ReadyToStart, ReadyForDeal, ReadyForCards, BoardFinished, Finished }
    public enum HostMode { SingleTableTwoRounds, SingleTableInstantReplay, TwoTables }

    public delegate void HandleHostEvent<T>(TableManagerHost<T> sender, HostEvents hostEvent, object eventData) where T : ClientData;
    public delegate void HandleReceivedMessage<T>(TableManagerHost<T> sender, DateTime received, string message) where T : ClientData;

    /// <summary>
    /// Implementation of the server side of the Bridge Network Protocol
    /// as described in http://www.bluechipbridge.co.uk/protocol.htm
    /// </summary>
    public abstract class TableManagerHost<T> : BridgeEventBusClient where T : ClientData
    {
        public event HandleHostEvent<T> OnHostEvent;
        public event HandleReceivedMessage<T> OnRelevantBridgeInfo;

        protected SeatCollection<T> seatedClients;
        protected List<T> unseatedClients;
        private string lastRelevantMessage;
        private bool moreBoards;
        //private bool allReadyForStartOfBoard;
        //private bool allReadyForDeal;
        private TournamentController c;
        private BoardResultRecorder CurrentResult;
        private DirectionDictionary<TimeSpan> boardTime;
        private System.Diagnostics.Stopwatch lagTimer;
        private SemaphoreSlim waiter;
        private HostMode mode;
        private bool rotateHands;

        public DirectionDictionary<System.Diagnostics.Stopwatch> ThinkTime { get; private set; }

        public Tournament HostedTournament { get; private set; }

        protected TableManagerHost(HostMode _mode, BridgeEventBus bus, string name) : base(bus, name)
		{
            this.seatedClients = new SeatCollection<T>();
            this.unseatedClients = new List<T>();
            this.moreBoards = true;
            this.mode = _mode;
            this.rotateHands = false;
            this.lagTimer = new System.Diagnostics.Stopwatch();
            this.ThinkTime = new DirectionDictionary<System.Diagnostics.Stopwatch>(new System.Diagnostics.Stopwatch(), new System.Diagnostics.Stopwatch());
            this.boardTime = new DirectionDictionary<TimeSpan>(new TimeSpan(), new TimeSpan());
            this.waiter = new SemaphoreSlim(initialCount: 0);
            Task.Run(async () =>
            {
                await this.ProcessMessages();
            });
        }

        public void AddUnseated(T client)
        {
            this.unseatedClients.Add(client);
            client.seatTaken = false;
            client.hostActionWhenSeating = (client2, message) => this.Seat((T)client2, message);
        }

        public void HostTournament(string pbnTournament, int firstBoard)
        {
            this.HostedTournament = TournamentLoader.LoadAsync(File.OpenRead(pbnTournament)).Result;
            this.c = new TMController(this, this.HostedTournament, new ParticipantInfo() { ConventionCardNS = this.seatedClients[Seats.North].teamName, ConventionCardWE = this.seatedClients[Seats.East].teamName, MaxThinkTime = 120, UserId = Guid.NewGuid(), PlayerNames = new Participant(this.seatedClients[Seats.North].teamName, this.seatedClients[Seats.East].teamName, this.seatedClients[Seats.North].teamName, this.seatedClients[Seats.East].teamName) }, this.EventBus);
            //this.allReadyForStartOfBoard = false;
            this.ThinkTime[Directions.NorthSouth].Reset();
            this.ThinkTime[Directions.EastWest].Reset();
            this.c.StartTournament(firstBoard);
            this.OnRelevantBridgeInfo?.Invoke(this, DateTime.UtcNow, "Event " + this.HostedTournament.EventName);
        }

        public async Task HostTournamentAsync(string pbnTournament, int firstBoard)
        {
            this.HostedTournament = await TournamentLoader.LoadAsync(File.OpenRead(pbnTournament));
            this.c = new TMController(this, this.HostedTournament, new ParticipantInfo() { ConventionCardNS = this.seatedClients[Seats.North].teamName, ConventionCardWE = this.seatedClients[Seats.East].teamName, MaxThinkTime = 120, UserId = Guid.NewGuid(), PlayerNames = new Participant(this.seatedClients[Seats.North].teamName, this.seatedClients[Seats.East].teamName, this.seatedClients[Seats.North].teamName, this.seatedClients[Seats.East].teamName) }, this.EventBus);
            //this.allReadyForStartOfBoard = false;
            this.ThinkTime[Directions.NorthSouth].Reset();
            this.ThinkTime[Directions.EastWest].Reset();
            await this.c.StartTournamentAsync(firstBoard);
        }

        public bool IsProcessing
        {
            get
            {
                for (Seats seat = Seats.North; seat <= Seats.West; seat++)
                {
                    if (this.seatedClients[seat].messages.Count > 0) return true;
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
                    //Log.Trace(4, "{0} main message loop", this.Name);
#endif
                    waitForNewMessage = 20;
                    for (Seats seat = Seats.North; seat <= Seats.West; seat++)
                    {
                        if (this.seatedClients[seat] != null && !this.seatedClients[seat].Pause && !this.seatedClients[seat].messages.IsEmpty)
                        {
                            lock (this.seatedClients[seat].messages)
                            {
                                this.seatedClients[seat].messages.TryDequeue(out m);
                            }

                            if (m != null)
                            {
#if syncTrace
                                Log.Trace(3, "{2} dequeued {0}'s '{1}'", seat, m, this.Name);
#endif
                                waitForNewMessage = minimumWait;
                                lock (this.seatedClients)
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
                    this.seatedClients[seat].messages.TryDequeue(out m);
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

        public void Seat(T client, string message)
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
                    if (this.seatedClients[client.seat] == null)
                    {
                        int p = message.IndexOf("\"");
                        var teamName = message.Substring(p + 1, message.IndexOf("\"", p + 1) - (p + 1));
                        client.teamName = teamName;
                        client.hand = client.seat.ToString();
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
                        if (this.seatedClients[partner] != null)
                        {
                            if (this.seatedClients[partner].teamName == null)
                            {
                                this.seatedClients[partner].teamName = teamName;
                            }
                            else
                            {
                                partnerTeamName = this.seatedClients[partner].teamName;
                            }
                        }

                        if (teamName == partnerTeamName)
                        {
                            client.state = TableManagerProtocolState.WaitForSeated;
                            client.seatTaken = true;
                            this.seatedClients[client.seat] = client;
                            //client.WriteData("{1} (\"{0}\") seated", client.teamName, client.hand);
                            //this.OnHostEvent(this, HostEvents.Seated, client.seat + "|" + teamName);
                            this.Seated(client, message, string.Format("{1} (\"{0}\") seated", client.teamName, client.hand));
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

        protected virtual void Seated(T client, string request, string response)
        {
            client.WriteData(response);
            if (this.OnHostEvent != null) this.OnHostEvent(this, HostEvents.Seated, client.seat + "|" + client.teamName);
        }

        protected virtual void ProcessMessage(string message, Seats seat)
		{
#if syncTrace
			Log.Trace(2, "{1} processing '{0}'", message, this.Name);
#endif
            var received = DateTime.UtcNow;
            switch (this.seatedClients[seat].state)
			{
				case TableManagerProtocolState.WaitForSeated:
					ChangeState(message, this.seatedClients[seat].hand + " ready for teams", TableManagerProtocolState.WaitForTeams, seat);
					break;

				case TableManagerProtocolState.WaitForTeams:
                    this.UpdateCommunicationLag(seat, this.lagTimer.ElapsedTicks);
					ChangeState(message, this.seatedClients[seat].hand + " ready to start", TableManagerProtocolState.WaitForStartOfBoard, seat);
					break;

				case TableManagerProtocolState.WaitForStartOfBoard:
                    this.UpdateCommunicationLag(seat, this.lagTimer.ElapsedTicks);
                    ChangeState(message, this.seatedClients[seat].hand + " ready for deal", TableManagerProtocolState.WaitForBoardInfo, seat);
					break;

				case TableManagerProtocolState.WaitForBoardInfo:
                    this.UpdateCommunicationLag(seat, this.lagTimer.ElapsedTicks);
                    ChangeState(message, this.seatedClients[seat].hand + " ready for cards", TableManagerProtocolState.WaitForMyCards, seat);
					this.OnHostEvent(this, HostEvents.ReadyForCards, seat);
					break;

				case TableManagerProtocolState.WaitForMyCards:
                    lock (this.seatedClients) this.seatedClients[seat].Pause = true;
                    if (seat == Rotated(this.CurrentResult.Auction.WhoseTurn))
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
                        ChangeState(message, this.seatedClients[seat].hand + " ", TableManagerProtocolState.WaitForOtherBid, seat);
                        this.OnRelevantBridgeInfo?.Invoke(this, received, message);
                    }
                    else
                    {
                        ChangeState(message, string.Format("{0} ready for {1}'s bid", this.seatedClients[seat].hand, Rotated(this.CurrentResult.Auction.WhoseTurn)), TableManagerProtocolState.WaitForOtherBid, seat);
                    }
                    break;

                case TableManagerProtocolState.WaitForCardPlay:
                    lock (this.seatedClients) this.seatedClients[seat].Pause = true;
                    // ready for dummy's card mag ook ready for xx's card
                    if (this.CurrentResult.Play.whoseTurn == this.CurrentResult.Play.Dummy)
                    {
                        if (seat == Rotated(this.CurrentResult.Play.Dummy))
                        {
                            ChangeState(message, string.Format("{0} ready for dummy's card to trick {2}", this.seatedClients[seat].hand, message.Contains("dummy") ? "dummy" : this.Rotated(this.CurrentResult.Play.whoseTurn).ToString(), this.CurrentResult.Play.currentTrick), TableManagerProtocolState.WaitForOtherCardPlay, seat);
                        }
                        else
                        {
                            ChangeState(message, string.Format("{0} ready for {1}'s card to trick {2};{0} ready for dummy's card to trick {2}", this.seatedClients[seat].hand, this.Rotated(this.CurrentResult.Play.whoseTurn).ToString(), this.CurrentResult.Play.currentTrick), TableManagerProtocolState.WaitForOtherCardPlay, seat);
                        }
                    }
                    else
                    {
                        ChangeState(message, string.Format("{0} ready for {1}'s card to trick {2}", this.seatedClients[seat].hand, this.Rotated(this.CurrentResult.Play.whoseTurn).ToString(), this.CurrentResult.Play.currentTrick), TableManagerProtocolState.WaitForOtherCardPlay, seat);
                    }
					break;

				case TableManagerProtocolState.WaitForOwnCardPlay:
                    lock (this.seatedClients) this.seatedClients[seat].Pause = true;
                    this.lastRelevantMessage = message;
#if syncTrace
                    //Log.Trace("{1} lastRelevantMessage={0}", message, this.hostName);
#endif
                    ChangeState(message, string.Format("{0} plays ", this.Rotated(this.CurrentResult.Play.whoseTurn)), TableManagerProtocolState.WaitForOtherCardPlay, seat);
                    this.OnRelevantBridgeInfo?.Invoke(this, received, message);
                    break;

				case TableManagerProtocolState.WaitForDummiesCardPlay:
					ChangeState(message, string.Format("{0} plays ", this.Rotated(this.CurrentResult.Play.whoseTurn)), TableManagerProtocolState.WaitForOtherCardPlay, seat);
                    this.OnRelevantBridgeInfo?.Invoke(this, received, message);
                    break;

				case TableManagerProtocolState.WaitForDummiesCards:
					ChangeState(message, string.Format("{0} ready for dummy", this.seatedClients[seat].hand), TableManagerProtocolState.GiveDummiesCards, seat);
					break;

				default:
#if syncTrace
                    Log.Trace(0, "{3} unexpected '{0}' from {1} in state {2}", message, seat, this.seatedClients[seat].state, this.Name);
                    this.DumpQueue();
#endif
                    this.seatedClients[seat].Refuse("Unexpected '{0}' in state {1}", message, this.seatedClients[seat].state);
                    throw new InvalidOperationException(string.Format("Unexpected '{0}' in state {1}", message, this.seatedClients[seat].state));
			}
		}

		private void ChangeState(string message, string expected, TableManagerProtocolState newState, Seats seat)
		{
            var exp = expected.Split(';');
			if (message.ToLowerInvariant().Replace("  ", " ").StartsWith(exp[0].ToLowerInvariant()) || (exp.Length >= 2 && message.ToLowerInvariant().Replace("  ", " ").StartsWith(exp[1].ToLowerInvariant())))
			{
				this.seatedClients[seat].state = newState;
                var allReady = true;
                var answer = string.Empty;
                for (Seats s = Seats.North; s <= Seats.West; s++) if (this.seatedClients[s] == null || this.seatedClients[s].state != newState) { allReady = false; break; }
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
                            answer = "Teams : N/S : \"" + this.seatedClients[Seats.North].teamName + "\" E/W : \"" + this.seatedClients[Seats.East].teamName + "\"";
                            this.BroadCast(answer);
                            this.OnRelevantBridgeInfo?.Invoke(this, DateTime.UtcNow, answer);
                            if (this.OnHostEvent != null) this.OnHostEvent(this, HostEvents.ReadyForTeams, null);
                            break;
                        case TableManagerProtocolState.WaitForStartOfBoard:
                            this.c.StartNextBoard().Wait();
                            break;
                        case TableManagerProtocolState.WaitForBoardInfo:
                            answer = string.Format("Board number {0}. Dealer {1}. {2} vulnerable.", this.c.currentBoard.BoardNumber, Rotated(this.c.currentBoard.Dealer).ToXMLFull(), ProtocolHelper.Translate(RotatedV(this.c.currentBoard.Vulnerable)));
                            this.BroadCast(answer);
                            this.OnRelevantBridgeInfo?.Invoke(this, DateTime.UtcNow, answer);
                            break;
                        case TableManagerProtocolState.WaitForMyCards:
                            for (Seats s = Seats.North; s <= Seats.West; s++)
                            {
                                answer = Rotated(s).ToXMLFull() + ProtocolHelper.Translate(s, this.c.currentBoard.Distribution);
                                this.seatedClients[Rotated(s)].WriteData(answer);
                                this.OnRelevantBridgeInfo?.Invoke(this, DateTime.UtcNow, answer);
                            }
                            break;
                        case TableManagerProtocolState.WaitForCardPlay:
                            break;
                        case TableManagerProtocolState.WaitForOtherBid:
                            for (Seats s = Seats.North; s <= Seats.West; s++) this.seatedClients[s].state = TableManagerProtocolState.WaitForCardPlay;
                            ProtocolHelper.HandleProtocolBid(UnRotated(this.lastRelevantMessage), this.EventBus);
                            break;
                        case TableManagerProtocolState.WaitForOtherCardPlay:
                            ProtocolHelper.HandleProtocolPlay(UnRotated(this.lastRelevantMessage), this.EventBus);
                            break;
                        case TableManagerProtocolState.WaitForOwnCardPlay:
                            break;
                        case TableManagerProtocolState.WaitForDummiesCardPlay:
                            break;
                        case TableManagerProtocolState.GiveDummiesCards:
                            var cards = "Dummy" + ProtocolHelper.Translate(this.CurrentResult.Play.Dummy, this.c.currentBoard.Distribution);
                            for (Seats s = Seats.North; s <= Seats.West; s++)
                            {
                                if (s != this.Rotated(this.CurrentResult.Play.Dummy))
                                {
                                    this.seatedClients[s].WriteData(cards);
                                }
                            }
                            for (Seats s = Seats.North; s <= Seats.West; s++)
                            {
                                this.seatedClients[s].state = (s == this.Rotated(this.CurrentResult.Auction.Declarer) ? TableManagerProtocolState.WaitForOwnCardPlay : TableManagerProtocolState.WaitForCardPlay);
                                lock (this.seatedClients) this.seatedClients[s].Pause = false;
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
                this.seatedClients[seat].Refuse("Expected '{0}'", expected);
                throw new InvalidOperationException(string.Format("Expected '{0}'", expected));
            }

            Vulnerable RotatedV(Vulnerable v)
            {
                if (this.rotateHands)
                {
                    switch (v)
                    {
                        case Vulnerable.Neither:
                            return Vulnerable.Neither;
                        case Vulnerable.NS:
                            return Vulnerable.EW;
                        case Vulnerable.EW:
                            return Vulnerable.NS;
                        default:
                            return Vulnerable.Both;
                    }
                }
                else
                {
                    return v;
                }
            }
        }

        //T Client(Seats s)
        //{
        //    return this.seatedClients[Rotated(s)];
        //}

        private Seats Rotated(Seats p)
        {
            if (this.rotateHands) return p.Next();
            return p;
        }

        private string UnRotated(string message)
        {
            if (!this.rotateHands) return message;

            string[] answer = message.Split(' ');
            var player = SeatsExtensions.FromXML(answer[0]);
            message = player.Previous().ToXMLFull();
            for (int i = 1; i < answer.Length; i++)
            {
                message += " " + answer[i];
            }
            return message;
        }

        public void BroadCast(string message, params object[] args)
        {
            for (Seats s = Seats.North; s <= Seats.West; s++)
            {
                if (this.seatedClients[s].PauseBeforeSending) Threading.Sleep(300);
                this.seatedClients[s].WriteData(message, args);
            }

            this.lagTimer.Restart();
        }

        private void UpdateCommunicationLag(Seats source, long lag)
        {
            //Log.Trace("Host UpdateCommunicationLag for {0} old lag={1} lag={2}", source, this.clients[source].communicationLag, lag);
            this.seatedClients[source].communicationLag += lag;
            this.seatedClients[source].communicationLag /= 2;
            //Log.Trace("Host UpdateCommunicationLag for {0} new lag={1}", source, this.clients[source].communicationLag);
        }

        protected virtual void ExplainBid(Seats source, Bid bid)
        {
            // opportunity to implement manual alerting
        }

        protected virtual void Stop()
        {
            Log.Trace(4, "TableManagerHost.Stop");
            SeatsExtensions.ForEachSeat(s => this.seatedClients[s]?.Dispose());
            this.waiter.Release();
        }

        public async Task WaitForCompletionAsync()
        {
            await this.waiter.WaitAsync();
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
                this.seatedClients[s].Pause = true;
                this.seatedClients[s].state = TableManagerProtocolState.WaitForStartOfBoard;
            }

            Threading.Sleep(20);
            this.BroadCast("Start of board");
            for (Seats s = Seats.North; s <= Seats.West; s++)
            {
                this.seatedClients[s].Pause = false;
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
            var timingInfo = string.Format("Timing - N/S : this board  {0:mm\\:ss},  total  {1:h\\:mm\\:ss}.  E/W : this board  {2:mm\\:ss},  total  {3:h\\:mm\\:ss}."
                , this.boardTime[Directions.NorthSouth].RoundToSeconds()
                , this.ThinkTime[Directions.NorthSouth].Elapsed.RoundToSeconds()
                , this.boardTime[Directions.EastWest].RoundToSeconds()
                , this.ThinkTime[Directions.EastWest].Elapsed.RoundToSeconds()
                );
            this.BroadCast(timingInfo);
            this.OnRelevantBridgeInfo?.Invoke(this, DateTime.UtcNow, timingInfo);
            this.boardTime[Directions.NorthSouth] = this.ThinkTime[Directions.NorthSouth].Elapsed;
            this.boardTime[Directions.EastWest] = this.ThinkTime[Directions.EastWest].Elapsed;
            for (Seats s = Seats.North; s <= Seats.West; s++)
            {
                this.seatedClients[s].state = TableManagerProtocolState.WaitForStartOfBoard;
                this.seatedClients[s].Pause = false;
            }

            if (this.OnHostEvent != null) this.OnHostEvent(this, HostEvents.BoardFinished, currentResult);
        }

        public override void HandleTournamentStopped()
        {
#if syncTrace
            Log.Trace(4, "TableManagerHost.HandleTournamentStopped");
#endif
            Threading.Sleep(20);
            this.BroadCast("End of session");
            this.OnRelevantBridgeInfo?.Invoke(this, DateTime.UtcNow, "End of session");
            this.moreBoards = false;
            this.OnHostEvent(this, HostEvents.Finished, null);
            this.Stop();
        }

        #endregion

        private class TMController : TournamentController
        {
            private TableManagerHost<T> host;

            public TMController(TableManagerHost<T> h, Tournament t, ParticipantInfo p, BridgeEventBus bus) : base(t, p, bus)
            {
                this.host = h;
            }

            protected override async Task GetNextBoard()
            {
                if (this.host.mode == HostMode.SingleTableInstantReplay && HasBeenPlayedOnce(this.currentBoard))
                {
                    Log.Trace(3, "TMController.GetNextBoard instant replay this board");
                    this.host.rotateHands = true;
                }
                else
                {
                    this.host.rotateHands = false;
                    await base.GetNextBoard();
                }

                bool HasBeenPlayedOnce(Board2 board)
                {
                    if (board == null) return false;
                    var played = 0;
                    foreach (var result in board.Results)
                    {
                        if (result.Play.PlayEnded)
                        {
                            if (HasBeenPlayedBy(result, this.host.seatedClients[Seats.North].teamName, this.host.seatedClients[Seats.East].teamName)) played++;
                            else if (HasBeenPlayedBy(result, this.host.seatedClients[Seats.East].teamName, this.host.seatedClients[Seats.North].teamName)) played++;
                        }
                    }

                    return played == 1;

                    bool HasBeenPlayedBy(BoardResult result, string team1, string team2)
                    {
                        return result.Participants.Names[Seats.North] == team1 && result.Participants.Names[Seats.East] == team2;
                    }
                }
            }

            protected override BoardResultRecorder NewBoardResult(int boardNumber)
            {
                this.host.CurrentResult = new HostBoardResult(this.host, this.currentBoard, Rotate(this.participant.PlayerNames.Names), this.EventBus);
                return this.host.CurrentResult;

                SeatCollection<string> Rotate(SeatCollection<string> names)
                {
                    if (!this.host.rotateHands) return names;
                    var rotatedNames = new SeatCollection<string>();
                    SeatsExtensions.ForEachSeat(s =>
                    {
                        rotatedNames[s] = names[s.Next()];
                    });
                    return rotatedNames;
                }
            }
        }

        private class HostBoardResult : BoardResultEventPublisher
        {
            private TableManagerHost<T> host;

            public HostBoardResult(TableManagerHost<T> h, Board2 board, SeatCollection<string> newParticipants, BridgeEventBus bus)
                : base("HostBoardResult", board, newParticipants, bus, null)
            {
                this.host = h;
            }

            public override void HandleBidNeeded(Seats whoseTurn, Bid lastRegularBid, bool allowDouble, bool allowRedouble)
            {
                base.HandleBidNeeded(whoseTurn, lastRegularBid, allowDouble, allowRedouble);
                Log.Trace(2, $"start think time for {this.host.Rotated(whoseTurn).Direction()} at {this.host.ThinkTime[this.host.Rotated(whoseTurn).Direction()].ElapsedMilliseconds}");
                this.host.ThinkTime[this.host.Rotated(whoseTurn).Direction()].Start();
            }

            public override void HandleBidDone(Seats source, Bid bid)
            {
                this.host.ThinkTime[this.host.Rotated(source).Direction()].Stop();
#if syncTrace
                Log.Trace(2, $"stop  think time for {this.host.Rotated(source).Direction()} at {this.host.ThinkTime[this.host.Rotated(source).Direction()].ElapsedMilliseconds}");
                Log.Trace(4, "HostBoardResult.HandleBidDone");
#endif
                if (this.BidMayBeAlerted(bid) || this.host.seatedClients[source.Next()].CanAskForExplanation)
                {
#if syncTrace
                    Log.Trace(2, "HostBoardResult.HandleBidDone explain opponents bid");
#endif
                    if (!this.host.seatedClients[source.Next()].CanAskForExplanation) this.host.ExplainBid(source, bid);
                    if (bid.Alert || this.host.seatedClients[source.Next()].CanAskForExplanation)
                    {   // the operator has indicated this bid needs an explanation
                        Log.Trace(2, "HostBoardResult.HandleBidDone host operator wants an alert");
                        if (this.host.seatedClients[source.Next()].CanAskForExplanation)
                        {   // client implements this new part of the protocol
                            var answer = this.host.seatedClients[source.Next()].WriteAndWait("Explain {0}'s {1}", source, ProtocolHelper.Translate(bid));
                            bid.Explanation = answer;
                        }
                    }
                    else
                    {
                        Log.Trace(2, "HostBoardResult.HandleBidDone host operator does not want an alert");
                    }
                }

                base.HandleBidDone(source, bid);
                for (Seats s = Seats.North; s <= Seats.West; s++)
                {
                    this.host.seatedClients[s].state = TableManagerProtocolState.WaitForMyCards;
                    if (s != this.host.Rotated(source))
                    {
                        //if (bid.Alert && s.IsSameDirection(source))
                        //{   // remove alert info for his partner
                        //    var unalerted = new Bid(bid.Index, "", false, "");
                        //    this.host.seatedClients[s].WriteData(ProtocolHelper.Translate(unalerted, source));
                        //}
                        //else
                        //{
                        //    this.host.seatedClients[s].WriteData(ProtocolHelper.Translate(bid, source));
                        //}

                        this.host.seatedClients[s].WriteData(ProtocolHelper.Translate(bid.Alert && s.IsSameDirection(this.host.Rotated(source)) ? new Bid(bid.Index, "", false, "") : bid, this.host.Rotated(source)));
                    }
                }

                lock (this.host.seatedClients) for (Seats s = Seats.North; s <= Seats.West; s++)
                {
                    this.host.seatedClients[s].Pause = this.Auction.Ended;
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
                    if (this.host.seatedClients[this.host.Rotated(controller)].PauseBeforeSending) Threading.Sleep(200);
                    this.host.seatedClients[this.host.Rotated(controller)].WriteData("{0} to lead", whoseTurn == this.Play.Dummy ? "Dummy" : this.host.Rotated(whoseTurn).ToXMLFull());
                }

                for (Seats s = Seats.North; s <= Seats.West; s++)
                {
                    this.host.seatedClients[s].state = (s == this.host.Rotated(controller) ? TableManagerProtocolState.WaitForOwnCardPlay : TableManagerProtocolState.WaitForCardPlay);
                    lock (this.host.seatedClients) this.host.seatedClients[s].Pause = false;
                }

                this.host.ThinkTime[this.host.Rotated(whoseTurn).Direction()].Start();
            }

            public override void HandleCardPlayed(Seats source, Suits suit, Ranks rank)
            {
#if syncTrace
                Log.Trace(4, "HostBoardResult.HandleCardPlayed {0} plays {2}{1}", source, suit.ToXML(), rank.ToXML());
#endif
                this.host.ThinkTime[this.host.Rotated(source).Direction()].Stop();
                base.HandleCardPlayed(source, suit, rank);
                for (Seats s = Seats.North; s <= Seats.West; s++)
                {
                    if ((s != this.host.Rotated(source) && !(s == this.host.Rotated(this.Auction.Declarer) && source == this.Play.Dummy))
                        || (s == this.host.Rotated(source) && source == this.Play.Dummy)
                        )
                    {
                        this.host.seatedClients[s].WriteData("{0} plays {2}{1}", this.host.Rotated(source), suit.ToXML(), rank.ToXML());
                    }

                    if (this.Play.currentTrick == 1 && this.Play.man == 2)
                    {   // 1st card: need to send dummies cards
#if syncTrace
                        //Log.Trace("HostBoardResult.HandleCardPlayed 1st card to {0}", s);
#endif
                        var mustPause = s == this.host.Rotated(this.Play.Dummy);
                        lock (this.host.seatedClients) this.host.seatedClients[s].Pause = mustPause;
                        this.host.seatedClients[s].state = s == this.host.Rotated(this.Play.Dummy) ? TableManagerProtocolState.GiveDummiesCards : TableManagerProtocolState.WaitForDummiesCards;
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

    public abstract class ClientData : IDisposable
    {
        public ClientData()
        {
            this.state = TableManagerProtocolState.Initial;
            this.Pause = false;
            this.messages = new ConcurrentQueue<string>();
            this.waitForAnswer = false;
            this.seatTaken = false;
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
        private bool waitForAnswer;
        private readonly ManualResetEvent mre = new ManualResetEvent(false);
        private string answer;
        public Action<ClientData, string> hostActionWhenSeating;

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
            Log.Trace(0, "{2} sends {0} '{1}'", seat, message, "Host");
            this.WriteToDevice(message);
        }

        protected abstract void WriteToDevice(string message);

        public virtual void Refuse(string reason, params object[] args)
        {
            this.WriteData(reason, args);
        }

        public void ProcessIncomingMessage(string message, params object[] args)
        {
            message = string.Format(message, args);
            if (this.seatTaken)
            {
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
                    Log.Trace(3, "{3} queued {0}'s '{1}' ({2} messages on q)", seat, message, this.messages.Count, "Host");
#endif
                }
            }
            else
            {
                this.hostActionWhenSeating(this, message);
            }
        }

        public string WriteAndWait(string message, params object[] args)
        {
            this.waitForAnswer = true;
            this.mre.Reset();
            this.WriteData(message, args);
            this.mre.WaitOne();
#if syncTrace
            Log.Trace(3, "{0} received explanation", "Host");
#endif
            return this.answer;
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects).
                }

                this.mre.Close();

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~ClientData() {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }
        #endregion
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
