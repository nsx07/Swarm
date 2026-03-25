using Microsoft.EntityFrameworkCore;
using Swarm.Cluster.Models;

namespace Swarm.Cluster.Data;

public class ClusterDbContext : DbContext
{
    public ClusterDbContext(DbContextOptions<ClusterDbContext> options) : base(options) { }

    public DbSet<Node> Nodes { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Node configuration
        modelBuilder.Entity<Node>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.Name).IsRequired().HasMaxLength(255);
            entity.Property(e => e.Status).IsRequired().HasMaxLength(50);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.LastHeartbeatAt);
        });
    }
}
