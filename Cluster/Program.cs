using Microsoft.EntityFrameworkCore;
using Serilog;
using Swarm.Cluster.Data;
using Swarm.Cluster.Logging;
using Swarm.Cluster.Middleware;
using Swarm.Cluster.Services;

var configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json")
    .AddEnvironmentVariables()
    .Build();

Log.Logger = SerilogConfiguration.CreateLogger(configuration);

try
{
    Log.Information("Starting Swarm Cluster application");
    
    var builder = WebApplication.CreateBuilder(args);
    
    builder.Configuration.AddConfiguration(configuration);
    
    // Replace default logging with Serilog
    builder.Host.UseSerilog(Log.Logger);
    
    // Add services
    builder.Services.AddDbContext<ClusterDbContext>(options =>
        options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));
    
    builder.Services.AddStackExchangeRedisCache(options =>
    {
        var redisConfig = configuration.GetSection("Redis");
        options.Configuration = redisConfig["Connection"];
    });
    
    builder.Services.AddScoped<NodeService>();
    builder.Services.AddScoped<TaskService>();
    builder.Services.AddScoped<ExecutionService>();
    builder.Services.AddScoped<RabbitMqService>();
    builder.Services.AddScoped<CacheService>();
    builder.Services.AddScoped<SseService>();
    builder.Services.AddHostedService<HeartbeatBackgroundService>();
    
    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();
    
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowFrontend", policy =>
        {
            policy.WithOrigins("http://localhost:3000", "http://localhost:5173")
                  .AllowAnyMethod()
                  .AllowAnyHeader()
                  .AllowCredentials();
        });
    });
    
    var app = builder.Build();
    
    // Apply migrations (gracefully handle missing database)
    try
    {
        using (var scope = app.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ClusterDbContext>();
            await dbContext.Database.MigrateAsync();
            Log.Information("Database migrations applied successfully");
        }
    }
    catch (Exception ex)
    {
        Log.Warning(ex, "Could not apply database migrations - database may not be available. Service will start without database.");
    }
    
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }
    
    app.UseHttpsRedirection();
    app.UseCors("AllowFrontend");
    
    app.UseMiddleware<ApiKeyAuthMiddleware>();
    
    app.MapControllers();
    
    app.MapGet("/health", () => new { status = "ok" })
        .WithName("Health")
        .WithOpenApi()
        .AllowAnonymous();
    
    Log.Information("Cluster application started successfully");
    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "An unhandled exception occurred during startup");
}
finally
{
    await Log.CloseAndFlushAsync();
}
