using System;
using Microsoft.AspNetCore.SignalR.Client;
using System.Threading.Tasks;
using System.Threading;

namespace Bridge.Networking
{
    //public class TableManagerSocketClient : TableManagerClient, IDisposable
    //{
    //    private string tableName;
    //    private string teamName;
    //    private HubConnection connection;
    //    private Guid tableId;
    //    private SemaphoreSlim responseReceived = new SemaphoreSlim(0);

    //    public TableManagerSocketClient() : this(new BridgeEventBus("SocketClient")) { }

    //    public TableManagerSocketClient(BridgeEventBus bus) : base(bus)
    //    {
    //    }

    //    public async Task Connect(Seats _seat, string baseUrl, string _tableName, int _maxTimePerBoard, int _maxTimePerCard, string _teamName, int protocolVersion)
    //    {
    //        Log.Trace(2, "TableManagerSocketClient.Connect: Open connection to {0} {1}", baseUrl, tableName);
    //        this.seat = _seat;
    //        this.tableName = _tableName;
    //        this.teamName = _teamName;
    //        await this.StartSocketConnection(baseUrl, _tableName);
    //        base.Connect(_seat, _maxTimePerBoard, _maxTimePerCard, teamName, protocolVersion);
    //    }

    //    private async Task StartSocketConnection(string baseUrl, string tableName)
    //    {
    //        this.connection = CreateHubConnection();
    //        await this.connection.StartAsync();
    //        await this.connection.InvokeAsync("GetTable", tableName);
    //        await responseReceived.WaitAsync();
    //        await TakeSeat();
    //        return;

    //        HubConnection CreateHubConnection()
    //        {
    //            HubConnection connection;
    //            connection = new HubConnectionBuilder()
    //                .WithUrl(baseUrl
    //                    //, transports: Microsoft.AspNetCore.Http.Connections.HttpTransportType.WebSockets
    //                    )
    //                .WithAutomaticReconnect()
    //                //.ConfigureLogging(options =>
    //                //{
    //                //    options.SetMinimumLevel(LogLevel.Trace);
    //                //})
    //                .Build();

    //            connection.On<string>("ReceiveMessage", (msg) =>
    //            {
    //                Log.Trace(3, $"ReceiveMessage {msg}");
    //                this.responseReceived.Release();
    //            });

    //            connection.On<string>("ReceiveProtocolMessage", msg =>
    //            {
    //                Log.Trace(3, $"{this.seat,5} received protocol message (over sockets): {msg}");
    //                this.responseReceived.Release();
    //                this.ProcessIncomingMessage(msg);
    //            });

    //            connection.On<Guid>("ReceiveTableId", (msg) =>
    //            {
    //                this.tableId = msg;
    //                Log.Trace(3, $"tableId={tableId}");
    //                this.responseReceived.Release();
    //            });

    //            connection.Closed += async (error) =>
    //            {
    //                await Task.CompletedTask;
    //                if (error != null) Log.Trace(3, $"Connection closed due to error: {error}");
    //                Log.Trace(3, $"Connection was closed");
    //            };

    //            connection.Reconnected += async (arg) =>
    //            {
    //                await TakeSeat();
    //            };

    //            return connection;
    //        }
    //    }

    //    private async Task TakeSeat()
    //    {
    //        await this.connection.InvokeAsync("Sit", this.tableId, this.seat, this.teamName);
    //    }

    //    protected override async Task WriteProtocolMessageToRemoteMachine(string message)
    //    {
    //        await this.connection.SendAsync("SendProtocolMessage", tableId, this.seat, message);
    //        Log.Trace(0, "TM {1} sends '{0}'", message, this.seat.ToString().PadRight(5));
    //    }

    //    public void Dispose()
    //    {
    //        Dispose(true);
    //        GC.SuppressFinalize(this);
    //    }

    //    ~TableManagerSocketClient()
    //    {
    //        Dispose(false);
    //    }

    //    protected virtual void Dispose(bool disposing)
    //    {
    //        if (disposing)
    //        {
    //            // free managed resources
    //            this.connection.DisposeAsync().Wait();
    //        }
    //    }
    //}

    public class SocketsCommunicationDetails : CommunicationDetails
    {
        private string baseUrl;
        private string tableName;
        private string teamName;
        private HubConnection connection;
        private Guid tableId;
        private SemaphoreSlim responseReceived = new SemaphoreSlim(0);

        public SocketsCommunicationDetails(string _baseUrl, string _tableName, string _teamName)
        {
            this.baseUrl = _baseUrl;
            this.tableName = _tableName;
            this.teamName = _teamName;
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

        public override async Task WriteProtocolMessageToRemoteMachine(string message)
        {
            await this.connection.SendAsync("SendProtocolMessage", tableId, this.seat, message);
            Log.Trace(0, "TM {1} sends '{0}'", message, this.seat.ToString().PadRight(5));
        }

        private async Task TakeSeat()
        {
            await this.connection.InvokeAsync("Sit", this.tableId, this.seat, this.teamName);
        }

        public override void Dispose()
        {
            // free managed resources
            this.connection.DisposeAsync().Wait();
        }
    }
}
