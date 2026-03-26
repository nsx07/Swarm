using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Swarm.Cluster.Attributes;
using Swarm.Cluster.Data;

namespace Swarm.Cluster.Middleware;

public class ApiKeyAuthMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ApiKeyAuthMiddleware> _logger;

    public ApiKeyAuthMiddleware(RequestDelegate next, ILogger<ApiKeyAuthMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, ClusterDbContext dbContext)
    {
        var path = (context.Request.Path.Value ?? "").ToLower();
        _logger.LogInformation("Processing request for {Path} from {RemoteIp}", path, context.Connection.RemoteIpAddress);
        
        // Skip auth for health checks, Swagger, root, and gRPC
        if (path.StartsWith("/health") || 
            path.StartsWith("/swagger") || 
            path == "/" ||
            context.Request.ContentType?.StartsWith("application/grpc") == true)
        {
            await _next(context);
            return;
        }

        // Check if the endpoint requires API key authentication
        var endpoint = context.GetEndpoint();
        var hasApiKeyRequired = endpoint?.Metadata.GetOrderedMetadata<ApiKeyRequiredAttribute>().Any() ?? false;

        if (!hasApiKeyRequired)
        {
            await _next(context);
            return;
        }

        if (!context.Request.Headers.TryGetValue("X-API-Key", out var apiKeyValue))
        {
            _logger.LogWarning("Request without API key from {RemoteIp}", context.Connection.RemoteIpAddress);
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new { error = "API key required" });
            return;
        }

        var apiKey = apiKeyValue.ToString();
        var node = await dbContext.Nodes.FirstOrDefaultAsync(/**n => n.ApiKey == apiKey*/);

        if (node == null)
        {
            _logger.LogWarning("Invalid API key from {RemoteIp}", context.Connection.RemoteIpAddress);
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new { error = "Invalid API key" });
            return;
        }

        context.Items["NodeId"] = node.Id;
        context.Items["Node"] = node;

        await _next(context);
    }
}
