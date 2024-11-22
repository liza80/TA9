using Microsoft.EntityFrameworkCore;

namespace ServiceB.Models
{
    public class GraphDbContext : DbContext
    {
        public GraphDbContext(DbContextOptions<GraphDbContext> options) : base(options) { }

        public DbSet<Node> Nodes { get; set; }
        public DbSet<Edge> Edges { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Node>()
                .Property(n => n.NodeId)
                .ValueGeneratedNever();

            modelBuilder.Entity<Edge>()
             .Property(n => n.EdgeId)
             .ValueGeneratedNever();

            modelBuilder.Entity<Edge>()
                .HasOne(e => e.SourceNode)
                .WithMany(n => n.OutgoingEdges)
                .HasForeignKey(e => e.SourceNodeId)
                .OnDelete(DeleteBehavior.Restrict)
                ;

            modelBuilder.Entity<Edge>()
                .HasOne(e => e.TargetNode)
                .WithMany(n => n.IncomingEdges)
                .HasForeignKey(e => e.TargetNodeId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
