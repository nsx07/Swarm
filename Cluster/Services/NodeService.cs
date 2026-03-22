using Microsoft.EntityFrameworkCore;
using Swarm.Cluster.Data;
using Swarm.Cluster.Models;
using System.Text.Json;

namespace Swarm.Cluster.Services;

/// <summary>
/// Manages node registration, heartbeat, and capability discovery
/// </summary>
public class NodeService
{
    private readonly ClusterDbContext _dbContext;
    private readonly CacheService _cacheService;
    private readonly ILogger<NodeService> _logger;
    private const int HeartbeatTimeoutSeconds = 60;

    public NodeService(ClusterDbContext dbContext, CacheService cacheService, ILogger<NodeService> logger)
    {
        _dbContext = dbContext;
        _cacheService = cacheService;
        _logger = logger;
    }

    /// <summary>
    /// Register a new node with capabilities
    /// </summary>
    public async Task<(Guid NodeId, string ApiKey)> RegisterNodeAsync(string nodeName, Dictionary<string, object> capabilities, Dictionary<string, string>? environmentTags = null)
    {
        _logger.LogInformation("Registering node: {NodeName}", nodeName);

        // Generate API key
        var apiKey = GenerateApiKey();

        var node = new Node
        {
            Id = Guid.NewGuid(),
            Name = nodeName,
            ApiKey = apiKey,
            Status = "online",
            CreatedAt = DateTime.UtcNow,
            LastHeartbeatAt = DateTime.UtcNow,
            CapabilitiesJson = JsonSerializer.Serialize(capabilities),
            EnvironmentTagsJson = environmentTags != null ? JsonSerializer.Serialize(environmentTags) : null
        };

        _dbContext.Nodes.Add(node);
        await _dbContext.SaveChangesAsync();

        // Cache capabilities
        await _cacheService.SetNodeCapabilitiesAsync(node.Id, capabilities);

        _logger.LogInformation("Node registered successfully: {NodeId} ({NodeName})", node.Id, nodeName);

        return (node.Id, apiKey);
    }

    /// <summary>
    /// Record heartbeat from a node
    /// </summary>
    public async Task UpdateHeartbeatAsync(Guid nodeId)
    {
        var node = await _dbContext.Nodes.FindAsync(nodeId);
        if (node == null)
        {
            _logger.LogWarning("Heartbeat received from unknown node: {NodeId}", nodeId);
            return;
        }

        node.LastHeartbeatAt = DateTime.UtcNow;
        if (node.Status == "offline")
        {
            node.Status = "online";
            _logger.LogInformation("Node came online: {NodeId}", nodeId);
        }

        await _dbContext.SaveChangesAsync();
    }

    /// <summary>
    /// Get all nodes with optional filtering
    /// </summary>
    public async Task<List<Node>> GetNodesAsync(string? status = null)
    {
        var query = _dbContext.Nodes.AsQueryable();

        if (!string.IsNullOrEmpty(status))
        {
            query = query.Where(n => n.Status == status);
        }

        return await query.OrderByDescending(n => n.LastHeartbeatAt).ToListAsync();
    }

    /// <summary>
    /// Get a specific node by ID
    /// </summary>
    public async Task<Node?> GetNodeByIdAsync(Guid nodeId)
    {
        return await _dbContext.Nodes.FindAsync(nodeId);
    }

    /// <summary>
    /// Update node capabilities
    /// </summary>
    public async Task UpdateCapabilitiesAsync(Guid nodeId, Dictionary<string, object> capabilities)
    {
        var node = await _dbContext.Nodes.FindAsync(nodeId);
        if (node == null)
        {
            throw new InvalidOperationException($"Node {nodeId} not found");
        }

        node.CapabilitiesJson = JsonSerializer.Serialize(capabilities);
        await _dbContext.SaveChangesAsync();

        // Update cache
        await _cacheService.SetNodeCapabilitiesAsync(nodeId, capabilities);

        _logger.LogInformation("Node capabilities updated: {NodeId}", nodeId);
    }

    /// <summary>
    /// Mark offline nodes based on heartbeat timeout
    /// </summary>
    public async Task MarkOfflineNodesAsync()
    {
        var cutoffTime = DateTime.UtcNow.AddSeconds(-HeartbeatTimeoutSeconds);
        var offlineNodes = await _dbContext.Nodes
            .Where(n => n.Status == "online" && n.LastHeartbeatAt < cutoffTime)
            .ToListAsync();

        if (offlineNodes.Any())
        {
            foreach (var node in offlineNodes)
            {
                node.Status = "offline";
                _logger.LogWarning("Node marked offline due to missing heartbeat: {NodeId}", node.Id);
            }

            await _dbContext.SaveChangesAsync();
        }
    }

    /// <summary>
    /// Get nodes with specific capability
    /// </summary>
    public async Task<List<Node>> GetNodesWithCapabilityAsync(string capability)
    {
        var nodes = await GetNodesAsync("online");
        var result = new List<Node>();

        foreach (var node in nodes)
        {
            try
            {
                var capabilities = JsonSerializer.Deserialize<Dictionary<string, object>>(node.CapabilitiesJson);
                if (capabilities != null && capabilities.ContainsKey(capability))
                {
                    result.Add(node);
                }
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Failed to parse capabilities for node {NodeId}", node.Id);
            }
        }

        return result;
    }

    /// <summary>
    /// Delete a node
    /// </summary>
    public async Task DeleteNodeAsync(Guid nodeId)
    {
        var node = await _dbContext.Nodes.FindAsync(nodeId);
        if (node != null)
        {
            _dbContext.Nodes.Remove(node);
            await _dbContext.SaveChangesAsync();
            await _cacheService.DeleteNodeCapabilitiesAsync(nodeId);
            _logger.LogInformation("Node deleted: {NodeId}", nodeId);
        }
    }

    /// <summary>
    /// Generate a cryptographically secure API key
    /// </summary>
    private string GenerateApiKey()
    {
        return $"sk_{Guid.NewGuid().ToString().Replace("-", "").Substring(0, 32)}";
    }
}

