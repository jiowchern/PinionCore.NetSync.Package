using PinionCore.Network;
using PinionCore.Remote.Server.Web;
using System;
using System.Security.Cryptography;
using System.Text;

namespace PinionCore.NetSync.Web
{
    public class WebHandshark : IDisposable
    {
        private readonly Network.Tcp.Listener Listener_;
        private bool DisposedValue_;

        public WebHandshark(Network.Tcp.Listener listener)
        {
            Listener_ = listener;
            Listener_.AcceptEvent += _Accept;
        }
        event Action<WebPeer> _AcceptEvent;
        public event Action<WebPeer> AcceptEvent
        {
            add
            {
                _AcceptEvent += value;
            }
            remove
            {
                _AcceptEvent -= value;
            }
        }
        private string GenerateWebSocketAcceptKey(string secWebSocketKey)
        {

            string acceptKey = secWebSocketKey.Trim() + "258EAFA5-E914-47DA-95CA-C5AB0DC85B11";
            byte[] hash = SHA1.Create().ComputeHash(Encoding.UTF8.GetBytes(acceptKey));
            return Convert.ToBase64String(hash);
        }
        private string GetSecWebSocketKey(string header)
        {
            string keyHeader = "Sec-WebSocket-Key: ";
            int startIndex = header.IndexOf(keyHeader) + keyHeader.Length;
            if (startIndex == -1) return null;
            int endIndex = header.IndexOf("\r\n", startIndex);
            return header.Substring(startIndex, endIndex - startIndex);
        }

        private async void _Accept(Network.Tcp.Peer peer)
        {
            // handshark here
            IStreamable streamable = peer;
            var buffer = new byte[1024];
            var readed = await streamable.Receive(buffer, 0, buffer.Length);
            if (readed == 0)
            {
                return;
            }
            var request = System.Text.Encoding.UTF8.GetString(buffer, 0, readed);

            // check request
            if (!request.Contains("Upgrade: websocket"))
            {
                return;
            }

            string secWebSocketKey = GetSecWebSocketKey(request);


            string acceptKey = GenerateWebSocketAcceptKey(secWebSocketKey);

            string responseHeader =
                    "HTTP/1.1 101 Switching Protocols\r\n" +
                    "Connection: Upgrade\r\n" +
                    "Upgrade: websocket\r\n" +
                    $"Sec-WebSocket-Accept: {acceptKey}\r\n\r\n";
            var response_bytes = System.Text.Encoding.UTF8.GetBytes(responseHeader);
            int sended = 0;
            try
            {
                while (sended < response_bytes.Length)
                {
                    int count = await streamable.Send(response_bytes, sended, response_bytes.Length - sended);
                    if (count == 0)
                        return;
                    sended += count;
                }

            }
            catch (Exception)
            {

                return;
            }

            _AcceptEvent?.Invoke(new WebPeer(peer));
        }


        void IDisposable.Dispose()
        {
            Listener_.AcceptEvent -= _Accept;
        }
    }
}
