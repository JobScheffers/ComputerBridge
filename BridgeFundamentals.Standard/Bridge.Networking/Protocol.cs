using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
#if !NET6_0_OR_GREATER
using Bridge.NonBridgeHelpers;
#endif

namespace Bridge.Networking
{
    public class TournamentHost : AsyncHost
    {
        private readonly Tournament tournament;
        private readonly Scorings matchType;
        private readonly AlertMode alertMode;
        private int boardNumber;
        private Board2 currentBoard;
        private bool moreBoards;
        private bool rotateHands = false;
        private ParticipantInfo participant;

        public TournamentHost(AsyncHostProtocol _communicator, string teamNS, string teamEW, Scorings _matchType, AlertMode _alertMode, Tournament _tournament) : base(_communicator, teamNS, teamEW)
        {
            tournament = _tournament;
            matchType = _matchType;
            alertMode = _alertMode;
        }

        public void Run()
        {
            this.hostRunTask = this.Run2();
        }

        public async ValueTask WaitForCompletionAsync()
        {
            if (this.hostRunTask == null) return;
            await this.hostRunTask.ConfigureAwait(false);
        }

        private Task hostRunTask;

        private async Task Run2()
        {
            await this.Start();
            await this.WaitForClients();
            this.participant = new ParticipantInfo() { ConventionCardNS = this.teamNames[Directions.NorthSouth], ConventionCardWE = this.teamNames[Directions.EastWest], MaxThinkTime = 120, UserId = Guid.NewGuid(), PlayerNames = new Participant(this.teamNames[Directions.NorthSouth], this.teamNames[Directions.EastWest], this.teamNames[Directions.NorthSouth], this.teamNames[Directions.EastWest]) };
            this.tournament.Participants.Add(new Team { LastBoard = 0, LastPlay = DateTime.MinValue, Member1 = this.teamNames[Directions.NorthSouth], Member2 = this.teamNames[Directions.NorthSouth], TournamentScore = double.MinValue });
            this.tournament.Participants.Add(new Team { LastBoard = 0, LastPlay = DateTime.MinValue, Member1 = this.teamNames[Directions.EastWest], Member2 = this.teamNames[Directions.EastWest], TournamentScore = double.MinValue });
            this.boardNumber = 0;
            await this.HandleTournamentStarted(matchType, 120, 100, tournament.EventName);
            await this.HandleRoundStarted(null, null);

            moreBoards = true;
            do
            {
                await this.NextBoard().ConfigureAwait(false);
            } while (this.moreBoards && !cts.IsCancellationRequested);
            //{
            //    await AllAnswered("ready for deal").ConfigureAwait(false);
            //    if (cts.IsCancellationRequested) break;

            //    answer = $"Board number {this.currentBoard.BoardNumber}. Dealer {Rotated(this.currentBoard.Dealer).ToXMLFull()}. {ProtocolHelper.Translate(RotatedV(this.currentBoard.Vulnerable))} vulnerable.";
            //    await this.BroadCast(answer).ConfigureAwait(false);
            //    await this.SendRelevantBridgeInfo(DateTime.UtcNow, answer).ConfigureAwait(false);

            //    await AllAnswered("ready for cards").ConfigureAwait(false);

            //    for (Seats s = Seats.North; s <= Seats.West; s++)
            //    {
            //        var rotatedSeat = Rotated(s);
            //        answer = rotatedSeat.ToXMLFull() + ProtocolHelper.Translate(s, this.currentBoard.Distribution);
            //        await this.Send(rotatedSeat, answer).ConfigureAwait(false);
            //        await this.SendRelevantBridgeInfo(DateTime.UtcNow, answer).ConfigureAwait(false);
            //    }

            //    var boardResult = this.CurrentResult;
            //    var who = Rotated(boardResult.Auction.WhoseTurn);
            //    int passes = 0;
            //    while (passes < 4)        // cannot use this.CurrentResult.Auction.Ended since it takes a while before the last bid has been processed
            //    {
            //        await AllAnswered($"ready for {who}'s bid", who).ConfigureAwait(false);
            //        var bid = await GetMessage(who).ConfigureAwait(false);
            //        ProtocolHelper.HandleProtocolBid(UnRotated(bid), this.EventBus);
            //        await this.EventBus.WaitForEventCompletionAsync().ConfigureAwait(false);
            //        if (bid.ToLower().Contains("passes")) { passes++; } else { passes = 1; }
            //        who = who.Next();
            //    }

            //    while (!boardResult.Auction.Ended) await Task.Delay(200).ConfigureAwait(false);       // need some time to process the bid and note that the auction has ended
            //    if (!boardResult.Auction.FinalContract.Bid.IsPass)
            //    {
            //        var dummy = this.Rotated(boardResult.Play.Dummy);
            //        Log.Trace(4, $"dummy={dummy}");
            //        for (int trick = 1; trick <= 13; trick++)
            //        {
            //            who = Rotated(CurrentResult.Play.whoseTurn);
            //            for (int man = 1; man <= 4; man++)
            //            {
            //                string card;
            //                if (who == dummy)
            //                {
            //                    await AllAnswered($"ready for {who}'s card to trick {trick}", who.Partner(), dummy).ConfigureAwait(false);
            //                    //var dummiesAnswer = await GetMessage(dummy).ConfigureAwait(false);
            //                    //if (dummiesAnswer.ToLower() != $"{dummy.ToString().ToLower()} ready for dummy's card to trick {trick}") throw new Exception();
            //                    card = await GetMessage(who.Partner()).ConfigureAwait(false);
            //                }
            //                else
            //                {
            //                    await AllAnswered($"ready for {who}'s card to trick {trick}", who).ConfigureAwait(false);
            //                    card = await GetMessage(who).ConfigureAwait(false);
            //                }
            //                ProtocolHelper.HandleProtocolPlay(UnRotated(card), this.EventBus);

            //                if (trick == 1 && man == 1)
            //                {
            //                    await AllAnswered($"ready for dummy", dummy).ConfigureAwait(false);

            //                    var cards = "Dummy" + ProtocolHelper.Translate(boardResult.Play.Dummy, this.currentBoard.Distribution);
            //                    for (Seats s = Seats.North; s <= Seats.West; s++)
            //                    {
            //                        if (s != dummy)
            //                        {
            //                            var task = this.Send(s, cards).ConfigureAwait(false);
            //                        }
            //                    }
            //                }

            //                who = who.Next();
            //            }

            //            await this.EventBus.WaitForEventCompletionAsync().ConfigureAwait(false);
            //            await Task.Delay(100).ConfigureAwait(false);       // need some time to process the trick
            //        }
    //    }

    //    await this.EventBus.WaitForEventCompletionAsync().ConfigureAwait(false);
    //    await Task.Delay(100).ConfigureAwait(false);       // need some time to process the end of board
    //}

        }

