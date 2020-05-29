using System;
using System.Threading.Tasks;
using System.Threading;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Collections.Generic;

namespace Bridge.Networking
{
    public class SocketCommunicationDetails : SocketCommunicationDetailsBase
    {
        // SignalR protocol description: https://github.com/aspnet/SignalR/blob/master/specs/HubProtocol.md

        private ClientWebSocketWrapper client;
        private WebSocketWrapper socket;
        private bool disposing = false;
        private string signalRSignature = "" + '\u001e';

        public SocketCommunicationDetails(string _baseUrl, string _tableName, string _teamName) : base(_baseUrl, _tableName, _teamName)
        {
            this.client = ClientWebSocketWrapper.Create(_baseUrl);
        }

        public override async Task SendCommandAsync(string commandName, params object[] args)
        {
            var command = new SignalRCommand { type = 1, target = commandName, arguments = args };
            await this.SendSignalRCommandAsync(command);
        }

        private async Task SendSignalRCommandAsync(SignalRCommand command)
        {
            var jsonCommand = JsonSerializer.Serialize(command, new JsonSerializerOptions { IgnoreNullValues = true });
            await this.SendJsonCommandAsync(jsonCommand);
        }

        private async Task SendJsonCommandAsync(string jsonCommand)
        {
            jsonCommand += signalRSignature;    // SignalR requirement
            await this.socket.SendMessageAsync(jsonCommand);
        }

        private async Task<object[]> WaitForCommandAsync(string commandName)
        {
            var response = await this.socket.GetResponseAsync();
            var command = ParseResponse(response.Substring(0, response.Length - 1));    // remove SignalR EOM
            if (command.Item1 != commandName) throw new InvalidOperationException($"Received command '{command.Item1}', expected '{commandName}'");
            return command.Item2;
        }

        private (string, object[]) ParseResponse(string response)
        {
            // {"type":1,"target":"ReceiveTableId","arguments":["11ea9f16-17e5-4138-a1b8-db7cf18272d7"]}
            var command = JsonSerializer.Deserialize<SignalRCommand>(response);
            if (command.type != 1) throw new InvalidOperationException("");
            return (command.target, command.arguments);
        }

        protected override async Task Connect()
        {
            await this.client.ConnectAsync();
            this.socket = this.client.wsw;
            this.socket.OnMessage(OnMessage);
            this.socket.OnDisconnect(OnDisconnect);

            await this.SendJsonCommandAsync(@"{""protocol"":""json"", ""version"":1}");     // SignalR handshake
            var response = await this.socket.GetResponseAsync();
            if (response != "{}" + signalRSignature) throw new InvalidOperationException("SignalR handshake: Expected 0x1e instead of " + response);
            Log.Trace(3, $"{this.seat,5} received protocol handshake");

            await this.SendCommandAsync("GetTable", tableName);
            var tableResponse = await this.WaitForCommandAsync("ReceiveTableId");
            this.tableId = Guid.Parse(tableResponse[0].ToString());

            this.socket.StartListening();
            await TakeSeat();
            return;

            void OnMessage(string message, WebSocketWrapper c)
            {
                var command = JsonSerializer.Deserialize<SignalRCommand>(message.Substring(0, message.Length - 1));
                switch (command.type)
                {
                    case 1:
                        switch (command.target)
                        {
                            case "ReceiveProtocolMessage":
                                var protocolMessage = command.arguments[0].ToString();
                                Log.Trace(2, $"{this.seat,5} received protocol message: {protocolMessage}");
                                this.processMessage(protocolMessage);
                                break;
                            default:
                                throw new ArgumentOutOfRangeException("command", $"'{command}' is an unknown command");
                        }
                        break;
                    case 6:     // keep-alive
                        if (!this.disposing)
                        {
                            //Log.Trace(2, $"{this.seat,5} sends keep-alive message");
                            //SendSignalRCommandAsync(command).Wait();    // send a response to let the server know we are alive.
                        }
                        break;
                    default:
                        throw new ArgumentOutOfRangeException("command.type", $"'{command.type}' is an unknown command type");
                }
            }

            async void OnDisconnect(WebSocketWrapper c)
            {
                Log.Trace(3, $"Connection was closed.");
                if (!this.disposing)
                {
                    Log.Trace(3, $"Try to reconnect....");
                    await this.client.ConnectAsync();
                    await this.SendJsonCommandAsync(@"{""protocol"":""json"", ""version"":1}");     // SignalR handshake
                    await TakeSeat();
                }
            }
        }

