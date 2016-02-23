using System;
using System.Collections.Generic;
using Sodes.Bridge.Base;
using System.Threading.Tasks;
using BridgeNetworkProtocol2;
using Sodes.Base;

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
		private class ClientData
		{
			public TableManagerProtocolState state;
			public string teamName;
			public string hand;
            public bool Pause;
		}

		private class ClientMessage
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

		public Seats dummy;
		public bool isDummy;
		private SeatCollection<ClientData> clients;
		private Queue<ClientMessage> messages;
        private string lastRelevantMessage;
        private BoardResultEventPublisher boardResult;

        protected TableManagerHost(BridgeEventBus bus) : base(bus)
		{
			this.messages = new Queue<ClientMessage>();
			this.clients = new SeatCollection<ClientData>();
			for (Seats seat = Seats.North; seat <= Seats.West; seat++)
			{
				this.clients[seat] = new ClientData();
				this.clients[seat].state = TableManagerProtocolState.Initial;
                this.clients[seat].Pause = false;
			}
		}

		protected void ProcessIncomingMessage(string message, Seats seat)
		{
			lock (this.messages)
			{
				this.messages.Enqueue(new ClientMessage(seat, message));
			}

			Task.Factory.StartNew(() =>
			{
				this.ProcessMessages(null);
			});
		}

		private void ProcessMessages(Object stateInfo)
		{
			bool more = true;
			do
			{
				ClientMessage m = null;
				lock (this.messages)
				{
					if (this.messages.Count >= 1)
					{
						m = this.messages.Dequeue();
					}
					else
					{
						more = false;
					}
				}

				if (more && m != null)
				{
					lock (this.clients)
                    {		// ensure exclusive access to ProcessMessage
                        if (this.clients[m.Seat].Pause)
                        {
                            this.messages.Enqueue(m);
                        }
                        else
                        { 
                            this.ProcessMessage(m.Message, m.Seat);
                        }
                    }
				}
			} while (more);
		}

		private void ProcessMessage(string message, Seats seat)
		{
			Log.Trace("Host processing '{0}'", message);
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
                    this.clients[seat].Pause = true;
                    if (seat == this.boardResult.Auction.WhoseTurn)
                    {
                        this.lastRelevantMessage = message;
                        ChangeState(message, this.clients[seat].hand + " ", TableManagerProtocolState.WaitForOtherBid, seat);
                    }
                    else
                    {
                        ChangeState(message, string.Format("{0} ready for {1}'s bid", this.clients[seat].hand, this.boardResult.Auction.WhoseTurn), TableManagerProtocolState.WaitForOtherBid, seat);
                    }
                    break;

                case TableManagerProtocolState.WaitForCardPlay:
                    this.clients[seat].Pause = true;
                    // ready for dummy's card mag ook ready for xx's card
                    if (this.boardResult.Play.whoseTurn == this.dummy)
					{
						ChangeState(message, string.Format("{0} ready for {1}'s card to trick {2}", this.clients[seat].hand, message.Contains("dummy") ? "dummy" : this.boardResult.Play.whoseTurn.ToString(), this.boardResult.Play.currentTrick), TableManagerProtocolState.WaitForOtherCardPlay, seat);
					}
					else
					{
						ChangeState(message, string.Format("{0} ready for {1}'s card to trick {2}", this.clients[seat].hand, this.boardResult.Play.whoseTurn.ToString(), this.boardResult.Play.currentTrick), TableManagerProtocolState.WaitForOtherCardPlay, seat);
					}
					break;

				case TableManagerProtocolState.WaitForOwnCardPlay:
                    this.lastRelevantMessage = message;
                    ChangeState(message, string.Format("{0} plays ", this.clients[seat].hand), TableManagerProtocolState.WaitForOtherCardPlay, seat);
                    break;

				case TableManagerProtocolState.WaitForDummiesCardPlay:
					ChangeState(message, string.Format("{0} plays ", this.boardResult.Play.whoseTurn), TableManagerProtocolState.WaitForCardPlay, seat);
					//ChangeState(message, string.Format("{0} plays ", whoseTurn), TableManagerProtocolState.WaitForOtherCardPlay);
					break;

				case TableManagerProtocolState.WaitForDummiesCards:
					ChangeState(message, string.Format("{0} ready for dummy", this.clients[seat].hand), TableManagerProtocolState.WaitForOtherCardPlay, seat);
					break;

				default:
					this.Refuse(seat, "Unexpected '{0}'", message);
					break;
			}

			//if (this.Connected)
			//{
			//  // notify controller that a valid message has been received
			//  if (this.OnHostEvent != null) OnHostEvent(this, message);
			//  this.WaitForIncomingMessage(seat);
			//}
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
                    switch (newState)
                    {
                        case TableManagerProtocolState.Initial:
                            break;
                        case TableManagerProtocolState.WaitForSeated:
                            break;
                        case TableManagerProtocolState.WaitForTeams:
                            this.BroadCast("Teams : N/S : \"" + this.clients[Seats.North].teamName + "\". E/W : \"" + this.clients[Seats.East].teamName + "\"");
                            break;
                        case TableManagerProtocolState.WaitForStartOfBoard:
                            this.OnHostEvent(this, HostEvents.ReadyToStart, Seats.North, "");
                            break;
                        case TableManagerProtocolState.WaitForBoardInfo:
                            this.OnHostEvent(this, HostEvents.ReadyForDeal, Seats.North, "");
                            break;
                        case TableManagerProtocolState.WaitForMyCards:
                            break;
                        case TableManagerProtocolState.WaitForCardPlay:
                            break;
                        case TableManagerProtocolState.WaitForOtherBid:
                            ProtocolHelper.HandleProtocolBid(this.lastRelevantMessage, this.EventBus);
                            break;
                        case TableManagerProtocolState.WaitForOtherCardPlay:
                            ProtocolHelper.HandleProtocolPlay(this.lastRelevantMessage, this.EventBus);
                            break;
                        case TableManagerProtocolState.WaitForOwnCardPlay:
                            break;
                        case TableManagerProtocolState.WaitForDummiesCardPlay:
                            break;
                        case TableManagerProtocolState.WaitForDummiesCards:
                            this.OnHostEvent(this, HostEvents.ReadyForDummiesCards, this.boardResult.Play.Dummy, "");
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
				this.Refuse(seat, "Expected '{0}'", expected);
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
            this.BroadCast("Board number {0}. Dealer {1}. {2} vulnerable.", boardNumber, dealer.ToXMLFull(), ProtocolHelper.Translate(Vulnerable.Neither));
            this.boardResult = new BoardResultEventPublisher("TableManagerHost", new Board2(dealer, vulnerabilty, new Distribution()), new SeatCollection<string>(), this.EventBus);
        }
        public override void HandleBidDone(Seats source, Bid bid)
        {
            for (Seats s = Seats.North; s <= Seats.West; s++)
            {
                this.clients[s].state = TableManagerProtocolState.WaitForMyCards;
                this.clients[s].Pause = false;
                if (s != source)
                {
                    this.WriteData(s, ProtocolHelper.Translate(bid, source));
                }
            }
        }

        public override void HandleAuctionFinished(Seats declarer, Contract finalContract)
        {
            for (Seats s = Seats.North; s <= Seats.West; s++)
            {
                this.clients[s].state = TableManagerProtocolState.WaitForCardPlay;
            }
        }

        public override void HandleCardNeeded(Seats controller, Seats whoseTurn, Suits leadSuit, Suits trump, bool trumpAllowed, int leadSuitLength, int trick)
        {
            Log.Trace("TableManagerHost.HandleCardNeeded");
            if (leadSuit == Suits.NoTrump)
            {
                this.WriteData(controller, "{0} to lead", whoseTurn);
                this.clients[whoseTurn].state = TableManagerProtocolState.WaitForOwnCardPlay;
            }
        }

        public override void HandleCardPlayed(Seats source, Suits suit, Ranks rank)
        {
            Log.Trace("TableManagerHost.HandleCardNeeded.HandleCardPlayed");
            for (Seats s = Seats.North; s <= Seats.West; s++)
            {
                if (s != source)
                {
                    this.WriteData(s, this.lastRelevantMessage);
                    if (this.boardResult.Play.currentTrick == 1 && this.boardResult.Play.man == 1) this.clients[s].state = TableManagerProtocolState.WaitForDummiesCards;
                }
            }
        }

        public override void HandleNeedDummiesCards(Seats dummy)
        {
            Log.Trace("TableManagerHost.HandleNeedDummiesCards");

            for (Seats s = Seats.North; s <= Seats.West; s++)
            {
                this.clients[s].state = TableManagerProtocolState.WaitForDummiesCards;
            }
        }

        #endregion
    }
}
