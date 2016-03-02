using System;
using Sodes.Bridge.Base;
using System.Threading.Tasks;
using BridgeNetworkProtocol2;
using Sodes.Base;
using System.Collections.Concurrent;
using System.IO;

namespace Sodes.Bridge.Networking
{
	public enum HostEvents { Seated, ReadyForTeams, ReadyToStart, ReadyForDeal, ReadyForCards, ReadyForDummiesCards }
    public delegate void HandleHostEvent(TableManagerHost sender, HostEvents hostEvent, Seats seat, string message);

    /// <summary>
    /// Implementation of the server side of the Bridge Network Protocol
    /// as described in http://www.bluechipbridge.co.uk/protocol.htm
    /// </summary>
    public abstract class TableManagerHost : BridgeEventBusClient
	{
		internal class ClientData
		{
			public TableManagerProtocolState state;
			public string teamName;
			public string hand;
            public ConcurrentQueue<ClientMessage> messages;
            private bool _pause;
            public bool Pause
            {
                get { return _pause; }
                set
                {
                    if (value != _pause)
                    {
                        _pause = value;
                        //Log.Trace("Host {1} {0}", hand, _pause ? "pauses" : "resumes");
                    }
                }
            }
		}

        public class ClientMessage
		{
			public Seats Seat { get; set; }
			public string Message { get; set; }
			public ClientMessage(Seats s, string m)
			{
				this.Seat = s;
				this.Message = m;
			}
		}

		public event HandleHostEvent OnHostEvent;

		internal SeatCollection<ClientData> clients;
        private string lastRelevantMessage;
        private bool moreBoards;
        private bool allReadyForStartOfBoard;
        private bool allReadyForDeal;
        private TournamentController c;

        protected TableManagerHost(BridgeEventBus bus) : base(bus, "TableManagerHost")
		{
			this.clients = new SeatCollection<ClientData>();
			for (Seats seat = Seats.North; seat <= Seats.West; seat++)
			{
				this.clients[seat] = new ClientData();
				this.clients[seat].state = TableManagerProtocolState.Initial;
                this.clients[seat].Pause = false;
                this.clients[seat].messages = new ConcurrentQueue<ClientMessage>();
            }

            this.moreBoards = true;
            Task.Run(async () =>
            {
                await this.ProcessMessages();
            });
        }

        public void HostTournament(string pbnTournament)
        {
            var t = TournamentLoader.LoadAsync(File.OpenRead(pbnTournament)).Result;
            this.c = new TMController(this, t, new ParticipantInfo() { ConventionCardNS = this.clients[Seats.North].teamName, ConventionCardWE = this.clients[Seats.East].teamName, MaxThinkTime = 120, UserId = Guid.NewGuid(), PlayerNames = new Participant(this.clients[Seats.North].teamName, this.clients[Seats.East].teamName, this.clients[Seats.North].teamName, this.clients[Seats.East].teamName) }, this.EventBus);
            this.allReadyForStartOfBoard = false;
            this.c.StartTournament();
        }

        protected void ProcessIncomingMessage(string message, Seats seat)
		{
			lock (this.clients[seat].messages)
			{
				this.clients[seat].messages.Enqueue(new ClientMessage(seat, message));
                //Log.Trace("Host queued {0}'s '{1}' ({2} messages on q)", seat, message, this.clients[seat].messages.Count);
            }
		}

		private async Task ProcessMessages()
		{
            const int minimumWait = 10;
            var waitForNewMessage = minimumWait;
			ClientMessage m = null;
			do
			{
                waitForNewMessage *= 2;
                for (Seats seat = Seats.North; seat <= Seats.West; seat++)
                {
                    if (!this.clients[seat].Pause && !this.clients[seat].messages.IsEmpty)
                    {
                        lock (this.clients[seat].messages)
                        {
                            this.clients[seat].messages.TryDequeue(out m);
                        }

                        if (m != null)
                        {
                            //Log.Trace("Host dequeued {0}'s '{1}'", m.Seat, m.Message);
                            waitForNewMessage = minimumWait;
                            lock (this.clients)
                            {       // ensure exclusive access to ProcessMessage
                                this.ProcessMessage(m.Message, m.Seat);
                            }
                        }
                    }
                }

                if (waitForNewMessage > minimumWait)
                {
                    //Log.Trace("Host out of messages");
                    await Task.Delay(waitForNewMessage);
                }
            } while (this.moreBoards);
		}

        private void DumpQueue()
        {
            Log.Trace("Host remaining messages on queue:");
            for (Seats seat = Seats.North; seat <= Seats.West; seat++)
            {
                var more = true;
                while (more)
                {
                    ClientMessage m = null;
                    this.clients[seat].messages.TryDequeue(out m);
                    if (m == null)
                    {
                        more = false;
                    }
                    else
                    { 
                        Log.Trace("Host queue item {0} '{1}'", m.Seat, m.Message);
                    }
                }
            }
        }

