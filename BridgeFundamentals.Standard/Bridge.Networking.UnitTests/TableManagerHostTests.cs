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

        [TestMethod]
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
            await using var host = new TestHost(HostMode.SingleTableTwoRounds, this.hostEventBus, teamNS: "wbridge5");

            host.State = 1;
            var result = await host.Connect(0, string.Format("Connecting \"WBridge5\" as ANYPL using protocol version 18"));
            Assert.AreEqual(Seats.North - 1, result.Seat);
            Assert.AreEqual("Illegal hand 'anypl' specified", result.Response);

            result = await host.Connect(0, string.Format("Connecting \"RoboBridge\" as NORTH using protocol version 18"));
            Assert.AreEqual(Seats.North - 1, result.Seat);
            Assert.AreEqual("Team name must be 'wbridge5'", result.Response);

            result = await host.Connect(0, string.Format("Connecting \"WBridge5\" as NORTH using protocol version 18"));
            Assert.AreEqual(Seats.North, result.Seat);
            Assert.AreEqual("North (\"WBridge5\") seated", result.Response);
        }

        [TestMethod, DeploymentItem("TestData\\SingleBoard.pbn")]
        public async Task TableManagerHost_AlertsTest()
        {
            Log.Level = 5;
            this.hostEventBus = new BridgeEventBus("TM_Host");
            await using var host = new TestHost(HostMode.SingleTableTwoRounds, this.hostEventBus, await PbnHelper.LoadFile("SingleBoard.pbn"));

            host.State = 1;
            host.Run();
            await host.Process(0, "Connecting \"WBridge5\" as NORTH using protocol version 18");
            await host.Process(1, "Connecting \"RoboBridge\" as EAST using protocol version 18");
            await host.Process(2, "Connecting \"WBridge5\" as SOUTH using protocol version 18");
            await host.Process(3, "Connecting \"RoboBridge\" as WEST using protocol version 18");
            await host.Process(0, "NORTH ready for teams");
            await host.Process(1, "EAST ready for teams");
            await host.Process(2, "SOUTH ready for teams");
            await host.Process(3, "WEST ready for teams");
            await host.Process(0, "NORTH ready to start");
            await host.Process(1, "EAST ready to start");
            await host.Process(2, "SOUTH ready to start");
            await host.Process(3, "WEST ready to start");
            await host.Process(0, "NORTH ready for deal");
            await host.Process(1, "EAST ready for deal");
            await host.Process(2, "SOUTH ready for deal");
            await host.Process(3, "WEST ready for deal");
            await host.Process(0, "NORTH ready for cards");
            await host.Process(1, "EAST ready for cards");
            await host.Process(2, "SOUTH ready for cards");
            await host.Process(3, "WEST ready for cards");
            await host.Process(0, "North bids 1NT Infos.(!(ps0911*pa0012*D8*o1D*!H4*!S4*r23D5))*(!(ps0911*pa0012*C8*o1C*!H4*!S4*r23C5))*(!(pS1019*pa0612*S7*o7S3*r23S4))*(!(pH1019*pa0612*H7*o7H3*r23H4))*(!(pD1019*pa0712*D7*o7D1*!H4*!S4*r23D4))*(!(pC1019*pa0712*C7*o7C1*!H4*!S4*r23C4))*(!(pS0814*pa0510*(pa0509+th3)*S7*o7S1*!H4*r23S3))*(!(pH0814*pa0512*(pa0509+th3)*H7*o7H0*!S4*r23H3))*(!(pD0714*pa0011*D6*(D7+o7D6*(!S3+!H3))*o7D1*!H4*!S4*r23D3))*(!(pC0714*pa0011*C6*(C7+o7C6*(!S3+!H3))*o7C1*!H4*!S4*r23C3))*(!(S5*!S8*!>HS*o7S1*(!u+S6*(o7S3+th3+pS1011+pa0009))*r23S2*pa0011*pS0413*(pa0010+th3)*!(pS1213*pa1021*(H5+D5+C5+S7))*!(pa1115*=S5*H3*(D4+C4))*!(pa1019*S6*(H4+D4+C4))*!(vS6*pa1240)))*(!(H5*!H8*>HS*o7H1*(!u+H6*(o7H3+th3+pH1011+pa0009))*r23H2*pa0011*pH0413*(pa0010+th3)*!(pH1213*pa1021*(S5+D5+C5+H7))*!(pa1019*H6*(S4+D4+C4))*!(vH6*pa1240)))*(!(D5*!S5*!H5*(!u+D6*o7D1*(o7D3+th3))*r23D2*pa0311*pD0413*(pa0010+th3)))*(!(pN2022*S2*!S6*H2*!H6*D1*(D2+o7D3*C3*!C6*H3*!H5*S3*!S5)*!D7*C1*(C2+o7C3*D3*!D6*H3*!H5*S3*!S5)*!C7*!(S5*(H4*!t07+D5+C5))*!(H5*(S4*!t07+D5+C5))*!(pa2240*(S5+H5))))*(pN1517*pa1418*t07*(t01+th4+th3))");
            await host.Process(1, "EAST ready for north's bid");
            await host.Process(2, "SOUTH ready for north's bid");
            await host.Process(3, "WEST ready for north's bid");
            await host.Process(0, "NORTH ready for east's bid");
            await host.Process(1, "EAST bids 2S Alert.H5*C5");
            await host.Process(2, "SOUTH ready for east's bid");
            await host.Process(3, "WEST ready for east's bid");

            //await host.WaitForCompletionAsync();
            await Task.Delay(1000);
        }

        private class TestHost : AsyncTableHost<HostTestCommunication>
        {
            public TestHost(HostMode mode, BridgeEventBus bus, Tournament tournament = null, string teamNS = "", string teamEW = "")
                : base(mode, new(), bus, "", tournament, AlertMode.SelfExplaining, Scorings.scIMP, teamNS, teamEW)
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

            public async ValueTask Process(int clientId, string message)
            {
                await this.ProcessMessage(clientId, message);
            }
        }

        private class HostTestCommunication : HostCommunication
        {
            public HostTestCommunication() : base("") { }

            public override ValueTask<string> GetMessage(int clientId)
            {
                throw new System.NotImplementedException();
            }

            public override async ValueTask Run()
            {
                await ValueTask.CompletedTask;
            }

            public override void StopAcceptingNewClients()
            {
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
