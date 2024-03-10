using Bridge.Test.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Threading.Tasks;

namespace Bridge.Networking.UnitTests
{
    //[TestClass]
    //public class CommunicationTests : TcpTestBase
    //{
    //    [TestMethod]
    //    public async Task Communicate_Over_RawSocket()
    //    {
    //        Log.Level = 4;
    //        var port = GetNextPort();
    //        //            await CommunicationTester.Run<RawSocketCommunicationDetails<RealWebSocketClient>>(  port, "SingleBoard.pbn", seat => new RawSocketCommunicationDetails<RealWebSocketClient>(new RealWebSocketClient($"ws://localhost:{port}"), "TeamA-TeamB"));
    //        await CommunicationTester.Run<RawSocketCommunicationDetails<RealWebSocketClient>>(new HostSocketCommunication(port, "host"));
    //    }

    //    //[TestMethod]
    //    //public async Task Communicate_Over_Tcp()
    //    //{
    //    //    Log.Level = 4;
    //    //    var port = GetNextPort();
    //    //    var tester = new TcpCommunicationTester<ClientTcpCommunicationDetails>();
    //    //    await tester.Run(port, "SingleBoard.pbn", seat => new ClientTcpCommunicationDetails("localhost", port, seat.ToString()));
    //    //}
    //}

    //public static class CommunicationTester
    //{
    //    public static async Task Run<TClient>(HostCommunication communicationDetails) where TClient : ClientCommunicationDetails
    //    {
    //        var host = new AsyncHost(communicationDetails, "host");
    //        host.Run();

    //        var vms = new TestClient<TClient>();
    //        //await vms.Connect(s, 120, s.Direction() == Directions.EastWest ? 0 : 0, "Robo" + (s == Seats.North || s == Seats.South ? "NS" : "EW"), 18, c(s));

    //        await host.WaitForCompletionAsync();
    //        Log.Trace(2, "after host.WaitForCompletionAsync");
    //            await vms.Disconnect();
    //        Log.Trace(2, "after client.DisposeAsync");
    //    }

    //    private class TestClient<TCommunication> where TCommunication : ClientCommunicationDetails
    //    {
    //        private TableManagerClientAsync<TCommunication> connectionManager;

    //        public async Task Connect(Seats _seat, int _maxTimePerBoard, int _maxTimePerCard, string teamName, int protocolVersion, TCommunication communicationDetails)
    //        {
    //            connectionManager = new TableManagerClientAsync<TCommunication>(bus);
    //            await connectionManager.Connect(_seat, _maxTimePerBoard, _maxTimePerCard, teamName, protocolVersion, communicationDetails);
    //        }

    //        public async Task Disconnect()
    //        {
    //            try
    //            {
    //                await connectionManager.DisposeAsync();
    //                robot = null;
    //            }
    //            catch
    //            {

    //            }
    //        }

    //        private class ChampionshipRobot : TestRobot
    //        {
    //            private int maxTimePerCard;

    //            public ChampionshipRobot(Seats seat, int _maxTimePerCard, BridgeEventBus bus) : base(seat, bus)
    //            {
    //                this.maxTimePerCard = _maxTimePerCard;
    //            }

    //            public override void HandleTournamentStarted(Scorings _scoring, int _maxTimePerBoard, int _maxTimePerCard, string _tournamentName)
    //            {
    //                this.maxTimePerCard = _maxTimePerCard;
    //            }

    //            public override async Task<Bid> FindBid(Bid lastRegularBid, bool allowDouble, bool allowRedouble)
    //            {
    //                await Task.Delay(1000 * this.maxTimePerCard);
    //                return await base.FindBid(lastRegularBid, allowDouble, allowRedouble);
    //            }

    //            public override async Task<Card> FindCard(Seats whoseTurn, Suits leadSuit, Suits trump, bool trumpAllowed, int leadSuitLength, int trick)
    //            {
    //                await Task.Delay(1000 * this.maxTimePerCard);
    //                return await base.FindCard(whoseTurn, leadSuit, trump, trumpAllowed, leadSuitLength, trick);
    //            }
    //        }
    //    }
    //}

}