        private async ValueTask NextBoard()
        {
            Log.Trace(6, "TournamentController.NextBoard start");
            var alreadyPlayed = false;
            do
            {
                alreadyPlayed = false;
                await this.GetNextBoard().ConfigureAwait(false);
                if (this.currentBoard == null)
                {
                    Log.Trace(4, "TournamentController.NextBoard no next board");
                    this.moreBoards = false;
                    await this.HandleTournamentStopped();
                }
                else
                {
                    Log.Trace(4, $"TournamentController.NextBoard board={this.currentBoard.BoardNumber.ToString()}");
                    if (!(rotateHands && this.currentBoard.Results.Count == 1)      // otherwise endless loop when Team1 == Team2
                        && BoardHasBeenPlayedBy(this.currentBoard, this.teamNames[this.rotateHands ? Directions.EastWest : Directions.NorthSouth], this.teamNames[this.rotateHands ? Directions.NorthSouth : Directions.EastWest]))
                    {
                        Log.Trace(1, $"TournamentController.NextBoard skip board {this.currentBoard.BoardNumber.ToString()} because it has been played");
                        alreadyPlayed = true;
                    }
                    else
                    {
                        await this.HandleBoardStarted(this.currentBoard.BoardNumber, this.currentBoard.Dealer, this.currentBoard.Vulnerable);
                        for (int card = 0; card < currentBoard.Distribution.Deal.Count; card++)
                        {
                            var item = currentBoard.Distribution.Deal[card];
                            await this.HandleCardPosition(item.Seat, item.Suit, item.Rank);
                        }

                        await this.HandleCardDealingEnded();
                    }
                }
            } while (alreadyPlayed);
        }

