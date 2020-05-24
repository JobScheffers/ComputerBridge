using System;
using System.IO;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Bridge.Networking
{
    [Obsolete("Switch to TableManagerClient<TcpCommunicationDetails>")]
    public class TableManagerTcpClient : TableManagerClient, IDisposable
    {
        private TcpClient client;
        private NetworkStream stream;
        private byte[] streamBuffer;        // buffer for raw async NetworkStream
        private string rawMessageBuffer;		// String to store the response ASCII representation.
        private object locker = new object();
        private string serverAddress;
        private int serverPort;
        private const int defaultWaitTime = 10;
        //private int pauseTime;

        public TableManagerTcpClient() : this(null) { }

        public TableManagerTcpClient(BridgeEventBus bus) : base(bus)
        {
        }

        public void Connect(Seats _seat, string serverName, int portNumber, int _maxTimePerBoard, int _maxTimePerCard, string teamName)
        {
            Log.Trace(2, "TableManagerTcpClient.Connect Open connection to {0}:{1}", serverName, portNumber);
            this.serverAddress = serverName;
            this.serverPort = portNumber;
            this.Connect();
            base.Connect(_seat, _maxTimePerBoard, _maxTimePerCard, teamName);
        }

        private void Connect()
        {
            int retries = 0;
            do
            {
                try
                {
                    Log.Trace(2, "TableManagerTcpClient.Connect Create TcpClient {0}:{1}", this.serverAddress, this.serverPort);
                    // Create a TcpClient.
                    this.client = new TcpClient(this.serverAddress, this.serverPort);
                }
                catch (SocketException x)
                {
                    if (x.SocketErrorCode == SocketError.ConnectionRefused)
                    {
                        Log.Trace(1, "{0}: Connection refused", this.Name);
                        retries++;
                        if (retries > 10) throw;
                    }
                    else
                    {
                        throw;
                    }
                }
            } while (client == null);

            /// When NoDelay is false, a TcpClient does not send a packet over the network until it has collected a significant amount of outgoing data.
            /// Because of the amount of overhead in a TCP segment, sending small amounts of data is inefficient.
            /// However, situations do exist where you need to send very small amounts of data or expect immediate responses from each packet you send.
            /// Your decision should weigh the relative importance of network efficiency versus application requirements.
            //this.client.NoDelay = false;   // make sure that data is sent immediately to TM
            this.client.NoDelay = true;   // see if this has an effect on the number of ghost messages
            this.client.ReceiveTimeout = 30;
            this.stream = client.GetStream();
            this.streamBuffer = new Byte[this.client.ReceiveBufferSize];
            this.rawMessageBuffer = "";    // initialize the response buffer

            this.WaitForTcpData();
        }

        protected override async Task WriteProtocolMessageToRemoteMachine(string message)
        {
            if (!this.client.Connected)
            {
                Log.Trace(1, "{0}: Connection lost", this.Name);
                this.Connect();
                Log.Trace(1, "{0}: After connect", this.Name);
            }

            //this.pauseTime = defaultWaitTime;
            Byte[] data = System.Text.Encoding.ASCII.GetBytes(message + "\r\n");    // newline is required for TableManager protocol
            try
            {
                await stream.WriteAsync(data, 0, data.Length);
                await stream.FlushAsync();      // Send the message to the connected TcpServer (without Flush the message will stay in the buffer) 
            }
            catch (IOException x)
            {
                Log.Trace(0, "Error '{0}'", x.Message);
            }
            catch (Exception x)
            {
                Log.Trace(0, "Error '{0}'", x.Message);
            }
            finally
            {
                Log.Trace(0, "TM {1} sends '{0}'", message, this.seat.ToString().PadRight(5));
            }
        }

        private void ProcessRawMessage()
        {
            string newCommand = "";
            lock (this.locker)
            {
                int endOfLine = rawMessageBuffer.IndexOf("\r\n");
                if (endOfLine >= 0)
                {
                    newCommand = this.rawMessageBuffer.Substring(0, endOfLine);
                    this.rawMessageBuffer = this.rawMessageBuffer.Substring(endOfLine + 2);
                }
            }

            if (newCommand.Length > 0)
            {
                Log.Trace(0, "TM {1} rcves '{0}'", newCommand, this.seat.ToString().PadRight(5));
                this.ProcessIncomingMessage(newCommand);
            }
        }

        private void WaitForTcpData()
        {
            // make sure no messages get lost; go wait for another message on the tcp line
            this.stream.BeginRead(this.streamBuffer, 0, this.client.ReceiveBufferSize, new AsyncCallback(this.ReadData), null);
        }

        private void ReadData(IAsyncResult result)
        {
            try
            {
                int bytes2 = 0;
                if (this.stream.CanRead) bytes2 = this.stream.EndRead(result);
                if (bytes2 == 0)
                {
                    // nothing to do
                    Log.Trace(5, "{0}: no data from host", this.seat.ToString().PadRight(5));
                    //Thread.Sleep(this.pauseTime);
                    //if (this.pauseTime < 10000) this.pauseTime = (int)(1.2 * this.pauseTime);
                }
                else
                {
                    string newData = System.Text.Encoding.ASCII.GetString(this.streamBuffer, 0, bytes2);
                    lock (this.locker)
                    {
                        this.rawMessageBuffer += newData;
                    }

                    this.ProcessRawMessage();
                    //this.pauseTime = defaultWaitTime;
                    this.WaitForTcpData();      // make sure no data will be lost
                }
            }
            catch (ObjectDisposedException)
            {
            }
        }

        protected override void Stop()
        {
            this.client.Close();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~TableManagerTcpClient()
        {
            Dispose(false);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                // free managed resources
                if (this.client != null)
                {
                    this.client.Close();
                    this.client = null;
                }
            }
        }
    }

    public class TcpCommunicationDetails : CommunicationDetails
    {
        private TcpClient client;
        private NetworkStream stream;
        private byte[] streamBuffer;        // buffer for raw async NetworkStream
        private string rawMessageBuffer;		// String to store the response ASCII representation.
        private object locker = new object();
        private string serverAddress;
        private int serverPort;
        private const int defaultWaitTime = 10;

        public TcpCommunicationDetails(string _serverAddress, int _serverPort)
        {
            this.serverAddress = _serverAddress;
            this.serverPort = _serverPort;
        }

        protected override async Task Connect()
        {
            await Task.CompletedTask;
            int retries = 0;
            do
            {
                try
                {
                    Log.Trace(2, "TableManagerTcpClient.Connect Create TcpClient {0}:{1}", this.serverAddress, this.serverPort);
                    // Create a TcpClient.
                    this.client = new TcpClient(this.serverAddress, this.serverPort);
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
            } while (client == null);

            /// When NoDelay is false, a TcpClient does not send a packet over the network until it has collected a significant amount of outgoing data.
            /// Because of the amount of overhead in a TCP segment, sending small amounts of data is inefficient.
            /// However, situations do exist where you need to send very small amounts of data or expect immediate responses from each packet you send.
            /// Your decision should weigh the relative importance of network efficiency versus application requirements.
            //this.client.NoDelay = false;   // make sure that data is sent immediately to TM
            this.client.NoDelay = true;   // see if this has an effect on the number of ghost messages
            this.client.ReceiveTimeout = 30;
            this.stream = client.GetStream();
            this.streamBuffer = new Byte[this.client.ReceiveBufferSize];
            this.rawMessageBuffer = "";    // initialize the response buffer

            this.WaitForTcpData();
        }

        private void WaitForTcpData()
        {
            // make sure no messages get lost; go wait for another message on the tcp line
            this.stream.BeginRead(this.streamBuffer, 0, this.client.ReceiveBufferSize, new AsyncCallback(this.ReadData), null);
        }

        private void ReadData(IAsyncResult result)
        {
            try
            {
                int bytes2 = 0;
                if (this.stream.CanRead) bytes2 = this.stream.EndRead(result);
                if (bytes2 == 0)
                {
                    // nothing to do
                    Log.Trace(5, "no data from host");
                    //Thread.Sleep(this.pauseTime);
                    //if (this.pauseTime < 10000) this.pauseTime = (int)(1.2 * this.pauseTime);
                }
                else
                {
                    string newData = System.Text.Encoding.ASCII.GetString(this.streamBuffer, 0, bytes2);
                    lock (this.locker)
                    {
                        this.rawMessageBuffer += newData;
                    }

                    this.ProcessRawMessage();
                    //this.pauseTime = defaultWaitTime;
                    this.WaitForTcpData();      // make sure no data will be lost
                }
            }
            catch (ObjectDisposedException)
            {
            }
        }

        private void ProcessRawMessage()
        {
            string newCommand = "";
            lock (this.locker)
            {
                int endOfLine = rawMessageBuffer.IndexOf("\r\n");
                if (endOfLine >= 0)
                {
                    newCommand = this.rawMessageBuffer.Substring(0, endOfLine);
                    this.rawMessageBuffer = this.rawMessageBuffer.Substring(endOfLine + 2);
                }
            }

            if (newCommand.Length > 0)
            {
                this.processMessage(newCommand);
            }
        }

        public override async Task WriteProtocolMessageToRemoteMachine(string message)
        {
            if (!this.client.Connected)
            {
                Log.Trace(1, "Connection lost");
                this.Connect();
                Log.Trace(1, "After connect");
            }

            //this.pauseTime = defaultWaitTime;
            Byte[] data = System.Text.Encoding.ASCII.GetBytes(message + "\r\n");    // newline is required for TableManager protocol
            try
            {
                await stream.WriteAsync(data, 0, data.Length);
                await stream.FlushAsync();      // Send the message to the connected TcpServer (without Flush the message will stay in the buffer) 
            }
            catch (IOException x)
            {
                Log.Trace(0, "Error '{0}'", x.Message);
            }
            catch (Exception x)
            {
                Log.Trace(0, "Error '{0}'", x.Message);
            }
            finally
            {
                Log.Trace(0, "TM sends '{0}'", message);
            }
        }

        public override void Dispose()
        {
            // free managed resources
            if (this.stream != null) stream.Dispose();
            if (this.client != null) this.client.Dispose();
        }
    }
}
