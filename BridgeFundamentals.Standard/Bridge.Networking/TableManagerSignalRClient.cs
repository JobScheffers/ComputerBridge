using Microsoft.AspNetCore.SignalR.Client;
using System;
using System.Threading.Tasks;

namespace Bridge.Networking
{
    public class SignalRCommunicationDetails : SocketCommunicationDetailsBase
    {
        private HubConnection connection;

        public SignalRCommunicationDetails(string _baseUrl, string _tableName, string _teamName) : base(_baseUrl, _tableName, _teamName)
        {
        }

        protected override async Task Connect()
        {
            this.connection = CreateHubConnection();
            await this.connection.StartAsync();
            await this.connection.InvokeAsync("GetTable", tableName);
            await responseReceived.WaitAsync();
            await TakeSeat();
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
                    await Task.CompletedTask;
                    if (error != null) Log.Trace(3, $"Connection closed due to error: {error}");
                    Log.Trace(3, $"Connection was closed");
                };

                connection.Reconnected += async (arg) =>
                {
                    await TakeSeat();
                };

                return connection;
            }
        }

        protected new async Task TakeSeat()
        {
            //await this.SendCommandAsync("Sit", this.tableId, this.seat, this.teamName);
            await this.connection.SendAsync("Sit", this.tableId, this.seat, this.teamName);
        }

        public override async Task WriteProtocolMessageToRemoteMachine(string message)
        {
            await this.SendCommandAsync("SendProtocolMessage", tableId, this.seat, message);
            Log.Trace(0, "TM {1} sends '{0}'", message, this.seat.ToString().PadRight(5));
        }

        public override async Task DisposeAsync()
        {
            await this.SendCommandAsync("Unsit", tableId, this.seat);
            await this.connection.StopAsync();
            await this.connection.DisposeAsync();
        }

        public override async Task SendCommandAsync(string commandName, params object[] args)
        {
            switch (args.Length)
            {
                case 0:
                    await this.connection.SendAsync(commandName);
                    break;
                case 1:
                    await this.connection.SendAsync(commandName, args[0]);
                    break;
                case 2:
                    await this.connection.SendAsync(commandName, args[0], args[1]);
                    break;
                case 3:
                    await this.connection.SendAsync(commandName, args[0], args[1], args[2]);
                    break;
                default:
                    throw new ArgumentOutOfRangeException("Unexpected args.Length");
            }
        }

        public override Task DisposeConnectionAsync()
        {
            throw new NotImplementedException();
        }
    }
}
