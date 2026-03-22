using Microsoft.EntityFrameworkCore;
using Serilog;
using Swarm.Node;
using Swarm.Node.Data;
using Swarm.Node.Logging;
using Swarm.Node.Services;

var configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json")
    .AddEnvironmentVariables()
    .Build();

Log.Logger = SerilogConfiguration.CreateLogger(configuration);

try
{
    Log.Information("Starting Swarm Node worker service");
    
    var builder = Host.CreateDefaultBuilder(args)
        .ConfigureAppConfiguration(config =>
        {
            config.AddConfiguration(configuration);
        })
        .ConfigureServices(services =>
        {
            // Replace default logging with Serilog
            services.AddSerilog(Log.Logger);
            
            // Add database context
            services.AddDbContext<LocalDbContext>(options =>
                options.UseSqlite(configuration.GetConnectionString("LocalDatabase")));
            
            // Add services
            services.AddScoped<RegistrationService>();
            services.AddScoped<CapabilityDiscoveryService>();
            services.AddScoped<ExecutionService>();
            services.AddScoped<RabbitMqService>();
            services.AddScoped<ScheduleService>();
            
            // Add worker
            services.AddHostedService<NodeWorker>();
        });
    
    var host = builder.Build();
    
    // Apply migrations
    using (var scope = host.Services.CreateScope())
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<LocalDbContext>();
        await dbContext.Database.MigrateAsync();
    }
    
    Log.Information("Node worker service started successfully");
    await host.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "An unhandled exception occurred during startup");
}
finally
{
    await Log.CloseAndFlushAsync();
}
