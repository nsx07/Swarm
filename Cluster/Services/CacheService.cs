using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace Swarm.Cluster.Services;

/// <summary>
/// Manages Redis caching for node capabilities and task metadata
/// </summary>
public class CacheService
{
    private readonly ILogger<CacheService> _logger;
    private readonly IDistributedCache _cache;
    private const int CacheDurationMinutes = 30;

    public CacheService(ILogger<CacheService> logger, IDistributedCache cache)
    {
        _logger = logger;
        _cache = cache;
    }

    /// <summary>
    /// Set node capabilities in cache
    /// </summary>
    public async Task SetNodeCapabilitiesAsync(Guid nodeId, Dictionary<string, object> capabilities)
    {
        try
        {
            var cacheKey = $"node:capabilities:{nodeId}";
            var json = JsonSerializer.Serialize(capabilities);
            
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(CacheDurationMinutes)
            };

            await _cache.SetStringAsync(cacheKey, json, options);
            _logger.LogDebug("Cached node capabilities: {NodeId}", nodeId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cache node capabilities: {NodeId}", nodeId);
            // Don't throw - cache failure shouldn't impact functionality
        }
    }

    /// <summary>
    /// Get node capabilities from cache
    /// </summary>
    public async Task<Dictionary<string, object>?> GetNodeCapabilitiesAsync(Guid nodeId)
    {
        try
        {
            var cacheKey = $"node:capabilities:{nodeId}";
            var json = await _cache.GetStringAsync(cacheKey);
            
            if (string.IsNullOrEmpty(json))
            {
                return null;
            }

            return JsonSerializer.Deserialize<Dictionary<string, object>>(json);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve node capabilities from cache: {NodeId}", nodeId);
            return null;
        }
    }

    /// <summary>
    /// Delete node capabilities from cache
    /// </summary>
    public async Task DeleteNodeCapabilitiesAsync(Guid nodeId)
    {
        try
        {
            var cacheKey = $"node:capabilities:{nodeId}";
            await _cache.RemoveAsync(cacheKey);
            _logger.LogDebug("Removed cached node capabilities: {NodeId}", nodeId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to remove node capabilities from cache: {NodeId}", nodeId);
        }
    }

    /// <summary>
    /// Set task metadata in cache
    /// </summary>
    public async Task SetTaskMetadataAsync(Guid taskId, Dictionary<string, object> metadata)
    {
        try
        {
            var cacheKey = $"task:metadata:{taskId}";
            var json = JsonSerializer.Serialize(metadata);
            
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(CacheDurationMinutes)
            };

            await _cache.SetStringAsync(cacheKey, json, options);
            _logger.LogDebug("Cached task metadata: {TaskId}", taskId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cache task metadata: {TaskId}", taskId);
        }
    }

    /// <summary>
    /// Get task metadata from cache
    /// </summary>
    public async Task<Dictionary<string, object>?> GetTaskMetadataAsync(Guid taskId)
    {
        try
        {
            var cacheKey = $"task:metadata:{taskId}";
            var json = await _cache.GetStringAsync(cacheKey);
            
            if (string.IsNullOrEmpty(json))
            {
                return null;
            }

            return JsonSerializer.Deserialize<Dictionary<string, object>>(json);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve task metadata from cache: {TaskId}", taskId);
            return null;
        }
    }

    /// <summary>
    /// Delete task metadata from cache
    /// </summary>
    public async Task DeleteTaskMetadataAsync(Guid taskId)
    {
        try
        {
            var cacheKey = $"task:metadata:{taskId}";
            await _cache.RemoveAsync(cacheKey);
            _logger.LogDebug("Removed cached task metadata: {TaskId}", taskId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to remove task metadata from cache: {TaskId}", taskId);
        }
    }
}

