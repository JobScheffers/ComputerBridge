﻿using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using Sodes.Bridge.Base;

namespace Sodes.Bridge.Networking
{
	public class TableManagerTcpHost : TableManagerHost
	{
		private List<TcpStuff> clients;
        private BridgeEventBus eventBus;

		protected class TcpStuff
		{
			public TcpListener listener;
			public TcpClient client;
			public NetworkStream stream;
			public byte[] buffer;
			public Seats seat;
			public bool seatTaken;
			public string rawMessageBuffer;		// String to store the response ASCII representation.
			public object locker = new object();
		}

		public TableManagerTcpHost(int port, BridgeEventBus bus) : base(bus)
		{
			this.clients = new List<TcpStuff>();
			var listener = new TcpListener(IPAddress.Any, port);
			listener.Start();
			listener.BeginAcceptTcpClient(new AsyncCallback(this.AcceptClient), listener);
            this.eventBus = bus;
		}

		private void AcceptClient(IAsyncResult result)
		{
			var newClient = new TcpStuff();
			this.clients.Add(newClient);
			newClient.seatTaken = false;
			newClient.listener = result.AsyncState as TcpListener;
			newClient.client = newClient.listener.EndAcceptTcpClient(result);
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
			int bytes2 = client.stream.EndRead(result);
			string message = System.Text.Encoding.ASCII.GetString(client.buffer, 0, bytes2);

			if (!client.seatTaken)
			{
				if (message.ToLowerInvariant().Contains("connecting") && message.ToLowerInvariant().Contains("using protocol version"))
				{
					var hand = message.Substring(message.IndexOf(" as ") + 4, 5).Trim().ToLowerInvariant();
					if (hand == "north" || hand == "east" || hand == "south" || hand == "west")
					{
                        client.seat = SeatsExtensions.FromXML(hand.Substring(0, 1).ToUpperInvariant());
						client.seatTaken = true;
					}
					else
					{
						TableManagerTcpHost.WriteData("Illegal hand specified", client);
					}
				}
				else
				{
					TableManagerTcpHost.WriteData("Expected 'Connecting ....'", client);
				}
			}

			this.ProcessRawMessage(message, client);
			this.WaitForIncomingMessage(client);
		}

		private void ProcessRawMessage(string message, TcpStuff client)
		{
			lock (client.locker)
			{
				client.rawMessageBuffer += message;
				int endOfLine = client.rawMessageBuffer.IndexOf("\r\n");
				if (endOfLine >= 0)
				{
					string newCommand = client.rawMessageBuffer.Substring(0, endOfLine);
					client.rawMessageBuffer = client.rawMessageBuffer.Substring(endOfLine + 2);
					this.ProcessIncomingMessage(newCommand, client.seat);
				}
			}
		}

		public override void WriteData(Seats seat, string message, params object[] args)
		{
			TableManagerTcpHost.WriteData(string.Format(message, args), FindClient(seat));
		}

		private static void WriteData(string message, TcpStuff client)
		{
			Byte[] data = System.Text.Encoding.ASCII.GetBytes(message + "\r\n");
			client.stream.Write(data, 0, data.Length);
		}

		private TcpStuff FindClient(Seats seat)
		{
			foreach (var item in this.clients)
			{
				if (item.seat == seat)
				{
					return item;
				}
			}

			throw new ArgumentOutOfRangeException("seat");
		}

		public override void Refuse(Seats seat, string reason, params object[] args)
		{
			var client = this.FindClient(seat);
			base.Refuse(seat, reason, args);
			//this.WriteData(reason, client.seat);
			client.stream.Close();
			client.client.Close();
		}

		//public override bool IsConnected()
		//{
		//  return this.clients.Count >= 1 && this.clients[0].stream.CanRead;
		//}
	}
}