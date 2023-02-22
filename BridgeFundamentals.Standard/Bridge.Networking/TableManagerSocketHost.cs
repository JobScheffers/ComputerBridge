using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace Bridge.Networking
{
    public class TableManagerSocketHost<T> : TableManagerHost<HostTcpCommunicationDetails<T>, T> where T : SocketClientData, new()
    {
        public TableManagerSocketHost(HostMode mode, int port, BridgeEventBus bus) : base(mode, bus, new HostTcpCommunicationDetails<T> { Port = port }, "")
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

        protected override void DisposeManagedObjects()
        {
            this.SendClose();
            base.DisposeManagedObjects();
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
}
