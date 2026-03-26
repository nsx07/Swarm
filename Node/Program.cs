using Serilog;
using Swarm.Node;
using Swarm.Node.Data;
using Swarm.Node.Logging;
using Swarm.Node.Services;
using Grpc.Net.Client;

var configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json")
    .AddEnvironmentVariables()
    .Build();

Log.Logger = SerilogConfiguration.CreateLogger(configuration);

Log.Information("Starting Swarm Node worker service");

var builder = Host.CreateDefaultBuilder(args)
    .ConfigureServices(services =>
    {
        // Replace default logging with Serilog
        services.AddSerilog(Log.Logger);
        
        var clusterUrl = configuration["ClusterUrl"] ?? throw new InvalidOperationException("ClusterUrl is not configured");
        
        // Configure gRPC channel
        services.AddSingleton(s => {
            var httpHandler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
            };

            return GrpcChannel.ForAddress(clusterUrl, new GrpcChannelOptions { HttpHandler = httpHandler });
        });
                
        // Add services
        services.AddScoped<RegistrationService>();
        services.AddScoped<HeartBeatService>();
        services.AddSingleton<AppDbConnection>();        

        services.AddHttpClient();
        
        // Add worker
        services.AddHostedService<NodeWorker>();

        services.Configure<DataConfiguration>(configuration.GetSection("Database"));
    });

var host = builder.Build();

Log.Information("Node worker service started successfully");

_ = Task.Run(async () =>
{
   await host.WaitForShutdownAsync();
   Log.Information("Application shutdown \n\n\n");
});

await host.RunAsync();
