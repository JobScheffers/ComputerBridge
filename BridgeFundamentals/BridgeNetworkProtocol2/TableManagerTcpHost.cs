using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using Sodes.Bridge.Base;
using Sodes.Base;
using System.IO;

namespace Sodes.Bridge.Networking
{
	public class TableManagerTcpHost : TableManagerHost
	{
		private List<TcpStuff> tcpclients;
        private BridgeEventBus eventBus;

		internal class TcpStuff : ClientData
		{
            public TcpStuff(TableManagerHost h) : base(h) { }

			public TcpListener listener;
			public TcpClient client;
			public NetworkStream stream;
			public byte[] buffer;
			public string rawMessageBuffer;		// String to store the response ASCII representation.
			public object locker = new object();

            protected override void WriteData2(string message)
            {
                Byte[] data = System.Text.Encoding.ASCII.GetBytes(message + "\r\n");
                this.stream.Write(data, 0, data.Length);
                this.stream.Flush();
            }

            public override void Refuse(string reason, params object[] args)
            {
                base.Refuse(reason, args);
                this.stream.Close();
                this.client.Close();
            }

            public void ProcessRawMessage(string message)
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
                        this.ProcessIncomingMessage(newCommand);
                    }
                }
            }
        }

        public TableManagerTcpHost(int port, BridgeEventBus bus) : base(bus, "Host@" + port)
		{
            this.eventBus = bus;
			this.tcpclients = new List<TcpStuff>();
			var listener = new TcpListener(IPAddress.Any, port);
			listener.Start();
			listener.BeginAcceptTcpClient(new AsyncCallback(this.AcceptClient), listener);
		}

		private void AcceptClient(IAsyncResult result)
		{
			var newClient = new TcpStuff(this);
			this.tcpclients.Add(newClient);
			newClient.seatTaken = false;
			newClient.listener = result.AsyncState as TcpListener;
			newClient.client = newClient.listener.EndAcceptTcpClient(result);
            newClient.client.NoDelay = true;
			newClient.buffer = new Byte[newClient.client.ReceiveBufferSize];
			newClient.rawMessageBuffer = string.Empty;
			newClient.stream = newClient.client.GetStream();
			this.WaitForIncomingMessage(newClient);
			newClient.listener.BeginAcceptTcpClient(new AsyncCallback(this.AcceptClient), newClient.listener);
		}

		private void WaitForIncomingMessage(TcpStuff client)
		{
			client.stream.BeginRead(client.buffer, 0, client.client.ReceiveBufferSize, new AsyncCallback(ReadData), client);
		}

		private void ReadData(IAsyncResult result)
		{
			var client = result.AsyncState as TcpStuff;
            string message = string.Empty;
            try
            {
                int bytes2 = client.stream.EndRead(result);
                message = System.Text.Encoding.ASCII.GetString(client.buffer, 0, bytes2);
            }
            catch (IOException)
            {
            }
            //Log.Trace(3, "Host received {0}", message);

            this.WaitForIncomingMessage(client);        // be ready for the next message

            if (message.Length == 0)    // probably connection error
            {
                //Log.Trace(4, "TestHost.ReadData: empty message");
                return;
            }

            if (client.seatTaken)
            {
                client.ProcessRawMessage(message);
            }
            else
            {
                this.Seat(client, message);
            }
		}
	}
}
