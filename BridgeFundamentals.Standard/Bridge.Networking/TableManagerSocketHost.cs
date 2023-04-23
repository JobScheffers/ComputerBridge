using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Bridge.Networking
{


    //public class TableManagerSocketHost<T> : AsyncTableHost<HostSocketCommunication>
    //{
    //    public TableManagerSocketHost(HostMode mode, int port, BridgeEventBus bus, string hostName, string tournamentFileName) : base(mode, new(port, hostName), bus, hostName, tournamentFileName)
    //    {
    //    }
    //}

    public class HostSocketCommunication<T> : HostTcpCommunicationDetails<T> where T : TcpClientData, new()
    {
        public HostSocketCommunication(int port, string hostName) : base()
        {

        }
    }

    public class HostTcpCommunicationDetails<T> : HostCommunicationDetails<T> where T : TcpClientData, new()
    {
        private TcpListener listener;

        public int Port { get; set; }

        public override event HandleClientAccepted<T> OnClientAccepted;

        public override void Start()
        {

            this.listener = new TcpListener(IPAddress.Any, Port);

            // trick to prevent error in unittests "Only one usage of each socket address (protocol/network address/port) is normally permitted"
            // https://social.msdn.microsoft.com/Forums/en-US/e1cc5f98-5a85-4da7-863e-f4d5623d80a0/forcing-tcplisteneros-to-release-port?forum=netfxcompact
            this.listener.Server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, 1);

            this.listener.Start();
            this.listener.BeginAcceptTcpClient(new AsyncCallback(this.AcceptClient), null);
        }

        private void AcceptClient(IAsyncResult result)
        {
            if (!this.IsDisposed)
            {
                var newClient = new T();
                this.OnClientAccepted(newClient);
                newClient.AddTcpClient(this.listener.EndAcceptTcpClient(result));
                this.listener.BeginAcceptTcpClient(new AsyncCallback(this.AcceptClient), null);
            }
        }

        protected override async ValueTask DisposeManagedObjects()
        {
            await Task.CompletedTask;
            this.listener.Stop();
        }
    }


    public class TableManagerSocketHost<T> : TableManagerHost<HostSocketCommunication<T>, T> where T : SocketClientData, new()
    {
        public TableManagerSocketHost(HostMode mode, int port, BridgeEventBus bus) : base(mode, bus, new HostSocketCommunication<T>(port, ""), "")
        {
        }
    }

    public class SocketClientData : TcpClientData
    {
        private bool receivedClose;
        private bool sentClose;

        protected override void Handshake()
        {
            this.receivedClose = false;
            this.sentClose = false;
            WaitForData();

            stream.Read(buffer, 0, client.Available);
            string s = Encoding.UTF8.GetString(buffer);

            if (Regex.IsMatch(s, "^GET", RegexOptions.IgnoreCase))
            {
                Log.Trace(4, $"=====Handshaking from client=====\n{s}");

                // 1. Obtain the value of the "Sec-WebSocket-Key" request header without any leading or trailing whitespace
                // 2. Concatenate it with "258EAFA5-E914-47DA-95CA-C5AB0DC85B11" (a special GUID specified by RFC 6455)
                // 3. Compute SHA-1 and Base64 hash of the new value
                // 4. Write the hash back as the value of "Sec-WebSocket-Accept" response header in an HTTP response
                string swk = Regex.Match(s, "Sec-WebSocket-Key: (.*)").Groups[1].Value.Trim();
                string swka = swk + "258EAFA5-E914-47DA-95CA-C5AB0DC85B11";
                byte[] swkaSha1 = System.Security.Cryptography.SHA1.Create().ComputeHash(Encoding.UTF8.GetBytes(swka));
                string swkaSha1Base64 = Convert.ToBase64String(swkaSha1);

                // HTTP/1.1 defines the sequence CR LF as the end-of-line marker
                byte[] response = Encoding.UTF8.GetBytes(
                    "HTTP/1.1 101 Switching Protocols\r\n" +
                    "Connection: Upgrade\r\n" +
                    "Upgrade: websocket\r\n" +
                    "Sec-WebSocket-Accept: " + swkaSha1Base64 + "\r\n\r\n");

                stream.Write(response, 0, response.Length);

                this.WriteToDevice("connected");

                var message = WaitForMessage();
                this.WriteToDevice($"match {Guid.NewGuid()}");
            }
            else
            {
                throw new InvalidOperationException("expected socket handshake");
            }

            string WaitForMessage()
            {
                WaitForData();
                int bytesRead = client.Available;
                stream.Read(buffer, 0, bytesRead);
                return Buffer2String(bytesRead);
            }
        }

        private void WaitForData()
        {
            while (!stream.DataAvailable) Thread.Sleep(50);
            while (client.Available < 3) Thread.Sleep(50);
        }

        protected override void WriteToDevice(string message)
        {
            if (message.Length > 60) return;// throw new ArgumentException("message too long");
            while (message.Length > 0)
            {
                var splitNeeded = message.Length > 60;
                var sendNow = splitNeeded ? message.Substring(0, 60) : message;
                message = splitNeeded ? message.Substring(61) : "";
                var messageBuffer = Encoding.UTF8.GetBytes(sendNow);
                var buffer = new byte[messageBuffer.Length + 2];
                messageBuffer.CopyTo(buffer, 2);
                buffer[0] = 0;
                if (!splitNeeded) buffer[0] |= 0b10000000;    // complete message
                buffer[0] |= 0b00000001;    // text message
                buffer[1] = (byte)messageBuffer.Length;
                stream.Write(buffer, 0, buffer.Length);
            }
        }

        protected override string Buffer2String(int bytesRead)
        {
            bool fin = (buffer[0] & 0b10000000) != 0;
            if (!fin) throw new InvalidDataException("cannot handle multi-frame messages");

            bool mask = (buffer[1] & 0b10000000) != 0; // must be true, "All messages from the client to the server have this bit set"
            if (!mask) throw new InvalidDataException("mask bit not set");

            int opcode = buffer[0] & 0b00001111; // expecting 1 - text message
            if (opcode == 8)
            {
                this.receivedClose = true;
                this.SendClose();
                return "";    // close 
            }
            if (opcode == 10) return "";    // pong 
            if (opcode != 1) throw new InvalidDataException("only text messages");

            int msglen = buffer[1] - 128, // & 0111 1111
                offset = 2;

            if (msglen == 0) return "";

            byte[] decoded = new byte[msglen];
            byte[] masks = new byte[4] { buffer[offset], buffer[offset + 1], buffer[offset + 2], buffer[offset + 3] };
            offset += 4;

            for (int i = 0; i < msglen; ++i)
                decoded[i] = (byte)(buffer[offset + i] ^ masks[i % 4]);

            string text = Encoding.UTF8.GetString(decoded);
            Log.Trace(1, text);
            return text + "\r\n";
        }

        protected override async ValueTask DisposeManagedObjects()
        {
            this.SendClose();
            await base.DisposeManagedObjects();
        }

        private void SendClose()
        {
            if (!this.sentClose)
            {
                Log.Trace(4, $"SocketClientData: Send Close to {this.seat}");
                var buffer = new byte[2];
                buffer[0] |= 0b10000000;    // complete message
                buffer[0] |= 8;    // close
                buffer[1] = 0;
                stream.Write(buffer, 0, buffer.Length);
                this.sentClose = true;
            }

            if (!this.receivedClose)
            {
                //WaitForData();
                //int bytesRead = client.Available;
                //stream.Read(buffer, 0, bytesRead);
                //int opcode = buffer[0] & 0b00001111; // expecting 1 - text message
                //if (opcode != 8) throw new InvalidDataException("only close messages");
            }
        }
    }


    public class TcpClientData : ClientData
    {
        protected TcpClient client;
        protected NetworkStream stream;
        protected byte[] buffer;
        private string rawMessageBuffer;        // String to store the response ASCII representation.
        private object locker = new object();
        private const int defaultWaitTime = 10;
        private int pauseTime;
        protected bool stopped = false;

        public void AddTcpClient(TcpClient _client)
        {
            this.client = _client;
            /// When NoDelay is false, a TcpClient does not send a packet over the network until it has collected a significant amount of outgoing data.
            /// Because of the amount of overhead in a TCP segment, sending small amounts of data is inefficient.
            /// However, situations do exist where you need to send very small amounts of data or expect immediate responses from each packet you send.
            /// Your decision should weigh the relative importance of network efficiency versus application requirements.
            this.client.NoDelay = true;
            this.buffer = new Byte[this.client.ReceiveBufferSize];
            this.stream = this.client.GetStream();
            this.rawMessageBuffer = string.Empty;
            this.pauseTime = 10;
            this.Handshake();
            this.WaitForIncomingMessage();
        }

        protected virtual void Handshake() { }

        protected override void WriteToDevice(string message)
        {
            Log.Trace(2, $"TcpClientData.WriteToDevice {this.seat,5}: {message}");
            var data = Encoding.ASCII.GetBytes(message + "\r\n");
            this.stream.Write(data, 0, data.Length);
            this.stream.Flush();
            this.pauseTime = defaultWaitTime;
        }

        public override void Refuse(string reason, params object[] args)
        {
            base.Refuse(reason, args);
            this.stream.Close();
            this.client.Close();
        }

        private void WaitForIncomingMessage()
        {
            if (this.stream.CanRead && !this.stopped) this.stream.BeginRead(this.buffer, 0, this.client.ReceiveBufferSize, new AsyncCallback(ReadData), null);
        }

        private void ReadData(IAsyncResult result)
        {
            try
            {
                if (!this.stopped)
                {
                    int bytesRead = this.stream.EndRead(result);
                    if (bytesRead == 0)
                    {
                        // nothing to do
                        Log.Trace(5, "{1}: no data from {0}", this.seat, "Host");
                        Thread.Sleep(this.pauseTime);
                        if (this.pauseTime < 10000) this.pauseTime = (int)(1.2 * this.pauseTime);
                    }
                    else
                    {
                        this.pauseTime = defaultWaitTime;
                        var message = this.Buffer2String(bytesRead);

                        if (message.Length > 0)    // otherwise probably connection error
                        {
                            this.ProcessRawMessage(message);
                        }
                    }
                }
            }
            catch (IOException)
            {
                // might be a temporary connection error
            }
            //Log.Trace(3, "Host received {0}", message);

            this.WaitForIncomingMessage();        // be ready for the next message
        }

        protected virtual string Buffer2String(int bytesRead)
        {
            return Encoding.ASCII.GetString(this.buffer, 0, bytesRead);
        }

        private void ProcessRawMessage(string message)
        {
            lock (this.locker)
            {
                this.rawMessageBuffer += message;
                Log.Trace(3, $"Host {this.seat} rawMessageBuffer={this.rawMessageBuffer}");
                int endOfLine = this.rawMessageBuffer.IndexOf("\r\n");
                if (endOfLine >= 0)
                {
                    string newCommand = this.rawMessageBuffer.Substring(0, endOfLine);
                    this.rawMessageBuffer = this.rawMessageBuffer.Substring(endOfLine + 2);
                    Log.Trace(0, "{2} rcves {0} '{1}'", this.seat, newCommand, "Host");

                    this.ProcessIncomingMessage(newCommand);
                }
            }
        }

        protected override async ValueTask DisposeManagedObjects()
        {
            this.stopped = true;
            this.client.Dispose();
            await this.stream.DisposeAsync();
            await base.DisposeManagedObjects();
        }
    }
}
