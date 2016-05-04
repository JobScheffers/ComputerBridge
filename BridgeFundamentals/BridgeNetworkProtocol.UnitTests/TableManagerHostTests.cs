using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sodes.Base;
using Sodes.Bridge.Base;
using Sodes.Bridge.Networking;
using System.Threading;

namespace RoboBridge.TableManager.Client.UI.UnitTests
{
    [TestClass]
    public class TableManagerHostTests
    {
        private BridgeEventBus hostEventBus;
        private ManualResetEvent ready = new ManualResetEvent(false);

        [TestMethod, DeploymentItem("TestData\\WC2005final01.pbn")]
        public void TableManagerHost_Test()
        {
            Log.Level = 2;
            this.hostEventBus = new BridgeEventBus("TM_Host");
            var host = new TestHost(this.hostEventBus, this.ready);
            host.OnHostEvent += Host_OnHostEvent;

            SeatsExtensions.ForEachSeat((s) => host.Send(s, "Connecting \"{1}\" as {0} using protocol version 19", s, s.Direction()));

            ready.WaitOne();
        }

        private void Host_OnHostEvent(TableManagerHost sender, HostEvents hostEvent, object eventData)
        {
            switch (hostEvent)
            {
                case HostEvents.ReadyForTeams:
                    sender.HostTournament("WC2005final01.pbn");
                    break;
                case HostEvents.Finished:
                    this.ready.Set();
                    break;
            }
        }

        private class TestHost : TableManagerHost
        {
            private Seats whoseTurn;
            private Bid lastBid;
            private int notified;
            private ManualResetEvent ready;

            public TestHost(BridgeEventBus bus, ManualResetEvent r) : base(bus)
            {
                this.hostName = "TestHost";
                this.ready = r;
            }

            protected override void WriteData2(Seats seat, string message)
            {
                Log.Trace(1, "Host sends {1} {0}", message, seat);
                if (message.Contains("seated"))
                {
                    this.Send(seat, "{0} ready for teams", seat);
                    return;
                }

                if (message.Contains("Teams : N/S : \""))
                {
                    this.Send(seat, "{0} ready to start", seat);
                    return;
                }

                if (message.Contains("Start of board"))
                {
                    this.Send(seat, "{0} ready for deal", seat);
                    return;
                }

                if (message.Contains("Board number "))
                {
                    this.Send(seat, "{0} ready for cards", seat);
                    this.notified = 0;
                    return;
                }

                if (message.Contains("'s cards : "))
                {
                    this.notified++;
                    if (seat == Seats.North)
                    {
                        this.lastBid = Bid.C("1H");
                        this.Send(Seats.North, "North bids 1H");
                        this.Send(seat, "{0} ready for {1}'s bid", seat, this.whoseTurn.Next());
                    }
                    else this.Send(seat, "{0} ready for North's bid", seat);
                    if (this.notified == 4)
                    {
                        this.whoseTurn = Seats.East;
                        this.notified = 0;
                    }
                    return;
                }

                if (message.Contains("Explain "))
                {
                    this.Send(seat, "C5*D5");
                    return;
                }

                if (message.Contains(" bids ") || message.Contains(" passes"))
                {
                    this.notified++;
                    if (seat == this.whoseTurn)
                    {
                        Bid newBid;
                        if (lastBid.Equals(1, Suits.Hearts)) newBid = Bid.C("2NT"); else newBid = Bid.C("p");
                        this.Send(seat, "{0} bids {1}", seat, newBid.ToXML());
                        this.Send(seat, "{0} ready for {1}'s bid", seat, this.whoseTurn.Next());
                        this.lastBid = newBid;
                    }
                    else this.Send(seat, "{0} ready for {1}'s bid", seat, this.whoseTurn);
                    if (this.notified == 3)
                    {
                        this.whoseTurn = this.whoseTurn.Next();
                        this.notified = 0;
                    }

                    return;
                }

                this.ready.Set();
                Assert.Fail();
            }

            public void Send(Seats to, string message, params object[] args)
            {
                message = string.Format(message, args);
                this.ProcessIncomingMessage(message, to);
            }

            public override void ExplainBid(Seats source, Bid bid)
            {
                if (bid.Equals(2, Suits.NoTrump)) bid.NeedsAlert();
            }
        }
    }
}
