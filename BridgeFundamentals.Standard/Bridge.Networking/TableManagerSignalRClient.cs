﻿using Microsoft.AspNetCore.SignalR.Client;
using System;
using System.Threading.Tasks;

namespace Bridge.Networking
{
    public class SignalRCommunicationDetails : SocketCommunicationDetailsBase
    {
        private HubConnection connection;
        private string baseUrl;

        public SignalRCommunicationDetails(string _baseUrl, string _tableName, string _teamName) : base(_tableName, _teamName)
        {
            this.baseUrl = _baseUrl;
        }

        protected override async ValueTask Connect()
        {
            this.connection = CreateHubConnection();
            await this.connection.StartAsync().ConfigureAwait(false);
            await this.connection.InvokeAsync("GetTable", tableName).ConfigureAwait(false);
            await responseReceived.WaitAsync().ConfigureAwait(false);
            await TakeSeat().ConfigureAwait(false);
            return;

            HubConnection CreateHubConnection()
            {
                HubConnection connection;
                connection = new HubConnectionBuilder()
                    .WithUrl(baseUrl
                        //, transports: Microsoft.AspNetCore.Http.Connections.HttpTransportType.WebSockets
                        )
                    .WithAutomaticReconnect()
                    //.ConfigureLogging(options =>
                    //{
                    //    options.SetMinimumLevel(LogLevel.Trace);
                    //})
                    .Build();

                connection.On<string>("ReceiveMessage", (msg) =>
                {
                    Log.Trace(3, $"ReceiveMessage {msg}");
                    this.responseReceived.Release();
                });

                connection.On<string>("ReceiveProtocolMessage", msg =>
                {
                    Log.Trace(3, $"{this.seat,5} received protocol message (over sockets): {msg}");
                    this.responseReceived.Release();
                    this.processMessage(msg);
                });

                connection.On<Guid>("ReceiveTableId", (msg) =>
                {
                    this.tableId = msg;
                    Log.Trace(3, $"tableId={tableId}");
                    this.responseReceived.Release();
                });

                connection.Closed += async (error) =>
                {
                    await Task.CompletedTask.ConfigureAwait(false);
                    if (error != null) Log.Trace(3, $"Connection closed due to error: {error}");
                    Log.Trace(3, $"Connection was closed");
                };

                connection.Reconnected += async (arg) =>
                {
                    await TakeSeat().ConfigureAwait(false);
                };

                return connection;
            }
        }

        protected new async ValueTask TakeSeat()
        {
            await this.connection.SendAsync("Sit", this.tableId, this.seat, this.teamName).ConfigureAwait(false);
        }

        public override async ValueTask WriteProtocolMessageToRemoteMachine(string message)
        {
            await this.SendCommandAsync("SendProtocolMessage", tableId, this.seat, message).ConfigureAwait(false);
            Log.Trace(0, "TM {1} sends '{0}'", message, this.seat.ToString().PadRight(5));
        }

        protected override async ValueTask DisposeManagedObjects()
        {
            await this.SendCommandAsync("Unsit", tableId, this.seat).ConfigureAwait(false);
            await this.connection.StopAsync().ConfigureAwait(false);
            await this.connection.DisposeAsync().ConfigureAwait(false);
        }

        public override async ValueTask SendCommandAsync(string commandName, params object[] args)
        {
            switch (args.Length)
            {
                case 0:
                    await this.connection.SendAsync(commandName).ConfigureAwait(false);
                    break;
                case 1:
                    await this.connection.SendAsync(commandName, args[0]).ConfigureAwait(false);
                    break;
                case 2:
                    await this.connection.SendAsync(commandName, args[0], args[1]).ConfigureAwait(false);
                    break;
                case 3:
                    await this.connection.SendAsync(commandName, args[0], args[1], args[2]).ConfigureAwait(false);
                    break;
                default:
                    throw new ArgumentOutOfRangeException("Unexpected args.Length");
            }
        }

        public override ValueTask<string> GetResponseAsync()
        {
            throw new NotImplementedException();
        }
    }
}
