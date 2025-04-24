using Bridge.Test;
using Bridge.Test.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Bridge.Networking.UnitTests
{
    [TestClass]
	public class BridgeEventHandlerTest : TcpTestBase
    {
        //[TestMethod]
        //public async Task BridgeEventHandler_Communication()
        //{
        //    Log.Level = 6;
        //    var port = GetNextPort();
        //    BaseAsyncTcpHost tcpHost = null;
        //    tcpHost = new BaseAsyncTcpHost(new IPEndPoint(IPAddress.Any, port), ProcessClientMessage, HandleNewClient, "host");
        //    var host = tcpHost.Run();
        //    var tcpClient = new ClientTcpCommunicationDetails("localhost", port, "client1");
        //    await tcpClient.Connect(ProcessHostMessage, Seats.North);
        //    await host;

        //    async ValueTask ProcessClientMessage(int clientId, string message)
        //    {
        //        if (message == "Thanks") await tcpHost.Send(clientId, "Let's start");
        //        if (message == "Stop") tcpHost.Stop();
        //    }

        //    async ValueTask HandleNewClient(int clientId)
        //    {
        //        await tcpHost.Send(clientId, "Welcome");
        //    }

        //    async ValueTask ProcessHostMessage(string message)
        //    {
        //        if (message == "Welcome") await tcpClient.WriteProtocolMessageToRemoteMachine("Thanks");
        //        if (message == "Let's start") await tcpClient.WriteProtocolMessageToRemoteMachine("Stop");
        //    }
        //}


        //[TestMethod, DeploymentItem("TestData\\SingleBoard.pbn")]
        [TestMethod, DeploymentItem("TestData\\WC2005final01.pbn")]
        public async Task Host_Tournament()
        {
            Log.Level = 1;
            var port1 = GetNextPort();
            var communicationFactory = new TcpCommunicationFactory(new IPEndPoint(new IPAddress([127, 0, 0, 1]), port1));
            var host1 = new TournamentHost(new HostComputerBridgeProtocol(communicationFactory.CreateHost()), "", "", Scorings.scIMP, AlertMode.SelfExplaining, await PbnHelper.LoadFile(
                //"SingleBoard.pbn"
                "WC2005final01.pbn"
                ));
            //host1.OnHostEvent += ConnectionManager_OnHostEvent;
            host1.Run();

            var vms = new SeatCollection<TestClient>();
            await SeatsExtensions.ForEachSeatAsync(async s =>
            {
                vms[s] = new TestClient(s, new ClientComputerBridgeProtocol("Robo" + (s == Seats.North || s == Seats.South ? "NS" : "EW"), 19, communicationFactory.CreateClient()));
                await vms[s].Connect();
            });

            Log.Trace(1, "wait for host1 completion");
            await host1.WaitForCompletionAsync();
            Log.Trace(0, "******** end of AsyncTableHostTest");
        }

        [TestMethod]
        public async Task BridgeEventHandler_InProcess()
        {
            Log.Level = 4;
            var hostCommunication = new HostInProcessProtocol();
            var host = new TestHost(hostCommunication);
            await BridgeEventHandler_Test(host, seat => new TestClient(seat, new ClientInProcessProtocol(hostCommunication)));
        }

        [TestMethod]
        public async Task BridgeEventHandler_InProcess_ComputerBridge()
        {
            Log.Level = 6;
            var communicationFactory = new InProcessCommunicationFactory();
            var hostCommunication = new HostComputerBridgeProtocol(communicationFactory.CreateHost());
            var host = new TestHost(hostCommunication);
            await BridgeEventHandler_Test(host, seat => new TestClient(seat, new ClientComputerBridgeProtocol(seat == Seats.North || seat == Seats.South ? "TeamA" : "TeamB", 19, communicationFactory.CreateClient())));
        }

        [TestMethod]
        public async Task BridgeEventHandler_Tcp_ComputerBridge()
        {
            Log.Level = 5;
            var communicationFactory = new TcpCommunicationFactory(new IPEndPoint(new IPAddress([127, 0, 0, 1]), GetNextPort()));
            var hostCommunication = new HostComputerBridgeProtocol(communicationFactory.CreateHost());
            var host = new TestHost(hostCommunication);
            
            await BridgeEventHandler_Test(host, seat => new TestClient(seat, new ClientComputerBridgeProtocol(seat == Seats.North || seat == Seats.South ? "TeamA" : "TeamB", 19, communicationFactory.CreateClient())));
        }

        private async Task BridgeEventHandler_Test(AsyncHost host, Func<Seats, AsyncClient> clientFactory)
        {
            var seats = new SeatCollection<AsyncClient>();
            await host.Start();
            await SeatsExtensions.ForEachSeatAsync(async seat =>
            {
                seats[seat] = clientFactory(seat);
                await seats[seat].Connect();
            });
            await host.WaitForClients();
            await host.HandleTournamentStarted(Scorings.scIMP, 120, 100, "");
            await host.HandleRoundStarted(new SeatCollection<string>(), new DirectionDictionary<string>("", ""));
            await host.HandleBoardStarted(1, Seats.East, Vulnerable.Neither);
            await host.HandleCardPosition(Seats.North, Suits.Spades, Ranks.Ten);
            await host.HandleCardPosition(Seats.North, Suits.Spades, Ranks.Five);
            await host.HandleCardPosition(Seats.North, Suits.Hearts, Ranks.Nine);
            await host.HandleCardPosition(Seats.North, Suits.Hearts, Ranks.Eight);
            await host.HandleCardPosition(Seats.North, Suits.Hearts, Ranks.Two);
            await host.HandleCardPosition(Seats.North, Suits.Diamonds, Ranks.Eight);
            await host.HandleCardPosition(Seats.North, Suits.Diamonds, Ranks.Seven);
            await host.HandleCardPosition(Seats.North, Suits.Diamonds, Ranks.Four);
            await host.HandleCardPosition(Seats.North, Suits.Clubs, Ranks.Ace);
            await host.HandleCardPosition(Seats.North, Suits.Clubs, Ranks.Queen);
            await host.HandleCardPosition(Seats.North, Suits.Clubs, Ranks.Six);
            await host.HandleCardPosition(Seats.North, Suits.Clubs, Ranks.Three);
            await host.HandleCardPosition(Seats.North, Suits.Clubs, Ranks.Two);
            await host.HandleCardPosition(Seats.East, Suits.Spades, Ranks.King);
            await host.HandleCardPosition(Seats.East, Suits.Spades, Ranks.Four);
            await host.HandleCardPosition(Seats.East, Suits.Spades, Ranks.Three);
            await host.HandleCardPosition(Seats.East, Suits.Hearts, Ranks.Seven);
            await host.HandleCardPosition(Seats.East, Suits.Hearts, Ranks.Three);
            await host.HandleCardPosition(Seats.East, Suits.Diamonds, Ranks.King);
            await host.HandleCardPosition(Seats.East, Suits.Diamonds, Ranks.Queen);
            await host.HandleCardPosition(Seats.East, Suits.Diamonds, Ranks.Five);
            await host.HandleCardPosition(Seats.East, Suits.Clubs, Ranks.King);
            await host.HandleCardPosition(Seats.East, Suits.Clubs, Ranks.Jack);
            await host.HandleCardPosition(Seats.East, Suits.Clubs, Ranks.Ten);
            await host.HandleCardPosition(Seats.East, Suits.Clubs, Ranks.Five);
            await host.HandleCardPosition(Seats.East, Suits.Clubs, Ranks.Four);
            await host.HandleCardPosition(Seats.South, Suits.Spades, Ranks.Ace);
            await host.HandleCardPosition(Seats.South, Suits.Spades, Ranks.Jack);
            await host.HandleCardPosition(Seats.South, Suits.Spades, Ranks.Nine);
            await host.HandleCardPosition(Seats.South, Suits.Hearts, Ranks.Ace);
            await host.HandleCardPosition(Seats.South, Suits.Hearts, Ranks.Queen);
            await host.HandleCardPosition(Seats.South, Suits.Hearts, Ranks.Ten);
            await host.HandleCardPosition(Seats.South, Suits.Hearts, Ranks.Six);
            await host.HandleCardPosition(Seats.South, Suits.Diamonds, Ranks.Jack);
            await host.HandleCardPosition(Seats.South, Suits.Diamonds, Ranks.Ten);
            await host.HandleCardPosition(Seats.South, Suits.Diamonds, Ranks.Six);
            await host.HandleCardPosition(Seats.South, Suits.Diamonds, Ranks.Two);
            await host.HandleCardPosition(Seats.South, Suits.Clubs, Ranks.Nine);
            await host.HandleCardPosition(Seats.South, Suits.Clubs, Ranks.Eight);
            await host.HandleCardPosition(Seats.West, Suits.Spades, Ranks.Queen);
            await host.HandleCardPosition(Seats.West, Suits.Spades, Ranks.Eight);
            await host.HandleCardPosition(Seats.West, Suits.Spades, Ranks.Seven);
            await host.HandleCardPosition(Seats.West, Suits.Spades, Ranks.Six);
            await host.HandleCardPosition(Seats.West, Suits.Spades, Ranks.Two);
            await host.HandleCardPosition(Seats.West, Suits.Hearts, Ranks.King);
            await host.HandleCardPosition(Seats.West, Suits.Hearts, Ranks.Jack);
            await host.HandleCardPosition(Seats.West, Suits.Hearts, Ranks.Five);
            await host.HandleCardPosition(Seats.West, Suits.Hearts, Ranks.Four);
            await host.HandleCardPosition(Seats.West, Suits.Diamonds, Ranks.Ace);
            await host.HandleCardPosition(Seats.West, Suits.Diamonds, Ranks.Nine);
            await host.HandleCardPosition(Seats.West, Suits.Diamonds, Ranks.Three);
            await host.HandleCardPosition(Seats.West, Suits.Clubs, Ranks.Seven);
            await host.HandleCardDealingEnded();
            

            //await host.HandleCardNeeded(Seats.South, Seats.South, Suits.NoTrump, Suits.NoTrump, true, 0, 1);
            //await host.HandleCardNeeded(Seats.East , Seats.West , Suits.NoTrump, Suits.NoTrump, true, 0, 1);
            //await host.HandleCardNeeded(Seats.North, Seats.North, Suits.NoTrump, Suits.NoTrump, true, 0, 1);
            //await host.HandleCardNeeded(Seats.East , Seats.East , Suits.NoTrump, Suits.NoTrump, true, 0, 1);
            //await host.HandleCardNeeded(Seats.East , Seats.East , Suits.NoTrump, Suits.NoTrump, true, 0, 2);
            //await host.HandleCardNeeded(Seats.South, Seats.South, Suits.NoTrump, Suits.NoTrump, true, 0, 2);
            //await host.HandleCardNeeded(Seats.East, Seats.West , Suits.NoTrump, Suits.NoTrump, true, 0, 2);
            //await host.HandleCardNeeded(Seats.North, Seats.North, Suits.NoTrump, Suits.NoTrump, true, 0, 2);
            //await host.HandleCardNeeded(Seats.East, Seats.West, Suits.NoTrump, Suits.NoTrump, true, 0, 3);
            //await host.HandleCardNeeded(Seats.North, Seats.North, Suits.NoTrump, Suits.NoTrump, true, 0, 3);
            //await host.HandleCardNeeded(Seats.East, Seats.East, Suits.NoTrump, Suits.NoTrump, true, 0, 3);
            //await host.HandleCardNeeded(Seats.South, Seats.South, Suits.NoTrump, Suits.NoTrump, true, 0, 3);
            //await host.HandleCardNeeded(Seats.North, Seats.North, Suits.NoTrump, Suits.Spades, true, 0, 4);
            //await host.HandleCardNeeded(Seats.East, Seats.East, Suits.NoTrump, Suits.Spades, true, 0, 4);
            //await host.HandleCardNeeded(Seats.South, Seats.South, Suits.NoTrump, Suits.Spades, true, 0, 4);
            //await host.HandleCardNeeded(Seats.East, Seats.West, Suits.NoTrump, Suits.Spades, true, 0, 4);
            //await host.HandleCardNeeded(Seats.East, Seats.West, Suits.NoTrump, Suits.Spades, true, 0, 5);
            //await host.HandleCardNeeded(Seats.North, Seats.North, Suits.NoTrump, Suits.Spades, true, 0, 5);
            //await host.HandleCardNeeded(Seats.East, Seats.East, Suits.NoTrump, Suits.Spades, true, 0, 5);
            //await host.HandleCardNeeded(Seats.South, Seats.South, Suits.NoTrump, Suits.Spades, true, 0, 5);
            //await host.HandleCardNeeded(Seats.East, Seats.East, Suits.NoTrump, Suits.Spades, true, 0, 6);
            //await host.HandleCardNeeded(Seats.South, Seats.South, Suits.NoTrump, Suits.Spades, true, 0, 6);
            //await host.HandleCardNeeded(Seats.East, Seats.West, Suits.NoTrump, Suits.Spades, true, 0, 6);
            //await host.HandleCardNeeded(Seats.North, Seats.North, Suits.NoTrump, Suits.Spades, true, 0, 6);
            //await host.HandleCardNeeded(Seats.South, Seats.South, Suits.NoTrump, Suits.Spades, true, 0, 7);
            //await host.HandleCardNeeded(Seats.East, Seats.West, Suits.NoTrump, Suits.Spades, true, 0, 7);
            //await host.HandleCardNeeded(Seats.North, Seats.North, Suits.NoTrump, Suits.Spades, true, 0, 7);
            //await host.HandleCardNeeded(Seats.East, Seats.East, Suits.NoTrump, Suits.Spades, true, 0, 7);
            //await host.HandleCardNeeded(Seats.North, Seats.North, Suits.NoTrump, Suits.Spades, true, 0, 8);
            //await host.HandleCardNeeded(Seats.East, Seats.East, Suits.NoTrump, Suits.Spades, true, 0, 8);
            //await host.HandleCardNeeded(Seats.South, Seats.South, Suits.NoTrump, Suits.Spades, true, 0, 8);
            //await host.HandleCardNeeded(Seats.East, Seats.West, Suits.NoTrump, Suits.Spades, true, 0, 8);
            //await host.HandleCardNeeded(Seats.North, Seats.North, Suits.NoTrump, Suits.Spades, true, 0, 9);
            //await host.HandleCardNeeded(Seats.East, Seats.East, Suits.NoTrump, Suits.Spades, true, 0, 9);
            //await host.HandleCardNeeded(Seats.South, Seats.South, Suits.NoTrump, Suits.Spades, true, 0, 9);
            //await host.HandleCardNeeded(Seats.East, Seats.West, Suits.NoTrump, Suits.Spades, true, 0, 9);
            //await host.HandleCardNeeded(Seats.East, Seats.East, Suits.NoTrump, Suits.Spades, true, 0, 10);
            //await host.HandleCardNeeded(Seats.South, Seats.South, Suits.NoTrump, Suits.Spades, true, 0, 10);
            //await host.HandleCardNeeded(Seats.East, Seats.West, Suits.NoTrump, Suits.Spades, true, 0, 10);
            //await host.HandleCardNeeded(Seats.North, Seats.North, Suits.NoTrump, Suits.Spades, true, 0, 10);
            //await host.HandleCardNeeded(Seats.East, Seats.East, Suits.NoTrump, Suits.Spades, true, 0, 11);
            //await host.HandleCardNeeded(Seats.South, Seats.South, Suits.NoTrump, Suits.Spades, true, 0, 11);
            //await host.HandleCardNeeded(Seats.East, Seats.West, Suits.NoTrump, Suits.Spades, true, 0, 11);
            //await host.HandleCardNeeded(Seats.North, Seats.North, Suits.NoTrump, Suits.Spades, true, 0, 11);
            //await host.HandleCardNeeded(Seats.East, Seats.East, Suits.NoTrump, Suits.Spades, true, 0, 12);
            //await host.HandleCardNeeded(Seats.South, Seats.South, Suits.NoTrump, Suits.Spades, true, 0, 12);
            //await host.HandleCardNeeded(Seats.East, Seats.West, Suits.NoTrump, Suits.Spades, true, 0, 12);
            //await host.HandleCardNeeded(Seats.North, Seats.North, Suits.NoTrump, Suits.Spades, true, 0, 12);
            //await host.HandleCardNeeded(Seats.South, Seats.South, Suits.NoTrump, Suits.Spades, true, 0, 13);
            //await host.HandleCardNeeded(Seats.East, Seats.West, Suits.NoTrump, Suits.Spades, true, 0, 13);
            //await host.HandleCardNeeded(Seats.North, Seats.North, Suits.NoTrump, Suits.Spades, true, 0, 13);
            //await host.HandleCardNeeded(Seats.East, Seats.East, Suits.NoTrump, Suits.Spades, true, 0, 13);
            //await host.HandlePlayFinished(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));
            await host.HandleTournamentStopped();

            await SeatsExtensions.ForEachSeatAsync(async seat =>
            {
                await seats[seat].Finish();
            });
            await host.Finish();
        }

        private class TestHost(AsyncHostProtocol _communicator) : AsyncHost(_communicator, "", "")
        {
        }
    }

    public class TestClient(Seats _seat, AsyncClientProtocol communicator) : AsyncClient(_seat, communicator, "TestClient")
    {
        private readonly SimpleRobot robot = new(_seat);

        public override async ValueTask HandleTournamentStarted(Scorings scoring, int maxTimePerBoard, int maxTimePerCard, string tournamentName)
        {
            //Log.Trace(3, $"TestClient.{base.seat}.HandleTournamentStarted");
            await robot.HandleTournamentStarted(scoring, maxTimePerBoard, maxTimePerCard, tournamentName);
        }

        public override async ValueTask HandleRoundStarted(SeatCollection<string> participantNames, DirectionDictionary<string> conventionCards)
        {
            //Log.Trace(3, $"TestClient.{base.seat}.HandleRoundStarted");
            await robot.HandleRoundStarted(participantNames, conventionCards);
        }

        public override async ValueTask HandleBoardStarted(int boardNumber, Seats dealer, Vulnerable vulnerabilty)
        {
            //Log.Trace(3, $"TestClient.{base.seat}.HandleBoardStarted");
            await robot.HandleBoardStarted(boardNumber, dealer, vulnerabilty);
        }

        public override async ValueTask HandleCardPosition(Seats seat, Suits suit, Ranks rank)
        {
            //Log.Trace(3, $"TestClient.{base.seat}.HandleCardPosition {suit.ToXML()}{rank.ToXML()}");
            await robot.HandleCardPosition(seat, suit, rank);
        }

        public override async ValueTask HandleCardDealingEnded()
        {
            //Log.Trace(3, $"TestClient.{base.seat}.HandleCardDealingEnded");
            await robot.HandleCardDealingEnded();
        }

        public override async ValueTask HandleBidNeeded(Seats whoseTurn, Bid lastRegularBid, bool allowDouble, bool allowRedouble)
        {
            //Log.Trace(3, $"TestClient.{base.seat}.HandleBidNeeded");
            var bid = await robot.FindBid(lastRegularBid, allowDouble, allowRedouble);
            await communicator.SendBid(bid);
            await this.HandleBidDone(whoseTurn, bid, DateTimeOffset.UtcNow);
        }

        public override async ValueTask HandleBidDone(Seats source, Bid bid, DateTimeOffset when)
        {
            //Log.Trace(3, $"TestClient.{base.seat}.HandleBidDone: {source} bids {bid}");
            await robot.HandleBidDone(source, bid, when);
        }

        public override async ValueTask HandleCardNeeded(Seats controller, Seats whoseTurn, Suits leadSuit, Suits trump, bool trumpAllowed, int leadSuitLength, int trick)
        {
            await Task.Delay(whoseTurn.Direction() == Directions.NorthSouth ? 100 : 200);
            var card = await robot.FindCard(whoseTurn, leadSuit, trump, trumpAllowed, leadSuitLength, trick);
            var signal = "test";
            await communicator.SendCard(whoseTurn, card, signal);
            await this.HandleCardPlayed(whoseTurn, card.Suit, card.Rank, signal, DateTimeOffset.UtcNow);
        }

        public override async ValueTask HandleCardPlayed(Seats source, Suits suit, Ranks rank, string signal, DateTimeOffset when)
        {
            //Log.Trace(3, $"TestClient.{base.seat}.HandleCardPlayed: {source} plays {rank.ToXML()}{suit.ToXML()}");
            await robot.HandleCardPlayed(source, suit,rank, signal, when);
        }
    }
}
