﻿using Bridge.NonBridgeHelpers;
using System;
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Bridge.Networking
{
    //public class TcpCommunicationDetails : CommunicationDetails
    //{
    //    private TcpClient client;
    //    private NetworkStream stream;
    //    private byte[] streamBuffer;        // buffer for raw async NetworkStream
    //    private string rawMessageBuffer;		// String to store the response ASCII representation.
    //    private readonly object locker = new object();
    //    private readonly string serverAddress;
    //    private readonly int serverPort;
    //    //private const int defaultWaitTime = 10;

    //    public TcpCommunicationDetails(string _serverAddress, int _serverPort)
    //    {
    //        this.serverAddress = _serverAddress;
    //        this.serverPort = _serverPort;
    //    }

    //    protected override async ValueTask Connect()
    //    {
    //        await Task.CompletedTask;
    //        int retries = 0;
    //        do
    //        {
    //            try
    //            {
    //                Log.Trace(2, "TableManagerTcpClient.Connect Create TcpClient {0}:{1}", this.serverAddress, this.serverPort);
    //                // Create a TcpClient.
    //                this.client = new TcpClient(this.serverAddress, this.serverPort);
    //            }
    //            catch (SocketException x)
    //            {
    //                if (x.SocketErrorCode == SocketError.ConnectionRefused)
    //                {
    //                    Log.Trace(1, "Connection refused");
    //                    retries++;
    //                    if (retries > 10) throw;
    //                }
    //                else
    //                {
    //                    throw;
    //                }
    //            }
    //        } while (client == null);

    //        /// When NoDelay is false, a TcpClient does not send a packet over the network until it has collected a significant amount of outgoing data.
    //        /// Because of the amount of overhead in a TCP segment, sending small amounts of data is inefficient.
    //        /// However, situations do exist where you need to send very small amounts of data or expect immediate responses from each packet you send.
    //        /// Your decision should weigh the relative importance of network efficiency versus application requirements.
    //        //this.client.NoDelay = false;   // make sure that data is sent immediately to TM
    //        this.client.NoDelay = true;   // see if this has an effect on the number of ghost messages
    //        this.client.ReceiveTimeout = 30;
    //        this.stream = client.GetStream();
    //        this.streamBuffer = new Byte[this.client.ReceiveBufferSize];
    //        this.rawMessageBuffer = "";    // initialize the response buffer

    //        this.WaitForTcpData();
    //    }

    //    private void WaitForTcpData()
    //    {
    //        // make sure no messages get lost; go wait for another message on the tcp line
    //        Log.Trace(9, "WaitForTcpData");
    //        this.stream.BeginRead(this.streamBuffer, 0, this.client.ReceiveBufferSize, new AsyncCallback(this.ReadData), null);
    //    }

    //    private void ReadData(IAsyncResult result)
    //    {
    //        try
    //        {
    //            int bytes2 = 0;
    //            if (this.stream.CanRead) bytes2 = this.stream.EndRead(result);
    //            if (bytes2 == 0)
    //            {
    //                // nothing to do
    //                Log.Trace(5, "no data from host");
    //                //Thread.Sleep(this.pauseTime);
    //                //if (this.pauseTime < 10000) this.pauseTime = (int)(1.2 * this.pauseTime);
    //                //if (!this.client.Connected)
    //                {
    //                    this.stream.Close();
    //                    _ = this.Connect();
    //                }
    //            }
    //            else
    //            {
    //                string newData = System.Text.Encoding.ASCII.GetString(this.streamBuffer, 0, bytes2);
    //                lock (this.locker)
    //                {
    //                    this.rawMessageBuffer += newData;
    //                }

    //                this.ProcessRawMessage();
    //                //this.pauseTime = defaultWaitTime;
    //                this.WaitForTcpData();      // make sure no data will be lost
    //            }
    //        }
    //        catch (ObjectDisposedException)
    //        {
    //        }
    //        catch (AggregateException x) when (x.InnerException is ObjectDisposedException)
    //        {
    //        }
    //        catch (SocketException)
    //        {
    //            this.stream.Close();
    //            _ = this.Connect();
    //        }
    //    }

    //    private void ProcessRawMessage()
    //    {
    //        string newCommand = "";
    //        lock (this.locker)
    //        {
    //            int endOfLine = rawMessageBuffer.IndexOf("\r\n");
    //            if (endOfLine >= 0)
    //            {
    //                newCommand = this.rawMessageBuffer.Substring(0, endOfLine);
    //                this.rawMessageBuffer = this.rawMessageBuffer.Substring(endOfLine + 2);
    //            }
    //        }

    //        if (newCommand.Length > 0)
    //        {
    //            this.processMessage(newCommand);
    //        }
    //    }

    //    public override async ValueTask WriteProtocolMessageToRemoteMachine(string message)
    //    {
    //        if (!this.client.Connected)
    //        {
    //            Log.Trace(1, "Connection lost");
    //            await this.Connect();
    //            Log.Trace(1, "After connect");
    //        }

    //        //this.pauseTime = defaultWaitTime;
    //        Byte[] data = System.Text.Encoding.ASCII.GetBytes(message + "\r\n");    // newline is required for TableManager protocol
    //        try
    //        {
    //            await stream.WriteAsync(data, 0, data.Length);
    //            await stream.FlushAsync();      // Send the message to the connected TcpServer (without Flush the message will stay in the buffer) 
    //        }
    //        catch (IOException x)
    //        {
    //            Log.Trace(0, "Error '{0}'", x.Message);
    //        }
    //        catch (Exception x)
    //        {
    //            Log.Trace(0, "Error '{0}'", x.Message);
    //        }
    //        finally
    //        {
    //            Log.Trace(0, "TM sends '{0}'", message);
    //        }
    //    }

    //    protected override async ValueTask DisposeManagedObjects()
    //    {
    //        // free managed resources
    //        if (this.stream is not null) await this.stream.DisposeAsync();
    //        if (this.client is not null) this.client.Dispose();
    //    }

    //    public override ValueTask<string> GetResponseAsync()
    //    {
    //        throw new NotImplementedException();
    //    }

    //    protected void Close()
    //    {
    //        this.stream.Close();
    //    }
    //}

    public class TcpCommunicationDetails : CommunicationDetails
    {
        private readonly MyTcpClient client;
        private readonly object locker = new object();
        private readonly string serverAddress;
        private readonly int serverPort;
        private ValueTask clientRunTask;

        public TcpCommunicationDetails(string _serverAddress, int _serverPort)
        {
            this.serverAddress = _serverAddress;
            this.serverPort = _serverPort;
            this.client = new MyTcpClient("client");
        }

        protected override async ValueTask Connect()
        {
            this.clientRunTask = ValueTask.CompletedTask;
            this.client.SetMessageProcessor(this.processMessage);
            int retries = 0;
            do
            {
                try
                {
                    Log.Trace(2, "TableManagerTcpClient.Connect Create TcpClient {0}:{1}", this.serverAddress, this.serverPort);
                    // Create a TcpClient.
                    await this.client.Connect(this.serverAddress, this.serverPort);
                    this.clientRunTask = this.client.Run();
                }
                catch (SocketException x)
                {
                    if (x.SocketErrorCode == SocketError.ConnectionRefused)
                    {
                        Log.Trace(1, "Connection refused");
                        retries++;
                        if (retries > 10) throw;
                    }
                    else
                    {
                        throw;
                    }
                }
            } while (this.clientRunTask == ValueTask.CompletedTask);

            /// When NoDelay is false, a TcpClient does not send a packet over the network until it has collected a significant amount of outgoing data.
            /// Because of the amount of overhead in a TCP segment, sending small amounts of data is inefficient.
            /// However, situations do exist where you need to send very small amounts of data or expect immediate responses from each packet you send.
            /// Your decision should weigh the relative importance of network efficiency versus application requirements.
            //this.client.NoDelay = false;   // make sure that data is sent immediately to TM
            //this.client.NoDelay = true;   // see if this has an effect on the number of ghost messages
            //this.client.ReceiveTimeout = 30;

            //this.WaitForTcpData();
        }

        //private void WaitForTcpData()
        //{
        //    // make sure no messages get lost; go wait for another message on the tcp line
        //    Log.Trace(9, "WaitForTcpData");
        //    this.stream.BeginRead(this.streamBuffer, 0, this.client.ReceiveBufferSize, new AsyncCallback(this.ReadData), null);
        //}

        //private void ReadData(IAsyncResult result)
        //{
        //    try
        //    {
        //        int bytes2 = 0;
        //        if (this.stream.CanRead) bytes2 = this.stream.EndRead(result);
        //        if (bytes2 == 0)
        //        {
        //            // nothing to do
        //            Log.Trace(5, "no data from host");
        //            //Thread.Sleep(this.pauseTime);
        //            //if (this.pauseTime < 10000) this.pauseTime = (int)(1.2 * this.pauseTime);
        //            //if (!this.client.Connected)
        //            {
        //                this.stream.Close();
        //                _ = this.Connect();
        //            }
        //        }
        //        else
        //        {
        //            string newData = System.Text.Encoding.ASCII.GetString(this.streamBuffer, 0, bytes2);
        //            lock (this.locker)
        //            {
        //                this.rawMessageBuffer += newData;
        //            }

        //            this.ProcessRawMessage();
        //            //this.pauseTime = defaultWaitTime;
        //            this.WaitForTcpData();      // make sure no data will be lost
        //        }
        //    }
        //    catch (ObjectDisposedException)
        //    {
        //    }
        //    catch (AggregateException x) when (x.InnerException is ObjectDisposedException)
        //    {
        //    }
        //    catch (SocketException)
        //    {
        //        this.stream.Close();
        //        _ = this.Connect();
        //    }
        //}

        //private void ProcessRawMessage()
        //{
        //    string newCommand = "";
        //    lock (this.locker)
        //    {
        //        int endOfLine = rawMessageBuffer.IndexOf("\r\n");
        //        if (endOfLine >= 0)
        //        {
        //            newCommand = this.rawMessageBuffer.Substring(0, endOfLine);
        //            this.rawMessageBuffer = this.rawMessageBuffer.Substring(endOfLine + 2);
        //        }
        //    }

        //    if (newCommand.Length > 0)
        //    {
        //        this.processMessage(newCommand);
        //    }
        //}

        public override async ValueTask WriteProtocolMessageToRemoteMachine(string message)
        {
            await this.client.Write(message);
        }

        protected override async ValueTask DisposeManagedObjects()
        {
            // free managed resources
            if (this.client is not null) await this.client.DisposeAsync();
        }

        public override ValueTask<string> GetResponseAsync()
        {
            throw new NotImplementedException();
        }

        //protected void Close()
        //{
        //}
    }

    public class MyTcpClient : BaseAsyncTcpClient
    {
        private Action<string> processMessage;

        public MyTcpClient(string _name) : base(_name) 
        {
        }

        public void SetMessageProcessor(Action<string> _processMessage)
        {
            this.processMessage = _processMessage;
        }

        protected override void ProcessMessage(string message)
        {
            this.processMessage(message);
        }
    }

    public abstract class BaseAsyncTcpClient : BaseAsyncDisposable
    {
        private readonly TcpClient client;
        private readonly string name;
        private NetworkStream stream;
        private StreamWriter w;
        private bool isRunning = false;

        public BaseAsyncTcpClient(string _name)
        {
            this.name = _name;
            this.client = new();
        }

        public BaseAsyncTcpClient(string _name, TcpClient client)
        {
            this.name = _name;
            this.client = client;
            this.AfterConnect();
        }

        protected override async ValueTask DisposeManagedObjects()
        {
            Log.Trace(2, $"{this.name} dispose begin");
            this.isRunning = false;
            await ValueTask.CompletedTask;
            this.client.Dispose();
            this.stream.Dispose();
            this.w.Dispose();
        }

        public async ValueTask Connect(string address, int port)
        {
            Log.Trace(2, $"{this.name} Connect begin");
            await this.client.ConnectAsync(address, port);
            this.AfterConnect();
        }

        public void Stop()
        {
            this.isRunning = false;
        }

        private void AfterConnect()
        {
            this.stream = client.GetStream();
            this.w = new StreamWriter(this.stream);
        }

        public async ValueTask Run()
        {
            Log.Trace(5, $"AsyncClient.Run {this.name} begin");
            this.isRunning = true;
            var r = new StreamReader(this.stream);
            while (this.isRunning)
            {
                var message = await r.ReadLineAsync();
                if (!string.IsNullOrWhiteSpace(message))
                {
                    Log.Trace(6, $"{this.name} receives '{message}' (isRunning={this.isRunning})");
                    this.ProcessMessage(message);
                }
            }
            Log.Trace(5, $"AsyncClient.Run {this.name} end");
        }

        protected abstract void ProcessMessage(string message);

        public async ValueTask Write(string message)
        {
            Log.Trace(6, $"{this.name} writes '{message}'");
            await this.w.WriteLineAsync(message);
            await this.w.FlushAsync();
        }
    }
}