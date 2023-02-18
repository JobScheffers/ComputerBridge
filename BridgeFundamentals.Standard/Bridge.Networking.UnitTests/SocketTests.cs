using Bridge.Test.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Threading.Tasks;

namespace Bridge.Networking.UnitTests
{
    [TestClass]
    public class TableManagerTests : TcpTestBase
    {
        [TestMethod, DeploymentItem("TestData\\SingleBoard.pbn")]
        public async Task RawSocket()
        {
            Log.Level = 5;
            var port = GetNextPort();
            var tester = new CommunicationTester<SocketClientData, RawSocketCommunicationDetails<RealWebSocketClient>>();
            await tester.Run(port, "SingleBoard.pbn", () => new RawSocketCommunicationDetails<RealWebSocketClient>(new RealWebSocketClient($"ws://localhost:{port}"), "TeamA-TeamB"));
        }

        [TestMethod, DeploymentItem("TestData\\SingleBoard.pbn")]
        public async Task Tcp()
        {
            Log.Level = 1;
            var port = GetNextPort();
            var tester = new CommunicationTester<TcpClientData, TcpCommunicationDetails>();
            await tester.Run(port, "SingleBoard.pbn", () => new TcpCommunicationDetails("localhost", port));
        }
    }

    public class CommunicationTester<THostData, TClient> where THostData : TcpClientData, new() where TClient : CommunicationDetails
    {
        public async Task Run(int port, string tournamentName, Func<TClient> c)
        {
            var host = new TestHost<THostData>(HostMode.SingleTableInstantReplay, port, new BridgeEventBus("host"), tournamentName);

            var vms = new SeatCollection<TestClient<TClient>>();
            for (Seats s = Seats.North; s <= Seats.West; s++)
            {
                vms[s] = new TestClient<TClient>();
                await vms[s].Connect(s, 120, s.Direction() == Directions.EastWest ? 0 : 0, "Robo" + (s == Seats.North || s == Seats.South ? "NS" : "EW"), 18, c());
            };

            await host.WaitForCompletionAsync();
            Log.Trace(2, "after host.WaitForCompletionAsync");
            for (Seats s = Seats.North; s <= Seats.West; s++)
            {
                await vms[s].Disconnect();
            };
            Log.Trace(2, "after client.DisposeAsync");
        }
    }
}
