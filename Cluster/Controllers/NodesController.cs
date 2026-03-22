using Microsoft.AspNetCore.Mvc;
using Swarm.Cluster.Models;
using Swarm.Cluster.Services;

namespace Swarm.Cluster.Controllers;

[ApiController]
[Route("api/[controller]")]
public class NodesController : ControllerBase
{
    private readonly ILogger<NodesController> _logger;
    private readonly NodeService _nodeService;

    public NodesController(ILogger<NodesController> logger, NodeService nodeService)
    {
        _logger = logger;
        _nodeService = nodeService;
    }

    /// <summary>
    /// Register a new node
    /// </summary>
    [HttpPost("register")]
    public async Task<ActionResult<NodeRegistrationResponse>> RegisterNode([FromBody] NodeRegistrationRequest request)
    {
        _logger.LogInformation("Node registration request: {NodeName}", request.NodeName);

        var (nodeId, apiKey) = await _nodeService.RegisterNodeAsync(
            request.NodeName,
            request.Capabilities ?? new Dictionary<string, object>(),
            request.EnvironmentTags
        );

        return Ok(new NodeRegistrationResponse
        {
            NodeId = nodeId,
            ApiKey = apiKey,
            Message = "Node registered successfully"
        });
    }

    /// <summary>
    /// Record heartbeat from a node
    /// </summary>
    [HttpPost("{id}/heartbeat")]
    public async Task<ActionResult> RecordHeartbeat(Guid id)
    {
        _logger.LogDebug("Heartbeat received from node: {NodeId}", id);
        await _nodeService.UpdateHeartbeatAsync(id);
        return Ok();
    }

    /// <summary>
    /// Get all nodes
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<NodeResponse>>> GetNodes([FromQuery] string? status = null)
    {
        _logger.LogInformation("Fetching nodes with status filter: {Status}", status ?? "all");
        
        var nodes = await _nodeService.GetNodesAsync(status);
        
        var response = nodes.Select(n => new NodeResponse
        {
            Id = n.Id,
            Name = n.Name,
            Status = n.Status,
            CapabilitiesJson = n.CapabilitiesJson,
            LastHeartbeatAt = n.LastHeartbeatAt,
            CreatedAt = n.CreatedAt
        }).ToList();

        return Ok(response);
    }

    /// <summary>
    /// Get a specific node by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<NodeResponse>> GetNode(Guid id)
    {
        _logger.LogInformation("Fetching node: {NodeId}", id);
        
        var node = await _nodeService.GetNodeByIdAsync(id);
        if (node == null)
        {
            _logger.LogWarning("Node not found: {NodeId}", id);
            return NotFound(new { error = "Node not found" });
        }

        var response = new NodeResponse
        {
            Id = node.Id,
            Name = node.Name,
            Status = node.Status,
            CapabilitiesJson = node.CapabilitiesJson,
            LastHeartbeatAt = node.LastHeartbeatAt,
            CreatedAt = node.CreatedAt
        };

        return Ok(response);
    }

    /// <summary>
    /// Get nodes with a specific capability
    /// </summary>
    [HttpGet("capability/{capability}")]
    public async Task<ActionResult<List<NodeResponse>>> GetNodesWithCapability(string capability)
    {
        _logger.LogInformation("Fetching nodes with capability: {Capability}", capability);
        
        var nodes = await _nodeService.GetNodesWithCapabilityAsync(capability);
        
        var response = nodes.Select(n => new NodeResponse
        {
            Id = n.Id,
            Name = n.Name,
            Status = n.Status,
            CapabilitiesJson = n.CapabilitiesJson,
            LastHeartbeatAt = n.LastHeartbeatAt,
            CreatedAt = n.CreatedAt
        }).ToList();

        return Ok(response);
    }

    /// <summary>
    /// Delete a node
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteNode(Guid id)
    {
        _logger.LogInformation("Deleting node: {NodeId}", id);
        
        var node = await _nodeService.GetNodeByIdAsync(id);
        if (node == null)
        {
            return NotFound(new { error = "Node not found" });
        }

        await _nodeService.DeleteNodeAsync(id);
        return Ok(new { message = "Node deleted successfully" });
    }

    /// <summary>
    /// Trigger offline node detection (can be called periodically via scheduler)
    /// </summary>
    [HttpPost("check-offline")]
    public async Task<ActionResult> CheckOfflineNodes()
    {
        _logger.LogInformation("Running offline node detection");
        await _nodeService.MarkOfflineNodesAsync();
        return Ok(new { message = "Offline node detection completed" });
    }
}

// DTOs
public class NodeRegistrationRequest
{
    public required string NodeName { get; set; }
    public Dictionary<string, object>? Capabilities { get; set; }
    public Dictionary<string, string>? EnvironmentTags { get; set; }
}

public class NodeRegistrationResponse
{
    public Guid NodeId { get; set; }
    public string ApiKey { get; set; } = null!;
    public string Message { get; set; } = null!;
}

public class NodeResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public string Status { get; set; } = null!;
    public string CapabilitiesJson { get; set; } = null!;
    public DateTime LastHeartbeatAt { get; set; }
    public DateTime CreatedAt { get; set; }
}

