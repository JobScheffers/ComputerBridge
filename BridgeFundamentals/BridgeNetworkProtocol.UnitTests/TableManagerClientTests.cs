using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading;
using System;
using System.Threading.Tasks;
using Bridge;
using Bridge.Networking;

namespace Bridge.Networking.UnitTests
{
    [TestClass]
    public class TableManagerClientTests
    {
        private BridgeEventBus clientEventBus;
        private ManualResetEvent ready = new ManualResetEvent(false);

        [TestMethod, DeploymentItem("TestData\\WC2005final01.pbn")]
        public void TableManagerClient_TestIsolated()
        {
            Log.Level = 2;
            this.clientEventBus = new BridgeEventBus("TM_Client");
            var client = new TestClient(this.clientEventBus, this.ready);

            client.Connect(Seats.North, 120, 60, "RoboNS");

            ready.WaitOne();
        }

        private class TestClient : TableManagerClient
        {
            private int testState;
            private ManualResetEvent ready;

            public TestClient(BridgeEventBus bus, ManualResetEvent r) : base(bus)
            {
                this.ready = r;
                this.testState = 1;
            }

            protected override async Task WriteProtocolMessageToRemoteMachine(string message)
            {
                switch (this.testState)
                {
                    case 1:
                        Assert.AreEqual("Connecting \"RoboNS\" as North using protocol version 18", message);
                        this.testState = 2;
                        this.ProcessIncomingMessage("North (\"RoboNS\") seated");
                        return;
                    case 2:
                        Assert.AreEqual("North ready for teams", message);
                        this.testState = 3;
                        this.ProcessIncomingMessage("Teams : N/S : \"RoboNS\" E/W : \"RoboEW\"");
                        return;
                    case 3:
                        Assert.AreEqual("North ready to start", message);
                        this.testState = 4;
                        this.ProcessIncomingMessage("Start of board");
                        return;
                    case 4:
                        Assert.AreEqual("North ready for deal", message);
                        this.testState = 5;
                        this.ProcessIncomingMessage("Board number 1. Dealer North. Neither vulnerable.");
                        return;
                    case 5:
                        Assert.AreEqual("North ready for cards", message);
                        this.testState = 6;
                        this.ProcessIncomingMessage("North's cards : S A T 4 3. H T 8 5 3. D K T. C K T 6. ");
                        return;
                    case 6:
                        Assert.AreEqual("North bids 1NT", message);
                        this.testState = 7;
                        return;
                    case 7:
                        Assert.AreEqual("North ready for East's bid", message);
                        this.testState = 8;
                        this.ProcessIncomingMessage("East passes");
                        return;
                    case 8:
                        Assert.AreEqual("North ready for South's bid", message);
                        this.testState = 9;
                        this.ProcessIncomingMessage("South passes");
                        return;
                    case 9:
                        Assert.AreEqual("North ready for West's bid", message);
                        this.testState = 10;
                        this.ProcessIncomingMessage("Explain West's 2D");
                        return;
                    case 10:
                        Assert.AreEqual("My explanation", message);
                        this.testState = 11;
                        this.ProcessIncomingMessage("West bids 2D");
                        return;
                    case 11:
                        Assert.AreEqual("North passes", message);
                        this.testState = 12;
                        //this.ProcessIncomingMessage("West bids 2D");
                        this.Stop();
                        return;
                }

                this.Stop();
                Assert.Fail("Unknown state " + this.testState);
            }

            protected override void Stop()
            {
                this.ready.Set();
            }

            public override void HandleBidNeeded(Seats whoseTurn, Bid lastRegularBid, bool allowDouble, bool allowRedouble)
            {
                base.HandleBidNeeded(whoseTurn, lastRegularBid, allowDouble, allowRedouble);
                if (whoseTurn == Seats.North)
                    this.EventBus.HandleBidDone(Seats.North, Bid.C(lastRegularBid.IsPass ? "1NT" : "Pass"));
            }

            public override void HandleExplanationNeeded(Seats source, Bid bid)
            {
                bid.Explanation = "My explanation";
                this.EventBus.HandleExplanationDone(source, bid);
            }
        }
    }
}
