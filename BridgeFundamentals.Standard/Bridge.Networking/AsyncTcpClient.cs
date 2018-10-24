using System;
using System.IO;
using System.Net.Sockets;
using System.Net.Security;
using System.Threading;
using System.Threading.Tasks;
using System.Text;

namespace Bridge.Networking
{
    public class AsyncTcpClient : IDisposable
    {
        private TcpClient tcpClient;
        private Stream stream;
        private bool disposed = false;
        private byte[] streamBuffer;        // buffer for raw async NetworkStream
        private string rawMessageBuffer;		// String to store the response ASCII representation.
        private object locker = new object();

        public bool IsReceiving { set; get; }

        //public event EventHandler<byte[]> OnDataReceived;
        public event EventHandler OnDisconnected;

        public bool IsConnected
        {
            get
            {
                return this.tcpClient != null && this.tcpClient.Connected;
            }
        }

        public AsyncTcpClient()
        {
            this.tcpClient = new TcpClient();
        }

        public AsyncTcpClient(TcpClient c)
        {
            this.tcpClient = c;
        }

        public async Task Connect(string host, int port, bool ssl = false, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                //await this.CloseAsync();
                cancellationToken.ThrowIfCancellationRequested();
                await this.tcpClient.ConnectAsync(host, port);
                await this.CloseIfCanceled(cancellationToken);
                // get stream and do SSL handshake if applicable

                this.stream = this.tcpClient.GetStream();
                await this.CloseIfCanceled(cancellationToken);
                if (ssl)
                {
                    var sslStream = new SslStream(this.stream);
                    await sslStream.AuthenticateAsClientAsync(host);
                    this.stream = sslStream;
                    await this.CloseIfCanceled(cancellationToken);
                }

                this.tcpClient.NoDelay = true;   // make sure that data is sent immediately to TM
                this.tcpClient.ReceiveTimeout = 30;
                this.streamBuffer = new Byte[this.tcpClient.ReceiveBufferSize];
                this.rawMessageBuffer = "";    // initialize the response buffer
            }
            catch (Exception)
            {
                this.CloseIfCanceled(cancellationToken).Wait();
                throw;
            }
        }

        public async Task Send(string data, CancellationToken token = default(CancellationToken))
        {
            await this.Send(Encoding.ASCII.GetBytes(data));
        }

        public async Task Send(byte[] data, CancellationToken token = default(CancellationToken))
        {
            try
            {
                await this.stream.WriteAsync(data, 0, data.Length, token);
                await this.stream.FlushAsync(token);
            }
            catch (IOException ex)
            {
                var onDisconnected = this.OnDisconnected;
                if (ex.InnerException != null && ex.InnerException is ObjectDisposedException)
                {
                    Console.WriteLine("innocuous ssl stream error");
                    // for SSL streams
                }
                else if (onDisconnected != null)
                {
                    onDisconnected(this, EventArgs.Empty);
                }
            }
        }

        public async Task<string> GetNextLine(CancellationToken token = default(CancellationToken))
        {
            var newCommand = string.Empty;
            try
            {
                if (!this.IsConnected || this.IsReceiving)
                    throw new InvalidOperationException();
                this.IsReceiving = true;
                while (this.IsConnected && newCommand.Length == 0)
                {
                    token.ThrowIfCancellationRequested();
                    int bytesRead = await this.stream.ReadAsync(this.streamBuffer, 0, this.streamBuffer.Length, token);
                    if (bytesRead > 0)
                    {
                        string newData = System.Text.Encoding.ASCII.GetString(this.streamBuffer, 0, bytesRead);
                        lock (this.locker)
                        {
                            this.rawMessageBuffer += newData;
                            int endOfLine = rawMessageBuffer.IndexOf("\r\n");
                            if (endOfLine >= 0)
                            {
                                newCommand = this.rawMessageBuffer.Substring(0, endOfLine);
                                this.rawMessageBuffer = this.rawMessageBuffer.Substring(endOfLine + 2);
                            }
                        }
                    }
                }

            }
            catch (ObjectDisposedException)
            {
                Console.WriteLine("ODE Exception in receive");
            }
            catch (IOException ex)
            {
                if (ex.InnerException != null && ex.InnerException is ObjectDisposedException)
                {
                    Console.WriteLine("innocuous ssl stream error");
                    // for SSL streams
                }
                this.OnDisconnected?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                if (ex.InnerException != null && ex.InnerException is ObjectDisposedException)
                {
                    Console.WriteLine("innocuous ssl stream error");
                    // for SSL streams
                }
                this.OnDisconnected?.Invoke(this, EventArgs.Empty);
            }
            finally
            {
                this.IsReceiving = false;
            }

            return newCommand;
        }

        public async Task CloseAsync()
        {
            await Task.Yield();
            this.Close();
        }

        private void Close()
        {
            if (this.tcpClient != null)
            {
                this.tcpClient.Dispose();
                this.tcpClient = null;
            }
            if (this.stream != null)
            {
                this.stream.Dispose();
                this.stream = null;
            }
        }

        private async Task CloseIfCanceled(CancellationToken token, Action onClosed = null)
        {
            if (token.IsCancellationRequested)
            {
                await this.CloseAsync();
                if (onClosed != null)
                    onClosed();
                token.ThrowIfCancellationRequested();
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (!disposed)
                {
                    this.Close();
                }
            }

            disposed = true;

            // If it is available, make the call to the
            // base class's Dispose(Boolean) method
            // base.Dispose(disposing);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}