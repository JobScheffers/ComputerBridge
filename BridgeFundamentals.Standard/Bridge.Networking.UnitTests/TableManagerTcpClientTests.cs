using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading;
using System;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Net;
using System.IO;
using Bridge.Test.Helpers;
using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities;

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
            Log.Level = 9;
            int uniqueTestPort = GetNextPort();
            using var host = new TestHost(uniqueTestPort + 1);
            var client = new TestClient(new BridgeEventBus("NoHost.North"));

            try
            {
                Log.Trace(1, $"port={uniqueTestPort}");
                await client.Connect(Seats.North, 120, 60, "RoboNS", 18, new TestTcpCommunicationDetails("localhost", uniqueTestPort));
                Assert.Fail("expected a SocketException");
            }
            catch (SocketException)
            {
            }
        }

        [TestMethod, DeploymentItem("TestData\\WC2005final01.pbn")]
        public async Task TableManagerTcpClient_LateHost()
        {
            Log.Level = 9;
            int uniqueTestPort = GetNextPort();
            var client = new TestClient(new BridgeEventBus("LateHost.North"));

            var t = Task.Run(async () =>
            {
                await client.Connect(Seats.North, 120, 60, "RoboNS", 18, new TestTcpCommunicationDetails("localhost", uniqueTestPort));
            });

            await Task.Delay(10 * 1000);

            using var host = new TestHost(uniqueTestPort);
            await host.WaitForCompletionAsync();
            t.Wait();
            if (t.IsFaulted) throw t.Exception;
        }

        private class TestClient : TableManagerClientAsync<TestTcpCommunicationDetails>
        {
            public TestClient(BridgeEventBus bus) : base(bus)
            {
            }

            protected new async Task WriteProtocolMessageToRemoteMachine(string message)
            {
                await base.WriteProtocolMessageToRemoteMachine(message);
                Log.Trace(1, "Message '{0}' has been written", message);
            }

            public override void HandleBidNeeded(Seats whoseTurn, Bid lastRegularBid, bool allowDouble, bool allowRedouble)
            {
                base.HandleBidNeeded(whoseTurn, lastRegularBid, allowDouble, allowRedouble);
                if (whoseTurn == Seats.North)
                    this.EventBus.HandleBidDone(Seats.North, Bid.C(lastRegularBid.IsPass ? "1NT" : "Pass"));
            }

            public override void HandleExplanationNeeded(Seats source, Bid bid)
            {
                bid.Explanation = "My explanation";
                this.EventBus.HandleExplanationDone(source, bid);
            }
        }

        private class TestTcpCommunicationDetails : TcpCommunicationDetails
        {

            public TestTcpCommunicationDetails(string _serverAddress, int _serverPort) : base(_serverAddress, _serverPort) { }

            public override async Task WriteProtocolMessageToRemoteMachine(string message)
            {
                await base.WriteProtocolMessageToRemoteMachine(message);

                if (RandomGenerator.Instance.Percentage(10))
                {
                    // simulate a network error:
                    Log.Trace(1, "Client simulates a network error by closing the stream");
                    try
                    {
                        this.Close();
                    }
                    catch (Exception x)
                    {
                        Log.Trace(1, $" error while closing: {x.Message}");
                    }
                }
            }
        }

        private class TestHost : IDisposable
        {
            public TestHost(int port)
            {
                Log.Trace(1, "TestHost({0})", port);
                this.testState = 1;
                this.sendAfterConnect = string.Empty;
                this.waiter = new SemaphoreSlim(initialCount: 0);
                this.listener = new TcpListener(IPAddress.Any, port);
                this.listener.Start();
                this.WaitForIncomingClient();
            }

            private byte[] buffer;
            private int testState;
            private TcpClient client;
            private readonly TcpListener listener;
            private readonly SemaphoreSlim waiter;
            private string sendAfterConnect;
            private bool disposedValue;

            private void WaitForIncomingClient()
            {
                if (disposedValue) return;      // host has been disposed
                Log.Trace(1, "TestHost.WaitForIncomingClient");
                this.listener.BeginAcceptTcpClient(new AsyncCallback(this.AcceptClient), listener);
            }

            private void AcceptClient(IAsyncResult result)
            {
                if (disposedValue) return;      // host has been disposed
                Log.Trace(1, $"TestHost.AcceptClient");
                var listener = result.AsyncState as TcpListener;
                this.client = listener.EndAcceptTcpClient(result);
                Log.Trace(2, $"{this.client.Client.LocalEndPoint}");
                /// When NoDelay is false, a TcpClient does not send a packet over the network until it has collected a significant amount of outgoing data.
                /// Because of the amount of overhead in a TCP segment, sending small amounts of data is inefficient.
                /// However, situations do exist where you need to send very small amounts of data or expect immediate responses from each packet you send.
                /// Your decision should weigh the relative importance of network efficiency versus application requirements.
                this.client.NoDelay = true;
                this.buffer = new Byte[this.client.ReceiveBufferSize];
                if (this.sendAfterConnect.Length > 0)
                {
                    Log.Trace(9, $"TestHost.AcceptClient sendAfterConnect='{this.sendAfterConnect}'");
                    this.WriteData(this.sendAfterConnect);
                }

                this.WaitForIncomingMessage();
                this.WaitForIncomingClient();
            }

            private void WaitForIncomingMessage()
            {
                Log.Trace(9, $"TestHost.WaitForIncomingMessage");
                try
                {
                    this.client.GetStream().BeginRead(this.buffer, 0, this.client.ReceiveBufferSize, new AsyncCallback(ReadData), this.client);
                    Thread.Sleep(10);   // gives opportunity to detect a closed stream
                }
                catch (IOException x)
                {
                    Log.Trace(9, $"TestHost.WaitForIncomingMessage {x.Message}");
                    this.WaitForIncomingClient();
                }
            }

            private void ReadData(IAsyncResult result)
            {
                Log.Trace(9, $"TestHost.ReadData IsCompleted={result.IsCompleted} CompletedSynchronously={result.CompletedSynchronously}");
                if (this.client.Connected)
                {
                    string message = string.Empty;
                    try
                    {
                        int bytes2 = this.client.GetStream().EndRead(result);
                        if (bytes2 > 0) message = System.Text.Encoding.ASCII.GetString(this.buffer, 0, bytes2).Trim();
                    }
                    catch (IOException)
                    {
                    }

                    if (message.Length == 0)    // probably connection error
                    {
                        this.client = null;
                        Log.Trace(1, "TestHost.ReadData: empty message");
                        this.WaitForIncomingClient();
                        return;
                    }

                    //if (!(result.CompletedSynchronously && message.Length == 0))
                        this.WaitForIncomingMessage();

                    Log.Trace(1, $"TestHost received '{message}'");
                    switch (this.testState)
                    {
                        case 1:
                            Assert.AreEqual("Connecting \"RoboNS\" as North using protocol version 18", message);
                            this.testState = 2;
                            this.WriteData("North (\"RoboNS\") seated");
                            return;
                        case 2:
                            Assert.AreEqual("North ready for teams", message);
                            this.testState = 3;
                            this.WriteData("Teams : N/S : \"RoboNS\" E/W : \"RoboEW\"");
                            return;

                        case 3:
                            Assert.AreEqual("North ready to start", message);
                            this.testState = 4;
                            this.WriteData("Start of board");
                            return;
                        case 4:
                            Assert.AreEqual("North ready for deal", message);
                            this.testState = 5;
                            this.WriteData("Board number 1. Dealer North. Neither vulnerable.");
                            return;
                        case 5:
                            Assert.AreEqual("North ready for cards", message);
                            this.testState = 6;
                            this.WriteData("North's cards : S A T 4 3. H T 8 5 3. D K T. C K T 6. ");
                            return;
                        case 6:
                            Assert.AreEqual("North bids 1NT", message);
                            this.testState = 7;
                            return;
                        case 7:
                            Assert.AreEqual("North ready for East's bid", message);
                            this.testState = 8;
                            this.WriteData("East passes");
                            return;
                        case 8:
                            Assert.AreEqual("North ready for South's bid", message);
                            this.testState = 9;
                            this.WriteData("South passes");
                            return;
                        case 9:
                            Assert.AreEqual("North ready for West's bid", message);
                            this.testState = 10;
                            this.WriteData("Explain West's 2D");
                            return;
                        case 10:
                            Assert.AreEqual("My explanation", message);
                            this.testState = 11;
                            this.WriteData("West bids 2D");
                            return;
                        case 11:
                            Assert.AreEqual("North passes", message);
                            this.testState = 12;
                            //this.ProcessIncomingMessage("West bids 2D");
                            this.Stop();
                            return;
                    }
                }
                else
                {
                    this.WaitForIncomingClient();
                }
            }

            private void WriteData(string message)
            {
                if (this.client == null)
                {   // client connection is interrupted; have to wait until client reconnects
                    Log.Trace(9, $"TestHost.WriteData sendAfterConnect='{message}'");
                    this.sendAfterConnect = message;
                }
                else
                {
                    this.sendAfterConnect = string.Empty;
                    Byte[] data = System.Text.Encoding.ASCII.GetBytes(message + "\r\n");
                    var stream = this.client.GetStream();
                    stream.Write(data, 0, data.Length);
                    stream.Flush();
                    Log.Trace(1, $"TestHost sends '{message}'");

                    if (RandomGenerator.Instance.Percentage(10))
                    {
                        // simulate a network error:
                        Log.Trace(1, "TestHost simulates a network error by closing the client");
                        stream.Close();
                        this.client.Close();
                    }
                }
            }

            protected void Stop()
            {
                this.waiter.Release();
            }

            public async Task WaitForCompletionAsync()
            {
                await this.waiter.WaitAsync();
            }

            protected virtual void Dispose(bool disposing)
            {
                if (!disposedValue)
                {
                    if (disposing)
                    {
                        // dispose managed state (managed objects)
                        this.waiter.Dispose();
                        this.client.Dispose();
                        this.listener.Stop();
                    }

                    // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                    // TODO: set large fields to null
                    disposedValue = true;
                }
            }

            // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
            // ~TestHost()
            // {
            //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            //     Dispose(disposing: false);
            // }

            public void Dispose()
            {
                // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
                Dispose(disposing: true);
                GC.SuppressFinalize(this);
            }
        }
    }
}
