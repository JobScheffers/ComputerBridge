﻿using Bridge.Test.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Bridge.Networking.UnitTests
{
    [TestClass]
    public class TableManagerTcpClientTests : TcpTestBase
    {
        //[TestMethod]
        //public async Task TableManagerSocketClient_Test()
        //{
        //    Log.Level = 2;
        //    //var tableName = Guid.NewGuid().ToString();
        //    var tableName = "WC2020/RR1";
        //    var clients = new SeatCollection<TableManagerClientAsync<SocketCommunicationDetails>>();
        //    await SeatsExtensions.ForEachSeatAsync(async s =>
        //    {
        //        clients[s] = new TableManagerClientAsync<SocketCommunicationDetails>(new BridgeEventBus("TM_Client.North"));
        //        await clients[s].Connect(s, 10, 1, "RoboBridge", 19, new SocketCommunicationDetails("wss://tablemanager.robobridge.com/tm", tableName, "RoboBridge"));
        //    });

        //    await Task.Delay(60000);
        //    await SeatsExtensions.ForEachSeatAsync(async s =>
        //    {
        //        await clients[s].DisposeAsync();
        //    });
        //}

        //[TestMethod]
        //public async Task TableManagerSignalRClient_Test()
        //{
        //    Log.Level = 2;
        //    //var tableName = Guid.NewGuid().ToString();
        //    var tableName = "WC2020/RR1";
        //    var clients = new SeatCollection<TableManagerClientAsync<SignalRCommunicationDetails>>();
        //    await SeatsExtensions.ForEachSeatAsync(async s =>
        //    {
        //        clients[s] = new TableManagerClientAsync<SignalRCommunicationDetails>(new BridgeEventBus("TM_Client.North"));
        //        await clients[s].Connect(s, 10, 1, "RoboBridge", 19, new SignalRCommunicationDetails("https://tablemanager.robobridge.com/tm", tableName, "RoboBridge"));
        //    });

        //    await Task.Delay(30000);
        //    await SeatsExtensions.ForEachSeatAsync(async s =>
        //    {
        //        await clients[s].DisposeAsync();
        //    });
        //}

        [TestMethod]
        public async Task TableManagerTcpClient_NoHost()
        {
            Log.Level = 4;
            await using var host = new TestTcpHost(HostMode.SingleTableTwoRounds, GetNextPort(), "WrongPort", null);
            host.Run();

            var communicationFactory = new TcpCommunicationFactory(new IPEndPoint(new IPAddress([127, 0, 0, 1]), GetNextPort()));
            var seat = Seats.North;
            var client = new TestClient(seat, new ClientComputerBridgeProtocol("RoboNS", 19, communicationFactory.CreateClient()));

            try
            {
                await client.Connect("");
                Assert.Fail("expected a SocketException");
            }
            catch (SocketException)
            {
            }
            finally
            {
                await client.Finish();
                host.HandleTournamentStopped();
                await host.WaitForCompletionAsync();
            }
        }

        [TestMethod, DeploymentItem("TestData\\SingleBoard.pbn")]
        public async Task TableManagerTcpClient_LateHost()
        {
            Log.Level = 4;
            int uniqueTestPort = GetNextPort();
            var communicationFactory = new TcpCommunicationFactory(new IPEndPoint(new IPAddress([127, 0, 0, 1]), uniqueTestPort));
            var seat = Seats.North;
            var client = new TestClient(seat, new ClientComputerBridgeProtocol("RoboNS", 19, communicationFactory.CreateClient()));

            var t = Task.Run(async () =>
            {
                await client.Connect("");
            });

            await Task.Delay(8 * 1000);

            await using var host = new TestTcpHost(HostMode.SingleTableTwoRounds, uniqueTestPort, "LateHost", null);
            host.Run();
            await Task.Delay(1 * 1000);
            host.HandleTournamentStopped();
            await host.WaitForCompletionAsync();
            await t;
            if (t.IsFaulted) throw t.Exception.InnerException;
            await client.Finish();
        }

        //private class TestClient : TableManagerClientAsync<TestTcpCommunicationDetails>
        //{
        //    public TestClient(BridgeEventBus bus) : base(bus)
        //    {
        //    }

        //    protected new async Task WriteProtocolMessageToRemoteMachine(string message)
        //    {
        //        await base.WriteProtocolMessageToRemoteMachine(message);
        //        Log.Trace(1, "Message '{0}' has been written", message);
        //    }

        //    public override void HandleBidNeeded(Seats whoseTurn, Bid lastRegularBid, bool allowDouble, bool allowRedouble)
        //    {
        //        base.HandleBidNeeded(whoseTurn, lastRegularBid, allowDouble, allowRedouble);
        //        if (whoseTurn == Seats.North)
        //            this.EventBus.HandleBidDone(Seats.North, Bid.C(lastRegularBid.IsPass ? "1NT" : "Pass"));
        //    }

        //    public override void HandleExplanationNeeded(Seats source, Bid bid)
        //    {
        //        bid.Explanation = "My explanation";
        //        this.EventBus.HandleExplanationDone(source, bid);
        //    }
        //}

        //private class TestTcpCommunicationDetails : ClientTcpCommunicationDetails
        //{

        //    public TestTcpCommunicationDetails(string _serverAddress, int _serverPort, string _name) : base(_serverAddress, _serverPort, _name) { }

        //    public override async ValueTask WriteProtocolMessageToRemoteMachine(string message)
        //    {
        //        await base.WriteProtocolMessageToRemoteMachine(message);

        //        //if (RandomGenerator.Instance.Percentage(10))
        //        //{
        //        //    // simulate a network error:
        //        //    Log.Trace(1, "Client simulates a network error by closing the stream");
        //        //    try
        //        //    {
        //        //        this.();
        //        //    }
        //        //    catch (Exception x)
        //        //    {
        //        //        Log.Trace(1, $" error while closing: {x.Message}");
        //        //    }
        //        //}
        //    }
        //}

        //private class TestHost : BaseAsyncDisposable
        //{
        //    public TestHost(int port)
        //    {
        //        Log.Trace(1, "TestHost({0})", port);
        //        this.testState = 1;
        //        this.sendAfterConnect = string.Empty;
        //        this.waiter = new SemaphoreSlim(initialCount: 0);
        //        this.listener = new TcpListener(IPAddress.Any, port);
        //        this.listener.Start();
        //        this.WaitForIncomingClient();
        //    }

        //    private byte[] buffer;
        //    private int testState;
        //    private TcpClient client;
        //    private readonly TcpListener listener;
        //    private readonly SemaphoreSlim waiter;
        //    private string sendAfterConnect;

        //    private void WaitForIncomingClient()
        //    {
        //        Log.Trace(1, "TestHost.WaitForIncomingClient");
        //        if (this.IsDisposed) return;      // host has been disposed
        //        this.listener.BeginAcceptTcpClient(new AsyncCallback(this.AcceptClient), listener);
        //    }

        //    private void AcceptClient(IAsyncResult result)
        //    {
        //        Log.Trace(1, $"TestHost.AcceptClient");
        //        var listener = result.AsyncState as TcpListener;
        //        if (this.IsDisposed) return;      // host has been disposed
        //        try
        //        {
        //            this.client = listener.EndAcceptTcpClient(result);
        //            Log.Trace(2, $"{this.client.Client.LocalEndPoint}");
        //            /// When NoDelay is false, a TcpClient does not send a packet over the network until it has collected a significant amount of outgoing data.
        //            /// Because of the amount of overhead in a TCP segment, sending small amounts of data is inefficient.
        //            /// However, situations do exist where you need to send very small amounts of data or expect immediate responses from each packet you send.
        //            /// Your decision should weigh the relative importance of network efficiency versus application requirements.
        //            this.client.NoDelay = true;
        //            this.buffer = new Byte[this.client.ReceiveBufferSize];
        //            if (this.sendAfterConnect.Length > 0)
        //            {
        //                Log.Trace(9, $"TestHost.AcceptClient sendAfterConnect='{this.sendAfterConnect}'");
        //                this.WriteData(this.sendAfterConnect);
        //            }

        //            this.WaitForIncomingMessage();
        //            this.WaitForIncomingClient();
        //        }
        //        catch (ObjectDisposedException)
        //        {
        //            // the TestHost can be disposed right after checking disposedValue
        //        }
        //    }

        //    private void WaitForIncomingMessage()
        //    {
        //        Log.Trace(9, $"TestHost.WaitForIncomingMessage");
        //        try
        //        {
        //            if (this.IsDisposed) return;      // host has been disposed
        //            this.client.GetStream().BeginRead(this.buffer, 0, this.client.ReceiveBufferSize, new AsyncCallback(ReadData), this.client);
        //            //Thread.Sleep(10);   // gives opportunity to detect a closed stream
        //            // cannot afford to sleep here: if the client sends another messge within 10ms, that message will be lost
        //        }
        //        catch (IOException x)
        //        {
        //            Log.Trace(1, $"TestHost.WaitForIncomingMessage {x.Message}");
        //            this.WaitForIncomingClient();
        //        }
        //        catch (AssertFailedException x)
        //        {
        //            Log.Trace(1, $"TestHost.WaitForIncomingMessage {x.Message}");
        //        }
        //        catch (Exception x)
        //        {
        //            Log.Trace(1, $"TestHost.WaitForIncomingMessage {x.Message} {x.InnerException}");
        //        }
        //    }

        //    private void ReadData(IAsyncResult result)
        //    {
        //        Log.Trace(9, $"TestHost.ReadData IsCompleted={result.IsCompleted} CompletedSynchronously={result.CompletedSynchronously}");
        //        if (this.IsDisposed) return;      // host has been disposed
        //        if (this.client.Connected)
        //        {
        //            string message = string.Empty;
        //            try
        //            {
        //                int bytes2 = this.client.GetStream().EndRead(result);
        //                if (bytes2 > 0) message = System.Text.Encoding.ASCII.GetString(this.buffer, 0, bytes2).Trim();
        //            }
        //            catch (IOException)
        //            {
        //            }

        //            if (message.Length == 0)    // probably connection error
        //            {
        //                this.client = null;
        //                Log.Trace(1, "TestHost.ReadData: empty message");
        //                this.WaitForIncomingClient();
        //                return;
        //            }

        //            this.WaitForIncomingMessage();

        //            Log.Trace(1, $"TestHost received '{message}'");
        //            switch (this.testState)
        //            {
        //                case 1:
        //                    Assert.AreEqual("Connecting \"RoboNS\" as North using protocol version 18", message);
        //                    this.testState = 2;
        //                    this.WriteData("North (\"RoboNS\") seated");
        //                    return;
        //                case 2:
        //                    Assert.AreEqual("North ready for teams", message);
        //                    this.testState = 3;
        //                    this.WriteData("Teams : N/S : \"RoboNS\" E/W : \"RoboEW\"");
        //                    return;

        //                case 3:
        //                    Assert.AreEqual("North ready to start", message);
        //                    this.testState = 4;
        //                    this.WriteData("Start of board");
        //                    return;
        //                case 4:
        //                    Assert.AreEqual("North ready for deal", message);
        //                    this.testState = 5;
        //                    this.WriteData("Board number 1. Dealer North. Neither vulnerable.");
        //                    return;
        //                case 5:
        //                    Assert.AreEqual("North ready for cards", message);
        //                    this.testState = 6;
        //                    this.WriteData("North's cards : S A T 4 3. H T 8 5 3. D K T. C K T 6. ");
        //                    return;
        //                case 6:
        //                    //if ("North bids 1NT" != message)
        //                    //{
        //                    //    Log.Trace(1, $"##### TestHost.ReadData: wrong message: 'North ready for cards' '{message}'");
        //                    //    this.Stop();
        //                    //    return;
        //                    //}
        //                    Assert.AreEqual("North bids 1NT", message);
        //                    this.testState = 7;
        //                    return;
        //                case 7:
        //                    Assert.AreEqual("North ready for East's bid", message);
        //                    this.testState = 8;
        //                    this.WriteData("East passes");
        //                    return;
        //                case 8:
        //                    Assert.AreEqual("North ready for South's bid", message);
        //                    this.testState = 9;
        //                    this.WriteData("South passes");
        //                    return;
        //                case 9:
        //                    Assert.AreEqual("North ready for West's bid", message);
        //                    this.testState = 10;
        //                    this.WriteData("Explain West's 2D");
        //                    return;
        //                case 10:
        //                    Assert.AreEqual("My explanation", message);
        //                    this.testState = 11;
        //                    this.WriteData("West bids 2D");
        //                    return;
        //                case 11:
        //                    Assert.AreEqual("North passes", message);
        //                    this.testState = 12;
        //                    //this.ProcessIncomingMessage("West bids 2D");
        //                    this.Stop();
        //                    return;
        //            }
        //        }
        //        else
        //        {
        //            this.WaitForIncomingClient();
        //        }
        //    }

        //    private void WriteData(string message)
        //    {
        //        if (this.client == null)
        //        {   // client connection is interrupted; have to wait until client reconnects
        //            Log.Trace(9, $"TestHost.WriteData sendAfterConnect='{message}'");
        //            this.sendAfterConnect = message;
        //        }
        //        else
        //        {
        //            this.sendAfterConnect = string.Empty;
        //            Byte[] data = System.Text.Encoding.ASCII.GetBytes(message + "\r\n");
        //            var stream = this.client.GetStream();
        //            stream.Write(data, 0, data.Length);
        //            stream.Flush();
        //            Log.Trace(1, $"TestHost sends '{message}'");

        //            //if (RandomGenerator.Instance.Percentage(10))
        //            //{
        //            //    // simulate a network error:
        //            //    Log.Trace(1, "TestHost simulates a network error by closing the client");
        //            //    stream.Close();
        //            //    this.client.Close();
        //            //}
        //        }
        //    }

        //    protected void Stop()
        //    {
        //        this.waiter.Release();
        //    }

        //    public async ValueTask WaitForCompletionAsync()
        //    {
        //        await this.waiter.WaitAsync();
        //    }

        //    protected override async ValueTask DisposeManagedObjects()
        //    {
        //        await ValueTask.CompletedTask;
        //        this.waiter.Dispose();
        //        if (this.client is not null) this.client.Dispose();
        //        this.listener.Stop();
        //    }
        //}

        [TestMethod]
        public async Task AsyncTcpListener()
        {
            Trace.WriteLine($"AsyncTcpListener");
            Log.Level = 4;
            var uniqueTestPort = GetNextPort();
            await using var server = new AsyncHost(new IPEndPoint(IPAddress.Any, uniqueTestPort), null, null);
            
            var serverRunTask = server.Run();

            await using var client1 = new AsyncClient("client");
            await client1.Connect("localhost", uniqueTestPort);
            var clientRunTask = client1.Run();
            await client1.Send("hello world. North bids 1NT Infos.(!(ps0911*pa0012*D8*o1D*!H4*!S4*r23D5))*(!(ps0911*pa0012*C8*o1C*!H4*!S4*r23C5))*(!(pS1019*pa0612*S7*o7S3*r23S4))*(!(pH1019*pa0612*H7*o7H3*r23H4))*(!(pD1019*pa0712*D7*o7D1*!H4*!S4*r23D4))*(!(pC1019*pa0712*C7*o7C1*!H4*!S4*r23C4))*(!(pS0814*pa0510*(pa0509+th3)*S7*o7S1*!H4*r23S3))*(!(pH0814*pa0512*(pa0509+th3)*H7*o7H0*!S4*r23H3))*(!(pD0714*pa0011*D6*(D7+o7D6*(!S3+!H3))*o7D1*!H4*!S4*r23D3))*(!(pC0714*pa0011*C6*(C7+o7C6*(!S3+!H3))*o7C1*!H4*!S4*r23C3))*(!(S5*!S8*!>HS*o7S1*(!u+S6*(o7S3+th3+pS1011+pa0009))*r23S2*pa0011*pS0413*(pa0010+th3)*!(pS1213*pa1021*(H5+D5+C5+S7))*!(pa1115*=S5*H3*(D4+C4))*!(pa1019*S6*(H4+D4+C4))*!(vS6*pa1240)))*(!(H5*!H8*>HS*o7H1*(!u+H6*(o7H3+th3+pH1011+pa0009))*r23H2*pa0011*pH0413*(pa0010+th3)*!(pH1213*pa1021*(S5+D5+C5+H7))*!(pa1019*H6*(S4+D4+C4))*!(vH6*pa1240)))*(!(D5*!S5*!H5*(!u+D6*o7D1*(o7D3+th3))*r23D2*pa0311*pD0413*(pa0010+th3)))*(!(pN2022*S2*!S6*H2*!H6*D1*(D2+o7D3*C3*!C6*H3*!H5*S3*!S5)*!D7*C1*(C2+o7C3*D3*!D6*H3*!H5*S3*!S5)*!C7*!(S5*(H4*!t07+D5+C5))*!(H5*(S4*!t07+D5+C5))*!(pa2240*(S5+H5))))*(pN1517*pa1418*t07*(t01+th4+th3))");
            await Task.Delay(1000);
            server.Stop();

            await serverRunTask;
            //await clientRunTask;
            if (serverRunTask.IsFaulted) throw new Exception("serverRunTask.IsFaulted");
            //if (clientRunTask.IsFaulted) throw new Exception("clientRunTask.IsFaulted");
        }

        public class AsyncClient : BaseAsyncDisposable
        {
            private readonly TcpClient client;
            private readonly string name;
            private readonly int id;
            private NetworkStream stream;
            private StreamWriter w;
            private bool isRunning = false;
            private readonly Func<int, string, ValueTask> processMessage;

            public AsyncClient(string _name)
            {
                this.name = _name;
                this.client = new();
            }

            public AsyncClient(string _name, int _id, TcpClient client, Func<int, string, ValueTask> _processMessage)
            {
                this.name = _name + _id.ToString();
                this.id = _id;
                this.client = client;
                this.processMessage = _processMessage;
                this.AfterConnect();
            }

            protected override async ValueTask DisposeManagedObjects()
            {
                Log.Trace(9, $"{this.name} dispose begin");
                this.isRunning = false;
                await ValueTask.CompletedTask;
                this.client.Dispose();
                this.stream.Dispose();
                this.w.Dispose();
            }

            public async ValueTask Connect(string address, int port)
            {
                await this.client.ConnectAsync(address, port);
                this.AfterConnect();
            }

            public void Stop()
            {
                this.isRunning = false;
            }

            private void AfterConnect()
            {
                this.stream = client.GetStream();
                this.w = new StreamWriter(this.stream);
            }

            public async ValueTask Run()
            {
                Log.Trace(8, $"AsyncClient.Run {this.name} begin");
                this.isRunning = true;
                var r = new StreamReader(this.stream);
                while (this.isRunning)
                {
                    var message = await r.ReadLineAsync();
                    Log.Trace(9, $"{this.name} receives '{message}' (isRunning={this.isRunning})");
                    await this.processMessage(this.id, message);
                }
                Log.Trace(8, $"AsyncClient.Run {this.name} end");
            }

            public async ValueTask Send(string message)
            {
                Log.Trace(9, $"{this.name} sends '{message}'");
                await this.w.WriteLineAsync(message);
                await this.w.FlushAsync();
            }
        }

        private class AsyncHost : BaseAsyncTcpHost 
        {
            public AsyncHost(IPEndPoint tcpPort, Func<int, string, ValueTask> _processMessage, Func<int, ValueTask> _processNewClient) : base(tcpPort, _processMessage, _processNewClient, "TestHost") { }
        }
    }
}
