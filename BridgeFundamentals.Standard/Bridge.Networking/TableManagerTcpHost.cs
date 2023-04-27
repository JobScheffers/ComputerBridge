using Bridge.NonBridgeHelpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Bridge.Networking
{
    public class TableManagerTcpHost : AsyncTableHost<HostTcpCommunication>
    {
        public TableManagerTcpHost(HostMode mode, HostTcpCommunication communicationDetails, BridgeEventBus bus, string hostName, string tournamentFileName) : base(mode, communicationDetails, bus, hostName, tournamentFileName)
        {
        }
    }

    public class BaseAsyncTcpHost : BaseAsyncDisposable
    {
        private bool isRunning = false;
        private readonly CancellationTokenSource cts;
        private readonly List<HostAsyncTcpClient> clients;
        private readonly IPEndPoint endPoint;
        private readonly Func<int, string, ValueTask> processMessage;
        private readonly Func<int, ValueTask> onNewClient;
        private readonly string name;
        public Func<int, ValueTask> OnClientConnectionLost;

        public BaseAsyncTcpHost(IPEndPoint tcpPort, Func<int, string, ValueTask> _processMessage, Func<int, ValueTask> _onNewClient, string hostName)
        {
            this.endPoint = tcpPort;
            this.cts = new CancellationTokenSource();
            this.clients = new List<HostAsyncTcpClient>();
            this.processMessage = _processMessage;
            this.onNewClient = _onNewClient;
            this.name = hostName + ".AsyncTcpHost";
        }

//#if NET6_0_OR_GREATER
        protected override async ValueTask DisposeManagedObjects()
        {
            Log.Trace(4, $"{this.name}.DisposeManagedObjects");
            cts.Dispose();
            foreach (var client in this.clients)
            {
                await client.DisposeAsync();
            }
        }
//#endif

        public async ValueTask Run()
        {
            Log.Trace(4, $"{this.name}.Run begin");
            var listener = new TcpListener(this.endPoint);

            // trick to prevent error in unittests "Only one usage of each socket address (protocol/network address/port) is normally permitted"
            // https://social.msdn.microsoft.com/Forums/en-US/e1cc5f98-5a85-4da7-863e-f4d5623d80a0/forcing-tcplisteneros-to-release-port?forum=netfxcompact
            //listener.Server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, 1);

            listener.Start();
            this.isRunning = true;
            while (this.isRunning)
            {
                try
                {
#if NET6_0_OR_GREATER
                    var c = await listener.AcceptTcpClientAsync(cts.Token);
#else
                    var c = await listener.AcceptTcpClientAsync();
#endif
                    Log.Trace(2, $"{this.name}.Run new client");
                    var client = new HostAsyncTcpClient($"{this.name}.client", clients.Count, c, this.ProcessClientMessage);
                    if (this.onNewClient != null) await this.onNewClient(clients.Count);
                    this.clients.Add(client);
                    client.OnClientConnectionLost = HandleConnectionLost;
                    client.Start();
                }
                catch (OperationCanceledException)
                {
                }
            }

            Log.Trace(4, $"{this.name}.Run stop clients");
            foreach (var client in this.clients)
            {
                client.Stop();
            }
            Log.Trace(4, $"{this.name}.Run end");
        }

        private async ValueTask HandleConnectionLost(int clientId)
        {
            Log.Trace(1, $"{this.name}: {clientId} lost connection. Wait for client to reconnect....");
            await OnClientConnectionLost(clientId);
        }

        private async ValueTask ProcessClientMessage(int clientId, string message)
        {
            if (this.processMessage != null) await this.processMessage(clientId, message);
        }

        public async ValueTask Send(int clientId, string message)
        {
            await this.clients[clientId].Send(message);
        }

        public async ValueTask<string> SendAndWait(int clientId, string message)
        {
            return await this.clients[clientId].SendAndWait(message);
        }

        public void Stop()
        {
            Log.Trace(2, $"{this.name}.Stop");
            isRunning = false;
            cts.Cancel();
        }

        private class HostAsyncTcpClient : BaseAsyncTcpClient
        {
            private readonly int id;
            private readonly Func<int, string, ValueTask> processMessage;

            public Func<int, ValueTask> OnClientConnectionLost;

            public HostAsyncTcpClient(string _name, int _id, TcpClient client, Func<int, string, ValueTask> _processMessage) : base(_name + _id.ToString(), client)
            {
                this.id = _id;
                this.processMessage = _processMessage;
                this.OnConnectionLost = this.HandleConnectionLost;
            }

            protected override async ValueTask ProcessMessage(string message)
            {
                await this.processMessage(this.id, message);
            }

            public new async ValueTask Send(string message)
            {
                // the lock is necessary to prevent 2 simultane sends from one client
                using (await AsyncLock.WaitForLockAsync(this.name))
                {
                    await base.Send(message);
                    //await Task.Delay(20);       // make sure the next Send will not be too quick
                }
            }

            private async ValueTask HandleConnectionLost()
            {
                Log.Trace(1, $"{this.name} lost connection. Wait for client to reconnect....");
                await this.OnClientConnectionLost(this.id);
            }
        }
    }

    public class AsyncTableHost<T> : BridgeEventBusClient, IAsyncDisposable where T : HostCommunication
    {
        private readonly T communicationDetails;
        private ValueTask hostRunTask;
        private readonly SeatCollection<string> teams;
        private readonly SeatCollection<TableManagerProtocolState> state;
        private readonly SeatCollection<bool> pause;
        private readonly SeatCollection<bool> CanAskForExplanation;
        private string lastRelevantMessage;
        private BoardResultRecorder CurrentResult;
        private DirectionDictionary<TimeSpan> boardTime;
        private readonly System.Diagnostics.Stopwatch lagTimer;
        private readonly HostMode mode;
        private bool rotateHands;
        private readonly string tournamentFileName;

        public Func<object, HostEvents, object, ValueTask> OnHostEvent;
        public Func<object, DateTime, string, ValueTask> OnRelevantBridgeInfo;

        public AsyncTableHost(HostMode _mode, T _communicationDetails, BridgeEventBus bus, string hostName, string _tournamentFileName = "") : base(bus, hostName)
        {
            this.communicationDetails = _communicationDetails;
            this.communicationDetails.ProcessConnect = this.ProcessConnect;
            this.communicationDetails.ProcessMessage = this.ProcessMessage;
            //this.communicationDetails.OnClientConnectionLost = this.HandleConnectionLost;
            this.teams = new SeatCollection<string>();
            this.state = new SeatCollection<TableManagerProtocolState>();
            this.pause = new SeatCollection<bool>();
            this.CanAskForExplanation = new SeatCollection<bool>();
            this.ThinkTime = new DirectionDictionary<System.Diagnostics.Stopwatch>(new System.Diagnostics.Stopwatch(), new System.Diagnostics.Stopwatch());
            this.lagTimer = new System.Diagnostics.Stopwatch();
            this.tournamentFileName = _tournamentFileName;
            this.boardTime = new DirectionDictionary<TimeSpan>(TimeSpan.Zero, TimeSpan.Zero);
            this.mode = _mode;
        }

        public void Run()
        {
            this.hostRunTask = this.communicationDetails.Run();
        }

        public async ValueTask WaitForCompletionAsync()
        {
            await this.hostRunTask;
        }

        protected async ValueTask<ConnectResponse> ProcessConnect(int clientId, string message)
        {
            Log.Trace(1, $"{this.Name}.ProcessConnect: client {clientId} sent '{message}'");

            var loweredMessage = message.ToLowerInvariant();
            if (!(loweredMessage.Contains("connecting") && loweredMessage.Contains("using protocol version")))
            {
                // check if this is a message from a disconnected seat
#if NET6_0_OR_GREATER
                var hand2 = loweredMessage[0..loweredMessage.IndexOf(" ")];
#else
                var hand2 = loweredMessage.Substring(0, loweredMessage.IndexOf(" "));
#endif
                if (hand2 == "north" || hand2 == "east" || hand2 == "south" || hand2 == "west")
                {
                    var seat2 = SeatsExtensions.FromXML(hand2);
                    await this.ProcessMessage(seat2, message);
                    return new ConnectResponse(seat2, "");
                }


                return new ConnectResponse(Seats.North - 1, "Expected 'Connecting .... as ... using protocol version ..'");
            }

            var hand = loweredMessage.Substring(loweredMessage.IndexOf(" as ") + 4, 5).Trim();
            if (!(hand == "north" || hand == "east" || hand == "south" || hand == "west"))
            {
                return new ConnectResponse(Seats.North - 1, "Illegal hand specified");
            }

#if NET6_0_OR_GREATER
            var seat = SeatsExtensions.FromXML(hand[0..1].ToUpperInvariant());
#else
            var seat = SeatsExtensions.FromXML(hand.Substring(0, 2).ToUpperInvariant());
#endif
            //if (this.clients[seat] < 0 || this.clients[seat] == clientId)
            {       // seat not taken yet
                int p = message.IndexOf("\"");
#if NET6_0_OR_GREATER
                var teamName = message[(p + 1)..message.IndexOf("\"", p + 1)];
#else
                var teamName = message.Substring(p + 1, message.IndexOf("\"", p + 1) - (p + 1));
#endif
                this.teams[seat] = teamName;
#if NET6_0_OR_GREATER
                var protocolVersion = int.Parse(message[(message.IndexOf(" version ") + 9)..]);
#else
                var protocolVersion = int.Parse(message.Substring(message.IndexOf(" version ") + 9));
#endif
                switch (protocolVersion)
                {
                    case 18:
                        //client.PauseBeforeSending = true;
                        this.CanAskForExplanation[seat] = false;
                        break;
                    case 19:
                        //client.PauseBeforeSending = false;
                        this.CanAskForExplanation[seat] = false;
                        break;
                    default:
                        throw new ArgumentException($"protocol version {protocolVersion} not supported");
                }

                var partner = seat.Partner();
                var partnerTeamName = teamName;
                //if (this.clients[partner] >= 0)
                {
                    if (this.teams[partner] == null)
                    {
                        this.teams[partner] = teamName;
                    }
                    else
                    {
                        partnerTeamName = this.teams[partner];
                    }
                }

                if (teamName == partnerTeamName)
                {
                    state[seat] = TableManagerProtocolState.WaitForSeated;
                    //this.clients[seat] = clientId;
                    //this.seats[clientId] = seat;
                    return new ConnectResponse(seat, $"{seat} (\"{teamName}\") seated");
                    //this.OnHostEvent(this, HostEvents.Seated, client.seat + "|" + teamName);
                }
                else
                {
                    return new ConnectResponse(Seats.North - 1, $"Expected team name '{partnerTeamName}'");
                }
            }
            //else
            //{
            //return new(Seats.North - 1, "Seat already has been taken");
            //}
        }

        //private async ValueTask ProcessClientMessage(Seats seat, string message)
        //{
        //    var loweredMessage = message.ToLowerInvariant();

        //    if (seats[clientId] >= Seats.North)
        //    {   // this tcp client is already seated 
        //        await this.ProcessMessage(seats[clientId], message);
        //    }
        //    else
        //    if (seats[clientId] < Seats.North && state[Seats.North] != TableManagerProtocolState.Initial)
        //    {   // this tcp client is reconnecting
        //        Log.Trace(4, $"{this.Name} client {clientId} is reconnecting");
        //        var seat = SeatsExtensions.FromXML(message.Split(' ')[0]);
        //        seats[clientId] = seat;
        //        clients[seat] = clientId;
        //        await this.ProcessMessage(seats[clientId], message);
        //    }
        //    }
        //}

        protected async ValueTask ProcessMessage(Seats seat, string message)
        {
            if (this.pause[seat]) Log.Trace(3, $"{this.Name} waits for paused {seat} ('{message}')");
            while (this.pause[seat]) await Task.Delay(200);
            Log.Trace(1, $"{this.Name} processing {seat}'s '{message}'");
            var received = DateTime.UtcNow;
            switch (this.state[seat])
            {
                case TableManagerProtocolState.WaitForSeated:
                    await ChangeState(message, $"{seat} ready for teams", TableManagerProtocolState.WaitForTeams, seat);
                    break;

                case TableManagerProtocolState.WaitForTeams:
                    this.UpdateCommunicationLag(seat, this.lagTimer.ElapsedTicks);
                    await ChangeState(message, $"{seat} ready to start", TableManagerProtocolState.WaitForStartOfBoard, seat);
                    break;

                case TableManagerProtocolState.WaitForStartOfBoard:
                    this.UpdateCommunicationLag(seat, this.lagTimer.ElapsedTicks);
                    await ChangeState(message, $"{seat} ready for deal", TableManagerProtocolState.WaitForBoardInfo, seat);
                    break;

                case TableManagerProtocolState.WaitForBoardInfo:
                    this.UpdateCommunicationLag(seat, this.lagTimer.ElapsedTicks);
                    await ChangeState(message, $"{seat} ready for cards", TableManagerProtocolState.WaitForMyCards, seat);
                    await this.PublishHostEvent(HostEvents.ReadyForCards, seat);
                    break;

                case TableManagerProtocolState.WaitForMyCards:
                    lock (this.pause) this.pause[seat] = true;
                    if (this.CurrentResult.Auction.Ended)
                    {
                        if (seat == this.CurrentResult.Play.whoseTurn)
                        {
                            await ChangeState(message, $"{seat} ", TableManagerProtocolState.WaitForCardPlay, seat);
                        }
                        else
                        {
                            await ChangeState(message, $"{seat} ready for {Rotated(this.CurrentResult.Auction.WhoseTurn)}'s card to trick", TableManagerProtocolState.WaitForCardPlay, seat);
                        }
                    }
                    if (seat == Rotated(this.CurrentResult.Auction.WhoseTurn))
                    {
                        if (message.Contains(" ready for "))
                        {
                            Log.Trace(0, "{1} expected '... bids ..' from {0}", seat, this.Name);
                            //this.DumpQueue();
                            throw new InvalidOperationException();
                        }

                        this.lastRelevantMessage = message;
                        //Log.Trace($"{this.Name} lastRelevantMessage={0}", message);
                        await ChangeState(message, $"{seat} ", TableManagerProtocolState.WaitForOtherBid, seat);
                        await this.SendRelevantBridgeInfo(received, message);
                    }
                    else
                    {
                        await ChangeState(message, $"{seat} ready for {Rotated(this.CurrentResult.Auction.WhoseTurn)}'s bid", TableManagerProtocolState.WaitForOtherBid, seat);
                    }
                    break;

                case TableManagerProtocolState.WaitForCardPlay:
                    lock (this.pause) this.pause[seat] = true;
                    // ready for dummy's card mag ook ready for xx's card
                    if (this.CurrentResult.Play.whoseTurn == this.CurrentResult.Play.Dummy)
                    {
                        if (seat == Rotated(this.CurrentResult.Play.Dummy))
                        {
                            await ChangeState(message, $"{seat} ready for dummy's card to trick {this.CurrentResult.Play.currentTrick}", TableManagerProtocolState.WaitForOtherCardPlay, seat);
                        }
                        else
                        {
                            await ChangeState(message, $"{seat} ready for {this.Rotated(this.CurrentResult.Play.whoseTurn)}'s card to trick {this.CurrentResult.Play.currentTrick};{seat} ready for dummy's card to trick {this.CurrentResult.Play.currentTrick}", TableManagerProtocolState.WaitForOtherCardPlay, seat);
                        }
                    }
                    else
                    {
                        await ChangeState(message, $"{seat} ready for {this.Rotated(this.CurrentResult.Play.whoseTurn)}'s card to trick {this.CurrentResult.Play.currentTrick}", TableManagerProtocolState.WaitForOtherCardPlay, seat);
                    }
                    break;

                case TableManagerProtocolState.WaitForOwnCardPlay:
                    lock (this.pause) this.pause[seat] = true;
                    this.lastRelevantMessage = message;
                    //Log.Trace("{1} lastRelevantMessage={0}", message, this.hostName);
                    await ChangeState(message, string.Format("{0} plays ", this.Rotated(this.CurrentResult.Play.whoseTurn)), TableManagerProtocolState.WaitForOtherCardPlay, seat);
                    await this.SendRelevantBridgeInfo(received, message);
                    break;

                case TableManagerProtocolState.WaitForDummiesCardPlay:
                    await ChangeState(message, string.Format("{0} plays ", this.Rotated(this.CurrentResult.Play.whoseTurn)), TableManagerProtocolState.WaitForOtherCardPlay, seat);
                    await this.SendRelevantBridgeInfo(received, message);
                    break;

                case TableManagerProtocolState.WaitForDummiesCards:
                    await ChangeState(message, $"{seat} ready for dummy", TableManagerProtocolState.GiveDummiesCards, seat);
                    break;

                default:
                    await this.Send(seat, $"Unexpected '{message}' in state {state[seat]}");
                    throw new InvalidOperationException($"Unexpected '{message}' in state {state[seat]}");
            }
            //Log.Trace(9, $"ProcessMessage end");
        }

        private async ValueTask ChangeState(string message, string expected, TableManagerProtocolState newState, Seats seat)
        {
            var exp = expected.Split(';');
            if (message.ToLowerInvariant().Replace("  ", " ").StartsWith(exp[0].ToLowerInvariant()) || (exp.Length >= 2 && message.ToLowerInvariant().Replace("  ", " ").StartsWith(exp[1].ToLowerInvariant())))
            {
                var allReady = true;
                string answer;
                for (Seats s = Seats.North; s <= Seats.West; s++)
                    if (s != seat && this.state[s] != newState) { allReady = false; break; }
                this.state[seat] = newState;
                Log.Trace(2, $"{this.Name} sets state[{seat}]={newState}");
                if (allReady)
                {
                    Log.Trace(2, $"all seats have state {newState}; ready for next step");
                    switch (newState)
                    {
                        case TableManagerProtocolState.Initial:
                            break;
                        case TableManagerProtocolState.WaitForSeated:
                            break;
                        case TableManagerProtocolState.WaitForTeams:
                            answer = "Teams : N/S : \"" + this.teams[Seats.North] + "\" E/W : \"" + this.teams[Seats.East] + "\"";
                            await this.BroadCast(answer);
                            await this.SendRelevantBridgeInfo(DateTime.UtcNow, answer);
                            if (this.tournamentFileName.Length > 0)
                            {
                                await this.HostTournamentAsync(this.tournamentFileName, 1);
                            }
                            else
                            {
                                await this.PublishHostEvent(HostEvents.ReadyForTeams, (Seats)(-1));
                            }
                            break;
                        case TableManagerProtocolState.WaitForStartOfBoard:
                            await this.NextBoard();
                            break;
                        case TableManagerProtocolState.WaitForBoardInfo:
                            answer = $"Board number {this.currentBoard.BoardNumber}. Dealer {Rotated(this.currentBoard.Dealer).ToXMLFull()}. {ProtocolHelper.Translate(RotatedV(this.currentBoard.Vulnerable))} vulnerable.";
                            await this.BroadCast(answer);
                            await this.SendRelevantBridgeInfo(DateTime.UtcNow, answer);
                            break;
                        case TableManagerProtocolState.WaitForMyCards:
                            for (Seats s = Seats.North; s <= Seats.West; s++)
                            {
                                answer = Rotated(s).ToXMLFull() + ProtocolHelper.Translate(s, this.currentBoard.Distribution);
                                await this.Send(Rotated(s), answer);
                                await this.SendRelevantBridgeInfo(DateTime.UtcNow, answer);
                            }
                            break;
                        case TableManagerProtocolState.WaitForCardPlay:
                            break;
                        case TableManagerProtocolState.WaitForOtherBid:
                            for (Seats s = Seats.North; s <= Seats.West; s++) this.state[s] = TableManagerProtocolState.WaitForCardPlay;
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
                            var cards = "Dummy" + ProtocolHelper.Translate(this.CurrentResult.Play.Dummy, this.currentBoard.Distribution);
                            for (Seats s = Seats.North; s <= Seats.West; s++)
                            {
                                if (s != this.Rotated(this.CurrentResult.Play.Dummy))
                                {
                                    await this.Send(s, cards);
                                }
                            }
                            for (Seats s = Seats.North; s <= Seats.West; s++)
                            {
                                this.state[s] = (s == this.Rotated(this.CurrentResult.Auction.Declarer) ? TableManagerProtocolState.WaitForOwnCardPlay : TableManagerProtocolState.WaitForCardPlay);
                                lock (this.pause) this.pause[s] = false;
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
                Log.Trace(1, $"{this.Name} expected '{expected}' from {seat} in state {this.state[seat]}");
                await this.Send(seat, $"Expected '{expected}'");
                throw new InvalidOperationException($"Expected '{expected}'");
            }
            //Log.Trace(9, $"ChangeState end");

            Vulnerable RotatedV(Vulnerable v)
            {
                if (this.rotateHands)
                {
                    return v switch
                    {
                        Vulnerable.Neither => Vulnerable.Neither,
                        Vulnerable.NS => Vulnerable.EW,
                        Vulnerable.EW => Vulnerable.NS,
                        _ => Vulnerable.Both,
                    };
                }
                else
                {
                    return v;
                }
            }
        }

        private async ValueTask SendRelevantBridgeInfo(DateTime when, string message)
        {
            if (this.OnRelevantBridgeInfo != null) await this.OnRelevantBridgeInfo(this, when, message);
        }

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

        private async ValueTask Send(Seats seat, string message)
        {
            Log.Trace(2, $"{this.Name} to {seat}: {message}");
            await this.communicationDetails.Send(seat, message);
        }

        private async ValueTask<string> SendAndWait(Seats seat, string message)
        {
            Log.Trace(2, $"{this.Name} wants answer from {seat}: {message}");
            var answer = await this.communicationDetails.SendAndWait(seat, message);
            Log.Trace(2, $"{this.Name} received answer from {seat}: {answer}");
            return answer;
        }

        public async ValueTask BroadCast(string message)
        {
            //for (Seats s = Seats.North; s <= Seats.West; s++)
            //{
            //    if (this.teams[s]?.Length > 0)
            //        await this.Send(s, message);
            //}
            await this.communicationDetails.Broadcast(message);
            this.lagTimer.Restart();
        }

        private void UpdateCommunicationLag(Seats source, long lag)
        {
            //Log.Trace($"{this.Name} UpdateCommunicationLag for {0} old lag={1} lag={2}", source, this.clients[source].communicationLag, lag);
            //this.seatedClients[source].communicationLag += lag;
            //this.seatedClients[source].communicationLag /= 2;
            //Log.Trace($"{this.Name} UpdateCommunicationLag for {0} new lag={1}", source, this.clients[source].communicationLag);
        }

        protected virtual void ExplainBid(Seats source, Bid bid)
        {
            // opportunity to implement manual alerting
        }

        public DirectionDictionary<System.Diagnostics.Stopwatch> ThinkTime { get; private set; }

        private async ValueTask HostTournamentAsync(string pbnTournament, int firstBoard)
        {
            this.currentTournament = await TournamentLoader.LoadAsync(File.OpenRead(pbnTournament));
            this.participant = new ParticipantInfo() { ConventionCardNS = this.teams[Seats.North], ConventionCardWE = this.teams[Seats.East], MaxThinkTime = 120, UserId = Guid.NewGuid(), PlayerNames = new Participant(this.teams[Seats.North], this.teams[Seats.East], this.teams[Seats.North], this.teams[Seats.East]) };
            this.ThinkTime[Directions.NorthSouth].Reset();
            this.ThinkTime[Directions.EastWest].Reset();
            this.StartTournamentAsync(firstBoard);
        }

        private Board2 currentBoard;
        private int boardNumber;
        private Tournament currentTournament;
        private ParticipantInfo participant;

        public void StartTournamentAsync(int firstBoard)
        {
            Log.Trace(4, "TournamentController.StartTournamentAsync begin");
            this.boardNumber = firstBoard - 1;
            this.EventBus.HandleTournamentStarted(this.currentTournament.ScoringMethod, 120, this.participant.MaxThinkTime, this.currentTournament.EventName);
            this.EventBus.HandleRoundStarted(this.participant.PlayerNames.Names, new DirectionDictionary<string>(this.participant.ConventionCardNS, this.participant.ConventionCardWE));
            Log.Trace(4, "TournamentController.StartTournamentAsync end");
        }

        private async ValueTask NextBoard()
        {
            Log.Trace(4, "TournamentController.NextBoard start");
            await this.GetNextBoard();
            if (this.currentBoard == null)
            {
                Log.Trace(4, "TournamentController.NextBoard no next board");
                this.EventBus.HandleTournamentStopped();
            }
            else
            {
                Log.Trace(4, $"TournamentController.NextBoard board={this.currentBoard.BoardNumber.ToString()}");
                this.EventBus.HandleBoardStarted(this.currentBoard.BoardNumber, this.currentBoard.Dealer, this.currentBoard.Vulnerable);
                for (int card = 0; card < currentBoard.Distribution.Deal.Count; card++)
                {
                    DistributionCard item = currentBoard.Distribution.Deal[card];
                    this.EventBus.HandleCardPosition(item.Seat, item.Suit, item.Rank);
                }

                this.EventBus.HandleCardDealingEnded();
            }
        }

        private async ValueTask GetNextBoard()
        {
            if (this.mode == HostMode.SingleTableInstantReplay && HasBeenPlayedOnce(this.currentBoard))
            {
                Log.Trace(4, "TMController.GetNextBoard instant replay this board");
                this.rotateHands = true;
            }
            else
            {
                this.rotateHands = false;
                this.boardNumber++;
                this.currentBoard = await this.currentTournament.GetNextBoardAsync(this.boardNumber, this.participant.UserId);
            }

            bool HasBeenPlayedOnce(Board2 board)
            {
                if (board == null) return false;
                var played = 0;
                foreach (var result in board.Results)
                {
                    if (result.Play.PlayEnded)
                    {
                        if (HasBeenPlayedBy(result, this.teams[Seats.North], this.teams[Seats.East])) played++;
                        else if (HasBeenPlayedBy(result, this.teams[Seats.East], this.teams[Seats.North])) played++;
                    }
                }

                return played == 1;

                bool HasBeenPlayedBy(BoardResult result, string team1, string team2)
                {
                    return result.Participants.Names[Seats.North] == team1 && result.Participants.Names[Seats.East] == team2;
                }
            }
        }

#region Bridge Events

        public override async void HandleBoardStarted(int boardNumber, Seats dealer, Vulnerable vulnerabilty)
        {
            Log.Trace(4, "{this.Name}.HandleBoardStarted");
            base.HandleBoardStarted(boardNumber, dealer, vulnerabilty);
            this.CurrentResult = new BoardResultEventPublisher($"{this.Name}.BoardResult", this.currentBoard, this.participant.PlayerNames.Names, this.EventBus, this.currentTournament);
            this.CurrentResult.HandleBoardStarted(boardNumber, dealer, vulnerabilty);
            for (Seats s = Seats.North; s <= Seats.West; s++)
            {
                this.pause[s] = true;
                this.state[s] = TableManagerProtocolState.WaitForStartOfBoard;
            }

            //Threading.Sleep(20);
            await this.BroadCast("Start of board");
            for (Seats s = Seats.North; s <= Seats.West; s++)
            {
                this.pause[s] = false;
            }
        }

        public override void HandleCardPosition(Seats seat, Suits suit, Ranks rank)
        {
            base.HandleCardPosition(seat, suit, rank);
            this.CurrentResult.HandleCardPosition(seat, suit, rank);
        }

        public override void HandleCardDealingEnded()
        {
            base.HandleCardDealingEnded();
            this.CurrentResult.HandleCardDealingEnded();
        }

        public override void HandleBidNeeded(Seats whoseTurn, Bid lastRegularBid, bool allowDouble, bool allowRedouble)
        {
            base.HandleBidNeeded(whoseTurn, lastRegularBid, allowDouble, allowRedouble);
            Log.Trace(4, $"start think time for {this.Rotated(whoseTurn).Direction()} at {this.ThinkTime[this.Rotated(whoseTurn).Direction()].ElapsedMilliseconds}");
            this.ThinkTime[this.Rotated(whoseTurn).Direction()].Start();
        }

        public override async void HandleBidDone(Seats source, Bid bid)
        {
            this.ThinkTime[this.Rotated(source).Direction()].Stop();
            //Log.Trace(2, $"stop  think time for {this.host.Rotated(source).Direction()} at {this.host.ThinkTime[this.host.Rotated(source).Direction()].ElapsedMilliseconds}");
            Log.Trace(4, $"{this.Name}HandleBidDone");
            if (BidMayBeAlerted(bid) || this.CanAskForExplanation[source.Next()])
            {
                Log.Trace(5, "HostBoardResult.HandleBidDone explain opponents bid");
                if (!this.CanAskForExplanation[source.Next()]) this.ExplainBid(source, bid);
                if (bid.Alert || this.CanAskForExplanation[source.Next()])
                {   // the operator has indicated this bid needs an explanation
                    Log.Trace(5, $"{this.Name}.HandleBidDone host operator wants an alert");
                    if (this.CanAskForExplanation[source.Next()])
                    {   // client implements this new part of the protocol
                        var answer = await this.SendAndWait(source.Next(), $"Explain {source}'s {ProtocolHelper.Translate(bid)}");
                        bid.Explanation = answer;
                    }
                }
                else
                {
                    Log.Trace(5, $"{this.Name}.HandleBidDone host operator does not want an alert");
                }
            }
            for (Seats s = Seats.North; s <= Seats.West; s++)
            {
                this.state[s] = TableManagerProtocolState.WaitForMyCards;
                if (s != this.Rotated(source))
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

                    await this.Send(s, ProtocolHelper.Translate(bid.Alert && s.IsSameDirection(this.Rotated(source)) ? new Bid(bid.Index, "", false, "") : bid, this.Rotated(source)));
                }
            }

            base.HandleBidDone(source, bid);
            this.CurrentResult.HandleBidDone(source, bid);

            lock (this.pause)
            {
                for (Seats s = Seats.North; s <= Seats.West; s++)
                {
                    this.pause[s] = this.CurrentResult.Auction.Ended;// && s == this.CurrentResult.Play.whoseTurn;
                }
            }

            return;

            bool BidMayBeAlerted(Bid bid)
            {
                if (bid.IsPass) return false;
                if (this.CurrentResult.Auction.LastRegularBid.IsPass) return false;
                return true;
            }
        }

        public override async void HandleCardNeeded(Seats controller, Seats whoseTurn, Suits leadSuit, Suits trump, bool trumpAllowed, int leadSuitLength, int trick)
        {
            Log.Trace(4, $"{this.Name}.HandleCardNeeded");
            if (leadSuit == Suits.NoTrump)
            {
                //if (this.host.seatedClients[this.host.Rotated(controller)].PauseBeforeSending) Threading.Sleep(200);
                await this.Send(this.Rotated(controller), $"{(whoseTurn == this.CurrentResult.Play.Dummy ? "Dummy" : this.Rotated(whoseTurn).ToXMLFull())} to lead");
            }

            for (Seats s = Seats.North; s <= Seats.West; s++)
            {
                this.state[s] = (s == this.Rotated(controller) ? TableManagerProtocolState.WaitForOwnCardPlay : TableManagerProtocolState.WaitForCardPlay);
                lock (this.pause) this.pause[s] = false;
            }

            this.ThinkTime[this.Rotated(whoseTurn).Direction()].Start();
        }

        public override async void HandleCardPlayed(Seats source, Suits suit, Ranks rank)
        {
            Log.Trace(4, $"{this.Name}.HandleCardPlayed");
            this.ThinkTime[this.Rotated(source).Direction()].Stop();
            base.HandleCardPlayed(source, suit, rank);
            this.CurrentResult.HandleCardPlayed(source, suit, rank);
            for (Seats s = Seats.North; s <= Seats.West; s++)
            {
                if ((s != this.Rotated(source) && !(s == this.Rotated(this.CurrentResult.Auction.Declarer) && source == this.CurrentResult.Play.Dummy))
                    || (s == this.Rotated(source) && source == this.CurrentResult.Play.Dummy)
                    )
                {
                    await this.Send(s, $"{this.Rotated(source)} plays {rank.ToXML()}{suit.ToXML()}");
                }

                if (this.CurrentResult.Play.currentTrick == 1 && this.CurrentResult.Play.man == 2)
                {   // 1st card: need to send dummies cards
                    Log.Trace(5, $"{this.Name}.HandleCardPlayed 1st card to {0}", s);
                    var mustPause = s == this.Rotated(this.CurrentResult.Play.Dummy);
                    lock (this.pause) this.pause[s] = mustPause;
                    this.state[s] = s == this.Rotated(this.CurrentResult.Play.Dummy) ? TableManagerProtocolState.GiveDummiesCards : TableManagerProtocolState.WaitForDummiesCards;
                }
            }
        }

        public override async void HandlePlayFinished(BoardResultRecorder currentResult)
        {
            Log.Trace(4, $"{this.Name}.HandlePlayFinished");
            base.HandlePlayFinished(currentResult);
            this.boardTime[Directions.NorthSouth] = this.ThinkTime[Directions.NorthSouth].Elapsed.Subtract(this.boardTime[Directions.NorthSouth]);
            this.boardTime[Directions.EastWest] = this.ThinkTime[Directions.EastWest].Elapsed.Subtract(this.boardTime[Directions.EastWest]);
            //Threading.Sleep(20);
            var timingInfo = string.Format("Timing - N/S : this board  {0:mm\\:ss},  total  {1:h\\:mm\\:ss}.  E/W : this board  {2:mm\\:ss},  total  {3:h\\:mm\\:ss}."
                , this.boardTime[Directions.NorthSouth].RoundToSeconds()
                , this.ThinkTime[Directions.NorthSouth].Elapsed.RoundToSeconds()
                , this.boardTime[Directions.EastWest].RoundToSeconds()
                , this.ThinkTime[Directions.EastWest].Elapsed.RoundToSeconds()
                );
            await this.BroadCast(timingInfo);
            await this.SendRelevantBridgeInfo(DateTime.UtcNow, timingInfo);
            this.boardTime[Directions.NorthSouth] = this.ThinkTime[Directions.NorthSouth].Elapsed;
            this.boardTime[Directions.EastWest] = this.ThinkTime[Directions.EastWest].Elapsed;
            for (Seats s = Seats.North; s <= Seats.West; s++)
            {
                this.state[s] = TableManagerProtocolState.WaitForStartOfBoard;
                this.pause[s] = false;
            }

            await this.PublishHostEvent(HostEvents.BoardFinished, currentResult);

            await this.NextBoard();
        }

        public override async void HandleTournamentStopped()
        {
            Log.Trace(4, $"{this.Name}.HandleTournamentStopped");
            await this.BroadCast("End of session");
            await this.SendRelevantBridgeInfo(DateTime.UtcNow, "End of session");
            await this.PublishHostEvent(HostEvents.Finished, null);
            this.communicationDetails.Stop();
        }

#endregion

        private async ValueTask PublishHostEvent(HostEvents e, object p)
        {
            if (this.OnHostEvent != null) await this.OnHostEvent(this, e, p);
        }

        public async ValueTask DisposeAsync()
        {
            await this.communicationDetails.DisposeAsync();
        }
    }

    public class HostTcpCommunication : HostCommunication
    {
        private readonly BaseAsyncTcpHost tcpHost;
        private readonly string name;
        private bool isReconnecting = false;

        public HostTcpCommunication(int port, string hostName) : base()
        {
            this.name = hostName + ".HostTcpCommunication";
            this.tcpHost = new BaseAsyncTcpHost(new IPEndPoint(IPAddress.Any, port), this.ProcessClientMessage, this.HandleNewClient, hostName);
            tcpHost.OnClientConnectionLost = this.HandleConnectionLost;
        }

        public override async ValueTask Run()
        {
            await this.tcpHost.Run();
        }

        private async ValueTask HandleNewClient(int clientId)
        {   // tcp host has accepted a new listener
            Log.Trace(4, $"{this.name}.HandleNewClient: new client {clientId}; no seat yet");
            if (this.DisconnectedSeats == 0)
            {
                Log.Trace(4, $"{this.name}.HandleNewClient: no new client expected. isReconnecting={this.isReconnecting}. What is the status of all TcpClient's?");
            }
            for (Seats s = Seats.North; s <= Seats.West; s++)
            {
                Log.Trace(4, $"{this.name}.HandleNewClient: seat {s} has connection {this.clients[s]}");
            }
            lock (this.seats)
            {
                this.seats.Add(clientId, (Seats)(-1));
                if (this.isReconnecting)
                {
                    if (this.DisconnectedSeats == 1)
                    {
                        for (Seats s = Seats.North; s <= Seats.West; s++)
                        {
                            if (this.clients[s] < 0)
                            {
                                Log.Trace(4, $"{this.name}.HandleNewClient: new client seated in {s}");
                                this.clients[s] = clientId;
                                this.seats[clientId] = s;
                                this.isReconnecting = false;
                                break;
                            }
                        }
                    }
                }
            }
            await Task.CompletedTask;
        }

        private async ValueTask ProcessClientMessage(int clientId, string message)
        {
            lock (this.seats)
            {
                if (!seats.ContainsKey(clientId))
                {
                    Log.Trace(4, $"{this.name}.ProcessClientMessage: new client {clientId}; no seat yet");
                    this.seats.Add(clientId, (Seats)(-1));
                }

                if (this.seats[clientId] < Seats.North && this.isReconnecting && this.DisconnectedSeats == 1)
                {
                    for (Seats s = Seats.North; s <= Seats.West; s++)
                    {
                        if (this.clients[s] < 0)
                        {
                            Log.Trace(4, $"{this.name}.ProcessClientMessage: new client seated in {s}");
                            this.clients[s] = clientId;
                            this.seats[clientId] = s;
                            this.isReconnecting = false;
                            break;
                        }
                    }
                }
            }

            if (this.seats[clientId] < Seats.North)
            {
                var result = await this.ProcessConnect(clientId, message);
                if (result.Seat >= Seats.North)
                {
                    Log.Trace(4, $"{this.name}.ProcessClientMessage: new client seated in {result.Seat}");
                    lock (this.seats)
                    {
                        this.seats[clientId] = result.Seat;
                        this.clients[result.Seat] = clientId;
                    }
                }

                if (result.Response.Length > 0) await this.tcpHost.Send(clientId, result.Response);
            }
            else
            {
                await this.ProcessMessage(this.seats[clientId], message);
            }
        }

        public override async ValueTask Send(Seats seat, string message)
        {
            do
            {
                try
                {
                    var clientId = this.clients[seat];
                    if (clientId < 0) throw new Exception("clientId < 0");
                    await this.tcpHost.Send(clientId, message);
                    break;
                }
                catch (Exception)
                {
                    Log.Trace(4, $"{this.name}.Send: Waits for seat {seat} to reconnect");
                    do
                    {
                        await this.HandleConnectionLost(seat);
                        await Task.Delay(1000);     // wait for reconnect
                    } while (this.clients[seat] < 0);
                }
            } while (true);
        }

        public override async ValueTask Broadcast(string message)
        {
            for (Seats s = Seats.North; s <= Seats.West; s++)
            {
                if (this.clients[s] >= 0)
                    await this.Send(s, message);
            }
        }

        public override async ValueTask<string> SendAndWait(Seats seat, string message)
        {
            return await this.tcpHost.SendAndWait(this.clients[seat], message);
        }

//#if NET6_0_OR_GREATER
        protected override async ValueTask DisposeManagedObjects()
        {
            await this.tcpHost.DisposeAsync();
        }
//#endif

        public override void Stop()
        {
            this.tcpHost.Stop();
        }

        private async ValueTask HandleConnectionLost(int clientId)
        {
            this.isReconnecting = true;
            if (this.seats[clientId] >= Seats.North) this.clients[this.seats[clientId]] = -1;
            this.seats.Remove(clientId);
            Log.Trace(1, $"{this.name}: {clientId} lost connection. Wait for client to reconnect....");
            await Task.CompletedTask;
        }

        private async ValueTask HandleConnectionLost(Seats seat)
        {
            Log.Trace(1, $"{this.name}: Seat {seat} lost connection. Wait for seat to reconnect....");
            var badClient = this.clients[seat];
            this.seats.Remove(badClient);
            this.clients[seat] = -1;
            this.isReconnecting = true;
            if (this.DisconnectedSeats == 1)
            {
                lock (this.seats)
                {
                    foreach (var connection in seats)
                    {
                        Log.Trace(4, $"Connection {connection.Key} has seat {connection.Value}");
                        if (connection.Key != badClient && connection.Value < Seats.North)
                        {
                            if (this.DisconnectedSeats > 1) break;
                            this.clients[seat] = connection.Key;
                            this.seats[connection.Key] = seat;
                            this.isReconnecting = false;
                            Log.Trace(1, $"Found a new connection {connection.Key} for seat {seat}");
                        }
                    }
                }
            }
            await Task.CompletedTask;
        }

        private int DisconnectedSeats
        {
            get
            {
                int count = 0;
                SeatsExtensions.ForEachSeat(s =>
                {
                    if (this.clients[s] < 0) count++;
                });
                return count;
            }
        }
    }

    public abstract class HostCommunication : BaseAsyncDisposable
    {
        protected readonly SeatCollection<int> clients;
        protected readonly Dictionary<int, Seats> seats;

        public Func<int, string, ValueTask<ConnectResponse>> ProcessConnect;
        public Func<Seats, string, ValueTask> ProcessMessage;
        public Func<Seats, ValueTask> OnClientConnectionLost;

        public HostCommunication()
        {
            this.clients = new SeatCollection<int>(new int[] { -1, -1, -1, -1 });
            this.seats = new Dictionary<int, Seats>();
        }

        public abstract ValueTask Run();

        public abstract void Stop();

        public abstract ValueTask Send(Seats seat, string message);

        public abstract ValueTask Broadcast(string message);

        public abstract ValueTask<string> SendAndWait(Seats seat, string message);
    }

    public struct ConnectResponse
    {
        public Seats Seat;
        public string Response;
        public ConnectResponse(Seats _seat, string _response)
        {
            this.Seat = _seat;
            this.Response = _response;
        }
    }
}
