using System.Net.WebSockets;
using System.Text;

namespace ServiceA
{
    public class WebSocketClient : IDisposable
    {
        private readonly ClientWebSocket _webSocket;

        public WebSocketClient()
        {
            _webSocket = new ClientWebSocket();
        }

        public async Task ConnectAsync(string uri)
        {
            if (_webSocket.State != WebSocketState.Open)
            {
                await _webSocket.ConnectAsync(new Uri(uri), CancellationToken.None);
            }
        }

        public async Task<string> SendMessageAsync(string message)
        {
            if (_webSocket.State != WebSocketState.Open)
            {
                throw new InvalidOperationException("WebSocket is not connected.");
            }

            var requestBytes = Encoding.UTF8.GetBytes(message);
            await _webSocket.SendAsync(new ArraySegment<byte>(requestBytes), WebSocketMessageType.Text, true, CancellationToken.None);

            // Receive response
            var buffer = new byte[1024 * 4];
            var result = await _webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

            return Encoding.UTF8.GetString(buffer, 0, result.Count);
        }

        public void Dispose()
        {
            _webSocket.Dispose();
        }
    }
  
}
