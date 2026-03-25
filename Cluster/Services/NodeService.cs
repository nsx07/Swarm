using Microsoft.EntityFrameworkCore;
using Serilog;
using Swarm.Cluster.Data;
using Swarm.Cluster.Models;
using Swarm.Cluster.Models.Dto;
using System.Text.Json;

namespace Swarm.Cluster.Services;

/// <summary>
/// Manages node registration, heartbeat, and capability discovery
/// </summary>
public class NodeService
{
    private readonly ClusterDbContext _dbContext;
    private readonly ILogger<NodeService> _logger;
    private readonly IConfiguration _config;
    private const int HeartbeatTimeoutSeconds = 300;

    public NodeService(ClusterDbContext dbContext, ILogger<NodeService> logger, IConfiguration config)
    {
        _dbContext = dbContext;
        _logger = logger;
        _config = config;
    }

    /// <summary>
    /// Register a new node with capabilities
    /// </summary>
    public async Task<RequestNodeRegistrationResponse> RegisterNodeAsync(string apiKey, Guid? nodeId, Dictionary<string, string>? environmentTags = null)
    {
        _logger.LogInformation("Registering node: {NodeName}", nodeId);

        var existentNodeByName = await _dbContext.Nodes.Where(x => x.Id == nodeId).Select(x => x.Name).FirstOrDefaultAsync();

        var node = new Node
        {
            Id = nodeId ?? Guid.NewGuid(),
            Name = existentNodeByName != null ? existentNodeByName : GenerateNodeName(),
            Status = "online",
            CreatedAt = DateTime.UtcNow,
            LastHeartbeatAt = DateTime.UtcNow,
            EnvironmentTagsJson = environmentTags != null ? JsonSerializer.Serialize(environmentTags) : null
        };

        if (existentNodeByName != null)
        {
            _dbContext.Nodes.Update(node);
        } else
        {
            _dbContext.Nodes.Add(node);
        }

        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Node registered successfully: {NodeId} ({NodeName})", node.Id, node.Name);

        return new RequestNodeRegistrationResponse()
        {
            NodeId = node.Id.ToString(),
            NodeName = node.Name,
            QueueParameters = new()
            {
                QueuePort = _config["RabbitMQ:Port"]!,
                QueueHost = _config["RabbitMQ:Hostname"]!,
                QueuePassword = _config["RabbitMQ:Password"]!,
                QueueUserName = _config["RabbitMQ:UserName"]!,
            }
        };
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
    /// Mark offline nodes based on heartbeat timeout
    /// </summary>
    public async Task MarkOfflineNodesAsync()
    {
        var cutoffTime = DateTime.UtcNow.AddSeconds(-HeartbeatTimeoutSeconds);
        var offlineNodes = await _dbContext.Nodes
            .Where(n => n.Status == "online" && n.LastHeartbeatAt < cutoffTime)
            .ToListAsync();

        if (offlineNodes.Count != 0)
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
    /// Delete a node
    /// </summary>
    public async Task DeleteNodeAsync(Guid nodeId)
    {
        var node = await _dbContext.Nodes.FindAsync(nodeId);
        if (node != null)
        {
            _dbContext.Nodes.Remove(node);
            await _dbContext.SaveChangesAsync();
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

    /// <summary>
    /// Generate a random name for nodeId
    /// </summary>
    /// <returns></returns>
    private string  GenerateNodeName()
    {
        Random random = new();
        char[] consonants = "bcdfghjklmnpqrstvwx".ToCharArray();
        char[] vowels = "aeiouy".ToCharArray();
        string name = "";

        _logger.LogInformation("{Consonants} - {Vowels}", consonants, vowels);

        name += consonants[random.Next(consonants.Length - 1)];
        name += vowels[random.Next(vowels.Length - 1)];
        short b = 2;

        while (b < 13)
        {
            name += consonants[random.Next(consonants.Length - 1)];
            b++;
            name += vowels[random.Next(vowels.Length - 1)];
            b++;
        }

        return name;
    }
}