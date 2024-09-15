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
            Log.Level = 1;
            var port = GetNextPort();
            var tester = new SocketCommunicationTester<RawSocketCommunicationDetails<RealWebSocketClient>>();
            await tester.Run(port, "SingleBoard.pbn", seat => new RawSocketCommunicationDetails<RealWebSocketClient>(new RealWebSocketClient($"ws://localhost:{port}"), "TeamA-TeamB"));
        }

        [TestMethod, DeploymentItem("TestData\\SingleBoard.pbn")]
        public async Task Tcp()
        {
            Log.Level = 1;
            var port = GetNextPort();
            var tester = new TcpCommunicationTester<ClientTcpCommunicationDetails>();
            await tester.Run(port, "SingleBoard.pbn", seat => new ClientTcpCommunicationDetails("localhost", port, seat.ToString()));
        }
    }

    public class TcpCommunicationTester<TClient> where TClient : ClientCommunicationDetails
    {
        public async Task Run(int port, string tournamentName, Func<Seats, TClient> c)
        {
            var tournament = await PbnHelper.LoadFile(tournamentName);
            var host = new TestTcpHost(HostMode.SingleTableInstantReplay, port, "host", tournament);
            host.Run();

            var vms = new SeatCollection<TestClient<TClient>>();
            for (Seats s = Seats.North; s <= Seats.West; s++)
            {
                vms[s] = new TestClient<TClient>();
                await vms[s].Connect(s, 120, s.Direction() == Directions.EastWest ? 0 : 0, "Robo" + (s == Seats.North || s == Seats.South ? "NS" : "EW"), 18, c(s));
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

    public class SocketCommunicationTester<TClient> where TClient : ClientCommunicationDetails
    {
        public async Task Run(int port, string tournamentName, Func<Seats, TClient> c)
        {
            var tournament = await PbnHelper.LoadFile(tournamentName);
            var host = new TestSocketHost(HostMode.SingleTableInstantReplay, port, "host", tournament);
            host.Run();

            var vms = new SeatCollection<TestClient<TClient>>();
            for (Seats s = Seats.North; s <= Seats.West; s++)
            {
                vms[s] = new TestClient<TClient>();
                await vms[s].Connect(s, 120, s.Direction() == Directions.EastWest ? 0 : 0, "Robo" + (s == Seats.North || s == Seats.South ? "NS" : "EW"), 18, c(s));
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
