using Swarm.Node.Data;
using Swarm.Node.Services;

namespace Swarm.Node;

public class NodeWorker(ILogger<NodeWorker> logger, IServiceProvider serviceProvider, IHostApplicationLifetime hostApplicationLifetime) : BackgroundService
{
    private readonly ILogger<NodeWorker> _logger = logger;
    private readonly IServiceProvider _serviceProvider = serviceProvider;
    private readonly IHostApplicationLifetime _hostApplicationLifetime = hostApplicationLifetime;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            _logger.LogInformation("Node worker starting main loop");

            await InitializeNodeAsync();

            var heartBeatService = _serviceProvider.GetRequiredService<HeartBeatService>();

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await heartBeatService.SendHeartBeatAsync();
                    await Task.Delay(TimeSpan.FromSeconds(120), stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in main event loop");
                }
            }

            _logger.LogInformation("Node worker stopped");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Node worker encountered an error");
            _hostApplicationLifetime.StopApplication();
        }
    }

    private async Task InitializeNodeAsync()
    {
        _logger.LogInformation("Initializing node");

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var dbConnection = scope.ServiceProvider.GetRequiredService<AppDbConnection>();
            await dbConnection.SetupDatabaseAsync();

            var registrationService = scope.ServiceProvider.GetRequiredService<RegistrationService>();
            var registered = await registrationService.RegisterWithClusterAsync();

            if (!registered)
            {
                _logger.LogError("Failed to register with cluster");
                throw new InvalidOperationException("Node registration failed");
            }

            _logger.LogInformation("Node initialization completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while initializing node");
            throw;
        }
    }
}
