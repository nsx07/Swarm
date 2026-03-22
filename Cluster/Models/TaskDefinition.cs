using System;

namespace Swarm.Cluster.Models;

public class TaskDefinition
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public string TaskType { get; set; } = null!; // e.g., "etl-generic"
    public string SchemaJson { get; set; } = null!; // JSON schema for task configuration
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
