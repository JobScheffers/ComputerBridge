using Bridge.Test.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;

namespace Bridge.Networking.UnitTests
{
    [TestClass]
    public class TableManagerHostTests : BridgeTestBase
    {
        private BridgeEventBus hostEventBus;

        [TestMethod]
        public async Task TableManagerHost_Test()
        {
            Log.Level = 4;
            this.hostEventBus = new BridgeEventBus("TM_Host");
            await using var host = new TestHost(HostMode.SingleTableTwoRounds, this.hostEventBus);
            host.OnHostEvent += ConnectionManager_OnHostEvent;

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
            host.OnHostEvent += ConnectionManager_OnHostEvent;

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

        private async ValueTask ConnectionManager_OnHostEvent(object sender, HostEvents hostEvent, object eventData)
        {
            await Task.CompletedTask;
            switch (hostEvent)
            {
                case HostEvents.Seated:
                    break;
                case HostEvents.ReadyForTeams:
                    break;
                case HostEvents.BoardFinished:
                    break;
                case HostEvents.Finished:
                    break;
            }
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
                await ValueTask.CompletedTask;
                //await this.ProcessMessage(seat, message);
            }
        }

        private class HostTestCommunication : HostCommunication
        {
            public HostTestCommunication() : base("") { }

            public override ValueTask<string> GetMessage(int clientId)
            {
                throw new System.NotImplementedException();
            }

            public override ValueTask Run()
            {
                throw new System.NotImplementedException();
            }

            public override async ValueTask Send(int clientId, string message)
            {
                await ValueTask.CompletedTask;
            }

            public override ValueTask<string> SendAndWait(int clientId, string message)
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
