using System;

namespace Swarm.Node.Services;

public class HeartBeatService(IConfiguration configuration, IHttpClientFactory _httpFactory, ILogger<HeartBeatService> logger)
{
    private readonly string _clusterUrl = configuration["ClusterUrl"] ?? throw new InvalidOperationException("ClusterUrl is not configured");
    private readonly string _nodeId = configuration["NodeId"] ?? throw new InvalidOperationException("NodeId is not configured");
    private readonly string _apiKey = configuration["ApiKey"] ?? throw new InvalidOperationException("ApiKey is not configured");
    private readonly ILogger<HeartBeatService> _logger = logger;

    public async Task<bool> SendHeartBeatAsync()
    {
        _logger.LogInformation("Sending heartbeat to cluster at {ClusterUrl} for node {NodeId} with key {ApiKey}", _clusterUrl, _nodeId, _apiKey);
        try
        {
            var httpClient = _httpFactory.CreateClient();
            httpClient.DefaultRequestHeaders.Add("X-API-Key", _apiKey);
            httpClient.DefaultRequestHeaders.Add("X-Node-Id", _nodeId);
            var response = await httpClient.PostAsync($"{_clusterUrl}/api/Nodes/{_nodeId}/heartbeat", null);
            
            _logger.LogInformation("Heartbeat response status code: {StatusCode}", response.StatusCode);
            _logger.LogInformation("Heartbeat response content: {Content}", await response.Content.ReadAsStringAsync());
            
            return response.IsSuccessStatusCode;
        } catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending heartbeat to cluster");
            return false;
        }
    }
}