        public override async Task DisposeConnectionAsync()
        {
            //var command = new SignalRCommand { type = 7 };
            //await this.SendSignalRCommandAsync(command);
            await this.client.DisconnectAsync();
        }

        public override Task DisposeAsync()
        {
            this.disposing = true;
            return base.DisposeAsync();
        }

        public override Task<string> GetResponseAsync()
        {
            throw new NotImplementedException();
        }

        private class SignalRCommand
        {
            // {"type":1,"target":"ReceiveTableId","arguments":["11ea9f16-17e5-4138-a1b8-db7cf18272d7"]}
            public int type { get; set; }
            public string target { get; set; }
            public object[] arguments { get; set; }
            public string error { get; set; }
        }
    }

    public class ClientWebSocketWrapper
    {
        private readonly ClientWebSocket _ws;
        private readonly Uri _uri;
        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        private readonly CancellationToken _cancellationToken;

        public WebSocketWrapper wsw;

        protected ClientWebSocketWrapper(string uri)
        {
            _ws = new ClientWebSocket();
            _ws.Options.KeepAliveInterval = TimeSpan.FromSeconds(20);
            _uri = new Uri(uri);
            _cancellationToken = _cancellationTokenSource.Token;
        }

        /// <summary>
        /// Creates a new instance.
        /// </summary>
        /// <param name="uri">The URI of the WebSocket server.</param>
        /// <returns></returns>
        public static ClientWebSocketWrapper Create(string uri)
        {
            return new ClientWebSocketWrapper(uri);
        }

        /// <summary>
        /// Connects to the WebSocket server.
        /// </summary>
        /// <returns></returns>
        public async Task ConnectAsync()
        {
            Log.Trace(3, $"WebSocketWrapper.ConnectAsync");
            await _ws.ConnectAsync(_uri, _cancellationToken);
            wsw = WebSocketWrapper.Create(_ws);
            Log.Trace(3, $"WebSocketWrapper.ConnectAsync done");
        }

        public async Task DisconnectAsync()
        {
            this._cancellationTokenSource.Cancel();
            if (_ws.State == WebSocketState.Open)
            {
                try
                {
                    await _ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Close by client request", CancellationToken.None);
                }
                catch (WebSocketException)
                {
                }
            }
        }
    }

    public class WebSocketWrapper
    {
        private const int ReceiveChunkSize = 1024;
        private const int SendChunkSize = 1024;

        private readonly WebSocket _ws;
        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        private readonly CancellationToken _cancellationToken;

        private Action<string, WebSocketWrapper> _onMessage;
        private Action<WebSocketWrapper> _onDisconnected;
        private byte[] buffer = new byte[ReceiveChunkSize];
        private SemaphoreSlim locker;

        public WebSocketWrapper(WebSocket socket)
        {
            _ws = socket;
            _cancellationToken = _cancellationTokenSource.Token;
            locker = new SemaphoreSlim(1);
        }

        /// <summary>
        /// Creates a new instance.
        /// </summary>
        /// <param name="socket">The web socket.</param>
        /// <returns></returns>
        public static WebSocketWrapper Create(WebSocket socket)
        {
            return new WebSocketWrapper(socket);
        }

        public async Task DisconnectAsync()
        {
            this._cancellationTokenSource.Cancel();
            if (_ws.State == WebSocketState.Open)
            {
                try
                {
                    await _ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Close by client request", CancellationToken.None);
                }
                catch (WebSocketException)
                {
                }
            }
        }

        /// <summary>
        /// Set the Action to call when the connection has been terminated.
        /// </summary>
        /// <param name="onDisconnect">The Action to call</param>
        /// <returns></returns>
        public WebSocketWrapper OnDisconnect(Action<WebSocketWrapper> onDisconnect)
        {
            _onDisconnected = onDisconnect;
            return this;
        }

        /// <summary>
        /// Set the Action to call when a messages has been received.
        /// </summary>
        /// <param name="onMessage">The Action to call.</param>
        /// <returns></returns>
        public WebSocketWrapper OnMessage(Action<string, WebSocketWrapper> onMessage)
        {
            _onMessage = onMessage;
            return this;
        }

