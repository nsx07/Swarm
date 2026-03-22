using System.Text.Json;
using Swarm.Cluster.Data;
using Swarm.Cluster.Models;
using Microsoft.EntityFrameworkCore;

namespace Swarm.Cluster.Services;

/// <summary>
/// Manages task definitions and instances
/// </summary>
public class TaskService
{
    private readonly ILogger<TaskService> _logger;
    private readonly ClusterDbContext _dbContext;
    private readonly CacheService _cacheService;

    public TaskService(
        ILogger<TaskService> logger,
        ClusterDbContext dbContext,
        CacheService cacheService)
    {
        _logger = logger;
        _dbContext = dbContext;
        _cacheService = cacheService;
    }

    /// <summary>
    /// Create a new task definition
    /// </summary>
    public async Task<TaskDefinition> CreateTaskDefinitionAsync(
        string name,
        string taskType,
        Dictionary<string, object> schema,
        string? description = null)
    {
        _logger.LogInformation("Creating task definition: {TaskName} (type: {TaskType})", name, taskType);

        var task = new TaskDefinition
        {
            Id = Guid.NewGuid(),
            Name = name,
            TaskType = taskType,
            SchemaJson = JsonSerializer.Serialize(schema),
            Description = description,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _dbContext.TaskDefinitions.Add(task);
        await _dbContext.SaveChangesAsync();

        await _cacheService.SetTaskMetadataAsync(task.Id, new Dictionary<string, object>
        {
            { "name", name },
            { "taskType", taskType },
            { "description", description ?? string.Empty }
        });

        _logger.LogInformation("Task definition created: {TaskId}", task.Id);
        return task;
    }

    /// <summary>
    /// Get task definition by ID
    /// </summary>
    public async Task<TaskDefinition?> GetTaskDefinitionAsync(Guid id)
    {
        _logger.LogDebug("Fetching task definition: {TaskId}", id);
        return await _dbContext.TaskDefinitions.FirstOrDefaultAsync(t => t.Id == id);
    }

    /// <summary>
    /// Get all task definitions, optionally filtered by type
    /// </summary>
    public async Task<List<TaskDefinition>> GetTaskDefinitionsAsync(string? taskType = null)
    {
        _logger.LogInformation("Fetching task definitions (type filter: {TaskType})", taskType ?? "all");

        var query = _dbContext.TaskDefinitions.AsQueryable();
        if (!string.IsNullOrEmpty(taskType))
        {
            query = query.Where(t => t.TaskType == taskType);
        }

        return await query.OrderByDescending(t => t.CreatedAt).ToListAsync();
    }

    /// <summary>
    /// Update a task definition
    /// </summary>
    public async Task<TaskDefinition?> UpdateTaskDefinitionAsync(
        Guid id,
        string? name = null,
        Dictionary<string, object>? schema = null,
        string? description = null)
    {
        _logger.LogInformation("Updating task definition: {TaskId}", id);

        var task = await _dbContext.TaskDefinitions.FirstOrDefaultAsync(t => t.Id == id);
        if (task == null)
        {
            _logger.LogWarning("Task definition not found: {TaskId}", id);
            return null;
        }

        if (!string.IsNullOrEmpty(name))
            task.Name = name;
        if (schema != null)
            task.SchemaJson = JsonSerializer.Serialize(schema);
        if (description != null)
            task.Description = description;

        await _dbContext.SaveChangesAsync();
        await _cacheService.DeleteTaskMetadataAsync(id);

        _logger.LogInformation("Task definition updated: {TaskId}", id);
        return task;
    }

    /// <summary>
    /// Delete a task definition
    /// </summary>
    public async Task DeleteTaskDefinitionAsync(Guid id)
    {
        _logger.LogInformation("Deleting task definition: {TaskId}", id);

        var task = await _dbContext.TaskDefinitions.FirstOrDefaultAsync(t => t.Id == id);
        if (task == null)
        {
            _logger.LogWarning("Task definition not found: {TaskId}", id);
            return;
        }

        _dbContext.TaskDefinitions.Remove(task);
        await _dbContext.SaveChangesAsync();
        await _cacheService.DeleteTaskMetadataAsync(id);

        _logger.LogInformation("Task definition deleted: {TaskId}", id);
    }

    /// <summary>
    /// Create a task instance for a node
    /// </summary>
    public async Task<TaskInstance> CreateTaskInstanceAsync(
        Guid taskDefinitionId,
        Guid nodeId,
        Dictionary<string, object> config,
        string? cronExpression = null,
        DateTime? scheduledAt = null)
    {
        _logger.LogInformation(
            "Creating task instance: taskDefId={TaskDefId}, nodeId={NodeId}",
            taskDefinitionId,
            nodeId);

        var taskDef = await _dbContext.TaskDefinitions.FirstOrDefaultAsync(t => t.Id == taskDefinitionId);
        if (taskDef == null)
        {
            throw new InvalidOperationException($"Task definition {taskDefinitionId} not found");
        }

        var node = await _dbContext.Nodes.FirstOrDefaultAsync(n => n.Id == nodeId);
        if (node == null)
        {
            throw new InvalidOperationException($"Node {nodeId} not found");
        }

        var instance = new TaskInstance
        {
            Id = Guid.NewGuid(),
            TaskDefinitionId = taskDefinitionId,
            NodeId = nodeId,
            ConfigJson = JsonSerializer.Serialize(config),
            Status = "pending",
            RetryCount = 0,
            ScheduledFor = scheduledAt ?? DateTime.UtcNow,
            CronExpression = cronExpression,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.TaskInstances.Add(instance);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Task instance created: {TaskInstanceId}", instance.Id);
        return instance;
    }

    /// <summary>
    /// Get task instance by ID
    /// </summary>
    public async Task<TaskInstance?> GetTaskInstanceAsync(Guid id)
    {
        _logger.LogDebug("Fetching task instance: {TaskInstanceId}", id);
        return await _dbContext.TaskInstances.FirstOrDefaultAsync(ti => ti.Id == id);
    }

    /// <summary>
    /// Get task instances for a node
    /// </summary>
    public async Task<List<TaskInstance>> GetTaskInstancesByNodeAsync(Guid nodeId, string? status = null)
    {
        _logger.LogInformation("Fetching task instances for node: {NodeId} (status: {Status})",
            nodeId, status ?? "all");

        var query = _dbContext.TaskInstances
            .Where(ti => ti.NodeId == nodeId);

        if (!string.IsNullOrEmpty(status))
        {
            query = query.Where(ti => ti.Status == status);
        }

        return await query.OrderByDescending(ti => ti.CreatedAt).ToListAsync();
    }

    /// <summary>
    /// Update task instance status
    /// </summary>
    public async Task<TaskInstance?> UpdateTaskInstanceStatusAsync(Guid id, string status)
    {
        _logger.LogInformation("Updating task instance status: {TaskInstanceId} -> {Status}", id, status);

        var instance = await _dbContext.TaskInstances.FirstOrDefaultAsync(ti => ti.Id == id);
        if (instance == null)
        {
            _logger.LogWarning("Task instance not found: {TaskInstanceId}", id);
            return null;
        }

        instance.Status = status;

        await _dbContext.SaveChangesAsync();
        return instance;
    }

    /// <summary>
    /// Increment retry count for task instance
    /// </summary>
    public async Task<TaskInstance?> IncrementRetryCountAsync(Guid id)
    {
        _logger.LogInformation("Incrementing retry count for task instance: {TaskInstanceId}", id);

        var instance = await _dbContext.TaskInstances.FirstOrDefaultAsync(ti => ti.Id == id);
        if (instance == null)
        {
            return null;
        }

        instance.RetryCount++;

        await _dbContext.SaveChangesAsync();
        return instance;
    }
}
