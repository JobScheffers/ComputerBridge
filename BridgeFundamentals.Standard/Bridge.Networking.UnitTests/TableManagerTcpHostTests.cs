#define useOwnHost

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;
using Bridge.Test.Helpers;
using System.IO;

namespace Bridge.Networking.UnitTests
{
    [TestClass]
    public class TableManagerTcpHostTests : TcpTestBase
    {
        [TestMethod, DeploymentItem("TestData\\SingleBoard.pbn")]
        public async Task TableManager_HighCpuAfterSessionEnd()
        {
            Log.Level = 1;
            var port = GetNextPort();
            var host = new TestHost(HostMode.SingleTableTwoRounds, port, new BridgeEventBus("TM_Host"), "SingleBoard.pbn");

            var vms = new SeatCollection<TestClient>();
            Parallel.For(0, 4, (i) =>
            {
                Seats s = (Seats)i;
                vms[s] = new TestClient();
                vms[s].Connect(s, "localhost", port, 120, 1, "Robo" + (s == Seats.North || s == Seats.South ? "NS" : "EW"), false);
            });

            await host.WaitForCompletionAsync();
            await Task.Delay(10000);
        }

        [TestMethod, DeploymentItem("TestData\\WC2005final01.pbn")]
        public async Task TableManager_Client_Test()
        {
            Log.Level = 1;
            // Comment the next 3 lines if you want to test against a real TableManager
            var port = 2000;
#if useOwnHost
            port = GetNextPort();
            var host = new TestHost(HostMode.SingleTableTwoRounds, port, new BridgeEventBus("TM_Host"), "WC2005final01.pbn");
#endif

            var vms = new SeatCollection<TestClient>();
            Parallel.For(0, 4, (i) =>
            {
                Seats s = (Seats)i;
                vms[s] = new TestClient();
                vms[s].Connect(s, "localhost", port, 120, 1, "Robo" + (s == Seats.North || s == Seats.South ? "NS" : "EW"), false);
            });

            await host.WaitForCompletionAsync();
        }

        [TestMethod, DeploymentItem("TestData\\events.log"), DeploymentItem("TestData\\events.table2.log")]
        public async Task TableManager_EventsClient_Test()
        {
            Log.Level = 1;
            var tmc = new TableManagerEventsClient();

            using (var sr = new StreamReader("events.log"))
            {
                while (!sr.EndOfStream)
                {
                    var eventLine = await sr.ReadLineAsync();
                    eventLine = eventLine.Substring(eventLine.IndexOf(' ') + 1);
                    await tmc.ProcessEvent(eventLine);
                }
            }

            Assert.AreEqual<int>(2, tmc.Tournament.Boards.Count);
            foreach (var board in tmc.Tournament.Boards)
            {
                Assert.AreEqual<int>(1, board.Results.Count);
            }

            using (var sr = new StreamReader("events.table2.log"))
            {
                while (!sr.EndOfStream)
                {
                    var eventLine = await sr.ReadLineAsync();
                    eventLine = eventLine.Substring(eventLine.IndexOf(' ') + 1);
                    await tmc.ProcessEvent(eventLine);
                }
            }

            Assert.AreEqual<int>(2, tmc.Tournament.Boards.Count);
            foreach (var board in tmc.Tournament.Boards)
            {
                Assert.AreEqual<int>(2, board.Results.Count);
            }

            tmc.Tournament.CalcTournamentScores();

            Assert.AreEqual<double>(8, tmc.Tournament.Boards[0].Results[0].TournamentScore);
            Assert.AreEqual<double>(-8, tmc.Tournament.Boards[0].Results[1].TournamentScore);
            Assert.AreEqual<double>(8, tmc.Tournament.Participants[0].TournamentScore);
            Assert.AreEqual<double>(0, tmc.Tournament.Participants[1].TournamentScore);

            var pbnBuffer = new MemoryStream();
            Pbn2Tournament.Save(tmc.Tournament, pbnBuffer);
            
        }

        [TestMethod, DeploymentItem("TestData\\events.log"), DeploymentItem("TestData\\events.table2.log")]
        public async Task TableManager_EventsClient_Replay()
        {
            Log.Level = 1;
            var tmc = new TableManagerEventsClient();

            using (var sr = new StreamReader("events.log"))
            {
                while (!sr.EndOfStream)
                {
                    var eventLine = await sr.ReadLineAsync();
                    eventLine = eventLine.Substring(eventLine.IndexOf(' ') + 1);
                    await tmc.ProcessEvent(eventLine);
                }
            }

            Assert.AreEqual<int>(2, tmc.Tournament.Boards.Count);
            foreach (var board in tmc.Tournament.Boards)
            {
                Assert.AreEqual<int>(1, board.Results.Count);
            }

            // replaying the same log
            using (var sr = new StreamReader("events.log"))
            {
                while (!sr.EndOfStream)
                {
                    var eventLine = await sr.ReadLineAsync();
                    eventLine = eventLine.Substring(eventLine.IndexOf(' ') + 1);
                    await tmc.ProcessEvent(eventLine);
                }
            }

            Assert.AreEqual<int>(2, tmc.Tournament.Boards.Count);
            foreach (var board in tmc.Tournament.Boards)
            {
                Assert.AreEqual<int>(1, board.Results.Count);
            }
        }

