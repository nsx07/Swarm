using Microsoft.EntityFrameworkCore;
using Swarm.Node.Models;

namespace Swarm.Node.Data;

public class LocalDbContext : DbContext
{
    public LocalDbContext(DbContextOptions<LocalDbContext> options) : base(options) { }

    public DbSet<LocalTask> LocalTasks { get; set; } = null!;
    public DbSet<ScheduledJob> ScheduledJobs { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // LocalTask configuration
        modelBuilder.Entity<LocalTask>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasDefaultValueSql("lower(hex(randomblob(16)))");
            entity.Property(e => e.ConfigJson).IsRequired();
            entity.Property(e => e.Status).IsRequired().HasMaxLength(50);
            entity.HasIndex(e => e.ClusterTaskId);
            entity.HasIndex(e => e.Status);
        });

        // ScheduledJob configuration
        modelBuilder.Entity<ScheduledJob>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasDefaultValueSql("lower(hex(randomblob(16)))");
            entity.Property(e => e.CronExpression).IsRequired().HasMaxLength(100);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.HasIndex(e => e.TaskDefinitionId);
            entity.HasIndex(e => e.NextRunAt);
        });
    }
}
