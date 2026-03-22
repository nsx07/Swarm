using System;

namespace Swarm.Cluster.Models;

public class TaskInstance
{
    public Guid Id { get; set; }
    public Guid TaskDefinitionId { get; set; }
    public Guid? NodeId { get; set; }
    public string ConfigJson { get; set; } = null!; // User-provided configuration
    public string Status { get; set; } = "pending"; // pending, running, completed, failed
    public int RetryCount { get; set; } = 0;
    public int MaxRetries { get; set; } = 3;
    public long BaseDelayMs { get; set; } = 1000;
    public double ExponentialBase { get; set; } = 2.0;
    public DateTime? ScheduledFor { get; set; } // For retry scheduling
    public string? CronExpression { get; set; } // For scheduled tasks
    public DateTime CreatedAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? LastError { get; set; }
    public int RowsProcessed { get; set; } = 0;
    public int ErrorCount { get; set; } = 0;
    public long? DurationMs { get; set; }
}
