using Microsoft.AspNetCore.Mvc;
using Swarm.Cluster.Services;

namespace Swarm.Cluster.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RunsController : ControllerBase
{
    private readonly ILogger<RunsController> _logger;
    private readonly ExecutionService _executionService;
    private readonly SseService _sseService;

    public RunsController(ILogger<RunsController> logger, ExecutionService executionService, SseService sseService)
    {
        _logger = logger;
        _executionService = executionService;
        _sseService = sseService;
    }

    // TODO: Implement GET /api/runs
    // TODO: Implement GET /api/runs/{id}
    // TODO: Implement GET /api/runs/{id}/logs
    // TODO: Implement GET /api/runs/{id}/stream (SSE endpoint)
    // TODO: Implement POST /api/tasks/{id}/execute
}
