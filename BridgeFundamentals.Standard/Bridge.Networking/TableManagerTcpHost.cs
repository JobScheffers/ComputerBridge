using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace Bridge.Networking
{
    public class TableManagerTcpHost<T> : TableManagerHost<T> where T : TcpClientData, new()
    {
        private TcpListener listener;
        private bool stopped;

        public TableManagerTcpHost(int port, BridgeEventBus bus) : base(bus, "Host@" + port)
		{
            this.stopped = false;
			this.listener = new TcpListener(IPAddress.Any, port);

            // trick to prevent error in unittests "Only one usage of each socket address (protocol/network address/port) is normally permitted"
            // https://social.msdn.microsoft.com/Forums/en-US/e1cc5f98-5a85-4da7-863e-f4d5623d80a0/forcing-tcplisteneros-to-release-port?forum=netfxcompact
            this.listener.Server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, 1);

            this.listener.Start();
            this.listener.BeginAcceptTcpClient(new AsyncCallback(this.AcceptClient), null);
		}

        private void AcceptClient(IAsyncResult result)
        {
            if (!this.stopped)
            {
                var newClient = new T();
                this.AddUnseated(newClient);
                newClient.AddTcpClient(this.listener.EndAcceptTcpClient(result));
                this.listener.BeginAcceptTcpClient(new AsyncCallback(this.AcceptClient), null);
            }
        }

        protected override void Stop()
        {
            this.stopped = true;
            this.listener.Stop();
            base.Stop();
        }
    }

    public class TcpClientData : ClientData
    {
        private TcpClient client;
        private NetworkStream stream;
        private byte[] buffer;
        private string rawMessageBuffer;        // String to store the response ASCII representation.
        private object locker = new object();
        private const int defaultWaitTime = 10;
        private int pauseTime;
        private bool stopped = false;

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
            this.WaitForIncomingMessage();
        }

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
            string message = string.Empty;
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
                        message = System.Text.Encoding.ASCII.GetString(this.buffer, 0, bytesRead);
                        this.pauseTime = defaultWaitTime;
                    }
                }
            }
            catch (IOException)
            {
                // might be a temporary connection error
            }
            //Log.Trace(3, "Host received {0}", message);

            if (message.Length > 0)    // otherwise probably connection error
            {
                this.ProcessRawMessage(message);
            }

            this.WaitForIncomingMessage();        // be ready for the next message
        }

        private void ProcessRawMessage(string message)
        {
            lock (this.locker)
            {
                this.rawMessageBuffer += message;
                //Log.Trace("Host {0} messagebuffer={1}", client.seat, client.rawMessageBuffer);
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

        protected override void Dispose(bool disposing)
        {
            this.stopped = true;
            this.client.Dispose();
            this.stream.Dispose();
            base.Dispose(disposing);
        }
    }
}