        [TestMethod, DeploymentItem("TestData\\WC2005final01.pbn")]
        public async Task TableManager_2Tables_Test()
        {
            Log.Level = 1;
            var port1 = GetNextPort();
            var host1 = new TestHost(HostMode.SingleTableTwoRounds, port1, new BridgeEventBus("Host1"), "WC2005final01.pbn");

            var vms = new SeatCollection<TestClient>();
            Parallel.For(0, 4, (i) =>
            {
                Seats s = (Seats)i;
                vms[s] = new TestClient();
                vms[s].Connect(s, "localhost", port1, 120, 1, "Robo" + (s == Seats.North || s == Seats.South ? "NS" : "EW"), false);
            });

            var port2 = GetNextPort();
            var host2 = new TestHost(HostMode.SingleTableTwoRounds, port2, new BridgeEventBus("Host2"), "WC2005final01.pbn");

            var vms2 = new SeatCollection<TestClient>();
            Parallel.For(0, 4, (i) =>
            {
                Seats s = (Seats)i;
                vms2[s] = new TestClient();
                vms2[s].Connect(s, "localhost", port2, 120, 1, "Robo" + (s == Seats.North || s == Seats.South ? "NS" : "EW"), false);
            });

            await host1.WaitForCompletionAsync();
        }

        [TestMethod, DeploymentItem("TestData\\WC2005final01.pbn"), DeploymentItem("TestData\\SingleBoard.pbn")]
        //[TestMethod, DeploymentItem("TestData\\SingleBoard.pbn")]
        public async Task TableManager_InstantReplay()
        {
            Log.Level = 1;
            var port = GetNextPort();
            //var host1 = new TestHost(HostMode.SingleTableInstantReplay, port, new BridgeEventBus("Host1"), "WC2005final01.pbn");
            var host1 = new TestHost(HostMode.SingleTableInstantReplay, port, new BridgeEventBus("Host1"), "SingleBoard.pbn");

            var vms = new SeatCollection<TestClient>();
            Parallel.For(0, 4, (i) =>
            {
                Seats s = (Seats)i;
                vms[s] = new TestClient();
                vms[s].Connect(s, "localhost", port, 120, s.Direction() == Directions.EastWest ? 0 : 0, "Robo" + (s == Seats.North || s == Seats.South ? "NS" : "EW"), false);
            });

            await host1.WaitForCompletionAsync();
        }


        private class TestHost : TableManagerTcpHost<TcpClientData>
        {
            private string tournamentFileName;

            public TestHost(HostMode mode, int port, BridgeEventBus bus, string _tournamentFileName) : base(mode, port, bus)
            {
                this.tournamentFileName = _tournamentFileName;
                this.OnHostEvent += HandleHostEvent;
            }

            private void HandleHostEvent(TableManagerHost<TcpClientData> sender, HostEvents hostEvent, object eventData)
            {
                switch (hostEvent)
                {
                    case HostEvents.ReadyForTeams:
                        sender.HostTournament(this.tournamentFileName, 1);
                        break;
                }
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
                var bot = new ChampionshipRobot(_seat, _maxTimePerCard, bus);
                bus.HandleTournamentStarted(Scorings.scIMP, _maxTimePerBoard, _maxTimePerCard, "");
                bus.HandleRoundStarted(new SeatCollection<string>(), new DirectionDictionary<string>("RoboBridge", "RoboBridge"));
                var connectionManager = new TableManagerClient<TcpCommunicationDetails>(bus);
                connectionManager.Connect(_seat, _maxTimePerBoard, _maxTimePerCard, teamName, 18, new TcpCommunicationDetails(serverName, portNumber));
            }

            private class ChampionshipRobot : TestRobot
            {
                private Scorings scoring;
                private int maxTimePerBoard;
                private int maxTimePerCard;
                private string tournamentName;

                public ChampionshipRobot(Seats seat, int _maxTimePerCard, BridgeEventBus bus) : base(seat, bus)
                {
                    this.maxTimePerCard = _maxTimePerCard;
                }

                public override void HandleTournamentStarted(Scorings _scoring, int _maxTimePerBoard, int _maxTimePerCard, string _tournamentName)
                {
                    this.scoring = _scoring;
                    this.maxTimePerBoard = _maxTimePerBoard;
                    this.maxTimePerCard = _maxTimePerCard;
                    this.tournamentName = _tournamentName;
                }

                public override async Task<Bid> FindBid(Bid lastRegularBid, bool allowDouble, bool allowRedouble)
                {
                    //TODO: implement your own logic
                    await Task.Delay(1000 * this.maxTimePerCard);
                    return await base.FindBid(lastRegularBid, allowDouble, allowRedouble);
                }

                public override async Task<Card> FindCard(Seats whoseTurn, Suits leadSuit, Suits trump, bool trumpAllowed, int leadSuitLength, int trick)
                {
                    //TODO: implement your own logic
                    await Task.Delay(1000 * this.maxTimePerCard);
                    return await base.FindCard(whoseTurn, leadSuit, trump, trumpAllowed, leadSuitLength, trick);
                }
            }
        }
    }

    public abstract class TcpTestBase : BridgeTestBase
    {
        private static int nextPort = 3000;
        protected int GetNextPort() => nextPort++;
    }
}
