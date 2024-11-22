using Microsoft.AspNetCore.Mvc;
using System.Net.WebSockets;
using System.Text.Json;
using System.Text;
using System.Xml.Linq;

namespace ServiceA.Controlers
{

    [ApiController]
    [Route("api/[controller]")]
    public class GraphController : ControllerBase
    {
        private readonly WebSocketClient _webSocketClient;

        public GraphController()
        {
            _webSocketClient = new WebSocketClient();
        }

        [HttpPost("node")]
        public async Task<IActionResult> AddNode([FromBody] Node node)
        {
            await _webSocketClient.ConnectAsync("ws://localhost:5007/ws");

            var message = JsonSerializer.Serialize(new WebSocketRequest { Action = "addnode", Payload = JsonSerializer.Serialize(node) });
            var response = await _webSocketClient.SendMessageAsync(message);

            return Ok(JsonSerializer.Deserialize<object>(response));
        }

        [HttpDelete("node/{id}")]
        public async Task<IActionResult> DeleteNode(int id)
        {
            await _webSocketClient.ConnectAsync("ws://localhost:5007/ws");

            var message = JsonSerializer.Serialize(new WebSocketRequest { Action = "deletenode", Payload = id.ToString() });
            var response = await _webSocketClient.SendMessageAsync(message);

            return Ok(JsonSerializer.Deserialize<object>(response));
        }

        [HttpGet("node/{id}/descendants")]
        public async Task<IActionResult> GetDescendants(int id)
        {
            await _webSocketClient.ConnectAsync("ws://localhost:5007/ws");

            var message = JsonSerializer.Serialize(new WebSocketRequest { Action = "getdescendants", Payload = id.ToString() });
            var response = await _webSocketClient.SendMessageAsync(message);

            return Ok(JsonSerializer.Deserialize<object>(response));
        }

        [HttpPost("edge")]
        public async Task<IActionResult> AddEdge([FromBody] Edge edge)
        {
            await _webSocketClient.ConnectAsync("ws://localhost:5007/ws");

            var message = JsonSerializer.Serialize(new WebSocketRequest { Action = "addedge", Payload = JsonSerializer.Serialize(edge) });
            var response = await _webSocketClient.SendMessageAsync(message);

            return Ok(JsonSerializer.Deserialize<object>(response));
        }

        [HttpDelete("edge/{id}")]
        public async Task<IActionResult> DeleteEdge(int id)
        {
            await _webSocketClient.ConnectAsync("ws://localhost:5007/ws");

            var message = JsonSerializer.Serialize(new WebSocketRequest { Action = "deleteedge", Payload = id.ToString() });
            var response = await _webSocketClient.SendMessageAsync(message);

            return Ok(JsonSerializer.Deserialize<object>(response));
        }

        public class WebSocketRequest
        {
            public string Action { get; set; }
            public string Payload { get; set; }
        }

    }
}
