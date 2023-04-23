﻿#define useOwnHost

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;
using Bridge.Test.Helpers;
using System.IO;

namespace Bridge.Networking.UnitTests
{
    [TestClass]
    public class TableManagerTcpHostTests : TcpTestBase
    {
#if DEBUG
        //[TestMethod]
#endif
        public async Task TableManager_Client_Test()
        {
            // test against a real TableManager
            Log.Level = 1;
            var port = 2000;

            var vms = new SeatCollection<TcpTestClient>();
            await SeatsExtensions.ForEachSeatAsync(async s =>
            {
                vms[s] = new TcpTestClient();
                await vms[s].Connect(s, "localhost", port, 120, 1, "Robo" + (s == Seats.North || s == Seats.South ? "NS" : "EW"));
            });

            await Task.Delay(10000);
        }

        [TestMethod, DeploymentItem("TestData\\SingleBoard.pbn")]
        public async Task TableManager_NewTcp_Test()
        {
            Log.Level = 4;
            var port1 = GetNextPort();
            await using var host1 = new TableManagerTcpHost(HostMode.SingleTableTwoRounds, new(port1, "Host1"), new BridgeEventBus($"Host1@{port1}"), "Host1", "SingleBoard.pbn");
            host1.Run();

            var vms = new SeatCollection<TcpTestClient>();
            await SeatsExtensions.ForEachSeatAsync(async s =>
            {
                vms[s] = new TcpTestClient();
                await vms[s].Connect(s, "localhost", port1, 120, 1, "Robo" + (s == Seats.North || s == Seats.South ? "NS" : "EW"));
            });

            await host1.WaitForCompletionAsync();
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
            Log.Level = 4;
            Log.Trace(0, "******** start of TableManager_2Tables_Test");
            var port1 = GetNextPort();
            await using var host1 = new TestHost(HostMode.SingleTableTwoRounds, port1, "Host1", "WC2005final01.pbn");
            host1.Run();

            var vms = new SeatCollection<TcpTestClient>();
            await SeatsExtensions.ForEachSeatAsync(async s =>
            {
                vms[s] = new TcpTestClient();
                await vms[s].Connect(s, "localhost", port1, 120, 1, "Robo" + (s == Seats.North || s == Seats.South ? "NS" : "EW"));
            });

            var port2 = GetNextPort();
            await using var host2 = new TestHost(HostMode.SingleTableTwoRounds, port2, "Host2", "WC2005final01.pbn");
            host2.Run();

            var vms2 = new SeatCollection<TcpTestClient>();
            await SeatsExtensions.ForEachSeatAsync(async s =>
            {
                vms2[s] = new TcpTestClient();
                await vms2[s].Connect(s, "localhost", port2, 120, 1, "Robo" + (s == Seats.North || s == Seats.South ? "NS" : "EW"));
            });

            Log.Trace(1, "wait for host1 completion");
            await host1.WaitForCompletionAsync();
            Log.Trace(1, "wait for host2 completion");
            await host2.WaitForCompletionAsync();
            Log.Trace(0, "******** end of TableManager_2Tables_Test");
        }

        [TestMethod, DeploymentItem("TestData\\SingleBoard.pbn")]
        public async Task AsyncTableHostTest()
        {
            Log.Level = 1;
            Log.Trace(0, "******** start of AsyncTableHostTest");
            var port1 = GetNextPort();
            await using var host1 = new AsyncTableHost<HostTcpCommunication>(HostMode.SingleTableTwoRounds, new HostTcpCommunication(port1, "Host"), new BridgeEventBus("Host"), "Host", "SingleBoard.pbn");
            host1.Run();

            var vms = new SeatCollection<TcpTestClient>();
            await SeatsExtensions.ForEachSeatAsync(async s =>
            {
                vms[s] = new TcpTestClient();
                await vms[s].Connect(s, "localhost", port1, 120, 1, "Robo" + (s == Seats.North || s == Seats.South ? "NS" : "EW"));
            });

            Log.Trace(1, "wait for host1 completion");
            await host1.WaitForCompletionAsync();
            Log.Trace(0, "******** end of AsyncTableHostTest");
        }

        private class TcpTestClient : TestClient<ClientTcpCommunicationDetails>
        {
            public async Task Connect(Seats _seat, string _serverAddress, int _serverPort, int _maxTimePerBoard, int _maxTimePerCard, string teamName)
            {
                await base.Connect(_seat, _maxTimePerBoard, _maxTimePerCard, teamName, 18, new ClientTcpCommunicationDetails(_serverAddress, _serverPort, _seat.ToString()));
            }
        }
    }

    /// <summary>
    /// Test client with robot for all communication protocols
    /// </summary>
    /// <typeparam name="TCommunication"></typeparam>
    public class TestClient<TCommunication> where TCommunication : ClientCommunicationDetails
    {
        private TableManagerClientAsync<TCommunication> connectionManager;

        public async Task Connect(Seats _seat, int _maxTimePerBoard, int _maxTimePerCard, string teamName, int protocolVersion, TCommunication communicationDetails)
        {
            var bus = new BridgeEventBus("TM_Client " + _seat);
            new ChampionshipRobot(_seat, _maxTimePerCard, bus);
            bus.HandleTournamentStarted(Scorings.scIMP, _maxTimePerBoard, _maxTimePerCard, "");
            bus.HandleRoundStarted(new SeatCollection<string>(), new DirectionDictionary<string>("RoboBridge", "RoboBridge"));
            connectionManager = new TableManagerClientAsync<TCommunication>(bus);
            await connectionManager.Connect(_seat, _maxTimePerBoard, _maxTimePerCard, teamName, protocolVersion, communicationDetails);
        }

        public async Task Disconnect()
        {
            try
            {
                await connectionManager.DisposeAsync();
            }
            catch
            {

            }
        }

        private class ChampionshipRobot : TestRobot
        {
            private int maxTimePerCard;

            public ChampionshipRobot(Seats seat, int _maxTimePerCard, BridgeEventBus bus) : base(seat, bus)
            {
                this.maxTimePerCard = _maxTimePerCard;
            }

            public override void HandleTournamentStarted(Scorings _scoring, int _maxTimePerBoard, int _maxTimePerCard, string _tournamentName)
            {
                this.maxTimePerCard = _maxTimePerCard;
            }

            public override async Task<Bid> FindBid(Bid lastRegularBid, bool allowDouble, bool allowRedouble)
            {
                await Task.Delay(1000 * this.maxTimePerCard);
                return await base.FindBid(lastRegularBid, allowDouble, allowRedouble);
            }

            public override async Task<Card> FindCard(Seats whoseTurn, Suits leadSuit, Suits trump, bool trumpAllowed, int leadSuitLength, int trick)
            {
                await Task.Delay(1000 * this.maxTimePerCard);
                return await base.FindCard(whoseTurn, leadSuit, trump, trumpAllowed, leadSuitLength, trick);
            }
        }
    }

    public class TestHost : TableManagerTcpHost
    {
        public TestHost(HostMode mode, int port, string hostName, string _tournamentFileName) : base(mode, new(port, hostName), new(hostName), hostName, _tournamentFileName)
        {
        }

        protected override void ExplainBid(Seats source, Bid bid)
        {
            bid.UnAlert();
        }
    }
}
