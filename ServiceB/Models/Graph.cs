using System.ComponentModel.DataAnnotations.Schema;

namespace ServiceB.Models
{
    public class Node
    {
        public int NodeId { get; set; }
        public string Name { get; set; }
        public string Data { get; set; }

        public ICollection<Edge> OutgoingEdges { get; set; }
        public ICollection<Edge> IncomingEdges { get; set; }
    }

    public class Edge
    {
        public int EdgeId { get; set; }
        public int SourceNodeId { get; set; }
        public int TargetNodeId { get; set; }
        public double Weight { get; set; }

        public Node SourceNode { get; set; }
        public Node TargetNode { get; set; }
    }
}
