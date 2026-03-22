using Microsoft.AspNetCore.Mvc;
using Swarm.Cluster.Models;
using Swarm.Cluster.Services;

namespace Swarm.Cluster.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TasksController : ControllerBase
{
    private readonly ILogger<TasksController> _logger;
    private readonly TaskService _taskService;
    private readonly ExecutionService _executionService;

    public TasksController(ILogger<TasksController> logger, TaskService taskService, ExecutionService executionService)
    {
        _logger = logger;
        _taskService = taskService;
        _executionService = executionService;
    }

    /// <summary>
    /// Get available task type definitions
    /// </summary>
    [HttpGet("types")]
    public ActionResult<List<TaskTypeDefinition>> GetTaskTypes()
    {
        _logger.LogInformation("Fetching task type definitions");
        var types = TaskTypeDefinitions.GetAllTaskTypes();
        return Ok(types);
    }

    /// <summary>
    /// Get specific task type definition
    /// </summary>
    [HttpGet("types/{taskType}")]
    public ActionResult<TaskTypeDefinition> GetTaskType(string taskType)
    {
        _logger.LogInformation("Fetching task type: {TaskType}", taskType);
        var type = TaskTypeDefinitions.GetTaskType(taskType);
        if (type == null)
        {
            return NotFound(new { error = "Task type not found" });
        }
        return Ok(type);
    }

