using System;
using System.Threading.Tasks;
using System.Threading;
using System.Net.WebSockets;
using Bridge.NonBridgeHelpers;

namespace Bridge.Networking
{
    public class TableManagerClientRawSocket : TableManagerClientAsync<RawSocketCommunicationDetails<WebSocketClientBase>>
    {
        public TableManagerClientRawSocket() : base(new BridgeEventBus("raw"))
        {

        }
    }

    public class RawSocketCommunicationDetails<TClient> : SocketCommunicationDetailsBase where TClient : WebSocketClientBase
    {
        private TClient client;
        private WebSocketWrapper socket;
        private string matchId;

        public RawSocketCommunicationDetails(TClient _client, string matchName) : base(matchName, "")
        {
            this.client = _client;
        }

        public override async ValueTask SendCommandAsync(string commandName, params object[] args)
        {
            await this.socket.SendMessageAsync(commandName);
        }

        public override async ValueTask WriteProtocolMessageToRemoteMachine(string message)
        {
            await this.socket?.SendMessageAsync(message);
        }

        protected override async ValueTask Connect()
        {
            Log.Trace(4, $"{this.seat.ToString().PadRight(5)} Connect");
            var rawSocket = await this.client.ConnectAsync();
            this.socket = WebSocketWrapper.Create(rawSocket);
            var response = await this.socket.GetResponseAsync(CancellationToken.None);
            if (response != "connected") throw new InvalidOperationException($"Expected 'connected'. Actual '{response}'");
            await this.socket.SendMessageAsync($"Match {this.tableName}");
            response = await this.socket.GetResponseAsync(CancellationToken.None);
            if (!response.StartsWith("match ")) throw new InvalidOperationException($"Expected 'match'. Actual '{response}'");
            this.matchId = response.Split(' ')[1];
            this.socket.OnMessage((message, _socket) => this.processMessage(message));
#if DEBUG
            // to test how a disconnect is handled server-side
            //await this.socket.DisconnectAsync();
#endif
            this.socket.StartListening();
        }

        protected override async ValueTask DisposeManagedObjects()
        {
            Log.Trace(5, $"{this.seat.ToString().PadRight(5)} DisposeAsync");
            if (this.socket.CanWrite) await this.WriteProtocolMessageToRemoteMachine($"Unsit {this.seat} from {this.matchId}");
            try
            {
                await this.socket?.DisconnectAsync();
            }
            catch (ObjectDisposedException)
            {
            }
            this.socket = null;
            await this.client.DisposeAsync();
            this.client = null;
            Log.Trace(5, $"{this.seat.ToString().PadRight(5)} DisposeAsync done");
        }

        public override async ValueTask<string> GetResponseAsync()
        {
            return await this.socket.GetResponseAsync(CancellationToken.None);
        }
    }

    public class RealWebSocketClient : WebSocketClientBase
    {
        private readonly ClientWebSocket client;

        public RealWebSocketClient(string _url) : base(_url)
        {
            this.client = new ClientWebSocket();
        }

        public override async ValueTask<WebSocket> ConnectAsync()
        {
            await this.client.ConnectAsync(new Uri(url), CancellationToken.None);
            return this.client;
        }

        protected override async ValueTask DisposeManagedObjects()
        {
            await this.client.CloseAsync(WebSocketCloseStatus.NormalClosure, null, CancellationToken.None);
            this.client.Dispose();
        }
    }

    public abstract class WebSocketClientBase : BaseAsyncDisposable
    {
        protected readonly string url;

        public WebSocketClientBase(string _url)
        {
            this.url = _url;
        }

        public abstract ValueTask<WebSocket> ConnectAsync();
    }
}
