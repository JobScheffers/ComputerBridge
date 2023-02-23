using System;
using System.Threading.Tasks;
using System.Threading;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Bridge.Networking
{
    public class SocketCommunicationDetails : SocketCommunicationDetailsBase
    {
        // SignalR protocol description: https://github.com/aspnet/SignalR/blob/master/specs/HubProtocol.md

        private readonly ClientWebSocketWrapper client;
        private WebSocketWrapper socket;
        private bool disposing = false;
        private readonly string signalRSignature = "" + '\u001e';

        public SocketCommunicationDetails(string _baseUrl, string _tableName, string _teamName) : base(_tableName, _teamName)
        {
            this.client = ClientWebSocketWrapper.Create(_baseUrl);
        }

        public override async ValueTask SendCommandAsync(string commandName, params object[] args)
        {
            var command = new SignalRCommand { Type = 1, Target = commandName, Arguments = args };
            await this.SendSignalRCommandAsync(command);
        }

        private async ValueTask SendSignalRCommandAsync(SignalRCommand command)
        {
            var jsonCommand = JsonSerializer.Serialize(command, new JsonSerializerOptions { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull });
            await this.SendJsonCommandAsync(jsonCommand);
        }

        private async ValueTask SendJsonCommandAsync(string jsonCommand)
        {
            jsonCommand += signalRSignature;    // SignalR requirement
            await this.socket.SendMessageAsync(jsonCommand);
        }

        private async ValueTask<object[]> WaitForCommandAsync(string commandName)
        {
            var response = await this.socket.GetResponseAsync(CancellationToken.None);
            var command = ParseResponse(response.Substring(0, response.Length - 1));    // remove SignalR EOM
            if (command.Item1 != commandName) throw new InvalidOperationException($"Received command '{command.Item1}', expected '{commandName}'");
            return command.Item2;
        }

        private (string, object[]) ParseResponse(string response)
        {
            // {"type":1,"target":"ReceiveTableId","arguments":["11ea9f16-17e5-4138-a1b8-db7cf18272d7"]}
            var command = JsonSerializer.Deserialize<SignalRCommand>(response);
            if (command.Type != 1) throw new InvalidOperationException("");
            return (command.Target, command.Arguments);
        }

        protected override async ValueTask Connect()
        {
            await this.client.ConnectAsync();
            this.socket = this.client.wsw;
            this.socket.OnMessage(OnMessage);
            this.socket.OnDisconnect(OnDisconnect);

            await this.SendJsonCommandAsync(@"{""protocol"":""json"", ""version"":1}");     // SignalR handshake
            var response = await this.socket.GetResponseAsync(CancellationToken.None);
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
                switch (command.Type)
                {
                    case 1:
                        switch (command.Target)
                        {
                            case "ReceiveProtocolMessage":
                                var protocolMessage = command.Arguments[0].ToString();
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
                        throw new ArgumentOutOfRangeException("command.type", $"'{command.Type}' is an unknown command type");
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

        protected override async ValueTask DisposeManagedObjects()
        {
            //var command = new SignalRCommand { type = 7 };
            //await this.SendSignalRCommandAsync(command);
            await this.client.DisconnectAsync();
            await base.DisposeManagedObjects();
        }

        public override async ValueTask<string> GetResponseAsync()
        {
            throw new NotImplementedException();
        }

        private class SignalRCommand
        {
            // {"type":1,"target":"ReceiveTableId","arguments":["11ea9f16-17e5-4138-a1b8-db7cf18272d7"]}
            public int Type { get; set; }
            public string Target { get; set; }
            public object[] Arguments { get; set; }
            public string Error { get; set; }
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
        private CancellationTokenSource _cancellationTokenSource;
        private CancellationToken _cancellationToken;

        private Action<string, WebSocketWrapper> _onMessage;
        private Action<WebSocketWrapper> _onDisconnected;
        private readonly byte[] buffer = new byte[ReceiveChunkSize];
        private readonly SemaphoreSlim locker;
        private bool continueListening;

        public WebSocketWrapper(WebSocket socket)
        {
            _ws = socket;
            locker = new SemaphoreSlim(1);
            this.continueListening = false;
            this.InitCancellation();
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
            Log.Trace(5, "WebSocketWrapper.DisconnectAsync");
            try
            {
                this._cancellationTokenSource.Cancel();
                if (_ws.State == WebSocketState.Open)
                {
                    try
                    {
                        await _ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Close by client request", CancellationToken.None);
                        Log.Trace(5, "WebSocketWrapper.DisconnectAsync socket has been closed");
                    }
                    catch (WebSocketException)
                    {
                    }
                }
            }
            finally
            {
                _ws.Dispose();
                _cancellationTokenSource.Dispose();
            }
        }

        private void InitCancellation()
        {
            Log.Trace(5, "WebSocketWrapper.InitCancellation");
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = new CancellationTokenSource();
            _cancellationToken = _cancellationTokenSource.Token;
            Log.Trace(5, $"WebSocketWrapper.InitCancellation IsCancellationRequested={_cancellationToken.IsCancellationRequested}");
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

            Log.Trace(4, $"WebSocketWrapper.SendMessageAsync: {message}");

            var messageBuffer = Encoding.UTF8.GetBytes(message);
            var messagesCount = (int)Math.Ceiling((double)messageBuffer.Length / SendChunkSize);

            await locker.WaitAsync();       // make sure only 1 message is being send at the same time
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

        public bool CanWrite => _ws.State == WebSocketState.Open;

        public async Task<string> GetResponseAsync(CancellationToken cancelToken)
        {
            var message = "";
            var allBytes = new List<byte>();
            WebSocketReceiveResult result;
            do
            {
                try
                {
                    do
                    {
                        Log.Trace(5, "GetResponseAsync: Wait for new message");

                        result = await _ws.ReceiveAsync(new ArraySegment<byte>(buffer), cancelToken);

                        if (result.MessageType == WebSocketMessageType.Close)
                        {
                            this.StopListening();
                            await _ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Close from receive", CancellationToken.None);
                            Log.Debug("GetResponseAsync: socket has been closed");
                            CallOnDisconnected();
                        }
                        else
                        {
                            for (int i = 0; i < result.Count; i++)
                            {
                                allBytes.Add(buffer[i]);
                            }
                        }

                    } while (!result.EndOfMessage && !cancelToken.IsCancellationRequested);
                    message = Encoding.UTF8.GetString(allBytes.ToArray(), 0, allBytes.Count);
                    Log.Trace(4, $"GetResponseAsync: message='{message}' cancellation={cancelToken.IsCancellationRequested}");
                }
                catch (OperationCanceledException)
                {
                    Log.Debug($"GetResponseAsync: OperationCanceledException cancellation={cancelToken.IsCancellationRequested}");
                }
            } while (message.Length == 0 && !cancelToken.IsCancellationRequested 
                        //&& result.MessageType != WebSocketMessageType.Close
                    );
            Log.Trace(5, $"GetResponseAsync: received '{message}'");
            return message;
        }

        public void StartListening()
        {
            //CallOnConnected();
            if (this.continueListening) throw new InvalidOperationException("already listening");
            this.continueListening = true;
            RunInTask(() => Listen());

            async void Listen()
            {
                Log.Trace(5, "Start of listening loop");
                while (this.continueListening && _ws.State == WebSocketState.Open)
                {
                    try
                    {
                        var message = await this.GetResponseAsync(this._cancellationToken);
                        if (message.Length > 0) CallOnMessage(message);
                    }
                    catch (ObjectDisposedException)
                    {
                    }
                }

                Log.Trace(5, "End of listening loop");
            }
        }

        public void StopListening()
        {
            Log.Trace(5, $"StopListening");
            this.continueListening = false;
            this._cancellationTokenSource.Cancel();
            this.InitCancellation();     // allow one last GetResponseAsync after listening has stopped (unsit answer-response)
            Log.Trace(5, $"StopListening done");
        }

        public void CallOnMessage(string message)
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
        protected string tableName;
        protected string teamName;
        protected Guid tableId;
        protected SemaphoreSlim responseReceived;

        public SocketCommunicationDetailsBase(string _tableName, string _teamName)
        {
            this.tableName = _tableName;
            this.teamName = _teamName;
            this.responseReceived = new SemaphoreSlim(0);
        }

        protected async ValueTask TakeSeat()
        {
            Log.Trace(0, $"{this.seat.ToString().PadRight(5)} sends 'Sit'");
            await this.SendCommandAsync("Sit", this.tableId, this.seat, this.teamName);
        }

        public override async ValueTask WriteProtocolMessageToRemoteMachine(string message)
        {
            Log.Trace(0, $"{this.seat.ToString().PadRight(5)} sends '{message}'");
            await this.SendCommandAsync("SendProtocolMessage", tableId, this.seat, message);
        }

        protected override async ValueTask DisposeManagedObjects()
        {
            Log.Trace(0, $"{this.seat.ToString().PadRight(5)} sends 'Unsit'");
            await this.SendCommandAsync("Unsit", tableId, this.seat);
            var response = await this.GetResponseAsync();
            if (response != "unseated") throw new InvalidOperationException($"Expected 'unseated'. Actual '{response}'");
            this.responseReceived.Dispose();
            Log.Trace(0, $"{this.seat.ToString().PadRight(5)} completed DisposeAsync");
        }

        public abstract ValueTask SendCommandAsync(string commandName, params object[] args);
    }
}
