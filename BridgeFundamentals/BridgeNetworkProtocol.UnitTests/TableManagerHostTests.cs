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

        [TestMethod, DeploymentItem("TestData\\WC2005final01.pbn")]
        public void TableManagerHost_Test()
        {
            Log.Level = 2;
            this.hostEventBus = new BridgeEventBus("TM_Host");
            var host = new TestHost(this.hostEventBus);
            host.OnHostEvent += Host_OnHostEvent;

            host.State = 1;
            SeatsExtensions.ForEachSeat((s) =>
            {
                var client = new TestClient(host);
                host.Seat(client, string.Format("Connecting \"{1}\" as {0} using protocol version 19", s, s.Direction()));
            });

            host.State = 3;
            host.ready.WaitOne();
            host.stopped = true;
        }

        [TestMethod, DeploymentItem("TestData\\WC2005final01.pbn")]
        public void TableManagerHost_SeatingTest()
        {
            Log.Level = 2;
            this.hostEventBus = new BridgeEventBus("TM_Host");
            var host = new TestHost(this.hostEventBus);
            host.OnHostEvent += Host_OnHostEvent;

            var north = new TestClient(host);
            host.State = 1;
            host.Seat(north, string.Format("Connecting \"{1}\" as {0} using protocol version 19", Seats.North, Seats.North.Direction()));
            host.State = 4;
            host.Seat(north, string.Format("Connecting \"{1}\" as {0} using protocol version 19", Seats.North, Seats.North.Direction()));

            var south = new TestClient(host);
            host.State = 2;
            host.Seat(south, string.Format("Connecting \"{1}\" as {0} using protocol version 19", Seats.South, Seats.South.Direction().ToString() + "a"));
            host.State = 1;
            host.Seat(south, string.Format("Connecting \"{1}\" as {0} using protocol version 19", Seats.South, Seats.South.Direction()));
            host.State = 3;
        }

        private void Host_OnHostEvent(TableManagerHost sender, HostEvents hostEvent, object eventData)
        {
            switch (hostEvent)
            {
                case HostEvents.ReadyForTeams:
                    sender.HostTournament("WC2005final01.pbn");
                    break;
                case HostEvents.Finished:
                    (sender as TestHost).Stop();
                    break;
            }
        }

        private class TestClient : ClientData
        {
            public TestClient(TestHost h) : base(h) { }

            private int passCount;

            protected override void WriteData2(string message)
            {
                var h = this.host as TestHost;
                if (!h.stopped) Verify(h.State, message);
            }

            public void Verify(int state, string message)
            {
                //Log.Trace(1, "Host sends {1} {0}", message, seat);
                switch (state)
                {
                    case 1:
                        if (message.Contains("seated"))
                        {
                            this.ProcessIncomingMessage("{0} ready for teams", this.seat);
                            return;
                        }
                        break;
                    case 2:
                        if (message == "Expected team name 'NorthSouth'") return;
                        break;
                    case 4:
                        if (message == "Seat already has been taken") return;
                        break;
                    case 3:
                        if (message.Contains("Teams : N/S : \""))
                        {
                            this.ProcessIncomingMessage("{0} ready to start", this.seat);
                            return;
                        }

                        if (message.Contains("Start of board"))
                        {
                            this.ProcessIncomingMessage("{0} ready for deal", this.seat);
                            return;
                        }

                        if (message.Contains("Board number "))
                        {
                            this.ProcessIncomingMessage("{0} ready for cards", this.seat);
                            return;
                        }

                        if (message.Contains("'s cards : "))
                        {
                            var whoseTurn = Seats.North;
                            if (this.seat == whoseTurn)
                            {
                                this.ProcessIncomingMessage("North bids 1H");
                                this.ProcessIncomingMessage("{0} ready for {1}'s bid", this.seat, whoseTurn.Next());
                            }
                            else
                            {
                                this.ProcessIncomingMessage("{0} ready for {1}'s bid", this.seat, whoseTurn);
                            }

                            this.passCount = 0;
                            return;
                        }

                        if (message.Contains("Explain "))
                        {
                            this.ProcessIncomingMessage("C5*D5");
                            return;
                        }

                        if (message.Contains(" bids ") || message.Contains(" passes"))
                        {
                            var parts = message.Split(' ');
                            if (parts[1] == "passes") this.passCount++; else this.passCount = 0;
                            if (passCount == 3) (this.host as TestHost).Stop();
                            else
                            {
                                var whoseBid = SeatsExtensions.FromXML(parts[0].Substring(0, 1));
                                var whoseTurn = whoseBid.Next();
                                if (this.seat == whoseTurn)
                                {
                                    Bid newBid;
                                    if (message.Contains(" bids 1H")) newBid = Bid.C("2NT");
                                    else
                                    {
                                        newBid = Bid.C("p");
                                        this.passCount++;
                                    }

                                    this.ProcessIncomingMessage("{0} bids {1}", this.seat, newBid.ToXML());
                                    if (this.passCount < 3) this.ProcessIncomingMessage("{0} ready for {1}'s bid", this.seat, whoseTurn.Next());
                                }
                                else
                                {
                                    this.ProcessIncomingMessage("{0} ready for {1}'s bid", this.seat, whoseTurn);
                                }
                            }

                            return;
                        }

                        if (message.Contains(" to lead"))
                        {
                            return;
                        }

                        break;
                    default:
                        break;
                }

                (this.host as TestHost).Stop();
                Assert.Fail();
            }
        }

        private class TestHost : TableManagerHost
        {
            public TestHost(BridgeEventBus bus) : base(bus, "TestHost")
            {
            }

            public ManualResetEvent ready = new ManualResetEvent(false);
            public bool stopped = false;

            public int State { get; set; }

            protected override void ExplainBid(Seats source, Bid bid)
            {
                if (bid.Equals(2, Suits.NoTrump)) bid.NeedsAlert();
            }

            public void Stop()
            {
                this.stopped = true;
                this.ready.Set();
            }
        }
    }
}
