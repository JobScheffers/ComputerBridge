﻿#define useOwnHost

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;
using Bridge.Test.Helpers;
using System.IO;
using System.Net;

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
            var communicationFactory = new TcpCommunicationFactory(new IPEndPoint(new IPAddress([127, 0, 0, 1]), 2000));
            var vms = new SeatCollection<TestClient>();
            await SeatsExtensions.ForEachSeatAsync(async s =>
            {
                vms[s] = new TestClient(s, new ClientComputerBridgeProtocol("Robo" + (s == Seats.North || s == Seats.South ? "NS" : "EW"), 19, communicationFactory.CreateClient()));
                await vms[s].Connect("");
            });

            await Task.Delay(10000);
        }

        [TestMethod, DeploymentItem("TestData\\SingleBoard.pbn")]
        public async Task TableManager_NewTcp_Test()
        {
            Log.Level = 4;
            var port1 = GetNextPort();
            await using var host1 = new TableManagerTcpHost(HostMode.SingleTableTwoRounds, new(port1, "Host1"), new BridgeEventBus($"Host1@{port1}"), "Host1", await PbnHelper.LoadFile("SingleBoard.pbn"), AlertMode.SelfExplaining, Scorings.scIMP, 1, "", "");
            host1.OnHostEvent = async (a, b, c) => 
            {
                await Task.CompletedTask;
            };
            host1.Run();

            var communicationFactory = new TcpCommunicationFactory(new IPEndPoint(new IPAddress([127, 0, 0, 1]), port1));
            var vms = new SeatCollection<TestClient>();
            await SeatsExtensions.ForEachSeatAsync(async s =>
            {
                vms[s] = new TestClient(s, new ClientComputerBridgeProtocol("Robo" + (s == Seats.North || s == Seats.South ? "NS" : "EW"), 19, communicationFactory.CreateClient()));
                await vms[s].Connect("");
            });

            var hacker = new TestClient(Seats.North, new ClientComputerBridgeProtocol("RoboX", 19, communicationFactory.CreateClient()));
            await hacker.Connect("");

            //await vms[Seats.East].Disconnect();   // test OnConnectionLost

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
            PbnHelper.Save(tmc.Tournament, pbnBuffer);
            
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

        //[TestMethod, DeploymentItem("TestData\\WC2005final01.pbn")]
        public async Task TableManager_2Tables_Test()
        {
            Log.Level = 4;
            Log.Trace(0, "******** start of TableManager_2Tables_Test");

            var tournament = await PbnHelper.LoadFile("WC2005final01.pbn");
            var port1 = GetNextPort();
            await using var host1 = new TestTcpHost(HostMode.SingleTableTwoRounds, port1, "Host1", tournament);
            host1.Run();

            var communicationFactory = new TcpCommunicationFactory(new IPEndPoint(new IPAddress([127, 0, 0, 1]), port1));
            var vms = new SeatCollection<TestClient>();
            await SeatsExtensions.ForEachSeatAsync(async s =>
            {
                vms[s] = new TestClient(s, new ClientComputerBridgeProtocol("Robo" + (s == Seats.North || s == Seats.South ? "NS" : "EW"), 19, communicationFactory.CreateClient()));
                await vms[s].Connect("");
            });

            var port2 = GetNextPort();
            await using var host2 = new TestTcpHost(HostMode.SingleTableTwoRounds, port2, "Host2", tournament);
            host2.Run();

            var communicationFactory2 = new TcpCommunicationFactory(new IPEndPoint(new IPAddress([127, 0, 0, 1]), port1));
            var vms2 = new SeatCollection<TestClient>();
            await SeatsExtensions.ForEachSeatAsync(async s =>
            {
                vms2[s] = new TestClient(s, new ClientComputerBridgeProtocol("Robo" + (s == Seats.North || s == Seats.South ? "NS" : "EW"), 19, communicationFactory2.CreateClient()));
                await vms[s].Connect("");
            });

            Log.Trace(1, "wait for host1 completion");
            await host1.WaitForCompletionAsync();
            Log.Trace(1, "wait for host2 completion");
            await host2.WaitForCompletionAsync();
            Log.Trace(0, "******** end of TableManager_2Tables_Test");
        }

        [TestMethod, DeploymentItem("TestData\\SingleBoard.pbn")]
        public async Task GibTest()
        {
            Log.Level = 5;
            Log.Trace(0, "******** start of AsyncTableHostTest");
            var port1 = GetNextPort();
            await using var host1 = new AsyncTableHost<HostTcpCommunication>(HostMode.SingleTableInstantReplay, new HostTcpCommunication(port1, "Host"), new BridgeEventBus("Host"), "Host", await PbnHelper.LoadFile("SingleBoard.pbn"), AlertMode.SelfExplaining, Scorings.scIMP, 1, "", "");
            host1.OnHostEvent += ConnectionManager_OnHostEvent;
            host1.Run();

            var communicationFactory = new TcpCommunicationFactory(new IPEndPoint(new IPAddress([127, 0, 0, 1]), port1));
            var vms = new SeatCollection<TestClient>();
            await SeatsExtensions.ForEachSeatAsync(async s =>
            {
                vms[s] = new TestClient(s, new ClientComputerBridgeProtocol("Gib" + (s == Seats.North || s == Seats.South ? "NS" : "EW"), 19, communicationFactory.CreateClient()));
                await vms[s].Connect("");
            });

            Log.Trace(1, "wait for host1 completion");
            await host1.WaitForCompletionAsync();
            Log.Trace(0, "******** end of AsyncTableHostTest");
        }

        [TestMethod, DeploymentItem("TestData\\interrupted.pbn")]
        public async Task AsyncTableHost_Resumes_Match()
        {
            Log.Level = 5;
            var port1 = GetNextPort();
            Log.Trace(0, $"******** start of AsyncTableHostTest on port {port1}");
            await using var host1 = new AsyncTableHost<HostTcpCommunication>(HostMode.SingleTableInstantReplay, new HostTcpCommunication(port1, "Host"), new BridgeEventBus("Host"), "Host", await PbnHelper.LoadFile("interrupted.pbn"), AlertMode.SelfExplaining, Scorings.scIMP, 1, "", "");
            host1.OnHostEvent += ConnectionManager_OnHostEvent;
            host1.Run();

            var communicationFactory = new TcpCommunicationFactory(new IPEndPoint(new IPAddress([127, 0, 0, 1]), port1));
            var vms = new SeatCollection<TestClient>();
            await SeatsExtensions.ForEachSeatAsync(async s =>
            {
                vms[s] = new TestClient(s, new ClientComputerBridgeProtocol("Robo" + (s == Seats.North || s == Seats.South ? "NS" : "EW"), 19, communicationFactory.CreateClient()));
                await vms[s].Connect("");
            });

            Log.Trace(1, "wait for host1 completion");
            await host1.WaitForCompletionAsync();
            Log.Trace(0, "******** end of AsyncTableHostTest");
        }

        [TestMethod, DeploymentItem("TestData\\SingleBoard.pbn")]
        //[TestMethod, DeploymentItem("TestData\\rb12rondpas.pbn")]
        public async Task AsyncTableHostTest()
        {
            Log.Level = 1;
            var port1 = GetNextPort();
            Log.Trace(0, $"******** start of AsyncTableHostTest on port {port1}");
            await using var host1 = new AsyncTableHost<HostTcpCommunication>(HostMode.SingleTableTwoRounds, new HostTcpCommunication(port1, "Host"), new BridgeEventBus("Host"), "Host", await PbnHelper.LoadFile(
                "SingleBoard.pbn"
                //"rb12rondpas.pbn"
                ), AlertMode.SelfExplaining, Scorings.scIMP, 1, "", "");
            host1.OnHostEvent += ConnectionManager_OnHostEvent;
            host1.Run();

            var communicationFactory = new TcpCommunicationFactory(new IPEndPoint(new IPAddress([127, 0, 0, 1]), port1));
            var vms = new SeatCollection<TestClient>();
            await SeatsExtensions.ForEachSeatAsync(async s =>
            {
                vms[s] = new TestClient(s, new ClientComputerBridgeProtocol("Robo" + (s == Seats.North || s == Seats.South ? "NS" : "EW"), 19, communicationFactory.CreateClient()));
                await vms[s].Connect("");
            });

            Log.Trace(1, "wait for host1 completion");
            await host1.WaitForCompletionAsync();
            Log.Trace(0, "******** end of AsyncTableHostTest");
        }

        [TestMethod, DeploymentItem("TestData\\SingleBoard.pbn")]
        public async Task Async2TableHostTest()
        {
            Log.Level = 1;
            var port1 = GetNextPort();
            var port2 = GetNextPort();
            Log.Trace(0, $"******** start of AsyncTableHostTest on ports {port1} {port2}");
            var tournament = await PbnHelper.LoadFile("SingleBoard.pbn");
            tournament.ScoringMethod = Scorings.scIMP;

            await using var host1 = new AsyncTableHost<HostTcpCommunication>(HostMode.TwoTables, new HostTcpCommunication(port1, "Host1"), new BridgeEventBus("Host1"), "Host1", tournament, AlertMode.SelfExplaining, Scorings.scIMP, 1, "Robo1", "Robo2");
            host1.OnHostEvent += ConnectionManager_OnHostEvent;
            host1.Run();

            var communicationFactory1 = new TcpCommunicationFactory(new IPEndPoint(new IPAddress([127, 0, 0, 1]), port1));
            var vms1 = new SeatCollection<TestClient>();
            await SeatsExtensions.ForEachSeatAsync(async s =>
            {
                vms1[s] = new TestClient(s, new ClientComputerBridgeProtocol("Robo" + (s == Seats.North || s == Seats.South ? "1" : "2"), 19, communicationFactory1.CreateClient()));
                await vms1[s].Connect(s == Seats.North || s == Seats.South ? "system 1" : "system 2");
            });

            await using var host2 = new AsyncTableHost<HostTcpCommunication>(HostMode.TwoTables, new HostTcpCommunication(port2, "Host2"), new BridgeEventBus("Host2"), "Host2", tournament, AlertMode.SelfExplaining, Scorings.scIMP, 2, "Robo2", "Robo1");
            host2.OnHostEvent += ConnectionManager_OnHostEvent;
            host2.Run();

            var communicationFactory2 = new TcpCommunicationFactory(new IPEndPoint(new IPAddress([127, 0, 0, 1]), port2));
            var vms2 = new SeatCollection<TestClient>();
            await SeatsExtensions.ForEachSeatAsync(async s =>
            {
                vms2[s] = new TestClient(s, new ClientComputerBridgeProtocol("Robo" + (s == Seats.North || s == Seats.South ? "2" : "1"), 19, communicationFactory2.CreateClient()));
                await vms2[s].Connect(s == Seats.North || s == Seats.South ? "system 2" : "system 1");
            });

            Log.Trace(1, "wait for hosts completion");
            await host1.WaitForCompletionAsync();
            await host2.WaitForCompletionAsync();
            Log.Trace(0, "******** end of AsyncTableHostTest");
            using (var stream = File.Create("2tables.pbn"))
            {
                PbnHelper.Save(tournament, stream);
            }
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
                    var host = sender as AsyncTableHost<HostTcpCommunication>;
                    var r = eventData as BoardResult;
                    r.Room = host.Name == "Host1" ? "Open" : "Closed";
                    break;
                case HostEvents.Finished:
                    var t = eventData as PbnTournament;
                    break;
            }
        }

        //private class TcpTestClient : TestClient<ClientTcpCommunicationDetails>
        //{
        //    public async Task Connect(Seats _seat, string _serverAddress, int _serverPort, int _maxTimePerBoard, int _maxTimePerCard, string teamName)
        //    {
        //        await base.Connect(_seat, _maxTimePerBoard, _maxTimePerCard, teamName, 18, new ClientTcpCommunicationDetails(_serverAddress, _serverPort, _seat.ToString()));
        //    }
        //}
    }

    /// <summary>
    /// Test client with robot for all communication protocols
    /// </summary>
    /// <typeparam name="TCommunication"></typeparam>
    public class TestClient<TCommunication> where TCommunication : ClientCommunicationDetails
    {
        private TableManagerClientAsync<TCommunication> connectionManager;
        private ChampionshipRobot robot;

        public async Task Connect(Seats _seat, int _maxTimePerBoard, int _maxTimePerCard, string teamName, int protocolVersion, string systemInfo, TCommunication communicationDetails)
        {
            var bus = new BridgeEventBus("TM_Client " + _seat);
            robot = new ChampionshipRobot(_seat, _maxTimePerCard, bus);
            bus.HandleTournamentStarted(Scorings.scIMP, _maxTimePerBoard, _maxTimePerCard, "");
            bus.HandleRoundStarted(new SeatCollection<string>(), new DirectionDictionary<string>("RoboBridge", "RoboBridge"));
            connectionManager = new TableManagerClientAsync<TCommunication>(bus);
            await connectionManager.Connect(_seat, _maxTimePerBoard, _maxTimePerCard, teamName, protocolVersion, systemInfo, communicationDetails);
        }

        public async Task Disconnect()
        {
            try
            {
                await connectionManager.DisposeAsync();
                robot = null;
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
                //return Bid.C("Pass");
            }

            public override async Task<ExplainedCard> FindCard(Seats whoseTurn, Suits leadSuit, Suits trump, bool trumpAllowed, int leadSuitLength, int trick)
            {
                await Task.Delay(1000 * this.maxTimePerCard);
                return await base.FindCard(whoseTurn, leadSuit, trump, trumpAllowed, leadSuitLength, trick);
            }
        }
    }

    public class TestTcpHost : TableManagerTcpHost
    {
        public TestTcpHost(HostMode mode, int port, string hostName, Tournament _tournament) : base(mode, new(port, hostName), new(hostName), hostName, _tournament, AlertMode.SelfExplaining, Scorings.scIMP, 1, "", "")
        {
        }

        protected override void ExplainBid(Seats source, Bid bid)
        {
            bid.UnAlert();
        }
    }

    public class TestSocketHost : TableManagerSocketHost
    {
        public TestSocketHost(HostMode mode, int port, string hostName, Tournament _tournament) : base(mode, new(port, hostName), new(hostName), hostName, _tournament, AlertMode.SelfExplaining, Scorings.scIMP, 1, "", "")
        {
        }

        protected override void ExplainBid(Seats source, Bid bid)
        {
            bid.UnAlert();
        }
    }
}
