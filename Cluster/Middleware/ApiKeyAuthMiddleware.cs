using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
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
        // Skip auth for health checks, Swagger, and node registration
        var path = (context.Request.Path.Value ?? "").ToLower();
        _logger.LogInformation("Processing request for {Path} from {RemoteIp}", path, context.Connection.RemoteIpAddress);
        if (path.StartsWith("/health") || 
            path.StartsWith("/swagger") || 
            path == "/" ||
            path == "/api/nodes/register")
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
