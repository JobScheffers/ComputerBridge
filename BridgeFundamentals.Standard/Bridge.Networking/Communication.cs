using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Bridge.Networking
{
#if NET6_0_OR_GREATER
    public class InProcessCommunicationFactory : CommunicationFactory
    {
        private InProcessCommunicationHost host;
        private int lastClientId = 0;

        public override CommunicationClient CreateClient()
        {
            if (host == null) throw new Exception("create host before creating clients");
            int newClientId = ++lastClientId;
            var newClient = new InProcessCommunicationClient(host, newClientId);
            host.Add(newClient, newClientId);
            return newClient;
        }

        public override CommunicationHost CreateHost()
        {
            if (host != null) throw new Exception("host already created");
            host = new InProcessCommunicationHost();
            return host;
        }
    }

    public class TcpCommunicationFactory(IPEndPoint _endPoint) : CommunicationFactory
    {
        private TcpCommunicationHost host;
        private readonly IPEndPoint endPoint = _endPoint;

        public override CommunicationClient CreateClient()
        {
            //if (host == null) throw new Exception("create host before creating clients");
            var newClient = new TcpCommunicationClient(this.endPoint);
            return newClient;
        }

        public override CommunicationHost CreateHost()
        {
            if (host != null) throw new Exception("host already created");
            host = new TcpCommunicationHost(this.endPoint);
            return host;
        }
    }

    public class InProcessCommunicationHost() : CommunicationHost("InProcessCommunicationHost")
    {
        private readonly Dictionary<int, InProcessCommunicationClient> clients = [];

        public override async ValueTask StartHosting()
        {
            await ValueTask.CompletedTask;
        }

        public void Add(InProcessCommunicationClient client, int clientId)
        {
            clients[clientId] = client;
        }

        public async ValueTask Receive(string message, int clientId)
        {
            Log.Trace(6, $"Host {clientId} receives '{message}' from client {clientId}");
            await ProcessMessage(clientId, message);
        }

        public override async ValueTask Send(string message, int clientId)
        {
            Log.Trace(6, $"Host {clientId} sends '{message}' to client {clientId}");
            await clients[clientId].Receive(message);
        }
    }

    public class TcpCommunicationHost : CommunicationHost
    {
        private readonly Dictionary<int, BaseAsyncHostClient> clients = [];
        private readonly IPEndPoint endPoint;
        protected bool isRunning = false;
        private int lastClientId = 0;

        public TcpCommunicationHost(IPEndPoint _endPoint) : base("TcpCommunicationHost")
        {
            endPoint = _endPoint;
            Start();
        }

        public override async ValueTask StartHosting()
        {
            Log.Trace(6, $"{this.NameForLog}.StartHosting begin");
            var listener = new TcpListener(this.endPoint);

            // trick to prevent error in unittests "Only one usage of each socket address (protocol/network address/port) is normally permitted"
            // https://social.msdn.microsoft.com/Forums/en-US/e1cc5f98-5a85-4da7-863e-f4d5623d80a0/forcing-tcplisteneros-to-release-port?forum=netfxcompact
            //listener.Server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, 1);

            listener.Start();
            Log.Trace(6, $"{this.NameForLog}.StartHosting listener started");
            this.isRunning = true;
            while (this.isRunning && !cts.IsCancellationRequested)
            {
                try
                {
                    Log.Trace(6, $"{this.NameForLog}.StartHosting wait for new client");
                    var tcpClient = await listener.AcceptTcpClientAsync(cts.Token).ConfigureAwait(false);
                    if (!cts.IsCancellationRequested)
                    {
                        //if (this.AcceptNewClients)
                        {
                            Log.Trace(2, $"{this.NameForLog}.StartHosting: Accepted new client");
                            int newClientId = ++lastClientId;
                            var client = new HostAsyncTcpClient($"{this.NameForLog}.client.", newClientId, tcpClient, this.Receive);
                            //if (this.onNewClient != null) await this.onNewClient(clients.Count).ConfigureAwait(false);
                            this.clients.Add(newClientId, client);
                            //client.OnClientConnectionLost = HandleConnectionLost;
                            client.Start();
#if DEBUG
                            //await client.Send("welcome").ConfigureAwait(false);
#endif
                        }
                        //else
                        //{
                        //    Log.Trace(2, $"{this.name}.Run: Rejected new client");
                        //}
                    }
                }
                catch (OperationCanceledException)
                {
                }
            }

            Log.Trace(4, $"{this.NameForLog}.Run stop clients");
            foreach (var client in this.clients.Values)
            {
                await client.Stop().ConfigureAwait(false);
            }
            Log.Trace(4, $"{this.NameForLog}.Run end");
        }

        public async ValueTask Receive(int clientId, string message)
        {
            Log.Trace(6, $"Host receives '{message}' from client {clientId}");
            await ProcessMessage(clientId, message);
        }

        public override async ValueTask Send(string message, int clientId)
        {
            Log.Trace(6, $"Host sends '{message}' to client {clientId}");
            await clients[clientId].Send(message);
        }

        private class HostAsyncTcpClient : BaseAsyncHostClient
        {
            private TcpClient client;
            private NetworkStream stream;
            private StreamWriter writer;
            private StreamReader reader;
            private bool isRunning = false;
            private Task runTask;
            private string remainingMessage = string.Empty;

            public HostAsyncTcpClient(string _name, int _id, TcpClient client, Func<int, string, ValueTask> _processMessage) : base(_name, _id, _processMessage)
            {
                this.client = client;
                this.client.NoDelay = true;
                this.AfterConnect();
            }

            protected override async ValueTask DisposeManagedObjects()
            {
                Log.Trace(2, $"{this.NameForLog} dispose begin");
                this.isRunning = false;
                await Task.CompletedTask.ConfigureAwait(false);
                if (this.client != null) this.client.Dispose();
                if (this.stream != null) this.stream.Dispose();
                this.writer.Dispose();
                this.cts.Dispose();
            }

            public override async ValueTask Stop()
            {
                await Task.CompletedTask.ConfigureAwait(false);
                this.isRunning = false;
                this.cts.Cancel();
            }

            private void AfterConnect()
            {
                Log.Trace(4, $"{this.NameForLog}.BaseAsyncTcpClient.AfterConnect begin");
                this.stream = client.GetStream();
                this.writer = new StreamWriter(this.stream);
                this.reader = new StreamReader(this.stream);
                Log.Trace(4, $"{this.NameForLog}.BaseAsyncTcpClient.AfterConnect end");
            }

            public void Start()
            {
                Log.Trace(4, $"{this.NameForLog} Start");
                this.runTask = this.Run();
            }

            public async Task Run()
            {
                Log.Trace(6, $"AsyncClient.Run {this.NameForLog} begin");
                this.isRunning = true;
                while (this.isRunning)
                {
                    var message = await this.ReadLineAsync().ConfigureAwait(false);
                    if (!string.IsNullOrWhiteSpace(message))
                    {
                        Log.Trace(3, $"{this.NameForLog} receives '{message}'");
                        await this.ProcessMessage(message).ConfigureAwait(false);
                    }
                }
                Log.Trace(6, $"AsyncClient.Run {this.NameForLog} end");
            }

            private char[] buffer = new char[1024];
            private async ValueTask<string> ReadLineAsync()
            {
                int charsRead = 0;
                do
                {
                    try
                    {
                        if (!this.isRunning || this.stream == null) return string.Empty;
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
                        Log.Trace(4, $"{this.NameForLog} failed read");
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
                            Log.Trace(3, $"{this.NameForLog}.HandleLostConnection");
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
                            Log.Trace(3, $"{this.NameForLog} waiting for other thread's reconnect");
                            while (this.isReconnecting) await Task.Delay(1000).ConfigureAwait(false);
                        }
                    }
                }
            }

            public override async ValueTask Send(string message)
            {
                // the lock is necessary to prevent 2 simultane sends from one client
                using (await AsyncLock.WaitForLockAsync(this.NameForLog).ConfigureAwait(false))
                {
                    Log.Trace(3, $"{this.NameForLog} sends '{message}'");
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
                            Log.Trace(4, $"{this.NameForLog} starts reconnect after failed send");
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

                            Log.Trace(4, $"{this.NameForLog} finished reconnect after failed send");
                        }
                    } while (true);
                    Log.Trace(4, $"{this.NameForLog} sent '{message}'");
                }

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

            public override async ValueTask<string> SendAndWait(string message)
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

            public override ValueTask<string> GetMessage()
            {
                throw new NotImplementedException();
            }
        }
    }

    public class InProcessCommunicationClient(InProcessCommunicationHost _host, int _clientId) : CommunicationClient($"InProcessCommunicationClient.{_clientId}")
    {
        private readonly InProcessCommunicationHost host = _host;
        private readonly int clientId = _clientId;

        public override ValueTask Send(string message)
        {
            Log.Trace(6, $"{NameForLog} sends '{message}'");
            return host.Receive(message, clientId);
        }

        public async ValueTask Receive(string message)
        {
            Log.Trace(6, $"{NameForLog} receives '{message}'");
            await ProcessMessage(message);
        }

        public override async ValueTask Connect()
        {
            await ValueTask.CompletedTask;
        }

        protected override async ValueTask StartListening()
        {
            await ValueTask.CompletedTask;
        }
    }

    public class TcpCommunicationClient(IPEndPoint _endpoint) : CommunicationClient("TcpCommunicationClient")
    {
        private readonly IPEndPoint endPoint = _endpoint;
        private TcpClient client;
        private NetworkStream stream;
        private StreamWriter writer;
        private StreamReader reader;
        private bool isRunning = false;
        private string remainingMessage = string.Empty;
        private readonly char[] buffer = new char[1024];

        public override async ValueTask Connect()
        {
            Log.Trace(6, $"{this.NameForLog}.Connect");
            this.client = new TcpClient
            {
                /// When NoDelay is false, a TcpClient does not send a packet over the network until it has collected a significant amount of outgoing data.
                /// Because of the amount of overhead in a TCP segment, sending small amounts of data is inefficient.
                /// However, situations do exist where you need to send very small amounts of data or expect immediate responses from each packet you send.
                /// Your decision should weigh the relative importance of network efficiency versus application requirements.
                NoDelay = true
            };

            int attempts = 0;
            do
            {
                attempts++;
                try
                {
                    await this.client.ConnectAsync(endPoint);
                    break;
                }
                catch (SocketException x) when (x.SocketErrorCode == SocketError.ConnectionRefused && attempts < 30)
                {
                    Log.Trace(3, $"{this.NameForLog}.Connect: no host listening at address {endPoint}");
                    await Task.Delay(2000);
                }
            } while (true);
            Log.Trace(6, $"{this.NameForLog}.Connect client has connected");
            //this.AfterConnect();
            this.stream = client.GetStream();
            this.writer = new StreamWriter(this.stream);
            this.reader = new StreamReader(this.stream);
        }

        protected override async ValueTask StartListening()
        {
            this.isRunning = true;
            while (this.isRunning && !cts.IsCancellationRequested)
            {
                var message = await ReadLineAsync().ConfigureAwait(false);
                if (!string.IsNullOrWhiteSpace(message) && !cts.IsCancellationRequested)
                {
                    Log.Trace(6, $"{this.NameForLog} receives '{message}'");
                    await this.ProcessMessage(message).ConfigureAwait(false);
                }
            }
            Log.Trace(6, $"{this.NameForLog} stopped listening");

            async ValueTask<string> ReadLineAsync()
            {
                int charsRead = 0;
                do
                {
                    try
                    {
                        if (!this.isRunning || this.stream == null || cts.IsCancellationRequested) return string.Empty;
                        charsRead = await this.reader.ReadAsync(buffer, cts.Token).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException)
                    {
                        return string.Empty;
                    }
                    catch (ObjectDisposedException)
                    {
                        //if (this._canReconnect)
                        //{
                        //    await this.HandleLostConnection().ConfigureAwait(false);
                        //    if (this.client == null) return string.Empty;
                        //    // reconnected; try to read from the stream again
                        //}
                        //else
                        {
                            this.isRunning = false;
                            return string.Empty;
                        }
                    }
                    catch (IOException)     // connection lost
                    {
                        Log.Trace(4, $"{this.NameForLog} failed read");
                        //await this.HandleLostConnection().ConfigureAwait(false);
                        //if (this._canReconnect)
                        //{
                        //    if (this.client == null) return string.Empty;
                        //    // reconnected; try to read from the stream again
                        //}
                        //else
                        {
                            return string.Empty;
                        }
                    }

                    if (cts.IsCancellationRequested) return string.Empty;
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
        }

        //public async ValueTask Receive(string message)
        //{
        //    Log.Trace(6, $"Client receives '{message}'");
        //    await ProcessMessage(message);
        //}

        public override async ValueTask Send(string message)
        {
            Log.Trace(6, $"{this.NameForLog} sends '{message}'");
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
                    Log.Trace(4, $"{this.NameForLog} starts reconnect after failed send");
                    //await this.HandleLostConnection().ConfigureAwait(false);
                    //if (this._canReconnect)
                    //{
                    //    if (this.client == null) throw;
                    //    // reconnected; try to send again
                    //}
                    //else
                    //{
                    //    throw;
                    //}

                    Log.Trace(4, $"{this.NameForLog} finished reconnect after failed send");
                }
            } while (true);
            Log.Trace(7, $"{this.NameForLog} sent '{message}'");

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
    }

    public abstract class CommunicationFactory
    {
        public abstract CommunicationHost CreateHost();
        public abstract CommunicationClient CreateClient();
    }

    public abstract class CommunicationHost(string nameForLog)
    {
        protected readonly string NameForLog = nameForLog;
        protected readonly CancellationTokenSource cts = new();
        public Func<int, string, ValueTask> ProcessMessage;
        private ValueTask hostingTask;

        public void Start()
        {
            hostingTask = StartHosting();
        }

        public async ValueTask Stop()
        {
            cts.Cancel();
            await hostingTask.ConfigureAwait(false);
        }

        public abstract ValueTask StartHosting();
        public abstract ValueTask Send(string message, int clientId);
    }

    public abstract class CommunicationClient(string nameForLog)
    {
        protected readonly string NameForLog = nameForLog;
        protected readonly CancellationTokenSource cts = new();
        private ValueTask listeningTask;
        public Func<string, ValueTask> ProcessMessage;

        public void Start()
        {
            listeningTask = StartListening();
        }

        public async ValueTask Stop()
        {
            cts.Cancel();
            await listeningTask.ConfigureAwait(false);
        }

        public abstract ValueTask Connect();
        protected abstract ValueTask StartListening();
        public abstract ValueTask Send(string message);
    }
#endif
}