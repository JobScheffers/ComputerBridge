using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Bridge.Networking
{
    public class TableManagerTcpHost<T> : TableManagerHost<HostTcpCommunicationDetails<T>, T> where T : TcpClientData, new()
    {
        public TableManagerTcpHost(HostMode mode, HostTcpCommunicationDetails<T> communicationDetails, BridgeEventBus bus, string tournamentFileName) : base(mode, bus, communicationDetails, $"Host@{communicationDetails.Port}", tournamentFileName)
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
            await ValueTask.CompletedTask;
            this.listener.Stop();
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

        protected virtual void Handshake() {}

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
            if (!this.IsDisposed && this.stream.CanRead) this.stream.BeginRead(this.buffer, 0, this.client.ReceiveBufferSize, new AsyncCallback(ReadData), null);
        }

        private void ReadData(IAsyncResult result)
        {
            try
            {
                if (!this.IsDisposed)
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
            this.client.Dispose();
            await this.stream.DisposeAsync();
            await base.DisposeManagedObjects();
        }
    }
}
