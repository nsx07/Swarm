using System;
using Swarm.Node.Data;
using Swarm.Node.Services;

namespace Swarm.Node.BackgroundServices;

public class StartupService(
    BackgroundMaestro gate,
    RegistrationService registrationService,
    AppDbConnection dbConnection, 
    ILogger<StartupService> logger,
    IHostApplicationLifetime appLifetime
    ) : IHostedService
{
    private readonly BackgroundMaestro _gate = gate;
    private readonly ILogger<StartupService> _logger = logger;
    private readonly AppDbConnection _dbConnection = dbConnection;
    private readonly IHostApplicationLifetime _appLifetime = appLifetime;
    private readonly RegistrationService _registrationService = registrationService;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Startup service initializing node. Locking background services until initialization is complete.");
            await InitializeNodeAsync();
            _gate.Release();
        } catch (Exception ex)
        {
            _logger.LogCritical(ex, "Startup service failed to initialize node. Shutting down application.");
            _appLifetime.StopApplication();
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private async Task InitializeNodeAsync()
    {
        _logger.LogInformation("Initializing node");

        try
        {
            await _dbConnection.SetupDatabaseAsync();

            var registered = await _registrationService.RegisterWithClusterAsync();

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
