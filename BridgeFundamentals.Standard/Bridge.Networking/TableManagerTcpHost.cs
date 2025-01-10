using System;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Bridge.Networking
{
    public class TableManagerTcpHost : AsyncTableHost<HostTcpCommunication>
    {
        public TableManagerTcpHost(HostMode mode, HostTcpCommunication communicationDetails, BridgeEventBus bus, string hostName, Tournament tournament, AlertMode alertMode) : base(mode, communicationDetails, bus, hostName, tournament, alertMode)
        {
        }
    }

    public class HostTcpCommunication : HostCommunication
    {
        private readonly BaseAsyncTcpHost tcpHost;

        public HostTcpCommunication(int port, string hostName) : base(hostName + ".HostTcpCommunication")
        {
            this.tcpHost = new BaseAsyncTcpHost(new IPEndPoint(IPAddress.Any, port), this.ProcessClientMessage, this.HandleNewClient, hostName);
            tcpHost.OnClientConnectionLost = this.HandleConnectionLost;
        }

        public override async ValueTask Run()
        {
            await this.tcpHost.Run().ConfigureAwait(false);
        }

        private async ValueTask HandleNewClient(int clientId)
        {   // tcp host has accepted a new listener
            Log.Trace(4, $"{this.name}.HandleNewClient: new client {clientId}; no seat yet");
            //if (this.DisconnectedSeats == 0)
            //{
            //    Log.Trace(4, $"{this.name}.HandleNewClient: no new client expected. isReconnecting={this.isReconnecting}. What is the status of all TcpClient's?");
            //}
            //for (Seats s = Seats.North; s <= Seats.West; s++)
            //{
            //    Log.Trace(4, $"{this.name}.HandleNewClient: seat {s} has connection {this.clients[s]}");
            //}
            //lock (this.seats)
            //{
            //    this.seats.Add(clientId, (Seats)(-1));
            //    if (this.isReconnecting)
            //    {
            //        if (this.DisconnectedSeats == 1)
            //        {
            //            for (Seats s = Seats.North; s <= Seats.West; s++)
            //            {
            //                if (this.clients[s] < 0)
            //                {
            //                    Log.Trace(4, $"{this.name}.HandleNewClient: new client seated in {s}");
            //                    this.clients[s] = clientId;
            //                    this.seats[clientId] = s;
            //                    this.isReconnecting = false;
            //                    break;
            //                }
            //            }
            //        }
            //    }
            //}
            await Task.CompletedTask.ConfigureAwait(false);
        }

        public override void StopAcceptingNewClients()
        {
            tcpHost.AcceptNewClients = false;
        }

        public override async ValueTask Send(int clientId, string message)
        {
            await this.tcpHost.Send(clientId, message).ConfigureAwait(false);
        }

        public override async ValueTask<string> SendAndWait(int clientId, string message)
        {
            return await this.tcpHost.SendAndWait(clientId, message).ConfigureAwait(false);
        }

        public override async ValueTask<string> GetMessage(int clientId)
        {
            return await this.tcpHost.GetMessage(clientId).ConfigureAwait(false);
        }

        protected override async ValueTask DisposeManagedObjects()
        {
            await this.tcpHost.DisposeAsync().ConfigureAwait(false);
        }

        public override void Stop()
        {
            this.tcpHost.Stop();
        }

        private async ValueTask HandleConnectionLost(int clientId)
        {
            this.isReconnecting = true;
            //if (this.seats[clientId] >= Seats.North) this.clients[this.seats[clientId]] = -1;
            //this.seats.Remove(clientId);
            Log.Trace(1, $"{this.name}: {clientId} lost connection. Wait for client to reconnect....");
            await this.OnClientConnectionLost(clientId);
        }

        private async ValueTask HandleConnectionLost(Seats seat)
        {
            Log.Trace(1, $"{this.name}: Seat {seat} lost connection. Wait for seat to reconnect....");
            //var badClient = this.clients[seat];
            //this.seats.Remove(badClient);
            //this.clients[seat] = -1;
            //this.isReconnecting = true;
            //if (this.DisconnectedSeats == 1)
            //{
            //    lock (this.seats)
            //    {
            //        foreach (var connection in seats)
            //        {
            //            Log.Trace(4, $"Connection {connection.Key} has seat {connection.Value}");
            //            if (connection.Key != badClient && connection.Value < Seats.North)
            //            {
            //                if (this.DisconnectedSeats > 1) break;
            //                this.clients[seat] = connection.Key;
            //                this.seats[connection.Key] = seat;
            //                this.isReconnecting = false;
            //                Log.Trace(1, $"Found a new connection {connection.Key} for seat {seat}");
            //            }
            //        }
            //    }
            //}
            await Task.CompletedTask.ConfigureAwait(false);
        }
    }

    public class BaseAsyncTcpHost : BaseAsyncHost
    {
        public BaseAsyncTcpHost(IPEndPoint tcpPort, Func<int, string, ValueTask> _processMessage, Func<int, ValueTask> _onNewClient, string hostName) : base(tcpPort, _processMessage, _onNewClient, hostName + ".AsyncTcpHost")
        {
            this.AcceptNewClients = true;
        }

        public bool AcceptNewClients { get; set; }

        public async ValueTask Run()
        {
            Log.Trace(4, $"{this.name}.Run begin");
            var listener = new TcpListener(this.endPoint);

            // trick to prevent error in unittests "Only one usage of each socket address (protocol/network address/port) is normally permitted"
            // https://social.msdn.microsoft.com/Forums/en-US/e1cc5f98-5a85-4da7-863e-f4d5623d80a0/forcing-tcplisteneros-to-release-port?forum=netfxcompact
            //listener.Server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, 1);

            listener.Start();
            this.isRunning = true;
            while (this.isRunning)
            {
                try
                {
#if NET6_0_OR_GREATER
                    var c = await listener.AcceptTcpClientAsync(cts.Token).ConfigureAwait(false);
#else
                    var c = await listener.AcceptTcpClientAsync().ConfigureAwait(false);
#endif
                    if (this.AcceptNewClients)
                    {
                        Log.Trace(2, $"{this.name}.Run: Accepted new client");
                        var client = new HostAsyncTcpClient($"{this.name}.client", clients.Count, c, this.ProcessClientMessage);
                        if (this.onNewClient != null) await this.onNewClient(clients.Count).ConfigureAwait(false);
                        this.clients.Add(client);
                        client.OnClientConnectionLost = HandleConnectionLost;
                        client.Start();
#if DEBUG
                        //await client.Send("welcome").ConfigureAwait(false);
#endif
                    }
                    else
                    {
                        Log.Trace(2, $"{this.name}.Run: Rejected new client");
                    }
                }
                catch (OperationCanceledException)
                {
                }
            }

            Log.Trace(4, $"{this.name}.Run stop clients");
            foreach (var client in this.clients)
            {
                await client.Stop().ConfigureAwait(false);
            }
            Log.Trace(4, $"{this.name}.Run end");
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
                Log.Trace(2, $"{this.name} dispose begin");
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

            public override async ValueTask Send(string message)
            {
                // the lock is necessary to prevent 2 simultane sends from one client
                using (await AsyncLock.WaitForLockAsync(this.name).ConfigureAwait(false))
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
}