		private void ProcessMessage(string message, Seats seat)
		{
			//Log.Trace("Host processing '{0}'", message);
			switch (this.clients[seat].state)
			{
				case TableManagerProtocolState.Initial:
					if (message.ToLowerInvariant().Contains("connecting") && message.ToLowerInvariant().Contains("using protocol version"))
					{
						int p = message.IndexOf("\"");
						this.clients[seat].teamName = message.Substring(p + 1, message.IndexOf("\"", p + 1) - (p + 1));
						this.clients[seat].hand = seat.ToString();		//.Substring(0, 1)
						this.WriteData(seat, "{1} (\"{0}\") seated", this.clients[seat].teamName, this.clients[seat].hand);
						this.clients[seat].state = TableManagerProtocolState.WaitForSeated;
					}
					else
					{
						this.Refuse(seat, "Expected 'Connecting ....'");
					}
					break;

				case TableManagerProtocolState.WaitForSeated:
					ChangeState(message, this.clients[seat].hand + " ready for teams", TableManagerProtocolState.WaitForTeams, seat);
					break;

				case TableManagerProtocolState.WaitForTeams:
					ChangeState(message, this.clients[seat].hand + " ready to start", TableManagerProtocolState.WaitForStartOfBoard, seat);
					break;

				case TableManagerProtocolState.WaitForStartOfBoard:
					ChangeState(message, this.clients[seat].hand + " ready for deal", TableManagerProtocolState.WaitForBoardInfo, seat);
					break;

				case TableManagerProtocolState.WaitForBoardInfo:
					ChangeState(message, this.clients[seat].hand + " ready for cards", TableManagerProtocolState.WaitForMyCards, seat);
					this.OnHostEvent(this, HostEvents.ReadyForCards, seat, string.Empty);
					break;

				case TableManagerProtocolState.WaitForMyCards:
                    lock (this.clients) this.clients[seat].Pause = true;
                    //Log.Trace("Host pauses {0}", seat);
                    if (seat == this.c.currentResult.Auction.WhoseTurn)
                    {
                        if (message.Contains(" ready for "))
                        {
                            Log.Trace("Host expected '... bids ..' from {0}", seat);
                            this.DumpQueue();
                            throw new InvalidOperationException();
                        }

                        this.lastRelevantMessage = message;
                        //Log.Trace("Host lastRelevantMessage={0}", message);
                        ChangeState(message, this.clients[seat].hand + " ", TableManagerProtocolState.WaitForOtherBid, seat);
                    }
                    else
                    {
                        ChangeState(message, string.Format("{0} ready for {1}'s bid", this.clients[seat].hand, this.c.currentResult.Auction.WhoseTurn), TableManagerProtocolState.WaitForOtherBid, seat);
                    }
                    break;

                case TableManagerProtocolState.WaitForCardPlay:
                    lock (this.clients) this.clients[seat].Pause = true;
                    //Log.Trace("Host pauses {0}", seat);
                    // ready for dummy's card mag ook ready for xx's card
                    if (this.c.currentResult.Play.whoseTurn == this.c.currentResult.Play.Dummy && seat == this.c.currentResult.Play.Dummy)
					{
						ChangeState(message, string.Format("{0} ready for dummy's card to trick {2}", this.clients[seat].hand, message.Contains("dummy") ? "dummy" : this.c.currentResult.Play.whoseTurn.ToString(), this.c.currentResult.Play.currentTrick), TableManagerProtocolState.WaitForOtherCardPlay, seat);
					}
					else
					{
						ChangeState(message, string.Format("{0} ready for {1}'s card to trick {2}", this.clients[seat].hand, this.c.currentResult.Play.whoseTurn.ToString(), this.c.currentResult.Play.currentTrick), TableManagerProtocolState.WaitForOtherCardPlay, seat);
					}
					break;

				case TableManagerProtocolState.WaitForOwnCardPlay:
                    lock (this.clients) this.clients[seat].Pause = true;
                    //Log.Trace("Host pauses {0}", seat);
                    this.lastRelevantMessage = message;
                    //Log.Trace("Host lastRelevantMessage={0}", message);
                    ChangeState(message, string.Format("{0} plays ", this.c.currentResult.Play.whoseTurn), TableManagerProtocolState.WaitForOtherCardPlay, seat);
                    break;

				case TableManagerProtocolState.WaitForDummiesCardPlay:
					ChangeState(message, string.Format("{0} plays ", this.c.currentResult.Play.whoseTurn), TableManagerProtocolState.WaitForOtherCardPlay, seat);
					break;

				case TableManagerProtocolState.WaitForDummiesCards:
					ChangeState(message, string.Format("{0} ready for dummy", this.clients[seat].hand), TableManagerProtocolState.GiveDummiesCards, seat);
					break;

				default:
                    Log.Trace("Host unexpected '{0}' from {1} in state {2}", message, seat, this.clients[seat].state);
                    this.DumpQueue();
					this.Refuse(seat, "Unexpected '{0}' in state {1}", message, this.clients[seat].state);
                    throw new InvalidOperationException(string.Format("Unexpected '{0}' in state {1}", message, this.clients[seat].state));
			}
		}