        private async ValueTask GetNextBoard()
        {
            int played;
            played = HasBeenPlayed(this.currentBoard);
            //if (this.mode == HostMode.SingleTableInstantReplay && played == 1)
            //{
            //    Log.Trace(4, "TMController.GetNextBoard instant replay this board; rotate hands");
            //    this.rotateHands = true;
            //}
            //else
            {
                this.rotateHands = false;
                this.boardNumber++;
                this.currentBoard = await this.tournament.GetNextBoardAsync(this.boardNumber, this.participant.UserId).ConfigureAwait(false);
            }

            int HasBeenPlayed(Board2 board)
            {
                if (board == null) return -1;
                var played = 0;
                foreach (var result in board.Results)
                {
                    if (result.Auction.Ended)
                    {
                        if (TournamentHost.HasBeenPlayedBy(result, this.teamNames[Directions.NorthSouth], this.teamNames[Directions.EastWest])) played++;
                        else if (TournamentHost.HasBeenPlayedBy(result, this.teamNames[Directions.EastWest], this.teamNames[Directions.NorthSouth])) played++;
                    }
                }

                return played;
            }
        }

        private bool BoardHasBeenPlayedBy(Board2 board, string team1, string team2)
        {
            foreach (var result in board.Results)
            {
                if (result.Auction.Ended)
                {
                    if (TournamentHost.HasBeenPlayedBy(result, team1, team2))
                    {
                        Log.Trace(4, $"TournamentController: board {this.currentBoard.BoardNumber.ToString()} has already been played by {team1}-{team2}");
                        return true;
                    }
                }
            }
            Log.Trace(4, $"TournamentController: board {this.currentBoard.BoardNumber.ToString()} has not yet been played by {team1}-{team2}");
            return false;
        }

        private static bool HasBeenPlayedBy(BoardResult result, string team1, string team2)
        {
            return result.Participants.Names[Seats.North] == team1 && result.Participants.Names[Seats.East] == team2;
        }
    }

    public abstract class AsyncHost : AsyncBridgeEventRecorder
    {
        private readonly AsyncHostProtocol communicator;
        private readonly DirectionDictionary<TimeSpan> BoardThinkTime = new(new TimeSpan(), new TimeSpan());
        private readonly DirectionDictionary<TimeSpan> TotalThinkTime = new(new TimeSpan(), new TimeSpan());
        private readonly DirectionDictionary<DateTimeOffset> StartThinkTime = new(DateTimeOffset.MaxValue, DateTimeOffset.MaxValue);
        protected readonly DirectionDictionary<string> teamNames;
        protected readonly CancellationTokenSource cts = new();

        public AsyncHost(AsyncHostProtocol _communicator, string teamNS, string teamEW) : base("Host")
        {
            teamNames = new(teamNS, teamEW);
            communicator = _communicator;
            communicator.SetOwner(this);
            AddEventHandler(communicator);
        }

