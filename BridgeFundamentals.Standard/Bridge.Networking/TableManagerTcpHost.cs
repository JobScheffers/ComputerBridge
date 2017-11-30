﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Bridge.Networking
{
    public class TableManagerTcpHost : TableManagerHost
	{
		private List<TcpClientData> tcpclients;

        public TableManagerTcpHost(int port, BridgeEventBus bus) : base(bus, "Host@" + port)
		{
			this.tcpclients = new List<TcpClientData>();
			var listener = new TcpListener(IPAddress.Any, port);
			listener.Start();
			listener.BeginAcceptTcpClient(new AsyncCallback(this.AcceptClient), listener);
		}

		private void AcceptClient(IAsyncResult result)
		{
			var listener = result.AsyncState as TcpListener;
			var newClient = new TcpClientData(this, listener.EndAcceptTcpClient(result));
			this.tcpclients.Add(newClient);
			listener.BeginAcceptTcpClient(new AsyncCallback(this.AcceptClient), listener);
		}

		internal class TcpClientData : ClientData
		{
            public TcpClientData(TableManagerHost h, TcpClient t) : base(h)
            {
                this.client = t;
                this.client.NoDelay = true;
                this.buffer = new Byte[this.client.ReceiveBufferSize];
                this.stream = this.client.GetStream();
                this.rawMessageBuffer = string.Empty;
                this.WaitForIncomingMessage();
            }

            private TcpClient client;
            private NetworkStream stream;
            private byte[] buffer;
            private string rawMessageBuffer;		// String to store the response ASCII representation.
			private object locker = new object();

            protected override void WriteToDevice(string message)
            {
                var data = Encoding.ASCII.GetBytes(message + "\r\n");
                this.stream.Write(data, 0, data.Length);
                this.stream.Flush();
            }

            public override void Refuse(string reason, params object[] args)
            {
                base.Refuse(reason, args);
                this.stream.Close();
                this.client.Close();
            }

            private void WaitForIncomingMessage()
            {
                if (this.stream.CanRead) this.stream.BeginRead(this.buffer, 0, this.client.ReceiveBufferSize, new AsyncCallback(ReadData), null);
            }

            private void ReadData(IAsyncResult result)
            {
                string message = string.Empty;
                try
                {
                    int bytesRead = this.stream.EndRead(result);
                    message = System.Text.Encoding.ASCII.GetString(this.buffer, 0, bytesRead);
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
                        Log.Trace(0, "{2} rcves {0} '{1}'", this.seat, newCommand, this.host.Name);

                        if (this.seatTaken)
                        {
                            this.ProcessIncomingMessage(newCommand);
                        }
                        else
                        {
                            this.host.Seat(this, message);
                        }
                    }
                }
            }
        }
	}
}