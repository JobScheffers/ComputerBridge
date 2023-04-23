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
            Log.Level = 4;
            this.hostEventBus = new BridgeEventBus("TM_Host");
            await using var host = new TestHost(HostMode.SingleTableTwoRounds, this.hostEventBus);

            host.State = 1;
            SeatsExtensions.ForEachSeat(s =>
            {
                var result = host.Connect((int)s, string.Format("Connecting \"{1}\" as {0} using protocol version 19", s, s.Direction()));
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
            var host = new TestHost(HostMode.SingleTableTwoRounds, this.hostEventBus);

            host.State = 1;
            var result = host.Connect(0, string.Format("Connecting \"{1}\" as {0} using protocol version 19", Seats.North, Seats.North.Direction()));
            host.State = 4;
            result = host.Connect(0, string.Format("Connecting \"{1}\" as {0} using protocol version 19", Seats.North, Seats.North.Direction()));

            host.State = 2;
            result = host.Connect(1, string.Format("Connecting \"{1}\" as {0} using protocol version 19", Seats.South, Seats.South.Direction().ToString() + "2"));
            host.State = 1;
            result = host.Connect(1, string.Format("Connecting \"{1}\" as {0} using protocol version 19", Seats.South, Seats.South.Direction()));
            host.State = 3;
        }

        [TestMethod]
        public async Task TableManagerHost_IllegalSeatTest()
        {
            Log.Level = 1;
            this.hostEventBus = new BridgeEventBus("TM_Host");
            await using var host = new TestHost(HostMode.SingleTableTwoRounds, this.hostEventBus);

            host.State = 1;
            var result = await host.Connect(0, string.Format("Connecting \"WBridge5\" as ANYPL using protocol version 18"));
            Assert.AreEqual(Seats.North - 1, result.Seat);
            Assert.AreEqual("Illegal hand specified", result.Response);
            result = await host.Connect(0, string.Format("Connecting \"WBridge5\" as NORTH using protocol version 18"));
            Assert.AreEqual(Seats.North, result.Seat);
            Assert.AreEqual("North (\"WBridge5\") seated", result.Response);
        }

        //private class TestClient : ClientData
        //{
        //    private TestHost host;

        //    public TestClient(TestHost h) : base()
        //    {
        //        this.host = h;
        //    }

        //    private int passCount;

        //    protected override void WriteToDevice(string message)
        //    {
        //        var h = this.host as TestHost;
        //        if (!h.stopped) Verify(h.State, message);
        //    }

        //    public void Verify(int state, string message)
        //    {
        //        //Log.Trace(1, "Host sends {1} {0}", message, seat);
        //        switch (state)
        //        {
        //            case 1:
        //                if (message.Contains("seated"))
        //                {
        //                    this.ProcessIncomingMessage("{0} ready for teams", this.seat);
        //                    return;
        //                }
        //                else if (message == "Illegal hand specified")
        //                {
        //                    return;
        //                }
        //                break;
        //            case 2:
        //                if (message == "Expected team name 'NorthSouth'") return;
        //                break;
        //            case 4:
        //                if (message == "Seat already has been taken") return;
        //                break;
        //            case 3:
        //                if (message.Contains("Teams : N/S : \""))
        //                {
        //                    this.ProcessIncomingMessage("{0} ready to start", this.seat);
        //                    return;
        //                }

        //                if (message.Contains("Start of board"))
        //                {
        //                    this.ProcessIncomingMessage("{0} ready for deal", this.seat);
        //                    return;
        //                }

        //                if (message.Contains("Board number "))
        //                {
        //                    this.ProcessIncomingMessage("{0} ready for cards", this.seat);
        //                    return;
        //                }

        //                if (message.Contains("'s cards : "))
        //                {
        //                    var whoseTurn = Seats.North;
        //                    if (this.seat == whoseTurn)
        //                    {
        //                        this.ProcessIncomingMessage("North bids 1H");
        //                        this.ProcessIncomingMessage("{0} ready for {1}'s bid", this.seat, whoseTurn.Next().ToString().PadLeft(5).ToUpper());
        //                    }
        //                    else
        //                    {
        //                        this.ProcessIncomingMessage("{0} ready for {1}'s bid", this.seat, whoseTurn);
        //                    }

        //                    this.passCount = 0;
        //                    return;
        //                }

        //                if (message.Contains("Explain "))
        //                {
        //                    this.ProcessIncomingMessage("C5*D5");
        //                    return;
        //                }

        //                var parts = message.Split(' ');

        //                if (message.Contains(" bids ") || message.Contains(" passes"))
        //                {
        //                    if (parts[1] == "passes") this.passCount++; else this.passCount = 0;
        //                    //if (passCount == 3) (this.host as TestHost).Abort();
        //                    //else
        //                    {
        //                        var whoseBid = SeatsExtensions.FromXML(parts[0].Substring(0, 1));
        //                        var whoseTurn = whoseBid.Next();
        //                        if (this.seat == whoseTurn)
        //                        {
        //                            Bid newBid;
        //                            if (message.Contains(" bids 1H")) newBid = Bid.C("2NT!C5*D5");
        //                            else
        //                            {
        //                                newBid = Bid.C("p");
        //                                this.passCount++;
        //                            }

        //                            this.ProcessIncomingMessage("{0} bids {1}", this.seat, newBid.ToXML());
        //                            if (this.passCount < 3) this.ProcessIncomingMessage("{0} ready for {1}'s bid", this.seat, whoseTurn.Next());
        //                        }
        //                        else
        //                        {
        //                            this.ProcessIncomingMessage("{0} ready for {1}'s bid", this.seat, whoseTurn);
        //                        }
        //                    }

        //                    return;
        //                }

        //                if (message.Contains(" to lead"))
        //                {
        //                    var whoseCard = SeatsExtensions.FromXML(parts[0].Substring(0, 1));
        //                    if (this.seat == whoseCard) this.ProcessIncomingMessage("{0} plays {1}", this.seat, "4C");
        //                    return;
        //                }

        //                break;
        //            default:
        //                break;
        //        }

        //        //(this.host as TestHost).HandleTournamentStopped();
        //        Assert.Fail();
        //    }

        //    protected override async ValueTask DisposeManagedObjects()
        //    {
        //        await base.DisposeManagedObjects();
        //    }
        //}

        private class TestHost : AsyncTableHost<HostTestCommunication>
        {
            public TestHost(HostMode mode, BridgeEventBus bus, string tournamentFileName = "") : base(mode, new(), bus, tournamentFileName)
            {
                this.OnRelevantBridgeInfo = HandleRelevantBridgeInfo;
            }

            private async ValueTask HandleRelevantBridgeInfo(object sender, System.DateTime received, string message)
            {
                Log.Trace(1, $"TestHost.HandleRelevantBridgeInfo: {message}");
                await ValueTask.CompletedTask;
            }

            public bool stopped = false;

            public int State { get; set; }

            protected override void ExplainBid(Seats source, Bid bid)
            {
                if (bid.Equals(2, Suits.NoTrump)) bid.NeedsAlert();
            }

            public override void HandleTournamentStopped()
            {
                base.HandleTournamentStopped();
                this.stopped = true;
            }

            public async ValueTask<ConnectResponse> Connect(int clientId, string message)
            {
                return await this.ProcessConnect(clientId, message);
            }

            public async ValueTask Process(Seats seat, string message)
            {
                await this.ProcessMessage(seat, message);
            }
        }

        private class HostTestCommunication : HostCommunication
        {
            public override ValueTask Run()
            {
                throw new System.NotImplementedException();
            }

            public override async ValueTask Send(Seats seat, string message)
            {
                await ValueTask.CompletedTask;
            }

            public override ValueTask Broadcast(string message)
            {
                throw new System.NotImplementedException();
            }

            public override ValueTask<string> SendAndWait(Seats seat, string message)
            {
                throw new System.NotImplementedException();
            }

            public override void Stop()
            {
                throw new System.NotImplementedException();
            }

            protected override async ValueTask DisposeManagedObjects()
            {
                await ValueTask.CompletedTask;
            }
        }
    }
}
