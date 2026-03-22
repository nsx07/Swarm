using System;

namespace Swarm.Cluster.Models;

public class ExecutionLog
{
    public Guid Id { get; set; }
    public Guid TaskInstanceId { get; set; }
    public DateTime Timestamp { get; set; }
    public string Level { get; set; } = null!; // INFO, WARNING, ERROR, CRITICAL
    public string Message { get; set; } = null!;
    public string? Phase { get; set; } // extract, transform, load
}
