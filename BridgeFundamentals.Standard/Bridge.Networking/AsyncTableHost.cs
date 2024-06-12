﻿using Bridge.NonBridgeHelpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Bridge.Networking
{
    public class AsyncTableHost<T> : BridgeEventBusClient, IAsyncDisposable where T : HostCommunication
    {
        private readonly T communicationDetails;
        private ValueTask hostRunTask;
        private readonly SeatCollection<string> teams;
        private readonly SeatCollection<bool> CanAskForExplanation;
        private readonly SeatCollection<Queue<string>> messages;
        private BoardResultRecorder CurrentResult;
        private DirectionDictionary<TimeSpan> boardTime;
        private readonly System.Diagnostics.Stopwatch lagTimer;
        private readonly HostMode mode;
        private bool rotateHands;
        private readonly string tournamentFileName;
        private readonly SeatCollection<int> clients;
        private readonly Dictionary<int, Seats> seats;
        private readonly SemaphoreSlim allSeatsFilled;
        private bool moreBoards;
        protected readonly CancellationTokenSource cts;

        public Func<object, HostEvents, object, ValueTask> OnHostEvent;
        public Func<object, DateTime, string, ValueTask> OnRelevantBridgeInfo;

        public AsyncTableHost(HostMode _mode, T _communicationDetails, BridgeEventBus bus, string hostName, string _tournamentFileName = "") : base(bus, hostName)
        {
            this.communicationDetails = _communicationDetails;
            this.communicationDetails.ProcessMessage = this.ProcessMessage;
            //this.communicationDetails.OnClientConnectionLost = this.HandleConnectionLost;
            this.teams = new SeatCollection<string>();
            this.CanAskForExplanation = new SeatCollection<bool>();
            this.ThinkTime = new DirectionDictionary<System.Diagnostics.Stopwatch>(new System.Diagnostics.Stopwatch(), new System.Diagnostics.Stopwatch());
            this.lagTimer = new System.Diagnostics.Stopwatch();
            this.tournamentFileName = _tournamentFileName;
            this.boardTime = new DirectionDictionary<TimeSpan>(TimeSpan.Zero, TimeSpan.Zero);
            this.mode = _mode;
            this.clients = new SeatCollection<int>(new int[] { -1, -1, -1, -1 });
            this.seats = new Dictionary<int, Seats>();
            this.allSeatsFilled = new SemaphoreSlim(0);
            this.messages = new SeatCollection<Queue<string>>();
            this.moreBoards = true;
            this.cts = new CancellationTokenSource();
        }

        public void Run()
        {
            this.hostRunTask = this.Run2();
        }

        private async ValueTask Run2()
        {
            var communicationRunTask = this.communicationDetails.Run();
            try
            {
                await this.allSeatsFilled.WaitAsync(cts.Token);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            Log.Trace(1, $"{this.Name}.Run: all seats taken");

            await AllAnswered("ready for teams");

            var answer = "Teams : N/S : \"" + this.teams[Seats.North] + "\" E/W : \"" + this.teams[Seats.East] + "\"";
            await this.SendRelevantBridgeInfo(DateTime.UtcNow, answer);
            if (this.tournamentFileName.Length > 0)
            {
                await this.HostTournamentAsync(this.tournamentFileName, 1);
            }
            else
            {
                await this.PublishHostEvent(HostEvents.ReadyForTeams, (Seats)(-1));
            }
            await this.BroadCast(answer);

            await AllAnswered("ready to start");

            await this.NextBoard();

            while (this.moreBoards)
            {
                await AllAnswered("ready for deal");

                answer = $"Board number {this.currentBoard.BoardNumber}. Dealer {Rotated(this.currentBoard.Dealer).ToXMLFull()}. {ProtocolHelper.Translate(RotatedV(this.currentBoard.Vulnerable))} vulnerable.";
                await this.BroadCast(answer);
                await this.SendRelevantBridgeInfo(DateTime.UtcNow, answer);

                await AllAnswered("ready for cards");

                for (Seats s = Seats.North; s <= Seats.West; s++)
                {
                    answer = Rotated(s).ToXMLFull() + ProtocolHelper.Translate(s, this.currentBoard.Distribution);
                    await this.Send(Rotated(s), answer);
                    await this.SendRelevantBridgeInfo(DateTime.UtcNow, answer);
                }

                var who = Rotated(CurrentResult.Auction.WhoseTurn);
                int passes = 0;
                while (passes < 4)
                {
                    await AllAnswered($"ready for {who}'s bid", who);
                    var bid = await GetMessage(who);
                    ProtocolHelper.HandleProtocolBid(UnRotated(bid), this.EventBus);
                    if (bid.ToLower().Contains("passes")) { passes++; } else { passes = 1; }
                    who = who.Next();
                }

                await Task.Delay(60);       // need some time to process the bid and note that the auction has ended
                var dummy = this.Rotated(this.CurrentResult.Play.Dummy);
                for (int trick = 1; trick <= 13; trick++)
                {
                    who = Rotated(CurrentResult.Play.whoseTurn);
                    for (int man = 1; man <= 4; man++)
                    {
                        string card;
                        if (who == dummy)
                        {
                            await AllAnswered($"ready for {who}'s card to trick {trick}", who.Partner(), dummy);
                            var dummiesAnswer = await GetMessage(dummy);
                            if (dummiesAnswer.ToLower() != $"{dummy.ToString().ToLower()} ready for dummy's card to trick {trick}") throw new Exception();
                            card = await GetMessage(who.Partner());
                        }
                        else
                        {
                            await AllAnswered($"ready for {who}'s card to trick {trick}", who);
                            card = await GetMessage(who);
                        }
                        ProtocolHelper.HandleProtocolPlay(UnRotated(card), this.EventBus);

                        if (trick == 1 && man == 1)
                        {
                            await AllAnswered($"ready for dummy", dummy);

                            var cards = "Dummy" + ProtocolHelper.Translate(dummy, this.currentBoard.Distribution);
                            for (Seats s = Seats.North; s <= Seats.West; s++)
                            {
                                if (s != dummy)
                                {
                                    await this.Send(s, cards);
                                }
                            }
                        }

                        who = who.Next();
                    }

                    await Task.Delay(50);       // need some time to process the trick
                }

                await Task.Delay(100);       // need some time to process the end of board
            }

            await communicationRunTask;
        }

        private async ValueTask AllAnswered(string expectedAnswer, Seats except = (Seats)(-1), Seats dummy = (Seats)(-1))
        {
            await SeatsExtensions.ForEachSeatAsync(async seat =>
            {
                if (seat != except && seat != dummy)
                {
                    var message = await GetMessage(seat);
                    if (message.ToLower() != $"{seat.ToString().ToLower()} {expectedAnswer.ToLower()}") throw new Exception();
                }
            });
            Log.Trace(3, $"{this.Name}: all seats {expectedAnswer}");
        }

        private async ValueTask<string> GetMessage(Seats seat)
        {

            while (this.messages[seat].Count == 0) await Task.Delay(100);
            return this.messages[seat].Dequeue();
        }

        public async ValueTask WaitForCompletionAsync()
        {
            await this.hostRunTask;
        }

        protected async ValueTask<ConnectResponse> ProcessConnect(int clientId, string message)
        {
            await Task.CompletedTask;
            Log.Trace(3, $"{this.Name}.ProcessConnect: client {clientId} sent '{message}'");

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
                    messages[seat2].Enqueue(message);
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
                        this.CanAskForExplanation[seat] = false;
                        break;
                    case 19:
                        this.CanAskForExplanation[seat] = false;
                        break;
                    default:
                        throw new ArgumentException($"protocol version {protocolVersion} not supported");
                }

                var partner = seat.Partner();
                var partnerTeamName = teamName;
                if (this.clients[partner] >= 0)
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
                    this.clients[seat] = clientId;
                    this.seats[clientId] = seat;
                    this.messages[seat] = new Queue<string>();
                    await this.PublishHostEvent(HostEvents.Seated, seat + "|" + teamName);
                    await this.PublishHostEvent(HostEvents.ReadyForTeams, null);
                    //if (this.OnHostEvent != null) await this.OnHostEvent(this, HostEvents.Seated, seat + "|" + teamName);
                    return new ConnectResponse(seat, $"{seat} (\"{teamName}\") seated");
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

        protected async ValueTask ProcessMessage(int clientId, string message)
        {
            using (await AsyncLock.WaitForLockAsync("seats"))
            {
                if (!this.seats.TryGetValue(clientId, out var seat))
                {
                    var response = await this.ProcessConnect(clientId, message);
                    if (response.Response.Length > 0) await this.communicationDetails.Send(clientId, response.Response);
                    if (SeatsExtensions.AllSeats(seat => this.clients[seat] >= 0)) allSeatsFilled.Release();
                    return;
                }

                messages[seat].Enqueue(message);
            }
        }

        private Vulnerable RotatedV(Vulnerable v)
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
            Log.Trace(1, $"{this.Name} to {seat}: {message}");
            await this.communicationDetails.Send(this.clients[seat], message);
        }

        private async ValueTask<string> SendAndWait(Seats seat, string message)
        {
            Log.Trace(2, $"{this.Name} wants answer from {seat}: {message}");
            var answer = await this.communicationDetails.SendAndWait(this.clients[seat], message);
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
            this.currentTournament.Participants.Add(new Team { LastBoard = 0, LastPlay = DateTime.MinValue, Member1 = this.teams[Seats.North], Member2 = this.teams[Seats.South], TournamentScore = double.MinValue });
            this.currentTournament.Participants.Add(new Team { LastBoard = 0, LastPlay = DateTime.MinValue, Member1 = this.teams[Seats.West], Member2 = this.teams[Seats.East], TournamentScore = double.MinValue });
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
            Log.Trace(4, $"{this.Name}.HandleBoardStarted");
            base.HandleBoardStarted(boardNumber, dealer, vulnerabilty);
            this.CurrentResult = new BoardResultEventPublisher($"{this.Name}.BoardResult", this.currentBoard, this.participant.PlayerNames.Names, this.EventBus, this.currentTournament);
            this.CurrentResult.HandleBoardStarted(boardNumber, dealer, vulnerabilty);
            await this.BroadCast("Start of board");
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
                await this.Send(this.Rotated(controller), $"{(whoseTurn == this.CurrentResult.Play.Dummy ? "Dummy" : this.Rotated(whoseTurn).ToXMLFull())} to lead");
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
            }
        }

        public override async void HandlePlayFinished(BoardResultRecorder currentResult)
        {
            Log.Trace(4, $"{this.Name}.HandlePlayFinished");
            base.HandlePlayFinished(currentResult);
            this.boardTime[Directions.NorthSouth] = this.ThinkTime[Directions.NorthSouth].Elapsed.Subtract(this.boardTime[Directions.NorthSouth]);
            this.boardTime[Directions.EastWest] = this.ThinkTime[Directions.EastWest].Elapsed.Subtract(this.boardTime[Directions.EastWest]);
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

            await this.currentTournament.SaveAsync(currentResult as BoardResult);
            //this.currentBoard.Results.Add(new BoardResult("", this.currentBoard, );
            await this.PublishHostEvent(HostEvents.BoardFinished, currentResult);

            await this.NextBoard();
        }

        public override async void HandleTournamentStopped()
        {
            this.moreBoards = false;
            Log.Trace(4, $"{this.Name}.HandleTournamentStopped");
            await this.BroadCast("End of session");
            await this.SendRelevantBridgeInfo(DateTime.UtcNow, "End of session");
            await this.PublishHostEvent(HostEvents.Finished, currentTournament);
            this.communicationDetails.Stop();
            this.cts.Cancel();
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

    public abstract class HostCommunication : BaseAsyncDisposable
    {
        private readonly List<int> clients;
        protected bool isReconnecting = false;
        protected readonly string name;

        public Func<int, string, ValueTask> ProcessMessage;
        public Func<int, ValueTask> OnClientConnectionLost;

        public HostCommunication(string _name)
        {
            this.name = _name;
            this.clients = new List<int>();
        }

        public async ValueTask Broadcast(string message)
        {
            foreach(var clientId in this.clients)
            {
                await this.Send(clientId, message);
            }
        }

        protected async ValueTask ProcessClientMessage(int clientId, string message)
        {
            lock (this.clients)
            {
                if (!clients.Contains(clientId))
                {
                    Log.Trace(4, $"{this.name}.ProcessClientMessage: new client {clientId}");
                    this.clients.Add(clientId);
                }
            }

            await this.ProcessMessage(clientId, message);
        }

        public abstract ValueTask Run();

        public abstract void Stop();

        public abstract ValueTask Send(int clientId, string message);

        public abstract ValueTask<string> SendAndWait(int clientId, string message);

        public abstract ValueTask<string> GetMessage(int clientId);
    }

    public abstract class BaseAsyncHost : BaseAsyncDisposable
    {
        protected bool isRunning = false;
        protected readonly CancellationTokenSource cts;
        protected readonly List<BaseAsyncHostClient> clients;
        protected readonly IPEndPoint endPoint;
        protected readonly Func<int, string, ValueTask> processMessage;
        protected readonly Func<int, ValueTask> onNewClient;
        protected readonly string name;
        public Func<int, ValueTask> OnClientConnectionLost;

        public BaseAsyncHost(IPEndPoint tcpPort, Func<int, string, ValueTask> _processMessage, Func<int, ValueTask> _onNewClient, string _name)
        {
            this.endPoint = tcpPort;
            this.cts = new CancellationTokenSource();
            this.processMessage = _processMessage;
            this.onNewClient = _onNewClient;
            this.name = _name;
            this.clients = new List<BaseAsyncHostClient>();
        }

        public async ValueTask Send(int clientId, string message)
        {
            await this.clients[clientId].Send(message);
        }

        public async ValueTask<string> SendAndWait(int clientId, string message)
        {
            return await this.clients[clientId].SendAndWait(message);
        }

        public async ValueTask<string> GetMessage(int clientId)
        {
            return await this.clients[clientId].GetMessage();
        }

        public void Stop()
        {
            Log.Trace(2, $"{this.name}.Stop");
            isRunning = false;
            cts.Cancel();
        }

        protected async ValueTask ProcessClientMessage(int clientId, string message)
        {
            if (this.processMessage != null) await this.processMessage(clientId, message);
        }

        protected async ValueTask HandleConnectionLost(int clientId)
        {
            Log.Trace(1, $"{this.name}: {clientId} lost connection. Wait for client to reconnect....");
            await OnClientConnectionLost(clientId);
        }

        protected override async ValueTask DisposeManagedObjects()
        {
            Log.Trace(4, $"{this.name}.DisposeManagedObjects");
            cts.Dispose();
            foreach (var client in this.clients)
            {
                await client.DisposeAsync();
            }
        }
    }

    public abstract class BaseAsyncHostClient : BaseAsyncDisposable
    {
        protected readonly string name;
        protected readonly int id;
        protected CancellationTokenSource cts;
        protected readonly bool _canReconnect;      // is the client server-side or client-side?
        protected Func<int, string, ValueTask> processMessage;
        public Func<int, ValueTask> OnClientConnectionLost;

        public Func<ValueTask> OnConnectionLost;

        public BaseAsyncHostClient(string _name, int _id, Func<int, string, ValueTask> _processMessage)
        {
            this.name = _name + _id.ToString();
            this.id = _id;
            this.cts = new CancellationTokenSource();
            this._canReconnect = false;      // server-side
            this.processMessage = _processMessage;
            this.OnConnectionLost = this.HandleConnectionLost;
        }

        protected async ValueTask ProcessMessage(string message)
        {
            await this.processMessage(this.id, message);
        }

        public abstract ValueTask Send(string message);
        public abstract ValueTask<string> SendAndWait(string message);
        public abstract ValueTask<string> GetMessage();

        public abstract ValueTask Stop();

        private async ValueTask HandleConnectionLost()
        {
            Log.Trace(1, $"{this.name} lost connection. Wait for client to reconnect....");
            await this.OnClientConnectionLost(this.id);
        }
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