        public ValueTask Start()
        {
            communicator.Start();
            return default;
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

        public bool IsSeatOk(Seats seat, string teamName, out string reason)
        {
            reason = "";
            if (string.IsNullOrWhiteSpace(teamNames[seat.Direction()]))
            {
                teamNames[seat.Direction()] = teamName;
                return true;
            }

            if (string.Equals(teamName, teamNames[seat.Direction()], StringComparison.InvariantCultureIgnoreCase)) return true;

            reason = $"{seat} should be team {teamNames[seat.Direction()]}";
            return false;
        }

        #region bridge events

        public override async ValueTask HandleTournamentStarted(Scorings scoring, int maxTimePerBoard, int maxTimePerCard, string tournamentName)
        {
            TotalThinkTime[Directions.NorthSouth] = TimeSpan.Zero;
            TotalThinkTime[Directions.EastWest] = TimeSpan.Zero;
            await base.HandleTournamentStarted(scoring, maxTimePerBoard, maxTimePerCard, tournamentName);
        }

        public override async ValueTask HandleRoundStarted(SeatCollection<string> participantNames, DirectionDictionary<string> conventionCards)
        {
            await base.HandleRoundStarted(new SeatCollection<string>([ teamNames[Directions.NorthSouth], teamNames[Directions.EastWest], teamNames[Directions.NorthSouth], teamNames[Directions.EastWest]]), teamNames);
        }

        public override async ValueTask HandleBoardStarted(int boardNumber, Seats dealer, Vulnerable vulnerabilty)
        {
            BoardThinkTime[Directions.NorthSouth] = TimeSpan.Zero;
            BoardThinkTime[Directions.EastWest] = TimeSpan.Zero;
            await base.HandleBoardStarted(boardNumber, dealer, vulnerabilty);
        }

        public override async ValueTask HandleCardPosition(Seats seat, Suits suit, Ranks rank)
        {
            await base.HandleCardPosition(seat, suit, rank);
        }

        public override async ValueTask HandleBidNeeded(Seats whoseTurn, Bid lastRegularBid, bool allowDouble, bool allowRedouble)
        {
            StartThinkTime[whoseTurn.Direction()] = DateTimeOffset.UtcNow;
            await base.HandleBidNeeded(whoseTurn, lastRegularBid, allowDouble, allowRedouble);
        }

        public override async ValueTask HandleBidDone(Seats source, AuctionBid bid, DateTimeOffset when)
        {
            var elapsed = when.Subtract(StartThinkTime[source.Direction()]);
            if (elapsed.Ticks > 0) BoardThinkTime[source.Direction()] += elapsed;
            await base.HandleBidDone(source, bid, when);
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
            await base.HandleAuctionFinished(declarer, finalContract);
            await this.HandleCardNeeded(this.Play.whoseTurn == this.Play.Dummy ? this.Auction.Declarer : this.Play.whoseTurn, this.Play.whoseTurn, this.Play.Trump, this.Play.leadSuit, true, 0, this.Play.currentTrick);
        }

        public override async ValueTask HandleCardNeeded(Seats controller, Seats whoseTurn, Suits leadSuit, Suits trump, bool trumpAllowed, int leadSuitLength, int trick)
        {
            StartThinkTime[whoseTurn.Direction()] = DateTimeOffset.UtcNow;
            await base.HandleCardNeeded(controller, whoseTurn, leadSuit, trump, trumpAllowed, leadSuitLength, trick);
        }

        public override async ValueTask HandleCardPlayed(Seats source, Suits suit, Ranks rank, string signal, DateTimeOffset when)
        {
            var elapsed = when.Subtract(StartThinkTime[source.Direction()]);
            if (elapsed.Ticks > 0) BoardThinkTime[source.Direction()] += elapsed;
            await base.HandleCardPlayed(source, suit, rank, signal, when);
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
            await base.HandleTrickFinished(trickWinner, tricksForDeclarer, tricksForDefense);
            await this.HandleCardNeeded(this.Play.whoseTurn == this.Play.Dummy ? this.Auction.Declarer : this.Play.whoseTurn, this.Play.whoseTurn, this.Play.Trump, this.Play.leadSuit, true, 0, this.Play.currentTrick);
        }

        public override async ValueTask HandlePlayFinished(TimeSpan boardByNS, TimeSpan totalByNS, TimeSpan boardByEW, TimeSpan totalByEW)
        {
            TotalThinkTime[Directions.NorthSouth] += BoardThinkTime[Directions.NorthSouth];
            TotalThinkTime[Directions.EastWest] += BoardThinkTime[Directions.EastWest];
            Log.Trace(1, $"board: {BoardThinkTime[Directions.NorthSouth].TotalSeconds}s {BoardThinkTime[Directions.EastWest].TotalSeconds}s, total: {TotalThinkTime[Directions.NorthSouth].TotalMinutes:F1}m {TotalThinkTime[Directions.EastWest].TotalMinutes:F1}m");
            await base.HandlePlayFinished(BoardThinkTime[Directions.NorthSouth], TotalThinkTime[Directions.NorthSouth], BoardThinkTime[Directions.EastWest], TotalThinkTime[Directions.EastWest]);
        }

        public override ValueTask HandleTournamentStopped()
        {
            return base.HandleTournamentStopped();
        }

        public override async ValueTask HandleCardDealingEnded()
        {
            await base.HandleCardDealingEnded();
            await this.HandleBidNeeded(this.Auction.WhoseTurn, this.Auction.LastRegularBid, this.Auction.AllowDouble, this.Auction.AllowRedouble);
        }

        //public override ValueTask HandleExplanationDone(Seats source, Bid bid)
        //{
        //    return base.HandleExplanationDone(source, bid);
        //}

        //public override ValueTask HandleExplanationNeeded(Seats source, Bid bid)
        //{
        //    return base.HandleExplanationNeeded(source, bid);
        //}

        //public override ValueTask HandleNeedDummiesCards(Seats dummy)
        //{
        //    return base.HandleNeedDummiesCards(dummy);
        //}

        //public override ValueTask HandleShowDummy(Seats dummy)
        //{
        //    return base.HandleShowDummy(dummy);
        //}

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
        protected AsyncHost Owner;

        public abstract void Start();

        public void SetOwner(AsyncHost _owner)
        {
            Owner = _owner;
        }

        public async ValueTask AllSeatsTaken(CancellationToken cancellationToken)
        {
            try
            {
                await this.allSeatsFilled.WaitAsync(cancellationToken).ConfigureAwait(false);
                Log.Trace(4, $"{NameForLog}.AllSeatsTaken done");
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

        public override ValueTask Finish()
        {
            return default;
        }

        public ValueTask Connect(Seats seat, AsyncClientProtocol clientCommunicator)
        {
            Log.Trace(4, $"{NameForLog}.Connect: {seat}");
            seats[seat] = clientCommunicator;
            seatsTaken++;
            if (seatsTaken >= 4) allSeatsFilled.Release();
            return default;
        }

        private void SendToAll(Func<AsyncClientProtocol, ValueTask> action, Seats except = Seats.Null)
        {
            foreach (var seat in SeatsExtensions.SeatsAscending)
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

        public override async ValueTask HandleBidDone(Seats source, AuctionBid bid, DateTimeOffset when)
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

        public override async ValueTask HandleCardPlayed(Seats source, Suits suit, Ranks rank, string signal, DateTimeOffset when)
        {
            Log.Trace(3, $"{NameForLog}.HandleCardPlayed");
            if (source != whoToWaitFor) throw new Exception($"Expected a card from {whoToWaitFor} but received a card from {source}");
            await base.HandleCardPlayed(source, suit, rank, signal, when);
            SendToAll(async seat =>
            {
                await seat.HandleCardPlayed(source, suit, rank, signal, when);
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
            await default(ValueTask);
        }

        //public override async ValueTask HandleNeedDummiesCards(Seats dummy)
        //{
        //    Log.Trace(3, $"{NameForLog}.HandleNeedDummiesCards");
        //    SendToAll(async seat =>
        //    {
        //        await seat.HandleNeedDummiesCards(dummy);
        //    });
        //    await default(ValueTask);
        //}

        //public override async ValueTask HandleShowDummy(Seats dummy)
        //{
        //    Log.Trace(3, $"{NameForLog}.HandleShowDummy");
        //    SendToAll(async seat =>
        //    {
        //        await seat.HandleShowDummy(dummy);
        //    });
        //    await default(ValueTask);
        //}

        #endregion
    }

    public class HostComputerBridgeProtocol : AsyncHostProtocol
    {
        private readonly SeatCollection<int> ClientIds = new();
        private readonly SeatCollection<Queue<TimedMessage>> messages = new();
        private readonly SeatCollection<SemaphoreSlim> answerReceived = new();
        private readonly CommunicationHost communicationHost;
        private readonly SeatCollection<bool> CanAskForExplanation;
        private readonly SeatCollection<bool> CanReceiveExplanations;

        public HostComputerBridgeProtocol(CommunicationHost _communicationHost) : base("HostComputerBridgeProtocol")
        {
            communicationHost = _communicationHost;
            communicationHost.ProcessMessage = this.ProcessMessage;
            this.CanAskForExplanation = new SeatCollection<bool>();
            this.CanReceiveExplanations = new SeatCollection<bool>();
        }

        private struct TimedMessage
        {
            public string Message { get; set; }
            public DateTimeOffset When { get; set; }
        }

        public override void Start()
        {
            foreach (var seat in SeatsExtensions.SeatsAscending)
            {
                messages[seat] = new();
                answerReceived[seat] = new(0);
            }
        }

        private async ValueTask ProcessMessage(int clientId, string message)
        {
            Log.Trace(5, $"{NameForLog}.Process '{message}' from client {clientId}");
            var clientSeat = Seats.Null;
            foreach (var seat in SeatsExtensions.SeatsAscending)
            {
                if (ClientIds[seat] == clientId)
                {
                    clientSeat = seat;
                    break;
                }
            }

            if (clientSeat == Seats.Null)
            {   // new client
                Log.Trace(1, $"Host received '{message}' from new client {clientId}");
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

#if NET6_0_OR_GREATER
                    var seat2 = SeatsExtensions.FromXML(hand[0..1].ToUpperInvariant());
#else
                    var seat2 = SeatsExtensions.FromXML(hand.Substring(0, 2).ToUpperInvariant());
#endif
                    int p = message.IndexOf('\"');
#if NET6_0_OR_GREATER
                    var teamName = message[(p + 1)..message.IndexOf('\"', p + 1)];
#else
                    var teamName = message.Substring(p + 1, message.IndexOf('\"', p + 1) - p - 1);
#endif
                    //if (this.teams[seat.Next()] == teamName || this.teams[seat.Previous()] == teamName) return new ConnectResponse(Seats.North - 1, $"Team name must differ from opponents team name '{(this.teams[seat.Next()].Length > 0 ? this.teams[seat.Next()] : this.teams[seat.Previous()])}'");
                    //if (this.teams[seat].Length > 0 && this.teams[seat].ToLower() != teamName.ToLower()) return new ConnectResponse(Seats.North - 1, $"Team name must be '{this.teams[seat]}'");
                    //this.teams[seat] = teamName;
                    var protocolVersion = 18;
                    p = message.IndexOf(" version ");
                    if (p >= 0)
                    {
                        protocolVersion = int.Parse(message.Substring(p + 9, 2));
                    }
                    switch (protocolVersion)
                    {
                        case 18:
                            this.CanAskForExplanation[seat2] = false;
                            this.CanReceiveExplanations[seat2] = false;
                            break;
                        case 19:
                            this.CanAskForExplanation[seat2] = false;
                            this.CanReceiveExplanations[seat2] = true;
                            break;
                        default:
                            throw new ArgumentException($"protocol version {protocolVersion} not supported");
                    }

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
                        if (Owner.IsSeatOk(seat2, teamName, out var reason))
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
                            await communicationHost.Send(reason, clientId).ConfigureAwait(false);
                            return;
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

        private void SendToAll(string message, Seats except1 = Seats.Null, Seats except2 = Seats.Null)
        {
            foreach (var seat in SeatsExtensions.SeatsAscending)
            {
                if (seat != except1 && seat != except2)
                {
                    SendTo(message, seat);
                }
            }
        }

        private async ValueTask SendToAllAndWait(string message, Seats except1 = Seats.Null, Seats except2 = Seats.Null)
        {
            foreach (var seat in SeatsExtensions.SeatsAscending)
            {
                if (seat != except1 && seat != except2)
                {
                    SendTo(message, seat);
                }
            }

            foreach (var seat in SeatsExtensions.SeatsAscending.ToArray())
            {
                if (seat != except1 && seat != except2)
                {
                    await waiter[seat];
                }
            }
        }

        private void SendTo(string message, Seats seat)
        {
            waiter[seat] = communicationHost.Send(message, ClientIds[seat]);
        }

        private async ValueTask AllAnswered(string expectedAnswer, Seats except = Seats.Null, Seats dummy = Seats.Null)
        {
            Log.Trace(3, $"{NameForLog}.AllAnswered waiting for '{expectedAnswer}'");
            foreach (var seat in SeatsExtensions.SeatsAscending.ToArray())
            {
                if (seat != except)
                {
                    var answer = await Answered(expectedAnswer, seat, dummy).ConfigureAwait(false);
                }
            }
            Log.Trace(3, $"{NameForLog}.AllAnswered '{expectedAnswer}'");
        }

        private async ValueTask<TimedMessage> Answered(string expectedAnswer, Seats seat, Seats dummy = Seats.Null)
        {
            Log.Trace(4, $"{NameForLog}.Answered waiting for {seat}");
            await answerReceived[seat].WaitAsync().ConfigureAwait(false);
            var answer = messages[seat].Dequeue();
#if NET6_0_OR_GREATER
            if (!answer.Message.Contains(expectedAnswer, StringComparison.InvariantCultureIgnoreCase))
#else
            if (!answer.Message.Contains(expectedAnswer, StringComparison.InvariantCultureIgnoreCase))
#endif
            {
#if NET6_0_OR_GREATER
#else
#endif
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
            Log.Trace(3, $"{NameForLog}.HandleBoardStarted");
            await base.HandleBoardStarted(boardNumber, _dealer, vulnerabilty).ConfigureAwait(false);
            //await Task.Delay(10);       // to prevent clients missing a message
            SendToAll("Start of board");
            await AllAnswered("ready for deal").ConfigureAwait(false);
            SendToAll($"Board number {boardNumber}. Dealer {_dealer}. {ProtocolHelper.Translate(vulnerabilty)} vulnerable.");
            await AllAnswered("ready for cards").ConfigureAwait(false);
        }

        public override async ValueTask HandleCardPosition(Seats seat, Suits suit, Ranks rank)
        {
            Log.Trace(3, $"{NameForLog}.HandleCardPosition: {seat.ToXML()} gets {rank.ToXML()}{suit.ToXML().ToLower()}");
            await base.HandleCardPosition(seat, suit, rank).ConfigureAwait(false);
        }

        public override async ValueTask HandleCardDealingEnded()
        {
            Log.Trace(3, $"{NameForLog}.HandleCardDealingEnded");
            await base.HandleCardDealingEnded().ConfigureAwait(false);
            foreach (var seat in SeatsExtensions.SeatsAscending)
            {
                var message = seat.ToString() + ProtocolHelper.Translate(seat, this.Distribution);
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
            await Owner.HandleBidDone(whoseTurn, bid, answer.When).ConfigureAwait(false);
        }

        public override async ValueTask HandleBidDone(Seats source, AuctionBid bid, DateTimeOffset when)
        {
            Log.Trace(3, $"{NameForLog}.HandleBidDone");
            await base.HandleBidDone(source, bid, when).ConfigureAwait(false);
            foreach (var seat in SeatsExtensions.SeatsAscending)
            {
                if (seat != source)
                {
                    var message = ProtocolHelper.Translate(bid, source, seat == source.Partner() || !CanReceiveExplanations[seat], AlertMode.SelfExplaining);
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
            await AllAnswered($"ready for {whoseTurn}'s card to trick {trick}", controller, trick > 1 || Play.man > 1 ? Dummy : Seats.Null).ConfigureAwait(false);
            if (Play.man == 1)
            {
                var whoToLead = controller;
                //await Task.Delay(300);      // give some time to process the previous message '.. plays ..' ( this is the only(?) protocol message that is sent directly after another message without receiving some confirmation message first)
                SendTo($"{(whoseTurn == this.Play.Dummy ? "Dummy" : whoseTurn.ToXMLFull())} to lead", whoToLead);
            }

            var answer = await Answered($"{whoseTurn} plays ", controller).ConfigureAwait(false);
            var card = ProtocolHelper.TranslateCard(answer.Message, out var player, out var signal);
            await Owner.HandleCardPlayed(player, card.Suit, card.Rank, signal, answer.When).ConfigureAwait(false);
        }

        public override async ValueTask HandleCardPlayed(Seats source, Suits suit, Ranks rank, string signal, DateTimeOffset when)
        {
            Log.Trace(3, $"{NameForLog}.HandleCardPlayed");
            await base.HandleCardPlayed(source, suit, rank, signal, when).ConfigureAwait(false);

            var message = $"{source} plays {rank.ToXML()}{suit.ToXML()}";
            foreach (var seat in SeatsExtensions.SeatsAscending)
            {
                if (seat != (source == Play.Dummy ? source.Partner() : source))
                {
                    SendTo($"{message}{(signal.Length > 0 && !source.IsSameDirection(seat) && this.CanReceiveExplanations[seat] ? $". {signal}" : "")}", seat);
                }
            }

            foreach (var seat in SeatsExtensions.SeatsAscending.ToArray())
            {
                if (seat != (source == Play.Dummy ? source.Partner() : source))
                {
                    await waiter[seat];
                }
            }

            if (Play.currentTrick == 1 && Play.man == 2)
            {
                await AllAnswered($"ready for dummy", Dummy).ConfigureAwait(false);
                var cards = "Dummy" + ProtocolHelper.Translate(Dummy, Distribution);
                await SendToAllAndWait(cards, Play.Dummy).ConfigureAwait(false);
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
            await SendToAllAndWait(timingInfo).ConfigureAwait(false);
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

        public async ValueTask Connect(string systemInfo)
        {
            await communicator.Connect(systemInfo);
        }

        public override async ValueTask Finish()
        {
            await communicator.Finish();
        }
    }

    public abstract class AsyncClientProtocol(string nameForLog) : AsyncBridgeEventRecorder(nameForLog)
    {
        public AsyncClient owner;

        public abstract ValueTask Connect(string systemInfo);

        public abstract ValueTask SendBid(AuctionBid bid);

        public abstract ValueTask SendCard(Seats source, Card card, string signal);
    }

    public class ClientInProcessProtocol(HostInProcessProtocol _host) : AsyncClientProtocol("ClientInProcessProtocol")
    {
        private readonly HostInProcessProtocol host = _host;

        public override async ValueTask Connect(string systemInfo)
        {
            await host.Connect(owner.seat, this);
        }

        public override ValueTask SendBid(AuctionBid bid)
        {
            return host.HandleBidDone(owner.seat, bid, DateTimeOffset.UtcNow);
        }

        public override ValueTask SendCard(Seats source, Card card, string signal)
        {
            return host.HandleCardPlayed(source, card.Suit, card.Rank, signal, DateTimeOffset.UtcNow);
        }

        public override async ValueTask Finish()
        {
            await default(ValueTask);
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

        public override async ValueTask HandleBidDone(Seats source, AuctionBid bid, DateTimeOffset when)
        {
            Log.Trace(3, $"AsyncClientCommunication.{owner.seat}.HandleBidDone");
            await owner.HandleBidDone(source, bid, when);
        }

        //public override async ValueTask HandleExplanationNeeded(Seats source, Bid bid)
        //{
        //    Log.Trace(3, $"AsyncClientCommunication.{owner.seat}.HandleExplanationNeeded");
        //    await owner.HandleExplanationNeeded(source, bid);
        //}

        //public override async ValueTask HandleExplanationDone(Seats source, Bid bid)
        //{
        //    Log.Trace(3, $"AsyncClientCommunication.{owner.seat}.HandleExplanationDone");
        //    await owner.HandleExplanationDone(source, bid);
        //}

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

        public override async ValueTask HandleCardPlayed(Seats source, Suits suit, Ranks rank, string signal, DateTimeOffset when)
        {
            Log.Trace(3, $"AsyncClientCommunication.{owner.seat}.HandleCardPlayed");
            await owner.HandleCardPlayed(source, suit, rank, signal, when);
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

        //public override async ValueTask HandleNeedDummiesCards(Seats dummy)
        //{
        //    Log.Trace(3, $"AsyncClientCommunication.{owner.seat}.HandleNeedDummiesCards");
        //    await owner.HandleNeedDummiesCards(dummy);
        //}

        //public override async ValueTask HandleShowDummy(Seats dummy)
        //{
        //    Log.Trace(3, $"AsyncClientCommunication.{owner.seat}.HandleShowDummy");
        //    await owner.HandleShowDummy(dummy);
        //}

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

        public override async ValueTask Connect(string systemInfo)
        {
            await communicationClient.Connect();
            communicationClient.Start();
            expectedAnswer = $"{owner.seat} (\"{teamName}\") seated";
            await communicationClient.Send($"Connecting \"{this.teamName}\" as {owner.seat} using protocol version {protocolVersion:00} {systemInfo}");
        }

        public override async ValueTask SendBid(AuctionBid bid)
        {
            await communicationClient.Send(ProtocolHelper.Translate(bid, owner.seat, false, AlertMode.SelfExplaining));
            await this.HandleBidDone(this.owner.seat, bid, DateTimeOffset.UtcNow);
        }

        public override async ValueTask SendCard(Seats source, Card card, string signal)
        {
            await communicationClient.Send($"{source} plays {card.Rank.ToXML()}{card.Suit.ToXML()}{(signal.Length > 0 ? $". {signal}" : "")}");
            await this.HandleCardPlayed(source, card.Suit, card.Rank, signal, DateTimeOffset.UtcNow);
        }

        private async ValueTask Receive(string message)
        {
            Log.Trace(1, $"{owner.seat,-5} received '{message}'");

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
                        await owner.HandleBidNeeded(whoseTurn, Bid.GetPass(), false, false);
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
                    var card = ProtocolHelper.TranslateCard(message, out var player, out var signal);
                    await owner.HandleCardPlayed(player, card.Suit, card.Rank, signal, DateTimeOffset.UtcNow);
                    await this.HandleCardPlayed(player, card.Suit, card.Rank, signal, DateTimeOffset.UtcNow);
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

        public override async ValueTask HandleBidDone(Seats source, AuctionBid bid, DateTimeOffset when)
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

        public override async ValueTask HandleCardPlayed(Seats source, Suits suit, Ranks rank, string signal, DateTimeOffset when)
        {
            await base.HandleCardPlayed(source, suit, rank, signal, when);

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
}
