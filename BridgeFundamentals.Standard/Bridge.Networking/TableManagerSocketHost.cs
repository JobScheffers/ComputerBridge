﻿using System;
using System.Net;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace Bridge.Networking
{
    public class TableManagerSocketHost : AsyncTableHost<HostSocketCommunication>
    {
        public TableManagerSocketHost(HostMode mode, HostSocketCommunication communicationDetails, BridgeEventBus bus, string hostName, Tournament tournament, AlertMode alertMode, Scorings _matchType, int table, string teamNS, string teamEW)
            : base(mode, communicationDetails, bus, hostName, tournament, alertMode, _matchType, table, teamNS, teamEW)
        {
        }
    }

    public class HostSocketCommunication : HostCommunication
    {
        private readonly BaseAsyncSocketHost host;

        public HostSocketCommunication(int port, string hostName) : base(hostName + ".HostSocketCommunication")
        {
            this.host = new BaseAsyncSocketHost(new IPEndPoint(IPAddress.Any, port), this.ProcessClientMessage, this.HandleNewClient, hostName);
            host.OnClientConnectionLost = this.HandleConnectionLost;
        }

        public override async ValueTask Run()
        {
            await this.host.Run().ConfigureAwait(false);
        }

        public override void StopAcceptingNewClients()
        {
            //TODO: refuse new clients when table has 4 seated robots
        }

        public override async ValueTask Send(int clientId, string message)
        {
            await this.host.Send(clientId, message).ConfigureAwait(false);
        }

        public override void Stop()
        {
            this.host.Stop();
        }

        protected override async ValueTask DisposeManagedObjects()
        {
            await this.host.DisposeAsync().ConfigureAwait(false);
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

            //if (!this.isReconnecting)
            //{
            var tableName = await this.host.SendAndWait(clientId, "connected").ConfigureAwait(false);
            await this.host.Send(clientId, $"match 1").ConfigureAwait(false);
            //}
        }

        public override ValueTask<string> SendAndWait(int clientId, string message)
        {
            throw new NotImplementedException();
        }

        private async ValueTask HandleConnectionLost(int clientId)
        {
            //this.isReconnecting = true;
            //if (this.seats[clientId] >= Seats.North) this.clients[this.seats[clientId]] = -1;
            //this.seats.Remove(clientId);
            Log.Trace(1, $"{this.name}: {clientId} lost connection. Wait for client to reconnect....");
            await Task.CompletedTask.ConfigureAwait(false);
        }

        public override async ValueTask<string> GetMessage(int clientId)
        {
            return await this.host.GetMessage(clientId).ConfigureAwait(false);
        }

        private class BaseAsyncSocketHost : BaseAsyncHost
        {
            public BaseAsyncSocketHost(IPEndPoint tcpPort, Func<int, string, ValueTask> _processMessage, Func<int, ValueTask> _onNewClient, string hostName) : base(tcpPort, _processMessage, _onNewClient, hostName + ".AsyncSocketHost")
            {
            }

            public async ValueTask Run()
            {
                Log.Trace(4, $"{this.name}.Run begin");
                var listener = new HttpListener();
                listener.Prefixes.Add($"http://localhost:{this.endPoint.Port}/");

                // trick to prevent error in unittests "Only one usage of each socket address (protocol/network address/port) is normally permitted"
                // https://social.msdn.microsoft.com/Forums/en-US/e1cc5f98-5a85-4da7-863e-f4d5623d80a0/forcing-tcplisteneros-to-release-port?forum=netfxcompact
                //listener.Server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, 1);

                listener.Start();
                this.isRunning = true;
                while (this.isRunning)
                {
                    try
                    {
                        HttpListenerContext context = await listener.GetContextAsync()
#if NET6_0_OR_GREATER
                            .WaitAsync(cts.Token)
#endif
                            .ConfigureAwait(false)
                            ;

                        if (context.Request.IsWebSocketRequest)
                        {
                            HttpListenerWebSocketContext wsContext = await context.AcceptWebSocketAsync(null).ConfigureAwait(false);
                            Log.Trace(2, $"{this.name}.Run new client");
                            var clientId = clients.Count;
                            var client = new HostAsyncSocketClient($"{this.name}.client", clientId, wsContext.WebSocket, this.ProcessClientMessage);
                            this.clients.Add(client);
                            client.OnClientConnectionLost = HandleConnectionLost;
                            if (this.onNewClient != null) await this.onNewClient(clientId).ConfigureAwait(false);
                            client.Start();
                        }
                        else
                        {
                            // TODO: Handle regular HTTP activities here
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

            private class HostAsyncSocketClient : BaseAsyncHostClient
            {
                //private bool isRunning = false;
                //private Task runTask;
                //private string remainingMessage = string.Empty;
                private readonly WebSocketWrapper socketWrapper;

                public HostAsyncSocketClient(string _name, int _id, WebSocket client, Func<int, string, ValueTask> _processMessage) : base(_name, _id, _processMessage)
                {
                    this.socketWrapper = new WebSocketWrapper(client);
                    this.socketWrapper.OnMessage(async (message, wrapper) => await this.ProcessMessage(message).ConfigureAwait(false));
                }

                protected override async ValueTask DisposeManagedObjects()
                {
                    Log.Trace(2, $"{this.NameForLog} dispose begin");
                    //this.isRunning = false;
                    await Task.CompletedTask.ConfigureAwait(false);
                    this.cts.Dispose();
                }

                public override async ValueTask Stop()
                {
                    Log.Trace(4, $"{this.NameForLog} Stop");
                    await this.socketWrapper.DisconnectAsync("Close by server request").ConfigureAwait(false);
                    //this.isRunning = false;
                    //this.socketWrapper.StopListening();
                    this.cts.Cancel();
                }

                public void Start()
                {
                    Log.Trace(4, $"{this.NameForLog} Start");
                    //this.runTask = this.Run();
                    this.socketWrapper.StartListening();
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
                                //this.isRunning = false;
                                try
                                {
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
                        await this.socketWrapper.SendMessageAsync(message).ConfigureAwait(false);
                        Log.Trace(4, $"{this.NameForLog} sent '{message}'");
                    }
                }

                public override async ValueTask<string> SendAndWait(string message)
                {
                    await this.Send(message).ConfigureAwait(false);
                    return await this.socketWrapper.GetResponseAsync(CancellationToken.None).ConfigureAwait(false);
                }

                public override async ValueTask<string> GetMessage()
                {
                    return await this.socketWrapper.GetResponseAsync(CancellationToken.None).ConfigureAwait(false);
                }
            }
        }
    }
}
