using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Serilog;
using Swarm.Cluster.Data;
using Swarm.Cluster.Logging;
using Swarm.Cluster.Middleware;
using Swarm.Cluster.Services;
  
var builder = WebApplication.CreateBuilder(args);

Log.Logger = SerilogConfiguration.CreateLogger(builder.Configuration);

builder.Host.UseSerilog(Log.Logger);

// Add services
builder.Services.AddDbContext<ClusterDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// builder.Services.AddStackExchangeRedisCache(options =>
// {
//     var redisConfig = builder.Configuration.GetSection("Redis");
//     options.builder.Configuration = redisConfig["Connection"];
// });

builder.Services.AddScoped<NodeService>();
builder.Services.AddHostedService<HeartbeatBackgroundService>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(gen =>
{
    gen.AddSecurityDefinition("ApiKey", new OpenApiSecurityScheme
    {
        Name = "X-API-Key",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Description = "API Key needed to access the endpoints."
    });
    gen.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "ApiKey"
                }
            },
            []
        }
    });
});

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

if (app.Environment.IsDevelopment() || true)
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("AllowFrontend");

app.UseAuthentication();
app.UseAuthorization();
// app.UseMiddleware<ApiKeyAuthMiddleware>();

app.MapControllers();

app.MapGet("/health", () => new { status = "ok" })
    .WithName("Health")
    .WithOpenApi()
    .AllowAnonymous();

Log.Information("Cluster application started successfully");
await app.RunAsync();
