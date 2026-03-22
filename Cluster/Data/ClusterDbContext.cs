using Microsoft.EntityFrameworkCore;
using Swarm.Cluster.Models;

namespace Swarm.Cluster.Data;

public class ClusterDbContext : DbContext
{
    public ClusterDbContext(DbContextOptions<ClusterDbContext> options) : base(options) { }

    public DbSet<Node> Nodes { get; set; } = null!;
    public DbSet<TaskDefinition> TaskDefinitions { get; set; } = null!;
    public DbSet<TaskInstance> TaskInstances { get; set; } = null!;
    public DbSet<ExecutionLog> ExecutionLogs { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Node configuration
        modelBuilder.Entity<Node>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.Name).IsRequired().HasMaxLength(255);
            entity.Property(e => e.ApiKey).IsRequired().HasMaxLength(255);
            entity.Property(e => e.Status).IsRequired().HasMaxLength(50);
            entity.Property(e => e.CapabilitiesJson).HasDefaultValue("{}");
            entity.HasIndex(e => e.ApiKey).IsUnique();
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.LastHeartbeatAt);
        });

        // TaskDefinition configuration
        modelBuilder.Entity<TaskDefinition>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.Name).IsRequired().HasMaxLength(255);
            entity.Property(e => e.TaskType).IsRequired().HasMaxLength(100);
            entity.Property(e => e.SchemaJson).IsRequired();
            entity.HasIndex(e => e.TaskType);
            entity.HasIndex(e => e.CreatedAt);
        });

        // TaskInstance configuration
        modelBuilder.Entity<TaskInstance>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.ConfigJson).IsRequired();
            entity.Property(e => e.Status).IsRequired().HasMaxLength(50);
            entity.Property(e => e.CronExpression).HasMaxLength(100);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.TaskDefinitionId);
            entity.HasIndex(e => e.NodeId);
            entity.HasIndex(e => e.CreatedAt);
            entity.HasIndex(e => e.ScheduledFor);
        });

        // ExecutionLog configuration
        modelBuilder.Entity<ExecutionLog>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.Level).IsRequired().HasMaxLength(20);
            entity.Property(e => e.Message).IsRequired();
            entity.Property(e => e.Phase).HasMaxLength(50);
            entity.HasIndex(e => e.TaskInstanceId);
            entity.HasIndex(e => e.Timestamp);
            entity.HasIndex(e => new { e.TaskInstanceId, e.Timestamp });
        });
    }
}
