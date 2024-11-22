using Microsoft.EntityFrameworkCore;
using ServiceB.Models;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;


namespace ServiceB
{
    public class WebSocketHandler
    {
        private readonly GraphDbContext _dbContext;

        public WebSocketHandler(GraphDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task HandleAsync(HttpContext context)
        {
            if (context.WebSockets.IsWebSocketRequest)
            {
                using var webSocket = await context.WebSockets.AcceptWebSocketAsync();
                await HandleMessagesAsync(webSocket);
            }
            else
            {
                context.Response.StatusCode = 400;
            }
        }

        private async Task HandleMessagesAsync(WebSocket webSocket)
        {
            var buffer = new byte[1024 * 4];
            while (webSocket.State == WebSocketState.Open)
            {
                var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closed", CancellationToken.None);
                    break;
                }

                var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                await ProcessMessageAsync(webSocket, message);
            }
        }

        private async Task ProcessMessageAsync(WebSocket webSocket, string message)
        {
            try
            {
                var request = JsonSerializer.Deserialize<WebSocketRequest>(message);
                if (request == null) throw new Exception("Invalid message format");

                switch (request.Action.ToLower())
                {
                    case "addnode":
                        var node = JsonSerializer.Deserialize<Node>(request.Payload);
                        if (node != null)
                        {
                            _dbContext.Nodes.Add(node);
                            await _dbContext.SaveChangesAsync();
                            await SendResponseAsync(webSocket, "Node added successfully", node);
                        }
                        break;

                    case "deletenode":
                        if (int.TryParse(request.Payload, out var nodeId))
                        {
                            var nodeToDelete = await _dbContext.Nodes.FindAsync(nodeId);
                            if (nodeToDelete != null)
                            {
                                _dbContext.Nodes.Remove(nodeToDelete);
                                await _dbContext.SaveChangesAsync();
                                await SendResponseAsync(webSocket, "Node deleted successfully", nodeToDelete);
                            }
                        }
                        break;
                    case "addedge":
                        var newEdge = JsonSerializer.Deserialize<Edge>(request.Payload);
                        if (newEdge != null)
                        {
                            _dbContext.Edges.Add(newEdge);
                            await _dbContext.SaveChangesAsync();
                            await SendResponseAsync(webSocket, "Edge added successfully", newEdge);
                        }
                        break;

                    case "deleteedge":
                        if (int.TryParse(request.Payload, out var edgeId))
                        {
                            var edge = await _dbContext.Edges.FindAsync(edgeId);
                            if (edge != null)
                            {
                                _dbContext.Edges.Remove(edge);
                                await _dbContext.SaveChangesAsync();
                                await SendResponseAsync(webSocket, "Edge deleted successfully", edge);
                            }
                        }
                        break;
                    case "getdescendants":
                        if (int.TryParse(request.Payload, out int id))
                        {
                            var descendants = await GetDescendantNodesAndEdges(id);
                            await SendResponseAsync(webSocket, "All decedents nodes and arcs", descendants);
                        }
                        else
                        {
                            await SendResponseAsync(webSocket, "There is no node with id {id}", string.Empty);
                        }
                        break;
                    default:
                        await SendResponseAsync(webSocket, "Invalid action", null);
                        break;
                }
            }
            catch (Exception ex)
            {
                await SendResponseAsync(webSocket, $"Error: {ex.Message}", null);
            }
        }
        private async Task<object> GetDescendantNodesAndEdges(int rootNodeId)
        {
            var nodes = new List<Node>();
            var edges = new List<Edge>();
            var nodeQueue = new Queue<int>();
            nodeQueue.Enqueue(rootNodeId);

            while (nodeQueue.Count > 0)
            {
                int currentNodeId = nodeQueue.Dequeue();

                var currentNode = await _dbContext.Nodes.FindAsync(currentNodeId);
                if (currentNode != null && !nodes.Any(n => n.NodeId == currentNode.NodeId))
                    nodes.Add(currentNode);

                var outgoingEdges = await _dbContext.Edges
                    .Where(e => e.SourceNodeId == currentNodeId)
                    .ToListAsync();

                foreach (var edge in outgoingEdges)
                {
                    if (!edges.Any(e => e.EdgeId == edge.EdgeId))
                        edges.Add(edge);

                    if (!nodes.Any(n => n.NodeId == edge.TargetNodeId))
                        nodeQueue.Enqueue(edge.TargetNodeId);
                }
            }

            return new { Nodes = nodes, Edges = edges };
        }

        private async Task SendResponseAsync(WebSocket webSocket, string message, object? payload)
        {
            var response = new WebSocketResponse
            {
                Message = message,
                Payload = payload
            };

            var jsonResponse = JsonSerializer.Serialize(response);
            var bytes = Encoding.UTF8.GetBytes(jsonResponse);
            await webSocket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, CancellationToken.None);
        }
    }

    public class WebSocketRequest
    {
        public string Action { get; set; }
        public string Payload { get; set; }
    }

    public class WebSocketResponse
    {
        public string Message { get; set; }
        public object? Payload { get; set; }
    }
}