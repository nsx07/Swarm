using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Data.Sqlite;
using Swarm.Node.Data;
using Swarm.Node.Extensions;
using Swarm.Node.Models.Dto;
using Grpc.Net.Client;
using Swarm.Cluster.Services;

namespace Swarm.Node.Services;

/// <summary>
/// Manages initial registration with cluster and periodic heartbeat
/// </summary>
public class RegistrationService(ILogger<RegistrationService> logger, IConfiguration configuration, AppDbConnection dbConnection, GrpcChannel grpcChannel)
{
    private readonly ILogger<RegistrationService> _logger = logger;
    private readonly IConfiguration _configuration = configuration;
    private readonly AppDbConnection _dbConnection = dbConnection;
    private readonly GrpcChannel _grpcChannel = grpcChannel;
    private readonly string _nodeId = configuration["NodeId"] ?? throw new InvalidOperationException("NodeId is not configured");
    private readonly string _apiKey = configuration["ApiKey"] ?? throw new InvalidOperationException("ApiKey is not configured");

    public async Task<bool> RegisterWithClusterAsync(Dictionary<string, object>? capabilities = null)
    {
        _logger.LogInformation("Registering node with cluster via gRPC");

        try
        {
            var client = new NodesService.NodesServiceClient(_grpcChannel);
            
            var envVars = _configuration.AsEnumerable()
                .ToDictionary(kv => kv.Key, kv => kv.Value ?? "");
            
            var request = new RegisterNodeRequest
            {
                ApiKey = _apiKey,
                NodeId = _nodeId,
                EnvironmentTags = { envVars }
            };

            _logger.LogInformation("Sending registration request for node: {NodeId}", _nodeId);
            
            var response = await client.RegisterNodeAsync(request);

            _logger.LogInformation("Registration response: NodeId={NodeId}, NodeName={NodeName}", 
                response.NodeId, response.NodeName);

            if (response?.NodeId.IsNullOrEmpty() ?? true)
            {
                throw new InvalidOperationException("Node failed while retrieving configuration");
            }
            
            using var dbConnection = new SqliteConnection(_dbConnection.GetConnectionString());

            dbConnection.Open();
            var command = dbConnection.CreateCommand();
            command.CommandText = """
                INSERT OR REPLACE INTO Configuration (Registered, NodeId, NodeName) VALUES (1, $1, $2);
                INSERT OR REPLACE INTO RemoteParameters (NodeId, QueueHost, QueuePort, QueueUserName, QueuePassword)
                    VALUES ($1, $3, $4, $5, $6)
                """;

            command.Parameters.Add(new SqliteParameter("$1", response.NodeId));
            command.Parameters.Add(new SqliteParameter("$2", response.NodeName));
            command.Parameters.Add(new SqliteParameter("$3", response.QueueParameters.QueueHost));
            command.Parameters.Add(new SqliteParameter("$4", response.QueueParameters.QueuePort));
            command.Parameters.Add(new SqliteParameter("$5", response.QueueParameters.QueueUserName));
            command.Parameters.Add(new SqliteParameter("$6", response.QueueParameters.QueuePassword));
            command.ExecuteNonQuery();
            
            return true;
        } 
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error registering node with cluster");
            return false;
        }
    }
}
