#define useOwnHost

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;
using Bridge.Test.Helpers;

namespace Bridge.Networking.UnitTests
{
    [TestClass]
    public class TableManagerTcpHostTests : BridgeTestBase
    {
        [TestMethod, DeploymentItem("TestData\\WC2005final01.pbn")]
        public async Task TableManager_Client_Test()
        {
            Log.Level = 4;
            // Comment the next 3 lines if you want to test against a real TableManager
#if useOwnHost
            var host = new TestHost(2001, new BridgeEventBus("TM_Host"));
            host.OnHostEvent += Host_OnHostEvent;
#endif

            var vms = new SeatCollection<TestClient>();
            Parallel.For(0, 4, (i) =>
            {
                Seats s = (Seats)i;
                vms[s] = new TestClient();
                vms[s].Connect(s, "localhost", 2001, 120, 1, "Robo" + (s == Seats.North || s == Seats.South ? "NS" : "EW"), false);
            });

            await host.WaitForCompletionAsync();
        }

        [TestMethod, DeploymentItem("TestData\\WC2005final01.pbn")]
        public async Task TableManager_2Tables_Test()
        {
            Log.Level = 1;
            var host1 = new TestHost(2002, new BridgeEventBus("Host1"));
            host1.OnHostEvent += Host_OnHostEvent;

            var vms = new SeatCollection<TestClient>();
            Parallel.For(0, 4, (i) =>
            {
                Seats s = (Seats)i;
                vms[s] = new TestClient();
                vms[s].Connect(s, "localhost", 2002, 120, 1, "Robo" + (s == Seats.North || s == Seats.South ? "NS" : "EW"), false);
            });

            var host2 = new TestHost(2003, new BridgeEventBus("Host2"));
            host2.OnHostEvent += Host_OnHostEvent;

            var vms2 = new SeatCollection<TestClient>();
            Parallel.For(0, 4, (i) =>
            {
                Seats s = (Seats)i;
                vms2[s] = new TestClient();
                vms2[s].Connect(s, "localhost", 2003, 120, 1, "Robo" + (s == Seats.North || s == Seats.South ? "NS" : "EW"), false);
            });

            await host1.WaitForCompletionAsync();
        }

        private void Host_OnHostEvent(TableManagerHost sender, HostEvents hostEvent, object eventData)
        {
            switch (hostEvent)
            {
                case HostEvents.ReadyForTeams:
                    sender.HostTournament("WC2005final01.pbn");
                    break;
            }
        }

        private class TestHost : TableManagerTcpHost
        {
            public TestHost(int port, BridgeEventBus bus) : base(port, bus)
            {
            }

            protected override void ExplainBid(Seats source, Bid bid)
            {
                bid.UnAlert();
            }
        }

        private class TestClient
        {
            public void Connect(Seats _seat, string serverName, int portNumber, int _maxTimePerBoard, int _maxTimePerCard, string teamName, bool _sendAlerts)
            {
                var bus = new BridgeEventBus("TM_Client " + _seat);
                var bot = new ChampionshipRobot(_seat, bus);
                bus.HandleTournamentStarted(Scorings.scIMP, _maxTimePerBoard, _maxTimePerCard, "");
                bus.HandleRoundStarted(new SeatCollection<string>(), new DirectionDictionary<string>("RoboBridge", "RoboBridge"));
                var connectionManager = new TableManagerTcpClient(bus);
                connectionManager.Connect(_seat, serverName, portNumber, _maxTimePerBoard, _maxTimePerCard, teamName);
            }

            private class ChampionshipRobot : TestRobot
            {
                private Scorings scoring;
                private int maxTimePerBoard;
                private int maxTimePerCard;
                private string tournamentName;

                public ChampionshipRobot(Seats seat, BridgeEventBus bus) : base(seat, bus)
                {
                }

                public override void HandleTournamentStarted(Scorings _scoring, int _maxTimePerBoard, int _maxTimePerCard, string _tournamentName)
                {
                    this.scoring = _scoring;
                    this.maxTimePerBoard = _maxTimePerBoard;
                    this.maxTimePerCard = _maxTimePerCard;
                    this.tournamentName = _tournamentName;
                }

                public override Bid FindBid(Bid lastRegularBid, bool allowDouble, bool allowRedouble)
                {
                    //TODO: implement your own logic
                    return base.FindBid(lastRegularBid, allowDouble, allowRedouble);
                }

                public override Card FindCard(Seats whoseTurn, Suits leadSuit, Suits trump, bool trumpAllowed, int leadSuitLength, int trick)
                {
                    //TODO: implement your own logic
                    //Thread.Sleep(1000);
                    return base.FindCard(whoseTurn, leadSuit, trump, trumpAllowed, leadSuitLength, trick);
                }
            }
        }
    }
}