		private void ChangeState(string message, string expected, TableManagerProtocolState newState, Seats seat)
		{
			if (message.ToLowerInvariant().StartsWith(expected.ToLowerInvariant()))
			{
				this.clients[seat].state = newState;
                var allReady = true;
                for (Seats s = Seats.North; s <= Seats.West; s++) if (this.clients[s].state != newState) allReady = false;
                if (allReady)
                {
                    //Log.Trace("Host ChangeState {0}", newState);
                    switch (newState)
                    {
                        case TableManagerProtocolState.Initial:
                            break;
                        case TableManagerProtocolState.WaitForSeated:
                            break;
                        case TableManagerProtocolState.WaitForTeams:
                            this.BroadCast("Teams : N/S : \"" + this.clients[Seats.North].teamName + "\". E/W : \"" + this.clients[Seats.East].teamName + "\"");
                            this.OnHostEvent(this, HostEvents.ReadyForTeams, Seats.North, "");
                            break;
                        case TableManagerProtocolState.WaitForStartOfBoard:
                            this.c.StartNextBoard().Wait();
                            break;
                        case TableManagerProtocolState.WaitForBoardInfo:
                            this.BroadCast("Board number {0}. Dealer {1}. {2} vulnerable.", this.c.currentResult.Board.BoardNumber, this.c.currentResult.Board.Dealer.ToXMLFull(), ProtocolHelper.Translate(this.c.currentResult.Board.Vulnerable));
                            break;
                        case TableManagerProtocolState.WaitForMyCards:
                            this.WriteData(Seats.North, ProtocolHelper.Translate(Seats.North, this.c.currentResult.Distribution));
                            this.WriteData(Seats.East, ProtocolHelper.Translate(Seats.East, this.c.currentResult.Distribution));
                            this.WriteData(Seats.South, ProtocolHelper.Translate(Seats.South, this.c.currentResult.Distribution));
                            this.WriteData(Seats.West, ProtocolHelper.Translate(Seats.West, this.c.currentResult.Distribution));
                            break;
                        case TableManagerProtocolState.WaitForCardPlay:
                            break;
                        case TableManagerProtocolState.WaitForOtherBid:
                            //Log.Trace("TableManagerHost.ChangeState all WaitForOtherBid");
                            for (Seats s = Seats.North; s <= Seats.West; s++) this.clients[s].state = TableManagerProtocolState.WaitForCardPlay;
                            ProtocolHelper.HandleProtocolBid(this.lastRelevantMessage, this.EventBus);
                            break;
                        case TableManagerProtocolState.WaitForOtherCardPlay:
                            //Log.Trace("TableManagerHost.ChangeState all WaitForOtherCardPlay");
                            ProtocolHelper.HandleProtocolPlay(this.lastRelevantMessage, this.EventBus);
                            break;
                        case TableManagerProtocolState.WaitForOwnCardPlay:
                            //Log.Trace("TableManagerHost.ChangeState all WaitForOwnCardPlay");
                            break;
                        case TableManagerProtocolState.WaitForDummiesCardPlay:
                            break;
                        case TableManagerProtocolState.GiveDummiesCards:
                            var cards = ProtocolHelper.Translate(this.c.currentResult.Play.Dummy, this.c.currentResult.Distribution).Replace(this.c.currentResult.Play.Dummy.ToXMLFull(), "Dummy");
                            for (Seats s = Seats.North; s <= Seats.West; s++)
                            {
                                if (s != this.c.currentResult.Play.Dummy)
                                {
                                    this.WriteData(s, cards);
                                }
                            }
                            for (Seats s = Seats.North; s <= Seats.West; s++)
                            {
                                this.clients[s].state = (s == this.c.currentResult.Auction.Declarer ? TableManagerProtocolState.WaitForOwnCardPlay : TableManagerProtocolState.WaitForCardPlay);
                                lock (this.clients) this.clients[s].Pause = false;
                            }
                            //this.EventBus.HandleShowDummy(this.c.currentResult.Play.Dummy);
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
                Log.Trace("Host expected '{0}'", expected);
                this.DumpQueue();
                this.Refuse(seat, "Expected '{0}'", expected);
                throw new InvalidOperationException(string.Format("Expected '{0}'", expected));
            }
        }

        public abstract void WriteData(Seats seat, string message, params object[] args);

		public virtual void Refuse(Seats seat, string reason, params object[] args)
		{
			this.WriteData(seat, reason, args);
		}

        public void BroadCast(string message, params object[] args)
        {
            for (Seats s = Seats.North; s <= Seats.West; s++)
            {
                this.WriteData(s, message, args);
            }
        }

        #region Bridge Events

        public override void HandleBoardStarted(int boardNumber, Seats dealer, Vulnerable vulnerabilty)
        {
            //Log.Trace("TableManagerHost.HandleBoardStarted");
            base.HandleBoardStarted(boardNumber, dealer, vulnerabilty);
            this.BroadCast("Start of board");
        }

        public override void HandleTournamentStopped()
        {
            //Log.Trace("TableManagerHost.HandleTournamentStopped");
            this.BroadCast("End of session");
            this.moreBoards = false;
        }

        #endregion

        private class TMController : TournamentController
        {
            private TableManagerHost host;

            public TMController(TableManagerHost h, Tournament t, ParticipantInfo p, BridgeEventBus bus) : base(t, p, bus)
            {
                this.host = h;
            }

            public override BoardResultEventPublisher NewBoardResult(Board2 currentBoard, SeatCollection<string> participants, BridgeEventBus eventBus)
            {
                return new HostBoardResult(this.host, currentBoard, participants, eventBus);
            }
        }

        private class HostBoardResult : BoardResultEventPublisher
        {
            private TableManagerHost host;

            public HostBoardResult(TableManagerHost h, Board2 board, SeatCollection<string> newParticipants, BridgeEventBus bus)
                : base("TMBoardResult", board, newParticipants, bus)
            {
                this.host = h;
            }

            public override void HandleBidDone(Seats source, Bid bid)
            {
                //Log.Trace("HostBoardResult.HandleBidDone");
                base.HandleBidDone(source, bid);
                for (Seats s = Seats.North; s <= Seats.West; s++)
                {
                    this.host.clients[s].state = TableManagerProtocolState.WaitForMyCards;
                    if (s != source)
                    {
                        this.host.WriteData(s, ProtocolHelper.Translate(bid, source));
                    }
                }

                lock (this.host.clients) for (Seats s = Seats.North; s <= Seats.West; s++)
                {
                    this.host.clients[s].Pause = this.Auction.Ended;
                }
            }

            public override void HandleCardNeeded(Seats controller, Seats whoseTurn, Suits leadSuit, Suits trump, bool trumpAllowed, int leadSuitLength, int trick)
            {
                //Log.Trace("HostBoardResult.HandleCardNeeded");
                if (leadSuit == Suits.NoTrump)
                {
                    Threading.Sleep(200);
                    this.host.WriteData(controller, "{0} to lead", whoseTurn == this.Play.Dummy ? "Dummy" : whoseTurn.ToXMLFull());
                }

                for (Seats s = Seats.North; s <= Seats.West; s++)
                {
                    this.host.clients[s].state = (s == controller ? TableManagerProtocolState.WaitForOwnCardPlay : TableManagerProtocolState.WaitForCardPlay);
                    lock (this.host.clients) this.host.clients[s].Pause = false;
                }
            }

            public override void HandleCardPlayed(Seats source, Suits suit, Ranks rank)
            {
                //Log.Trace("HostBoardResult.HandleCardPlayed {0} plays {2}{1}", source, suit.ToXML(), rank.ToXML());
                base.HandleCardPlayed(source, suit, rank);
                for (Seats s = Seats.North; s <= Seats.West; s++)
                {
                    if ((s != source && !(s == this.Auction.Declarer && source == this.Play.Dummy))
                        || (s == source && source == this.Play.Dummy)
                        )
                    {
                        this.host.WriteData(s, "{0} plays {2}{1}", source, suit.ToXML(), rank.ToXML());
                    }

                    if (this.Play.currentTrick == 1 && this.Play.man == 2)
                    {   // 1st card: need to send dummies cards
                        //Log.Trace("HostBoardResult.HandleCardPlayed 1st card to {0}", s);
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

            public override void HandlePlayFinished(BoardResultRecorder currentResult)
            {
                //Log.Trace("HostBoardResult.HandlePlayFinished");
                base.HandlePlayFinished(currentResult);
                Threading.Sleep(200);
                this.host.BroadCast("Timing - N/S : this board  00:06,  total  0:00:06.  E/W : this board  00:08,  total  0:00:08.");
                for (Seats s = Seats.North; s <= Seats.West; s++)
                {
                    this.host.clients[s].state = TableManagerProtocolState.WaitForStartOfBoard;
                    this.host.clients[s].Pause = false;
                }
            }
        }
    }
}
