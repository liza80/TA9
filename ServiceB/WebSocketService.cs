using ServiceB.Models;
using System.Net.WebSockets;
using System.Text;
using Newtonsoft.Json;


namespace ServiceB
{
    public class WebSocketService
    {
        private readonly GraphDbContext _context;

        public WebSocketService(GraphDbContext context)
        {
            _context = context;
        }

        public async Task ProcessWebSocketAsync(HttpContext httpContext)
        {
            if (httpContext.WebSockets.IsWebSocketRequest)
            {
                using var webSocket = await httpContext.WebSockets.AcceptWebSocketAsync();
                await ReceiveMessagesAsync(webSocket);
            }
            else
            {
                httpContext.Response.StatusCode = 400;
            }
        }

        private async Task ReceiveMessagesAsync(WebSocket webSocket)
        {
            var buffer = new byte[1024 * 4];
            while (webSocket.State == WebSocketState.Open)
            {
                var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, CancellationToken.None);
                }
                else
                {
                    var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    await ProcessMessageAsync(message);
                }
            }
        }

        private async Task ProcessMessageAsync(string message)
        {
            // Deserialize the message
            var data = JsonConvert.DeserializeObject<GraphData>(message);

            if (data != null)
            {
                // Save nodes
                foreach (var node in data.Nodes)
                {
                    _context.Nodes.Add(node);
                }

                // Save edges
                foreach (var edge in data.Edges)
                {
                    _context.Edges.Add(edge);
                }

                await _context.SaveChangesAsync();
            }
        }
    }
    public class GraphData
    {
        public List<Node> Nodes { get; set; }
        public List<Edge> Edges { get; set; }
    }
}
