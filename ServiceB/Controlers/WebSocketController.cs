using Azure.Core;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ServiceB.Models;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

namespace ServiceB.Controlers
{
    [ApiController]
    [Route("ws")]
    public class WebSocketController : ControllerBase
    {
        private readonly GraphDbContext _dbContext;

        public WebSocketController(GraphDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        [HttpGet]
        public async Task Get()
        {
            if (HttpContext.WebSockets.IsWebSocketRequest)
            {
                using var webSocket = await HttpContext.WebSockets.AcceptWebSocketAsync();
                await HandleWebSocketConnection(webSocket);
            }
            else
            {
                HttpContext.Response.StatusCode = 400;
            }
        }

        private async Task HandleWebSocketConnection(WebSocket webSocket)
        {
            var buffer = new byte[1024 * 4];

            while (webSocket.State == WebSocketState.Open)
            {
                var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                if (result.MessageType == WebSocketMessageType.Text)
                {
                    var request = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    var message = JsonSerializer.Deserialize<WebSocketRequest>(request);

                    var action = (string)message.Action;
                    var payload = message.Payload;

                    object response = action switch
                    {
                        "addnode" => await AddNode(JsonSerializer.Deserialize<Node>(payload.ToString())),
                        "deletenode" => await DeleteNode(Convert.ToInt32( payload)),
                        "getdescendants" => await GetDescendants(Convert.ToInt32(payload)),
                        "addedge" => await AddEdge(JsonSerializer.Deserialize<Edge>(payload.ToString())),
                        "deleteedge" => await DeleteEdge(Convert.ToInt32(payload)),
                        _ => new { error = "Invalid action." }
                    };

                    var responseJson = JsonSerializer.Serialize(response);
                    await webSocket.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(responseJson)), WebSocketMessageType.Text, true, CancellationToken.None);
                }
            }
        }

        private async Task<object> AddNode(Node node)
        {
            _dbContext.Nodes.Add(node);
            await _dbContext.SaveChangesAsync();
            return new { success = true, node };
        }

        private async Task<object> DeleteNode(int nodeId)
        {
            var node = await _dbContext.Nodes.FindAsync(nodeId);
            if (node == null) return new { success = false, error = "Node not found" };

            _dbContext.Nodes.Remove(node);
            await _dbContext.SaveChangesAsync();
            return new { success = true };
        }

        private async Task<object> GetDescendants(int rootNodeId)
        {

            var descendants = await GetDescendantNodesAndEdges(rootNodeId);
            return new { success = true, descendants };

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

        private async Task<object> AddEdge(Edge edge)
        {
            _dbContext.Edges.Add(edge);
            await _dbContext.SaveChangesAsync();
            return new { success = true, edge };
        }

        private async Task<object> DeleteEdge(int edgeId)
        {
            var edge = await _dbContext.Edges.FindAsync(edgeId);
            if (edge == null) return new { success = false, error = "Node not found" };

            _dbContext.Edges.Remove(edge);
            await _dbContext.SaveChangesAsync();
            return new { success = true };
        }
    }
    public class WebSocketRequest
    {
        public string Action { get; set; }
        public string Payload { get; set; }
    }
}
