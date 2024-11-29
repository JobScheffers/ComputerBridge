using Bridge.NonBridgeHelpers;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace Bridge.Networking
{
    public class ClientTcpCommunicationDetails : ClientCommunicationDetails
    {
        private readonly MyTcpClient client;
        private readonly string serverAddress;
        private readonly int serverPort;
        private readonly string name;
        private Task clientRunTask;

        public ClientTcpCommunicationDetails(string _serverAddress, int _serverPort, string _name)
        {
            this.serverAddress = _serverAddress;
            this.serverPort = _serverPort;
            this.name = _name + ".ClientTcpCommunicationDetails";
            this.client = new MyTcpClient(_name);
            this.client.OnConnectionLost = HandleConnectionLost;
        }

        protected override async ValueTask Connect()
        {
            this.clientRunTask = Task.CompletedTask;
            this.client.SetMessageProcessor(this.processMessage);
            int retries = 0;
            do
            {
                try
                {
                    Log.Trace(2, $"{this.name}.Connect {this.serverAddress}:{this.serverPort}");
                    await this.client.Connect(this.serverAddress, this.serverPort).ConfigureAwait(false);
                    Log.Trace(4, $"{this.name}.Connect tcp connect finished");
                    this.clientRunTask = this.client.Run();
                }
                catch (SocketException x)
                {
                    if (x.SocketErrorCode == SocketError.ConnectionRefused)
                    {
                        Log.Trace(1, "Connection refused");
                        retries++;
                        if (retries > 10) throw;
                    }
                    else
                    {
                        throw;
                    }
                }
            } while (this.clientRunTask == Task.CompletedTask);
        }

        private async ValueTask HandleConnectionLost()
        {
            Log.Trace(2, $"{this.name} lost connection");
            await this.Connect().ConfigureAwait(false);
        }

        //private void WaitForTcpData()
        //{
        //    // make sure no messages get lost; go wait for another message on the tcp line
        //    Log.Trace(9, "WaitForTcpData");
        //    this.stream.BeginRead(this.streamBuffer, 0, this.client.ReceiveBufferSize, new AsyncCallback(this.ReadData), null);
        //}

        //private void ReadData(IAsyncResult result)
        //{
        //    try
        //    {
        //        int bytes2 = 0;
        //        if (this.stream.CanRead) bytes2 = this.stream.EndRead(result);
        //        if (bytes2 == 0)
        //        {
        //            // nothing to do
        //            Log.Trace(5, "no data from host");
        //            //Thread.Sleep(this.pauseTime);
        //            //if (this.pauseTime < 10000) this.pauseTime = (int)(1.2 * this.pauseTime);
        //            //if (!this.client.Connected)
        //            {
        //                this.stream.Close();
        //                _ = this.Connect();
        //            }
        //        }
        //        else
        //        {
        //            string newData = System.Text.Encoding.ASCII.GetString(this.streamBuffer, 0, bytes2);
        //            lock (this.locker)
        //            {
        //                this.rawMessageBuffer += newData;
        //            }

        //            this.ProcessRawMessage();
        //            //this.pauseTime = defaultWaitTime;
        //            this.WaitForTcpData();      // make sure no data will be lost
        //        }
        //    }
        //    catch (ObjectDisposedException)
        //    {
        //    }
        //    catch (AggregateException x) when (x.InnerException is ObjectDisposedException)
        //    {
        //    }
        //    catch (SocketException)
        //    {
        //        this.stream.Close();
        //        _ = this.Connect();
        //    }
        //}

        //private void ProcessRawMessage()
        //{
        //    string newCommand = "";
        //    lock (this.locker)
        //    {
        //        int endOfLine = rawMessageBuffer.IndexOf("\r\n");
        //        if (endOfLine >= 0)
        //        {
        //            newCommand = this.rawMessageBuffer.Substring(0, endOfLine);
        //            this.rawMessageBuffer = this.rawMessageBuffer.Substring(endOfLine + 2);
        //        }
        //    }

        //    if (newCommand.Length > 0)
        //    {
        //        this.processMessage(newCommand);
        //    }
        //}

        public override async ValueTask WriteProtocolMessageToRemoteMachine(string message)
        {
            using (await AsyncLock.WaitForLockAsync(this.name).ConfigureAwait(false))
            {
                await this.client.Send(message).ConfigureAwait(false);
                await Task.Delay(40).ConfigureAwait(false);       // make sure the next Send will not be too quick
            }
        }

        protected override async ValueTask DisposeManagedObjects()
        {
            // free managed resources
            if (this.client != null) await this.client.DisposeAsync().ConfigureAwait(false);
        }

        public override ValueTask<string> GetResponseAsync()
        {
            throw new NotImplementedException();
        }

        private class MyTcpClient : BaseAsyncDisposable
        {
            protected readonly string name;
            private TcpClient client;
            private NetworkStream stream;
            private StreamWriter writer;
            private StreamReader reader;
            private bool isRunning = false;
            private CancellationTokenSource cts;
            private Task runTask;
            private string remainingMessage = string.Empty;
            protected readonly bool _canReconnect;      // is the client server-side or client-side?
            public Func<ValueTask> OnConnectionLost;
            protected Func<string, ValueTask> processMessage;

            public MyTcpClient(string _name)
            {
                this.name = _name;
                this.NewTcpClient();
                this.cts = new CancellationTokenSource();
                this._canReconnect = true;      // client-side
            }

            public void SetMessageProcessor(Func<string, ValueTask> _processMessage)
            {
                this.processMessage = _processMessage;
            }

            protected override async ValueTask DisposeManagedObjects()
            {
                Log.Trace(2, $"{this.name} dispose begin");
                this.isRunning = false;
                await Task.CompletedTask.ConfigureAwait(false);
                if (this.client != null) this.client.Dispose();
                if (this.stream != null) this.stream.Dispose();
                this.writer.Dispose();
                this.cts.Dispose();
            }

            public async ValueTask Connect(string address, int port)
            {
                Log.Trace(4, $"{this.name}.BaseAsyncTcpClient.Connect begin");
                //try
                {
                    if (this.client == null) this.NewTcpClient();       // happens after a lost connection
                    await this.client.ConnectAsync(address, port).ConfigureAwait(false);
                    Log.Trace(4, $"{this.name}.BaseAsyncTcpClient.Connect client has connected");
                    this.AfterConnect();
                }
                //catch (IOException x)
                //{

                //    throw;
                //}
                //catch (ObjectDisposedException x)
                //{

                //    throw;
                //}
                Log.Trace(4, $"{this.name}.BaseAsyncTcpClient.Connect end");
            }

            public async ValueTask Stop()
            {
                await Task.CompletedTask.ConfigureAwait(false);
                this.isRunning = false;
                this.cts.Cancel();
            }

            private void NewTcpClient()
            {
                this.client = new TcpClient();

                /// When NoDelay is false, a TcpClient does not send a packet over the network until it has collected a significant amount of outgoing data.
                /// Because of the amount of overhead in a TCP segment, sending small amounts of data is inefficient.
                /// However, situations do exist where you need to send very small amounts of data or expect immediate responses from each packet you send.
                /// Your decision should weigh the relative importance of network efficiency versus application requirements.
                this.client.NoDelay = true;
            }

            private void AfterConnect()
            {
                Log.Trace(4, $"{this.name}.BaseAsyncTcpClient.AfterConnect begin");
                this.stream = client.GetStream();
                this.writer = new StreamWriter(this.stream);
                this.reader = new StreamReader(this.stream);
                Log.Trace(4, $"{this.name}.BaseAsyncTcpClient.AfterConnect end");
            }

            public void Start()
            {
                Log.Trace(4, $"{this.name} Start");
                this.runTask = this.Run();
            }

            protected async ValueTask ProcessMessage(string message)
            {
                await this.processMessage(message).ConfigureAwait(false);
            }

            public async Task Run()
            {
                Log.Trace(6, $"AsyncClient.Run {this.name} begin");
                this.isRunning = true;
                while (this.isRunning)
                {
                    var message = await this.ReadLineAsync().ConfigureAwait(false);
                    if (!string.IsNullOrWhiteSpace(message))
                    {
                        Log.Trace(3, $"{this.name} receives '{message}'");
                        await this.ProcessMessage(message).ConfigureAwait(false);
                    }
                }
                Log.Trace(6, $"AsyncClient.Run {this.name} end");
            }

            private char[] buffer = new char[1024];
            private async ValueTask<string> ReadLineAsync()
            {
                int charsRead = 0;
                do
                {
                    try
                    {
                        if (!this.isRunning || this.stream == null || cts.IsCancellationRequested) return string.Empty;
#if NET6_0_OR_GREATER
                        charsRead = await this.reader.ReadAsync(buffer, cts.Token).ConfigureAwait(false);
#else
                    charsRead = await this.reader.ReadAsync(buffer, 0, 1024).ConfigureAwait(false);
#endif
                    }
                    catch (OperationCanceledException)
                    {
                        return string.Empty;
                    }
                    catch (ObjectDisposedException)
                    {
                        if (this._canReconnect)
                        {
                            await this.HandleLostConnection().ConfigureAwait(false);
                            if (this.client == null) return string.Empty;
                            // reconnected; try to read from the stream again
                        }
                        else
                        {
                            this.isRunning = false;
                            return string.Empty;
                        }
                    }
                    catch (IOException)     // connection lost
                    {
                        Log.Trace(4, $"{this.name} failed read");
                        await this.HandleLostConnection().ConfigureAwait(false);
                        if (this._canReconnect)
                        {
                            if (this.client == null) return string.Empty;
                            // reconnected; try to read from the stream again
                        }
                        else
                        {
                            return string.Empty;
                        }
                    }

                    if (charsRead > 0)
                    {
                        this.remainingMessage += new string(buffer, 0, charsRead);
                    }
                } while (!this.remainingMessage.Contains("\r\n"));

                string result;
                // check for websocket handshake
                if (this.remainingMessage.StartsWith("GET") && this.remainingMessage.Contains("Sec-WebSocket-Key"))
                {
                    result = this.remainingMessage;
                    this.remainingMessage = "";
                }
                else
                {
                    var lineBreakAt = this.remainingMessage.IndexOf("\r\n");
#if NET6_0_OR_GREATER
                    result = this.remainingMessage[..lineBreakAt];
                    this.remainingMessage = this.remainingMessage[(lineBreakAt + 2)..];
#else
                result = this.remainingMessage.Substring(0, lineBreakAt);
                this.remainingMessage = this.remainingMessage.Substring(lineBreakAt + 2);
#endif
                }

                return result;
            }

            private bool isReconnecting = false;
            private async ValueTask HandleLostConnection()
            {
                if (!this.isReconnecting)
                {
                    //using (await AsyncLock.WaitForLockAsync(this.name).ConfigureAwait(false))
                    {
                        if (!this.isReconnecting)
                        {
                            this.isReconnecting = true;
                            Log.Trace(3, $"{this.name}.HandleLostConnection");
                            this.isRunning = false;
                            try
                            {
                                this.client.Close();
                                this.client.Dispose();
                                this.client = null;
                                if (this.stream != null)
                                {
#if NET6_0_OR_GREATER
                                    await this.stream.DisposeAsync().ConfigureAwait(false);
#endif
                                    this.stream = null;
                                }
                            }
                            catch (Exception)
                            {
                            }
                            if (this.OnConnectionLost != null) await this.OnConnectionLost().ConfigureAwait(false);
                            this.isReconnecting = !this._canReconnect;
                        }
                        else
                        {
                            Log.Trace(3, $"{this.name} waiting for other thread's reconnect");
                            while (this.isReconnecting) await Task.Delay(1000).ConfigureAwait(false);
                        }
                    }
                }
            }

            public async ValueTask Send(string message)
            {
                Log.Trace(3, $"{this.name} sends '{message}'");
                do
                {
                    try
                    {
                        if (this.client.GetState() != TcpState.Established) throw new IOException();
                        await this.writer.WriteLineAsync(message).ConfigureAwait(false);
                        await this.writer.FlushAsync().ConfigureAwait(false);
                        break;      // out of the retry loop
                    }
                    catch (Exception x) when (x is IOException || x is ObjectDisposedException)     // connection lost
                    {
                        Log.Trace(4, $"{this.name} starts reconnect after failed send");
                        await this.HandleLostConnection().ConfigureAwait(false);
                        if (this._canReconnect)
                        {
                            if (this.client == null) throw;
                            // reconnected; try to send again
                        }
                        else
                        {
                            throw;
                        }

                        Log.Trace(4, $"{this.name} finished reconnect after failed send");
                    }
                } while (true);
                Log.Trace(4, $"{this.name} sent '{message}'");

#if DEBUG
                //if (RandomGenerator.Instance.Percentage(2))
                //{
                //    // simulate a network error:
                //    Log.Trace(1, $"{this.name} simulates a network error by closing the stream ###########################");
                //    try
                //    {
                //        this.client.Client.Disconnect(false);
                //        this.stream.Close();
                //    }
                //    catch (Exception x)
                //    {
                //        Log.Trace(1, $" error while closing: {x.Message}");
                //    }
                //}
#endif
            }

            public async ValueTask<string> SendAndWait(string message)
            {
                // todo: change to using semaphore, set boolean that message may not be processed
                await this.Stop().ConfigureAwait(false);
                await this.runTask.ConfigureAwait(false);
#if NET6_0_OR_GREATER
                if (!this.cts.TryReset())
#endif
                    this.cts = new CancellationTokenSource();
                await this.Send(message).ConfigureAwait(false);
                var answer = await this.ReadLineAsync().ConfigureAwait(false);
                this.Start();
                return answer;
            }
        }
    }

    public static class TcpClientExtensions
    {
        public static TcpState GetState(this TcpClient tcpClient)
        {
            if (tcpClient == null || tcpClient.Client == null || tcpClient.Client.RemoteEndPoint == null) return TcpState.Closed;
            var activeTcpConnections = IPGlobalProperties.GetIPGlobalProperties().GetActiveTcpConnections().Where(c => c.LocalEndPoint.Address.GetAddressBytes()[0] == 127 && c.LocalEndPoint.Port == 3000);
            var brokenTcpConnections = activeTcpConnections.Where(c => c.State != TcpState.Established);
            if (brokenTcpConnections.Count() == 0) return TcpState.Established;
            foreach (var connection in activeTcpConnections)
            {
                if (Equals(connection, tcpClient.Client)) return connection.State;
                //Log.Debug($"{connection.LocalEndPoint}/{connection.LocalEndPoint.Address.MapToIPv6()} {connection.RemoteEndPoint}/{connection.RemoteEndPoint.Address.MapToIPv6()} {tcpClient.Client.LocalEndPoint} {tcpClient.Client.RemoteEndPoint}");
            }
            return TcpState.Unknown;

            bool Equals(TcpConnectionInformation activeConnection, Socket suspectedConnection)
            {
                if (Equals(activeConnection.LocalEndPoint, suspectedConnection.LocalEndPoint) && Equals(activeConnection.RemoteEndPoint, suspectedConnection.RemoteEndPoint)) return true;
                if (Equals(activeConnection.LocalEndPoint, suspectedConnection.RemoteEndPoint) && Equals(activeConnection.RemoteEndPoint, suspectedConnection.LocalEndPoint)) return true;
                return false;

                bool Equals(IPEndPoint activeEndpoint, EndPoint suspectedEndpoint)
                {
                    var x1 = activeEndpoint.ToString().Replace("::ffff:", "").Replace("[", "").Replace("]", "");
                    var x2 = suspectedEndpoint.ToString().Replace("::ffff:", "").Replace("[", "").Replace("]", "");
                    return x1 == x2;
                }
            }
        }
    }
}