        /// <summary>
        /// Send a message to the WebSocket server.
        /// </summary>
        /// <param name="message">The message to send</param>
        public async Task SendMessageAsync(string message)
        {
            if (_ws.State != WebSocketState.Open)
            {
                throw new Exception("Connection is not open.");
            }

            Log.Trace(3, $"WebSocketWrapper.SendMessageAsync: " + message);

            var messageBuffer = Encoding.UTF8.GetBytes(message);
            var messagesCount = (int)Math.Ceiling((double)messageBuffer.Length / SendChunkSize);

            await locker.WaitAsync();
            try
            {
                for (var i = 0; i < messagesCount; i++)
                {
                    var offset = (SendChunkSize * i);
                    var count = SendChunkSize;
                    var lastMessage = ((i + 1) == messagesCount);

                    if ((count * (i + 1)) > messageBuffer.Length)
                    {
                        count = messageBuffer.Length - offset;
                    }

                    await _ws.SendAsync(new ArraySegment<byte>(messageBuffer, offset, count), WebSocketMessageType.Text, lastMessage, _cancellationToken);
                }
            }
            finally
            {
                locker.Release();
            }
        }

        public async Task<string> GetResponseAsync()
        {
            Log.Trace(3, "GetResponseAsync: Wait for new message");
            var allBytes = new List<byte>();
            WebSocketReceiveResult result;
            do
            {
                result = await _ws.ReceiveAsync(new ArraySegment<byte>(buffer), _cancellationToken);

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    await _ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Close from receive", CancellationToken.None);
                    CallOnDisconnected();
                }
                else
                {
                    for (int i = 0; i < result.Count; i++)
                    {
                        allBytes.Add(buffer[i]);
                    }
                }

            } while (!result.EndOfMessage);
            var message = Encoding.UTF8.GetString(allBytes.ToArray(), 0, allBytes.Count);
            Log.Trace(3, $"StartListen: received '{message}'");
            return message;
        }

        public void StartListening()
        {
            //CallOnConnected();
            RunInTask(() => StartListen());
        }

        private async void StartListen()
        {
            Log.Trace(3, "StartListen; Start of listening loop");
            var buffer = new byte[ReceiveChunkSize];

            try
            {
                while (_ws.State == WebSocketState.Open)
                {
                    var message = await this.GetResponseAsync();
                    if (message.Length > 0) CallOnMessage(message);
                }
            }
            catch (Exception)
            {
                CallOnDisconnected();
            }
            finally
            {
                _ws.Dispose();
            }
        }

        private void CallOnMessage(string message)
        {
            if (_onMessage != null)
                RunInTask(() => _onMessage(message, this));
        }

        private void CallOnDisconnected()
        {
            if (_onDisconnected != null)
                RunInTask(() => _onDisconnected(this));
        }

        private static void RunInTask(Action action)
        {
            Task.Factory.StartNew(action);
        }
    }

    public abstract class SocketCommunicationDetailsBase : CommunicationDetails
    {
        protected string baseUrl;
        protected string tableName;
        protected string teamName;
        protected Guid tableId;
        protected SemaphoreSlim responseReceived;

        public SocketCommunicationDetailsBase(string _baseUrl, string _tableName, string _teamName)
        {
            this.baseUrl = _baseUrl;
            this.tableName = _tableName;
            this.teamName = _teamName;
            this.responseReceived = new SemaphoreSlim(0);
        }

        protected async Task TakeSeat()
        {
            Log.Trace(0, $"{this.seat.ToString().PadRight(5)} sends 'Sit'");
            await this.SendCommandAsync("Sit", this.tableId, this.seat, this.teamName);
        }

        public override async Task WriteProtocolMessageToRemoteMachine(string message)
        {
            Log.Trace(0, $"{this.seat.ToString().PadRight(5)} sends '{message}'");
            await this.SendCommandAsync("SendProtocolMessage", tableId, this.seat, message);
        }

        public override async Task DisposeAsync()
        {
            Log.Trace(0, $"{this.seat.ToString().PadRight(5)} sends 'Unsit'");
            await this.SendCommandAsync("Unsit", tableId, this.seat);
            var response = await this.GetResponseAsync();
            if (response != "unseated") throw new InvalidOperationException($"Expected 'unseated'. Actual '{response}'");
            await this.DisposeConnectionAsync();
            Log.Trace(0, $"{this.seat.ToString().PadRight(5)} completed DisposeAsync");
        }

        public abstract Task SendCommandAsync(string commandName, params object[] args);

        public abstract Task DisposeConnectionAsync();
    }
}
