using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading;
using System;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Net;
using System.IO;
using Bridge.Test.Helpers;
using System.Text;

namespace Bridge.Networking.UnitTests
{
    [TestClass]
    public class TableManagerTcpClientTests : BridgeTestBase
    {
#if DEBUG
        [TestMethod, DeploymentItem("TestData\\WC2005final01.pbn")]
        public async Task AsyncTcpClient_Test()
        {
            Log.Level = 6;
            int uniqueTestPort = 2008;
            var host = new TestHost(uniqueTestPort);
            var client = new AsyncProtocolClient();

            await client.Connect("localhost", uniqueTestPort);
            await client.Send("Connecting \"RoboNS\" as North using protocol version 18");
            var answer = await client.GetNextLine();
            Assert.AreEqual("North (\"RoboNS\") seated", answer);
            await client.Send("North ready for teams");
            answer = await client.GetNextLine();
            Assert.AreEqual("Teams : N/S : \"RoboNS\" E/W : \"RoboEW\"", answer);

            //await host.WaitForCompletionAsync();
        }
#endif

        [TestMethod, DeploymentItem("TestData\\WC2005final01.pbn")]
        public async Task TableManagerTcpClient_TestIsolated()
        {
            Log.Level = 2;
            int uniqueTestPort = 2004;
            var host = new TestHost(uniqueTestPort);
            var client = new TestClient(new BridgeEventBus("TM_Client.North"));

            client.Connect(Seats.North, "localhost", uniqueTestPort, 120, 60, "RoboNS");

            await host.WaitForCompletionAsync();
        }

        [TestMethod, ExpectedException(typeof(SocketException))]
        public async Task TableManagerTcpClient_NoHost()
        {
            await Task.CompletedTask;       // only to prevent compiler warning
            Log.Level = 2;
            int uniqueTestPort = 2005;
            var client = new TestClient(new BridgeEventBus("TM_Client.North"));

            client.Connect(Seats.North, "localhost", uniqueTestPort, 120, 60, "RoboNS");
        }

        [TestMethod, DeploymentItem("TestData\\WC2005final01.pbn")]
        public async Task TableManagerTcpClient_LateHost()
        {
             Log.Level = 2;
           int uniqueTestPort = 2006;
            var client = new TestClient(new BridgeEventBus("TM_Client.North"));

            var t = Task.Run(() =>
            {
                client.Connect(Seats.North, "localhost", uniqueTestPort, 120, 60, "RoboNS");
            });

            await Task.Delay(10 * 1000);

            var host = new TestHost(uniqueTestPort);
            await host.WaitForCompletionAsync();
            t.Wait();
            if (t.IsFaulted) throw t.Exception;
        }

        private class TestClient : TableManagerTcpClient
        {
            public TestClient(BridgeEventBus bus) : base(bus)
            {
            }

            protected override async Task WriteProtocolMessageToRemoteMachine(string message)
            {
                if (message == "North ready to start") this.Stop();     // simulate network failure
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

        private class TestHost
        {
            public TestHost(int port)
            {
                Log.Trace(1, "TestHost({0})", port);
                this.testState = 1;
                this.waiter = new SemaphoreSlim(initialCount: 0);
                this.listener = new TcpListener(IPAddress.Any, port);
                this.listener.Start();
                this.WaitForIncomingClient();
            }

            private byte[] buffer;
            private int testState;
            private TcpClient client;
            private TcpListener listener;
            private SemaphoreSlim waiter;

            private void WaitForIncomingClient()
            {
                Log.Trace(1, "TestHost.WaitForIncomingClient");
                this.listener.BeginAcceptTcpClient(new AsyncCallback(this.AcceptClient), listener);
            }

            private void AcceptClient(IAsyncResult result)
            {
                Log.Trace(1, "TestHost.AcceptClient");
                var listener = result.AsyncState as TcpListener;
                this.client = listener.EndAcceptTcpClient(result);
                this.client.NoDelay = true;
                this.buffer = new Byte[this.client.ReceiveBufferSize];
                this.WaitForIncomingMessage();
                this.WaitForIncomingClient();
            }

            private void WaitForIncomingMessage()
            {
                this.client.GetStream().BeginRead(this.buffer, 0, this.client.ReceiveBufferSize, new AsyncCallback(ReadData), this.client);
            }

            private void ReadData(IAsyncResult result)
            {
                string message = string.Empty;
                try
                {
                    int bytes2 = this.client.GetStream().EndRead(result);
                    message = System.Text.Encoding.ASCII.GetString(this.buffer, 0, bytes2).Trim();
                }
                catch (IOException)
                {
                }

                this.WaitForIncomingMessage();
                if (message.Length == 0)    // probably connection error
                {
                    Log.Trace(1, "TestHost.ReadData: empty message");
                    return;
                }

                Log.Trace(1, "Host received {0}", message);
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

                        //// simulate a network error:
                        //Log.Trace(1, "TestHost simulates a network error by closing the client");
                        //this.client.Close();

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

            private void WriteData(string message)
            {
                Byte[] data = System.Text.Encoding.ASCII.GetBytes(message + "\r\n");
                var stream = this.client.GetStream();
                stream.Write(data, 0, data.Length);
                stream.Flush();
            }

            protected void Stop()
            {
                this.waiter.Release();
            }

            public async Task WaitForCompletionAsync()
            {
                await this.waiter.WaitAsync();
            }
        }
    }
}
