using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Bridge.Networking
{
#if NET6_0_OR_GREATER
    public abstract class AsyncHost : AsyncBridgeEventRecorder
    {
        private readonly AsyncHostProtocol communicator;
        private readonly DirectionDictionary<TimeSpan> BoardThinkTime = new(new TimeSpan(), new TimeSpan());
        private readonly DirectionDictionary<TimeSpan> TotalThinkTime = new(new TimeSpan(), new TimeSpan());
        private readonly DirectionDictionary<DateTimeOffset> StartThinkTime = new(DateTimeOffset.MaxValue, DateTimeOffset.MaxValue);
        protected readonly CancellationTokenSource cts = new();

        public AsyncHost(AsyncHostProtocol _communicator, string name) : base(name)
        {
            communicator = _communicator;
            communicator.owner = this;
        }

        public async ValueTask Start()
        {
            communicator.Start();
            await ValueTask.CompletedTask.ConfigureAwait(false);
        }

        public override async ValueTask Finish()
        {
            await communicator.Finish();
        }

        public async ValueTask WaitForClients()
        {
            try
            {
                await communicator.AllSeatsTaken(cts.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                return;
            }
        }

        #region bridge events

        public override async ValueTask HandleTournamentStarted(Scorings scoring, int maxTimePerBoard, int maxTimePerCard, string tournamentName)
        {
            TotalThinkTime[Directions.NorthSouth] = TimeSpan.Zero;
            TotalThinkTime[Directions.EastWest] = TimeSpan.Zero;
            await base.HandleTournamentStarted(scoring, maxTimePerBoard, maxTimePerCard, tournamentName);
            await communicator.HandleTournamentStarted(scoring, maxTimePerBoard, maxTimePerCard, tournamentName);
        }

        public override async ValueTask HandleRoundStarted(SeatCollection<string> participantNames, DirectionDictionary<string> conventionCards)
        {
            await base.HandleRoundStarted(participantNames, conventionCards);
            await communicator.HandleRoundStarted(participantNames, conventionCards);
        }

        public override async ValueTask HandleBoardStarted(int boardNumber, Seats dealer, Vulnerable vulnerabilty)
        {
            BoardThinkTime[Directions.NorthSouth] = TimeSpan.Zero;
            BoardThinkTime[Directions.EastWest] = TimeSpan.Zero;
            await base.HandleBoardStarted(boardNumber, dealer, vulnerabilty);
            await communicator.HandleBoardStarted(boardNumber, dealer, vulnerabilty);
        }

        public override async ValueTask HandleCardPosition(Seats seat, Suits suit, Ranks rank)
        {
            await base.HandleCardPosition(seat, suit, rank);
            await communicator.HandleCardPosition(seat, suit, rank);
        }

        public override async ValueTask HandleBidNeeded(Seats whoseTurn, Bid lastRegularBid, bool allowDouble, bool allowRedouble)
        {
            StartThinkTime[whoseTurn.Direction()] = DateTimeOffset.UtcNow;
            await communicator.HandleBidNeeded(whoseTurn, lastRegularBid, allowDouble, allowRedouble);
        }

        public override async ValueTask HandleBidDone(Seats source, Bid bid, DateTimeOffset when)
        {
            var elapsed = when.Subtract(StartThinkTime[source.Direction()]);
            if (elapsed.Ticks > 0) BoardThinkTime[source.Direction()] += elapsed;
            await base.HandleBidDone(source, bid, when);
            await communicator.HandleBidDone(source, bid, when);
            if (this.Auction.Ended)
            {
                await this.HandleAuctionFinished(this.Auction.Declarer, this.Auction.FinalContract);

            }
            else
            {
                await this.HandleBidNeeded(this.Auction.WhoseTurn, this.Auction.LastRegularBid, this.Auction.AllowDouble, this.Auction.AllowRedouble);
            }
        }

        public override async ValueTask HandleAuctionFinished(Seats declarer, Contract finalContract)
        {
            await communicator.HandleAuctionFinished(declarer, finalContract);
            await this.HandleCardNeeded(this.Play.whoseTurn == this.Play.Dummy ? this.Auction.Declarer : this.Play.whoseTurn, this.Play.whoseTurn, this.Play.Trump, this.Play.leadSuit, true, 0, this.Play.currentTrick);
        }

        public override async ValueTask HandleCardNeeded(Seats controller, Seats whoseTurn, Suits leadSuit, Suits trump, bool trumpAllowed, int leadSuitLength, int trick)
        {
            StartThinkTime[whoseTurn.Direction()] = DateTimeOffset.UtcNow;
            await communicator.HandleCardNeeded(controller, whoseTurn, leadSuit, trump, trumpAllowed, leadSuitLength, trick);
        }

        public override async ValueTask HandleCardPlayed(Seats source, Suits suit, Ranks rank, DateTimeOffset when)
        {
            var elapsed = when.Subtract(StartThinkTime[source.Direction()]);
            if (elapsed.Ticks > 0) BoardThinkTime[source.Direction()] += elapsed;
            await base.HandleCardPlayed(source, suit, rank, when);
            await communicator.HandleCardPlayed(source, suit, rank, when);
            if (this.Play.PlayEnded)
            {
                await this.HandlePlayFinished(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));
            }
            else if (this.Play.TrickEnded)
            {
                await this.HandleTrickFinished(this.Play.whoseTurn, 0, 0);
            }
            else
            {
                await this.HandleCardNeeded(this.Play.whoseTurn == this.Play.Dummy ? this.Auction.Declarer : this.Play.whoseTurn, this.Play.whoseTurn, this.Play.Trump, this.Play.leadSuit, true, 0, this.Play.currentTrick);
            }
        }

        public override async ValueTask HandleTrickFinished(Seats trickWinner, int tricksForDeclarer, int tricksForDefense)
        {
            await communicator.HandleTrickFinished(trickWinner, tricksForDeclarer, tricksForDefense);
            await this.HandleCardNeeded(this.Play.whoseTurn == this.Play.Dummy ? this.Auction.Declarer : this.Play.whoseTurn, this.Play.whoseTurn, this.Play.Trump, this.Play.leadSuit, true, 0, this.Play.currentTrick);
        }

        public override ValueTask HandlePlayFinished(TimeSpan boardByNS, TimeSpan totalByNS, TimeSpan boardByEW, TimeSpan totalByEW)
        {
            TotalThinkTime[Directions.NorthSouth] += BoardThinkTime[Directions.NorthSouth];
            TotalThinkTime[Directions.EastWest] += BoardThinkTime[Directions.EastWest];
            Log.Trace(1, $"board: {BoardThinkTime[Directions.NorthSouth].TotalSeconds}s {BoardThinkTime[Directions.EastWest].TotalSeconds}s, total: {TotalThinkTime[Directions.NorthSouth].TotalMinutes:F1}m {TotalThinkTime[Directions.EastWest].TotalMinutes:F1}m");
            return communicator.HandlePlayFinished(BoardThinkTime[Directions.NorthSouth], TotalThinkTime[Directions.NorthSouth], BoardThinkTime[Directions.EastWest], TotalThinkTime[Directions.EastWest]);
        }

        public override ValueTask HandleTournamentStopped()
        {
            return communicator.HandleTournamentStopped();
        }

        public override async ValueTask HandleCardDealingEnded()
        {
            await communicator.HandleCardDealingEnded();
            await this.HandleBidNeeded(this.Auction.WhoseTurn, this.Auction.LastRegularBid, this.Auction.AllowDouble, this.Auction.AllowRedouble);
        }

        public override ValueTask HandleExplanationDone(Seats source, Bid bid)
        {
            return communicator.HandleExplanationDone(source, bid);
        }

        public override ValueTask HandleExplanationNeeded(Seats source, Bid bid)
        {
            return communicator.HandleExplanationNeeded(source, bid);
        }

        public override ValueTask HandleNeedDummiesCards(Seats dummy)
        {
            return communicator.HandleNeedDummiesCards(dummy);
        }

        public override ValueTask HandleShowDummy(Seats dummy)
        {
            return communicator.HandleShowDummy(dummy);
        }

        #endregion
    }

    public abstract class AsyncHostProtocol(string name) : AsyncBridgeEventRecorder(name)
    {
        protected readonly SeatCollection<AsyncClientProtocol> seats = new();
        protected readonly SeatCollection<ValueTask> waiter = new();
        protected readonly SemaphoreSlim allSeatsFilled = new(0);
        protected int seatsTaken = 0;
        protected readonly SemaphoreSlim waitForAnswer = new(0);
        protected Seats whoToWaitFor;
        public AsyncHost owner;

        public abstract void Start();

        public async ValueTask AllSeatsTaken(CancellationToken cancellationToken)
        {
            try
            {
                await this.allSeatsFilled.WaitAsync(cancellationToken).ConfigureAwait(false);
                Log.Trace(2, $"AsyncHostCommunication.AllSeatsTaken done");
            }
            catch (OperationCanceledException)
            {
                return;
            }
        }
    }

    public class HostInProcessProtocol() : AsyncHostProtocol("HostInProcessProtocol")
    {
        public override void Start()
        {
        }

        public override async ValueTask Finish()
        {
            await ValueTask.CompletedTask;
        }

        public async ValueTask Connect(Seats seat, AsyncClientProtocol clientCommunicator)
        {
            Log.Trace(2, $"{NameForLog}.Connect: {seat}");
            await ValueTask.CompletedTask;
            seats[seat] = clientCommunicator;
            seatsTaken++;
            if (seatsTaken >= 4) allSeatsFilled.Release();
        }

        private void SendToAll(Func<AsyncClientProtocol, ValueTask> action, Seats except = (Seats)(-1))
        {
            for (Seats seat = Seats.North; seat <= Seats.West; seat++)
            {
                if (seat != except)
                {
                    waiter[seat] = action(seats[seat]);
                }
            }
        }

        #region bridge events

        public override async ValueTask HandleTournamentStarted(Scorings scoring, int maxTimePerBoard, int maxTimePerCard, string tournamentName)
        {
            Log.Trace(3, $"{NameForLog}.HandleTournamentStarted");
            await base.HandleTournamentStarted(scoring, maxTimePerBoard, maxTimePerCard, tournamentName);
            SendToAll(async seat =>
            {
                await seat.HandleTournamentStarted(scoring, maxTimePerBoard, maxTimePerCard, tournamentName);
            });
        }

        public override async ValueTask HandleRoundStarted(SeatCollection<string> participantNames, DirectionDictionary<string> conventionCards)
        {
            Log.Trace(3, $"{NameForLog}.HandleRoundStarted");
            await base.HandleRoundStarted(participantNames, conventionCards);
            SendToAll(async seat =>
            {
                await seat.HandleRoundStarted(participantNames, conventionCards);
            });
        }

        public override async ValueTask HandleBoardStarted(int boardNumber, Seats dealer, Vulnerable vulnerabilty)
        {
            Log.Trace(3, $"{NameForLog}.HandleBoardStarted");
            await base.HandleBoardStarted(boardNumber, dealer, vulnerabilty);
            SendToAll(async seat =>
            {
                await seat.HandleBoardStarted(boardNumber, dealer, vulnerabilty);
            });
        }

        public override async ValueTask HandleCardPosition(Seats seat, Suits suit, Ranks rank)
        {
            Log.Trace(3, $"{NameForLog}.HandleCardPosition");
            await base.HandleCardPosition(seat, suit, rank);
            if (!Auction.Ended)
            {
                await seats[seat].HandleCardPosition(seat, suit, rank);
            }
            else
            {
                SendToAll(async client =>
                {
                    await client.HandleCardPosition(seat, suit, rank);
                }, Auction.FinalContract.Declarer.Partner());
            }
        }

        public override async ValueTask HandleBidNeeded(Seats whoseTurn, Bid lastRegularBid, bool allowDouble, bool allowRedouble)
        {
            whoToWaitFor = whoseTurn;
            Log.Trace(3, $"{NameForLog}.HandleBidNeeded: wait for bid from {whoToWaitFor}");
            await seats[whoseTurn].HandleBidNeeded(whoseTurn, lastRegularBid, allowDouble, allowRedouble);
            await waitForAnswer.WaitAsync();
        }

        public override async ValueTask HandleBidDone(Seats source, Bid bid, DateTimeOffset when)
        {
            if (source != whoToWaitFor) throw new Exception($"Expected a bid from {whoToWaitFor} but received a bid from {source}");
            Log.Trace(3, $"{NameForLog}.HandleBidDone");
            await base.HandleBidDone(source, bid, when);
            SendToAll(async seat =>
            {
                await seat.HandleBidDone(source, bid, when);
            }, source);
            waitForAnswer.Release();
        }

        public override async ValueTask HandleAuctionFinished(Seats declarer, Contract finalContract)
        {
            Log.Trace(3, $"{NameForLog}.HandleAuctionFinished");
            await base.HandleAuctionFinished(declarer, finalContract);
            SendToAll(async seat =>
            {
                await seat.HandleAuctionFinished(declarer, finalContract);
            });
        }

        public override async ValueTask HandleCardNeeded(Seats controller, Seats whoseTurn, Suits leadSuit, Suits trump, bool trumpAllowed, int leadSuitLength, int trick)
        {
            whoToWaitFor = whoseTurn;
            Log.Trace(3, $"{NameForLog}.HandleCardNeeded: wait for card from {whoToWaitFor}");
            await seats[controller].HandleCardNeeded(controller, whoseTurn, leadSuit, trump, trumpAllowed, leadSuitLength, trick);
            await waitForAnswer.WaitAsync();
        }

        public override async ValueTask HandleCardPlayed(Seats source, Suits suit, Ranks rank, DateTimeOffset when)
        {
            Log.Trace(3, $"{NameForLog}.HandleCardPlayed");
            if (source != whoToWaitFor) throw new Exception($"Expected a card from {whoToWaitFor} but received a card from {source}");
            await base.HandleCardPlayed(source, suit, rank, when);
            SendToAll(async seat =>
            {
                await seat.HandleCardPlayed(source, suit, rank, when);
            }, source == Play.Dummy ? source.Partner() : source);
            waitForAnswer.Release();
        }

        public override async ValueTask HandleTrickFinished(Seats trickWinner, int tricksForDeclarer, int tricksForDefense)
        {
            Log.Trace(3, $"{NameForLog}.HandleTrickFinished");
            await base.HandleTrickFinished(trickWinner, tricksForDeclarer, tricksForDefense);
            SendToAll(async seat =>
            {
                await seat.HandleTrickFinished(trickWinner, tricksForDeclarer, tricksForDefense);
            });
        }

        public override async ValueTask HandlePlayFinished(TimeSpan boardByNS, TimeSpan totalByNS, TimeSpan boardByEW, TimeSpan totalByEW)
        {
            Log.Trace(3, $"{NameForLog}.HandlePlayFinished");
            await base.HandlePlayFinished(boardByNS, totalByNS, boardByEW, totalByEW);
            SendToAll(async seat =>
            {
                await seat.HandlePlayFinished(boardByNS, totalByNS, boardByEW, totalByEW);
            });
        }

        public override async ValueTask HandleTournamentStopped()
        {
            Log.Trace(3, $"{NameForLog}.HandleTournamentStopped");
            await base.HandleTournamentStopped();
            SendToAll(async seat =>
            {
                await seat.HandleTournamentStopped();
            });
        }

        public override async ValueTask HandleCardDealingEnded()
        {
            Log.Trace(3, $"{NameForLog}.HandleCardDealingEnded");
            SendToAll(async seat =>
            {
                await seat.HandleCardDealingEnded();
            });
            await ValueTask.CompletedTask;
        }

        public override async ValueTask HandleNeedDummiesCards(Seats dummy)
        {
            Log.Trace(3, $"{NameForLog}.HandleNeedDummiesCards");
            SendToAll(async seat =>
            {
                await seat.HandleNeedDummiesCards(dummy);
            });
            await ValueTask.CompletedTask;
        }

        public override async ValueTask HandleShowDummy(Seats dummy)
        {
            Log.Trace(3, $"{NameForLog}.HandleShowDummy");
            SendToAll(async seat =>
            {
                await seat.HandleShowDummy(dummy);
            });
            await ValueTask.CompletedTask;
        }

        #endregion
    }

    public class HostComputerBridgeProtocol : AsyncHostProtocol
    {
        private readonly SeatCollection<int> ClientIds = new();
        private readonly SeatCollection<Queue<TimedMessage>> messages = new();
        private readonly SeatCollection<SemaphoreSlim> answerReceived = new();
        private readonly Distribution cards = new();
        private readonly CommunicationHost communicationHost;

        public HostComputerBridgeProtocol(CommunicationHost _communicationHost) : base("HostComputerBridgeProtocol")
        {
            communicationHost = _communicationHost;
            communicationHost.ProcessMessage = this.ProcessMessage;
        }

        private struct TimedMessage
        {
            public string Message { get; set; }
            public DateTimeOffset When { get; set; }
        }

        public override void Start()
        {
            for (Seats seat = Seats.North; seat <= Seats.West; seat++)
            {
                messages[seat] = new();
                answerReceived[seat] = new(0);
            }
        }

        private async ValueTask ProcessMessage(int clientId, string message)
        {
            Log.Trace(5, $"{NameForLog}.Process '{message}' from client {clientId}");
            var clientSeat = SeatsExtensions.Null;
            for (Seats seat = Seats.North; seat <= Seats.West; seat++)
            {
                if (ClientIds[seat] == clientId)
                {
                    clientSeat = seat;
                    break;
                }
            }

            if (clientSeat == SeatsExtensions.Null)
            {   // new client
                var loweredMessage = message.ToLowerInvariant();
                if (loweredMessage.StartsWith("connecting ", StringComparison.InvariantCultureIgnoreCase))
                {
                    if (!(loweredMessage.Contains(" as ") && loweredMessage.Contains(" using protocol version ")))
                    {
                        await communicationHost.Send("Expected 'Connecting .... as ... using protocol version ..'", clientId).ConfigureAwait(false);
                        return;
                    }

                    var hand = loweredMessage.Substring(loweredMessage.IndexOf(" as ") + 4, 5).Trim();
                    if (!(hand == "north" || hand == "east" || hand == "south" || hand == "west"))
                    {
                        await communicationHost.Send($"Illegal hand '{hand}' specified", clientId).ConfigureAwait(false);
                        return;
                    }

                    var seat2 = SeatsExtensions.FromXML(hand[0..1].ToUpperInvariant());
                    int p = message.IndexOf("\"");
                    var teamName = message[(p + 1)..message.IndexOf("\"", p + 1)];
                    //if (this.teams[seat.Next()] == teamName || this.teams[seat.Previous()] == teamName) return new ConnectResponse(Seats.North - 1, $"Team name must differ from opponents team name '{(this.teams[seat.Next()].Length > 0 ? this.teams[seat.Next()] : this.teams[seat.Previous()])}'");
                    //if (this.teams[seat].Length > 0 && this.teams[seat].ToLower() != teamName.ToLower()) return new ConnectResponse(Seats.North - 1, $"Team name must be '{this.teams[seat]}'");
                    //this.teams[seat] = teamName;
                    var protocolVersion = int.Parse(message[(message.IndexOf(" version ") + 9)..]);
                    //switch (protocolVersion)
                    //{
                    //    case 18:
                    //        this.CanAskForExplanation[seat] = false;
                    //        break;
                    //    case 19:
                    //        this.CanAskForExplanation[seat] = false;
                    //        break;
                    //    default:
                    //        throw new ArgumentException($"protocol version {protocolVersion} not supported");
                    //}

                    var partner = seat2.Partner();
                    var partnerTeamName = teamName;
                    //if (this.clients[partner] >= 0)
                    //{
                    //    if (this.teams[partner] == null)
                    //    {
                    //        this.teams[partner] = teamName;
                    //    }
                    //    else
                    //    {
                    //        partnerTeamName = this.teams[partner];
                    //    }
                    //}


                    if (ClientIds[seat2] == 0)
                    {
                        //if (teamName == partnerTeamName)
                        //{
                        //    this.clients[seat] = clientId;
                        //    this.slowClient[seat] = false;  // teamName.ToLower().Contains("q");
                        //    if (this.slowClient[seat]) Log.Trace(1, $"Apply Q-Plus delays: wait 500ms before send");
                        //    this.seats[clientId] = seat;
                        //    this.messages[seat] = new Queue<string>();
                        //    await this.PublishHostEvent(HostEvents.Seated, seat + "|" + teamName).ConfigureAwait(false);
                        //    await this.PublishHostEvent(HostEvents.ReadyForTeams, null).ConfigureAwait(false);
                        //    //if (this.OnHostEvent != null) await this.OnHostEvent(this, HostEvents.Seated, seat + "|" + teamName).ConfigureAwait(false);
                        //    return new ConnectResponse(seat, $"{seat} (\"{teamName}\") seated");
                        //}
                        //else
                        //{
                        //    return new ConnectResponse(Seats.North - 1, $"Expected team name '{partnerTeamName}'");
                        //}

                        ClientIds[seat2] = clientId;
                        seatsTaken++;
                        Log.Trace(5, $"Client {clientId} is {seat2}. {seatsTaken} seats taken.");
                        await communicationHost.Send($"{seat2} (\"{teamName}\") seated", clientId).ConfigureAwait(false);

                        if (seatsTaken >= 4)
                        {
                            allSeatsFilled.Release();
                        }
                    }
                    else
                    {
                        await communicationHost.Send("Seat was already taken", clientId).ConfigureAwait(false);
                        return;
                    }
                }
            }
            else
            {
                messages[clientSeat].Enqueue(new TimedMessage { Message = message, When = DateTimeOffset.UtcNow });
                answerReceived[clientSeat].Release();
            }
        }

        private void SendToAll(string message, Seats except1 = (Seats)(-1), Seats except2 = (Seats)(-1))
        {
            for (Seats seat = Seats.North; seat <= Seats.West; seat++)
            {
                if (seat != except1 && seat != except2)
                {
                    SendTo(message, seat);
                }
            }
        }

        private void SendTo(string message, Seats seat)
        {
            waiter[seat] = communicationHost.Send(message, ClientIds[seat]);
        }

        private async ValueTask AllAnswered(string expectedAnswer, Seats except = (Seats)(-1), Seats dummy = (Seats)(-1))
        {
            Log.Trace(3, $"{NameForLog}.AllAnswered waiting for '{expectedAnswer}'");
            for (Seats seat = Seats.North; seat <= Seats.West; seat++)
            {
                if (seat != except)
                {
                    var answer = await Answered(expectedAnswer, seat, dummy).ConfigureAwait(false);
                }
            }
            Log.Trace(3, $"{NameForLog}.AllAnswered '{expectedAnswer}'");
        }

        private async ValueTask<TimedMessage> Answered(string expectedAnswer, Seats seat, Seats dummy = (Seats)(-1))
        {
            Log.Trace(4, $"{NameForLog}.Answered waiting for {seat}");
            await answerReceived[seat].WaitAsync().ConfigureAwait(false);
            var answer = messages[seat].Dequeue();
            if (!answer.Message.Contains(expectedAnswer, StringComparison.InvariantCultureIgnoreCase))
            {
                if (seat == dummy && answer.Message.Contains("Dummy's", StringComparison.InvariantCultureIgnoreCase) && expectedAnswer.Contains($"{seat}'s", StringComparison.InvariantCultureIgnoreCase))
                {
                    // dummy sends 'North ready for dummy's card to trick 9' instead of 'North ready for North's card to trick 9'
                }
                else
                {
                    throw new Exception();
                }
            }

            Log.Trace(1, $"Host received '{answer.Message}'");
            return answer;
        }

        public override async ValueTask HandleRoundStarted(SeatCollection<string> participantNames, DirectionDictionary<string> conventionCards)
        {
            Log.Trace(3, $"{NameForLog}.HandleRoundStarted");
            await base.HandleRoundStarted(participantNames, conventionCards).ConfigureAwait(false);
            await AllAnswered("ready for teams").ConfigureAwait(false);
            SendToAll("Teams : N/S : \"RoboBridge\" E/W : \"Robo2017\". Playing IMP'");
            await AllAnswered("ready to start").ConfigureAwait(false);
        }

        public override async ValueTask HandleBoardStarted(int boardNumber, Seats _dealer, Vulnerable vulnerabilty)
        {
            Log.Trace(3, $"{NameForLog}.HandleRoundStarted");
            await base.HandleBoardStarted(boardNumber, _dealer, vulnerabilty).ConfigureAwait(false);
            cards.Clear();
            SendToAll("Start of board");
            await AllAnswered("ready for deal").ConfigureAwait(false);
            SendToAll($"Board number {boardNumber}. Dealer {_dealer}. {ProtocolHelper.Translate(vulnerabilty)} vulnerable.");
            await AllAnswered("ready for cards").ConfigureAwait(false);
        }

        public override async ValueTask HandleCardPosition(Seats seat, Suits suit, Ranks rank)
        {
            await base.HandleCardPosition(seat, suit, rank).ConfigureAwait(false);
            cards.Give(seat, suit, rank);
            await ValueTask.CompletedTask.ConfigureAwait(false);
        }

        public override async ValueTask HandleCardDealingEnded()
        {
            Log.Trace(3, $"HostComputerBridgeProtocol.HandleCardDealingEnded");
            await base.HandleCardDealingEnded().ConfigureAwait(false);
            for (Seats seat = Seats.North; seat <= Seats.West; seat++)
            {
                var message = seat.ToString() + ProtocolHelper.Translate(seat, this.cards);
                SendTo(message, seat);
            }

            await AllAnswered($"ready for {Auction.WhoseTurn}'s bid", Auction.WhoseTurn).ConfigureAwait(false);
        }

        public override async ValueTask HandleBidNeeded(Seats whoseTurn, Bid lastRegularBid, bool allowDouble, bool allowRedouble)
        {
            Log.Trace(3, $"{NameForLog}.HandleBidNeeded");
            await base.HandleBidNeeded(whoseTurn, lastRegularBid, allowDouble, allowRedouble).ConfigureAwait(false);
            var answer = await Answered($"{whoseTurn} ", whoseTurn).ConfigureAwait(false);
            var bid = ProtocolHelper.TranslateBid(answer.Message, out var bidder);
            await owner.HandleBidDone(whoseTurn, bid, answer.When).ConfigureAwait(false);
        }

        public override async ValueTask HandleBidDone(Seats source, Bid bid, DateTimeOffset when)
        {
            Log.Trace(3, $"{NameForLog}.HandleBidDone");
            await base.HandleBidDone(source, bid, when).ConfigureAwait(false);
            for (Seats seat = Seats.North; seat <= Seats.West; seat++)
            {
                if (seat != source)
                {
                    var message = ProtocolHelper.Translate(bid, source, seat == source.Partner(), AlertMode.SelfExplaining);
                    SendTo(message, seat);
                }
            }

            if (Auction.Ended)
            {
            }
            else
            {
                var whoseTurn = source.Next();
                await AllAnswered($"ready for {whoseTurn}'s bid", whoseTurn).ConfigureAwait(false);
            }
        }

        public override async ValueTask HandleCardNeeded(Seats controller, Seats whoseTurn, Suits leadSuit, Suits trump, bool trumpAllowed, int leadSuitLength, int trick)
        {
            Log.Trace(3, $"{NameForLog}.HandleCardNeeded");
            await base.HandleCardNeeded(controller, whoseTurn, leadSuit, trump, trumpAllowed, leadSuitLength, trick).ConfigureAwait(false);
            await AllAnswered($"ready for {whoseTurn}'s card to trick {trick}", controller, trick > 1 || Play.man > 1 ? Dummy : (Seats)(-1)).ConfigureAwait(false);
            if (Play.man == 1)
            {
                var whoToLead = controller;
                //await Task.Delay(300);      // give some time to process the previous message '.. plays ..' ( this is the only(?) protocol message that is sent directly after another message without receiving some confirmation message first)
                SendTo($"{(whoseTurn == this.Play.Dummy ? "Dummy" : whoseTurn.ToXMLFull())} to lead", whoToLead);
            }

            var answer = await Answered($"{whoseTurn} plays ", controller).ConfigureAwait(false);
            var card = ProtocolHelper.TranslateCard(answer.Message, out var player);
            await owner.HandleCardPlayed(player, card.Suit, card.Rank, answer.When).ConfigureAwait(false);
        }

        public override async ValueTask HandleCardPlayed(Seats source, Suits suit, Ranks rank, DateTimeOffset when)
        {
            Log.Trace(3, $"{NameForLog}.HandleCardPlayed");
            await base.HandleCardPlayed(source, suit, rank, when).ConfigureAwait(false);

            var message = $"{source} plays {rank.ToXML()}{suit.ToXML()}";
            SendToAll(message, source == Play.Dummy ? source.Partner() : source);

            if (Play.currentTrick == 1 && Play.man == 2)
            {
                await AllAnswered($"ready for dummy", Dummy).ConfigureAwait(false);
                var cards = "Dummy" + ProtocolHelper.Translate(Dummy, Distribution);
                SendToAll(cards, Play.Dummy);
            }
        }

        public override async ValueTask HandlePlayFinished(TimeSpan boardByNS, TimeSpan totalByNS, TimeSpan boardByEW, TimeSpan totalByEW)
        {
            Log.Trace(3, $"{NameForLog}.HandlePlayFinished");
            await base.HandlePlayFinished(boardByNS, totalByNS, boardByEW, totalByEW).ConfigureAwait(false);

            var timingInfo = string.Format("Timing - N/S : this board  {0:mm\\:ss},  total  {1:h\\:mm\\:ss}.  E/W : this board  {2:mm\\:ss},  total  {3:h\\:mm\\:ss}."
                , boardByNS.RoundToSeconds()
                , totalByNS.RoundToSeconds()
                , boardByEW.RoundToSeconds()
                , totalByEW.RoundToSeconds()
                );
            SendToAll(timingInfo);
        }

        public override async ValueTask HandleTournamentStopped()
        {
            Log.Trace(3, $"{NameForLog}.HandleTournamentStopped");
            await base.HandleTournamentStopped().ConfigureAwait(false);
            SendToAll("End of session");
        }

        public override async ValueTask Finish()
        {
            await communicationHost.Stop();
        }
    }

    public abstract class AsyncClient : AsyncBridgeEventHandler
    {
        public readonly Seats seat;
        protected readonly AsyncClientProtocol communicator;

        public AsyncClient(Seats _seat, AsyncClientProtocol communication, string name) : base(name)
        {
            seat = _seat;
            communicator = communication;
            communicator.owner = this;
        }

        public async ValueTask Connect()
        {
            await communicator.Connect();
        }

        public override async ValueTask Finish()
        {
            await communicator.Finish();
        }
    }

    public abstract class AsyncClientProtocol(string nameForLog) : AsyncBridgeEventRecorder(nameForLog)
    {
        public AsyncClient owner;

        public abstract ValueTask Connect();

        public abstract ValueTask SendBid(Bid bid);

        public abstract ValueTask SendCard(Seats source, Card card);
    }

    public class ClientInProcessProtocol(HostInProcessProtocol _host) : AsyncClientProtocol("ClientInProcessProtocol")
    {
        private readonly HostInProcessProtocol host = _host;

        public override async ValueTask Connect()
        {
            await host.Connect(owner.seat, this);
        }

        public override ValueTask SendBid(Bid bid)
        {
            return host.HandleBidDone(owner.seat, bid, DateTimeOffset.UtcNow);
        }

        public override ValueTask SendCard(Seats source, Card card)
        {
            return host.HandleCardPlayed(source, card.Suit, card.Rank, DateTimeOffset.UtcNow);
        }

        public override async ValueTask Finish()
        {
            await ValueTask.CompletedTask;
        }

        #region bridge events

        public override async ValueTask HandleTournamentStarted(Scorings scoring, int maxTimePerBoard, int maxTimePerCard, string tournamentName)
        {
            Log.Trace(3, $"AsyncClientCommunication.{owner.seat}.HandleTournamentStarted");
            await owner.HandleTournamentStarted(scoring, maxTimePerBoard, maxTimePerCard, tournamentName);
        }

        public override async ValueTask HandleRoundStarted(SeatCollection<string> participantNames, DirectionDictionary<string> conventionCards)
        {
            Log.Trace(3, $"AsyncClientCommunication.{owner.seat}.HandleRoundStarted");
            await owner.HandleRoundStarted(participantNames, conventionCards);
        }

        public override async ValueTask HandleBoardStarted(int boardNumber, Seats dealer, Vulnerable vulnerabilty)
        {
            Log.Trace(3, $"AsyncClientCommunication.{owner.seat}.HandleBoardStarted");
            await owner.HandleBoardStarted(boardNumber, dealer, vulnerabilty);
        }

        public override async ValueTask HandleCardPosition(Seats seat, Suits suit, Ranks rank)
        {
            Log.Trace(3, $"AsyncClientCommunication.{owner.seat}.HandleCardPosition");
            await owner.HandleCardPosition(seat, suit, rank);
        }

        public override async ValueTask HandleBidNeeded(Seats whoseTurn, Bid lastRegularBid, bool allowDouble, bool allowRedouble)
        {
            Log.Trace(3, $"AsyncClientCommunication.{owner.seat}.HandleBidNeeded");
            await owner.HandleBidNeeded(whoseTurn, lastRegularBid, allowDouble, allowRedouble);
        }

        public override async ValueTask HandleBidDone(Seats source, Bid bid, DateTimeOffset when)
        {
            Log.Trace(3, $"AsyncClientCommunication.{owner.seat}.HandleBidDone");
            await owner.HandleBidDone(source, bid, when);
        }

        public override async ValueTask HandleExplanationNeeded(Seats source, Bid bid)
        {
            Log.Trace(3, $"AsyncClientCommunication.{owner.seat}.HandleExplanationNeeded");
            await owner.HandleExplanationNeeded(source, bid);
        }

        public override async ValueTask HandleExplanationDone(Seats source, Bid bid)
        {
            Log.Trace(3, $"AsyncClientCommunication.{owner.seat}.HandleExplanationDone");
            await owner.HandleExplanationDone(source, bid);
        }

        public override async ValueTask HandleAuctionFinished(Seats declarer, Contract finalContract)
        {
            Log.Trace(3, $"AsyncClientCommunication.{owner.seat}.HandleAuctionFinished");
            await owner.HandleAuctionFinished(declarer, finalContract);
        }

        public override async ValueTask HandleCardNeeded(Seats controller, Seats whoseTurn, Suits leadSuit, Suits trump, bool trumpAllowed, int leadSuitLength, int trick)
        {
            Log.Trace(3, $"AsyncClientCommunication.{owner.seat}.HandleCardNeeded");
            await owner.HandleCardNeeded(controller, whoseTurn, leadSuit, trump, trumpAllowed, leadSuitLength, trick);
        }

        public override async ValueTask HandleCardPlayed(Seats source, Suits suit, Ranks rank, DateTimeOffset when)
        {
            Log.Trace(3, $"AsyncClientCommunication.{owner.seat}.HandleCardPlayed");
            await owner.HandleCardPlayed(source, suit, rank, when);
        }

        public override async ValueTask HandleTrickFinished(Seats trickWinner, int tricksForDeclarer, int tricksForDefense)
        {
            Log.Trace(3, $"AsyncClientCommunication.{owner.seat}.HandleTrickFinished");
            await owner.HandleTrickFinished(trickWinner, tricksForDeclarer, tricksForDefense);
        }

        public override async ValueTask HandlePlayFinished(TimeSpan boardByNS, TimeSpan totalByNS, TimeSpan boardByEW, TimeSpan totalByEW)
        {
            Log.Trace(3, $"AsyncClientCommunication.{owner.seat}.HandlePlayFinished");
            await owner.HandlePlayFinished(boardByNS, totalByNS, boardByEW, totalByEW);
        }

        public override async ValueTask HandleTournamentStopped()
        {
            Log.Trace(3, $"AsyncClientCommunication.{owner.seat}.HandleTournamentStopped");
            await owner.HandleTournamentStopped();
        }

        public override async ValueTask HandleCardDealingEnded()
        {
            Log.Trace(3, $"AsyncClientCommunication.{owner.seat}.HandleCardDealingEnded");
            await owner.HandleCardDealingEnded();
        }

        public override async ValueTask HandleNeedDummiesCards(Seats dummy)
        {
            Log.Trace(3, $"AsyncClientCommunication.{owner.seat}.HandleNeedDummiesCards");
            await owner.HandleNeedDummiesCards(dummy);
        }

        public override async ValueTask HandleShowDummy(Seats dummy)
        {
            Log.Trace(3, $"AsyncClientCommunication.{owner.seat}.HandleShowDummy");
            await owner.HandleShowDummy(dummy);
        }

        #endregion region
    }

    public class ClientComputerBridgeProtocol : AsyncClientProtocol
    {
        private readonly CommunicationClient communicationClient;
        private readonly string teamName;
        private readonly int protocolVersion;
        private string expectedAnswer;

        public ClientComputerBridgeProtocol(string _teamName, int _protocolVersion, CommunicationClient _communicationClient) : base("ClientComputerBridgeProtocol")
        {
            communicationClient = _communicationClient;
            communicationClient.ProcessMessage = this.Receive;
            teamName = _teamName;
            protocolVersion = _protocolVersion;
        }

        public override async ValueTask Connect()
        {
            await communicationClient.Connect();
            communicationClient.Start();
            expectedAnswer = $"{owner.seat} (\"{teamName}\") seated";
            await communicationClient.Send($"Connecting \"{this.teamName}\" as {owner.seat} using protocol version {protocolVersion:00}");
        }

        public override async ValueTask SendBid(Bid bid)
        {
            await communicationClient.Send(ProtocolHelper.Translate(bid, owner.seat, false, AlertMode.SelfExplaining));
            await this.HandleBidDone(this.owner.seat, bid, DateTimeOffset.UtcNow);
        }

        public override async ValueTask SendCard(Seats source, Card card)
        {
            await communicationClient.Send($"{source} plays {card.Rank.ToXML()}{card.Suit.ToXML()}");
            await this.HandleCardPlayed(source, card.Suit, card.Rank, DateTimeOffset.UtcNow);
        }

        private async ValueTask Receive(string message)
        {
            Log.Trace(2, $"{owner.seat} receives '{message}' (expecting '{expectedAnswer}')");

            if (message.StartsWith("End of session", StringComparison.InvariantCultureIgnoreCase))
            {
                await owner.HandleTournamentStopped();
            }
            else
            if (message.StartsWith(expectedAnswer, StringComparison.InvariantCultureIgnoreCase))
            {
                if (message.Contains(" seated", StringComparison.InvariantCultureIgnoreCase))
                {
                    expectedAnswer = "Teams : ";
                    await communicationClient.Send($"{owner.seat} ready for teams");
                }
                else
                if (message.Contains("Teams : ", StringComparison.InvariantCultureIgnoreCase))
                {
                    var matchType = Scorings.scFirst;       // let the client UI decide what match type to be used
                    if (message.Contains(". playing imp", StringComparison.CurrentCultureIgnoreCase)) matchType = Scorings.scIMP;        // unless TableManager 'knows' better
                    if (message.Contains(". playing mp", StringComparison.CurrentCultureIgnoreCase)) matchType = Scorings.scPairs;

                    var teamNS = message.Substring(message.IndexOf("N/S : \"") + 7);
                    teamNS = teamNS.Substring(0, teamNS.IndexOf("\""));
                    var teamEW = message.Substring(message.IndexOf("E/W : \"") + 7);
                    teamEW = teamEW.Substring(0, teamEW.IndexOf("\""));
                    //if (this.team != (this.seat.IsSameDirection(Seats.North) ? this.teamNS : this.teamEW)) throw new ArgumentOutOfRangeException("team", "Seated in another team");

                    await owner.HandleTournamentStarted(matchType, 120, 90, "");
                    await owner.HandleRoundStarted(new SeatCollection<string>(new string[] { teamNS, teamEW, teamNS, teamEW }), new DirectionDictionary<string>("", ""));
                    expectedAnswer = "Start of board";
                    await communicationClient.Send($"{owner.seat} ready to start");
                }
                else
                if (message.Contains("Start of board", StringComparison.InvariantCultureIgnoreCase))
                {
                    expectedAnswer = "Board number ";
                    await communicationClient.Send($"{owner.seat} ready for deal");
                }
                else
                if (message.Contains("Board number ", StringComparison.InvariantCultureIgnoreCase))
                {
                    string[] dealInfoParts = message.Split('.');
                    int boardNumber = Convert.ToInt32(dealInfoParts[0].Substring(13));
                    var dealer = SeatsExtensions.FromXML(dealInfoParts[1].Substring(8));
                    var vulnerability = Vulnerable.Neither;
                    switch (dealInfoParts[2].Substring(1))
                    {
                        case "Both vulnerable":
                            vulnerability = Vulnerable.Both; break;
                        case "N/S vulnerable":
                            vulnerability = Vulnerable.NS; break;
                        case "E/W vulnerable":
                            vulnerability = Vulnerable.EW; break;
                    }
                    await owner.HandleBoardStarted(boardNumber, dealer, vulnerability);
                    await this.HandleBoardStarted(boardNumber, dealer, vulnerability);
                    expectedAnswer = $"{owner.seat}'s cards : ";
                    await communicationClient.Send($"{owner.seat} ready for cards");
                }
                else
                if (message.Contains($"{owner.seat}'s cards : ", StringComparison.InvariantCultureIgnoreCase))
                {
                    // "North's cards : S -.H A K T 8 4 3 2.D.C A K T 7 6 3."
                    string cardInfo = message.Substring(2 + message.IndexOf(":"));
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
                                    await owner.HandleCardPosition(owner.seat, s, rank);
                                    await this.HandleCardPosition(owner.seat, s, rank);
                                    cardsInSuit = cardsInSuit.Substring(2);
                                }
                            }
                        }
                    }

                    await owner.HandleCardDealingEnded();
                    var whoseTurn = this.Auction.WhoseTurn;
                    if (owner.seat == whoseTurn)
                    {
                        await owner.HandleBidNeeded(whoseTurn, new Bid(SpecialBids.Pass), false, false);
                    }
                    else
                    {
                        await communicationClient.Send($"{owner.seat} ready for {whoseTurn.ToXMLFull()}'s bid");
                        expectedAnswer = $"{whoseTurn} ";
                    }
                }
                else
                if (!Auction.Ended && message.Contains($"{Auction.WhoseTurn} ", StringComparison.InvariantCultureIgnoreCase))
                {
                    // "North bids 1S."

                    var bid = ProtocolHelper.TranslateBid(message, out var bidder);
                    await owner.HandleBidDone(bidder, bid, DateTimeOffset.UtcNow);
                    await this.HandleBidDone(bidder, bid, DateTimeOffset.UtcNow);
                }
                else
                if (message.Contains($"{(Play.whoseTurn == Play.Dummy ? "Dummy" : Play.whoseTurn)} to lead", StringComparison.InvariantCultureIgnoreCase))
                {
                    await owner.HandleCardNeeded(Play.whoseTurn, Play.whoseTurn, Play.leadSuit, Play.Trump, Play.man > 1, 0, Play.currentTrick);
                }
                else
                if (message.Contains($"{Play.whoseTurn} plays ", StringComparison.InvariantCultureIgnoreCase))
                {
                    var card = ProtocolHelper.TranslateCard(message, out var player);
                    await owner.HandleCardPlayed(player, card.Suit, card.Rank, DateTimeOffset.UtcNow);
                    await this.HandleCardPlayed(player, card.Suit, card.Rank, DateTimeOffset.UtcNow);
                }
                else
                if (message.Contains($"Dummy's cards : ", StringComparison.InvariantCultureIgnoreCase))
                {
                    // "Dummy's cards : S -.H A K T 8 4 3 2.D.C A K T 7 6 3."
                    string cardInfo = message.Substring(2 + message.IndexOf(":"));
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
                                    await owner.HandleCardPosition(Play.Dummy, s, rank);
                                    await this.HandleCardPosition(Play.Dummy, s, rank);
                                    cardsInSuit = cardsInSuit.Substring(2);
                                }
                            }
                        }
                    }

                    if (owner.seat == Play.Dummy.Partner())
                    {
                        await owner.HandleCardNeeded(owner.seat, Play.whoseTurn, Play.leadSuit, Play.Trump, Play.man > 1, 0, Play.currentTrick);
                    }
                    else
                    {
                        await communicationClient.Send($"{owner.seat} ready for {Play.whoseTurn}'s card to trick {Play.currentTrick}");
                        expectedAnswer = $"{Play.whoseTurn} plays ";
                    }
                }
                else
                if (message.Contains($"Timing - ", StringComparison.InvariantCultureIgnoreCase))
                {
                    expectedAnswer = $"Start of board";
                }
                else
                {

                }
            }
            else
            {
                throw new InvalidOperationException($"Received '{message}' instead of '{expectedAnswer}'");
            }
        }

        public override async ValueTask HandleBidDone(Seats source, Bid bid, DateTimeOffset when)
        {
            await base.HandleBidDone(source, bid, when);

            if (this.Auction.Ended)
            {
                var whoToLead = Auction.FinalContract.Declarer.Next();
                if (owner.seat == whoToLead)
                {

                    expectedAnswer = $"{whoToLead} to lead";
                }
                else
                {
                    await communicationClient.Send($"{owner.seat} ready for {whoToLead}'s card to trick {Play.currentTrick}");
                    expectedAnswer = $"{whoToLead} plays ";
                }
            }
            else
            {
                var whoseTurn = this.Auction.WhoseTurn;
                if (owner.seat == whoseTurn)
                {
                    await owner.HandleBidNeeded(whoseTurn, this.Auction.LastRegularBid, false, false);
                }
                else
                {
                    await communicationClient.Send($"{owner.seat} ready for {whoseTurn.ToXMLFull()}'s bid");
                    expectedAnswer = $"{whoseTurn} ";
                }
            }
        }

        public override async ValueTask HandleCardPlayed(Seats source, Suits suit, Ranks rank, DateTimeOffset when)
        {
            await base.HandleCardPlayed(source, suit, rank, when);

            if (Play.currentTrick == 1 && Play.man == 2 && owner.seat != this.Dummy)
            {   // dummy's cards
                await communicationClient.Send($"{owner.seat} ready for dummy");
                expectedAnswer = $"Dummy's cards : ";
                return;
            }

            if (Play.PlayEnded)
            {
                expectedAnswer = $"Timing - ";
            }
            else
            {
                var whoToLead = Play.whoseTurn;
                if ((owner.seat == whoToLead && whoToLead != Dummy) || (whoToLead == Dummy && owner.seat == Auction.Declarer))
                {
                    if (Play.man == 1)
                    {
                        expectedAnswer = $"{(whoToLead == Dummy ? "Dummy" : whoToLead)} to lead";
                    }
                    else
                    {
                        await owner.HandleCardNeeded(whoToLead, whoToLead, Play.leadSuit, Play.Trump, Play.man > 1, 0, Play.currentTrick);
                    }
                }
                else
                {
                    await communicationClient.Send($"{owner.seat} ready for {(whoToLead == Play.Dummy && owner.seat == Play.Dummy ? "dummy" : whoToLead.ToString())}'s card to trick {Play.currentTrick}");
                    expectedAnswer = $"{whoToLead} plays ";
                }
            }
        }

        public override async ValueTask Finish()
        {
            await communicationClient.Stop();
        }
    }
#endif
}
