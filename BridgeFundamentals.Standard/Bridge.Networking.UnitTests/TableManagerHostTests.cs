using Bridge.Test.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;

namespace Bridge.Networking.UnitTests
{
    [TestClass]
    public class TableManagerHostTests : BridgeTestBase
    {
        private BridgeEventBus hostEventBus;

        [TestMethod, DeploymentItem("TestData\\WC2005final01.pbn")]
        public async Task TableManagerHost_Test()
        {
            Log.Level = 1;
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
            await host.WaitForCompletionAsync();
            host.stopped = true;
        }

        [TestMethod, DeploymentItem("TestData\\WC2005final01.pbn")]
        public void TableManagerHost_SeatingTest()
        {
            Log.Level = 1;
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
            host.Seat(south, string.Format("Connecting \"{1}\" as {0} using protocol version 19", Seats.South, Seats.South.Direction().ToString() + "2"));
            host.State = 1;
            host.Seat(south, string.Format("Connecting \"{1}\" as {0} using protocol version 19", Seats.South, Seats.South.Direction()));
            host.State = 3;
        }

        [TestMethod]
        public void TableManagerHost_IllegalSeatTest()
        {
            Log.Level = 1;
            this.hostEventBus = new BridgeEventBus("TM_Host");
            var host = new TestHost(this.hostEventBus);
            host.OnHostEvent += Host_OnHostEvent;

            var north = new TestClient(host);
            host.State = 1;
            host.Seat(north, string.Format("Connecting \"WBridge5\" as ANYPL using protocol version 18"));
            host.Seat(north, string.Format("Connecting \"WBridge5\" as NORTH using protocol version 18"));
        }

        private void Host_OnHostEvent(TableManagerHost sender, HostEvents hostEvent, object eventData)
        {
            switch (hostEvent)
            {
                case HostEvents.ReadyForTeams:
                    sender.HostTournament("WC2005final01.pbn", 1);
                    break;
                case HostEvents.Finished:
                    (sender as TestHost).Abort();
                    break;
            }
        }

        private class TestClient : ClientData
        {
            public TestClient(TestHost h) : base(h) { }

            private int passCount;

            protected override void WriteToDevice(string message)
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
                        else if (message == "Illegal hand specified")
                        {
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
                                this.ProcessIncomingMessage("{0} ready for {1}'s bid", this.seat, whoseTurn.Next().ToString().PadLeft(5).ToUpper());
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

                        var parts = message.Split(' ');

                        if (message.Contains(" bids ") || message.Contains(" passes"))
                        {
                            if (parts[1] == "passes") this.passCount++; else this.passCount = 0;
                            //if (passCount == 3) (this.host as TestHost).Abort();
                            //else
                            {
                                var whoseBid = SeatsExtensions.FromXML(parts[0].Substring(0, 1));
                                var whoseTurn = whoseBid.Next();
                                if (this.seat == whoseTurn)
                                {
                                    Bid newBid;
                                    if (message.Contains(" bids 1H")) newBid = Bid.C("2NT!C5*D5");
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
                            var whoseCard = SeatsExtensions.FromXML(parts[0].Substring(0, 1));
                            if (this.seat == whoseCard) this.ProcessIncomingMessage("{0} plays {1}", this.seat, "4C");
                            return;
                        }

                        break;
                    default:
                        break;
                }

                (this.host as TestHost).Abort();
                Assert.Fail();
            }
        }

        private class TestHost : TableManagerHost
        {
            public TestHost(BridgeEventBus bus) : base(bus, "TestHost")
            {
                this.OnRelevantBridgeInfo += HandleRelevantBridgeInfo;
            }

            private void HandleRelevantBridgeInfo(TableManagerHost sender, System.DateTime received, string message)
            {
                Log.Trace(1, $"TestHost.HandleRelevantBridgeInfo: {message}");
            }

            public bool stopped = false;

            public int State { get; set; }

            protected override void ExplainBid(Seats source, Bid bid)
            {
                if (bid.Equals(2, Suits.NoTrump)) bid.NeedsAlert();
            }

            protected override void Stop()
            {
                this.stopped = true;
                //this.ready.Set();
                base.Stop();
            }

            public void Abort()
            {
                this.Stop();
            }
        }
    }
}