    /// <summary>
    /// Create a new task definition
    /// </summary>
    [HttpPost("definitions")]
    public async Task<ActionResult<TaskDefinitionResponse>> CreateTaskDefinition([FromBody] CreateTaskDefinitionRequest request)
    {
        _logger.LogInformation("Creating task definition: {TaskName}", request.Name);

        try
        {
            var taskDef = await _taskService.CreateTaskDefinitionAsync(
                request.Name,
                request.TaskType,
                request.Schema,
                request.Description
            );

            var response = new TaskDefinitionResponse
            {
                Id = taskDef.Id,
                Name = taskDef.Name,
                TaskType = taskDef.TaskType,
                SchemaJson = taskDef.SchemaJson,
                Description = taskDef.Description,
                CreatedAt = taskDef.CreatedAt,
                UpdatedAt = taskDef.UpdatedAt
            };

            return CreatedAtAction(nameof(GetTaskDefinition), new { id = taskDef.Id }, response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating task definition: {TaskName}", request.Name);
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get all task definitions
    /// </summary>
    [HttpGet("definitions")]
    public async Task<ActionResult<List<TaskDefinitionResponse>>> GetTaskDefinitions([FromQuery] string? taskType = null)
    {
        _logger.LogInformation("Fetching task definitions (type filter: {TaskType})", taskType ?? "all");

        var taskDefs = await _taskService.GetTaskDefinitionsAsync(taskType);
        var response = taskDefs.Select(t => new TaskDefinitionResponse
        {
            Id = t.Id,
            Name = t.Name,
            TaskType = t.TaskType,
            SchemaJson = t.SchemaJson,
            Description = t.Description,
            CreatedAt = t.CreatedAt,
            UpdatedAt = t.UpdatedAt
        }).ToList();

        return Ok(response);
    }

    /// <summary>
    /// Get a specific task definition
    /// </summary>
    [HttpGet("definitions/{id}")]
    public async Task<ActionResult<TaskDefinitionResponse>> GetTaskDefinition(Guid id)
    {
        _logger.LogInformation("Fetching task definition: {TaskDefinitionId}", id);

        var taskDef = await _taskService.GetTaskDefinitionAsync(id);
        if (taskDef == null)
        {
            return NotFound(new { error = "Task definition not found" });
        }

        var response = new TaskDefinitionResponse
        {
            Id = taskDef.Id,
            Name = taskDef.Name,
            TaskType = taskDef.TaskType,
            SchemaJson = taskDef.SchemaJson,
            Description = taskDef.Description,
            CreatedAt = taskDef.CreatedAt,
            UpdatedAt = taskDef.UpdatedAt
        };

        return Ok(response);
    }

    /// <summary>
    /// Update a task definition
    /// </summary>
    [HttpPut("definitions/{id}")]
    public async Task<ActionResult<TaskDefinitionResponse>> UpdateTaskDefinition(Guid id, [FromBody] UpdateTaskDefinitionRequest request)
    {
        _logger.LogInformation("Updating task definition: {TaskDefinitionId}", id);

        try
        {
            var taskDef = await _taskService.UpdateTaskDefinitionAsync(
                id,
                request.Name,
                request.Schema,
                request.Description
            );

            if (taskDef == null)
            {
                return NotFound(new { error = "Task definition not found" });
            }

            var response = new TaskDefinitionResponse
            {
                Id = taskDef.Id,
                Name = taskDef.Name,
                TaskType = taskDef.TaskType,
                SchemaJson = taskDef.SchemaJson,
                Description = taskDef.Description,
                CreatedAt = taskDef.CreatedAt,
                UpdatedAt = taskDef.UpdatedAt
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating task definition: {TaskDefinitionId}", id);
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Delete a task definition
    /// </summary>
    [HttpDelete("definitions/{id}")]
    public async Task<ActionResult> DeleteTaskDefinition(Guid id)
    {
        _logger.LogInformation("Deleting task definition: {TaskDefinitionId}", id);

        var existing = await _taskService.GetTaskDefinitionAsync(id);
        if (existing == null)
        {
            return NotFound(new { error = "Task definition not found" });
        }

        await _taskService.DeleteTaskDefinitionAsync(id);
        return Ok(new { message = "Task definition deleted successfully" });
    }

    /// <summary>
    /// Create a task instance for a node
    /// </summary>
    [HttpPost("instances")]
    public async Task<ActionResult<TaskInstanceResponse>> CreateTaskInstance([FromBody] CreateTaskInstanceRequest request)
    {
        _logger.LogInformation("Creating task instance for node: {NodeId}", request.NodeId);

        try
        {
            var instance = await _taskService.CreateTaskInstanceAsync(
                request.TaskDefinitionId,
                request.NodeId,
                request.Config,
                request.CronExpression,
                request.ScheduledAt
            );

            var response = new TaskInstanceResponse
            {
                Id = instance.Id,
                TaskDefinitionId = instance.TaskDefinitionId,
                NodeId = instance.NodeId ?? Guid.Empty,
                ConfigJson = instance.ConfigJson,
                Status = instance.Status,
                RetryCount = instance.RetryCount,
                ScheduledFor = instance.ScheduledFor,
                CronExpression = instance.CronExpression,
                CreatedAt = instance.CreatedAt
            };

            return CreatedAtAction(nameof(GetTaskInstance), new { id = instance.Id }, response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating task instance: {NodeId}", request.NodeId);
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get a specific task instance
    /// </summary>
    [HttpGet("instances/{id}")]
    public async Task<ActionResult<TaskInstanceResponse>> GetTaskInstance(Guid id)
    {
        _logger.LogInformation("Fetching task instance: {TaskInstanceId}", id);

        var instance = await _taskService.GetTaskInstanceAsync(id);
        if (instance == null)
        {
            return NotFound(new { error = "Task instance not found" });
        }

        var response = new TaskInstanceResponse
        {
            Id = instance.Id,
            TaskDefinitionId = instance.TaskDefinitionId,
            NodeId = instance.NodeId ?? Guid.Empty,
            ConfigJson = instance.ConfigJson,
            Status = instance.Status,
            RetryCount = instance.RetryCount,
            ScheduledFor = instance.ScheduledFor,
            CronExpression = instance.CronExpression,
            CreatedAt = instance.CreatedAt
        };

        return Ok(response);
    }

    /// <summary>
    /// Get task instances for a node
    /// </summary>
    [HttpGet("instances/by-node/{nodeId}")]
    public async Task<ActionResult<List<TaskInstanceResponse>>> GetTaskInstancesByNode(Guid nodeId, [FromQuery] string? status = null)
    {
        _logger.LogInformation("Fetching task instances for node: {NodeId} (status: {Status})", nodeId, status ?? "all");

        var instances = await _taskService.GetTaskInstancesByNodeAsync(nodeId, status);
        var response = instances.Select(i => new TaskInstanceResponse
        {
            Id = i.Id,
            TaskDefinitionId = i.TaskDefinitionId,
            NodeId = i.NodeId ?? Guid.Empty,
            ConfigJson = i.ConfigJson,
            Status = i.Status,
            RetryCount = i.RetryCount,
            ScheduledFor = i.ScheduledFor,
            CronExpression = i.CronExpression,
            CreatedAt = i.CreatedAt
        }).ToList();

        return Ok(response);
    }

    /// <summary>
    /// Update task instance status
    /// </summary>
    [HttpPatch("instances/{id}/status")]
    public async Task<ActionResult<TaskInstanceResponse>> UpdateTaskInstanceStatus(Guid id, [FromBody] UpdateTaskInstanceStatusRequest request)
    {
        _logger.LogInformation("Updating task instance status: {TaskInstanceId} -> {Status}", id, request.Status);

        var instance = await _taskService.UpdateTaskInstanceStatusAsync(id, request.Status);
        if (instance == null)
        {
            return NotFound(new { error = "Task instance not found" });
        }

        var response = new TaskInstanceResponse
        {
            Id = instance.Id,
            TaskDefinitionId = instance.TaskDefinitionId,
            NodeId = instance.NodeId ?? Guid.Empty,
            ConfigJson = instance.ConfigJson,
            Status = instance.Status,
            RetryCount = instance.RetryCount,
            ScheduledFor = instance.ScheduledFor,
            CronExpression = instance.CronExpression,
            CreatedAt = instance.CreatedAt
        };

        return Ok(response);
    }
}

// Request/Response DTOs
public class CreateTaskDefinitionRequest
{
    public required string Name { get; set; }
    public required string TaskType { get; set; }
    public required Dictionary<string, object> Schema { get; set; }
    public string? Description { get; set; }
}

public class UpdateTaskDefinitionRequest
{
    public string? Name { get; set; }
    public Dictionary<string, object>? Schema { get; set; }
    public string? Description { get; set; }
}

public class TaskDefinitionResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public string TaskType { get; set; } = null!;
    public string SchemaJson { get; set; } = null!;
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class CreateTaskInstanceRequest
{
    public required Guid TaskDefinitionId { get; set; }
    public required Guid NodeId { get; set; }
    public required Dictionary<string, object> Config { get; set; }
    public string? CronExpression { get; set; }
    public DateTime? ScheduledAt { get; set; }
}

public class UpdateTaskInstanceStatusRequest
{
    public required string Status { get; set; }
}

public class TaskInstanceResponse
{
    public Guid Id { get; set; }
    public Guid TaskDefinitionId { get; set; }
    public Guid NodeId { get; set; }
    public string ConfigJson { get; set; } = null!;
    public string Status { get; set; } = null!;
    public int RetryCount { get; set; }
    public DateTime? ScheduledFor { get; set; }
    public string? CronExpression { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Task type definitions for ETL orchestration
/// </summary>
public class TaskTypeDefinition
{
    public required string Type { get; set; }
    public required string Name { get; set; }
    public required string Description { get; set; }
    public required Dictionary<string, object> Schema { get; set; }
}

/// <summary>
/// Predefined task type definitions
/// </summary>
public static class TaskTypeDefinitions
{
    private static readonly List<TaskTypeDefinition> _taskTypes = new()
    {
        new TaskTypeDefinition
        {
            Type = "etl-generic",
            Name = "Generic ETL Task",
            Description = "General-purpose Extract-Transform-Load task",
            Schema = new Dictionary<string, object>
            {
                { "source", new { type = "string", description = "Data source connection string or path" } },
                { "sourceType", new { type = "string", description = "Source type (database, file, api, stream)" } },
                { "query", new { type = "string", description = "Query or selector for data extraction" } },
                { "transformations", new { type = "array", description = "List of transformation steps" } },
                { "destination", new { type = "string", description = "Destination for transformed data" } },
                { "destinationType", new { type = "string", description = "Destination type (database, file, api)" } }
            }
        },
        new TaskTypeDefinition
        {
            Type = "database-sync",
            Name = "Database Sync",
            Description = "Synchronize data between two databases",
            Schema = new Dictionary<string, object>
            {
                { "sourceDatabase", new { type = "string", description = "Source database connection" } },
                { "targetDatabase", new { type = "string", description = "Target database connection" } },
                { "tables", new { type = "array", description = "Tables to sync" } },
                { "syncMode", new { type = "string", description = "Sync mode (full, incremental)" } }
            }
        },
        new TaskTypeDefinition
        {
            Type = "file-processing",
            Name = "File Processing",
            Description = "Process files (CSV, JSON, Parquet, etc.)",
            Schema = new Dictionary<string, object>
            {
                { "inputPath", new { type = "string", description = "Input file path or pattern" } },
                { "fileFormat", new { type = "string", description = "File format (csv, json, parquet)" } },
                { "outputPath", new { type = "string", description = "Output file path" } },
                { "processingRules", new { type = "object", description = "Custom processing rules" } }
            }
        }
    };

    public static List<TaskTypeDefinition> GetAllTaskTypes() => new(_taskTypes);

    public static TaskTypeDefinition? GetTaskType(string type) =>
        _taskTypes.FirstOrDefault(t => t.Type == type);
}
