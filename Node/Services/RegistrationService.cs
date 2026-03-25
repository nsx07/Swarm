using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Data.Sqlite;
using Swarm.Node.Data;
using Swarm.Node.Extensions;
using Swarm.Node.Models.Dto;

namespace Swarm.Node.Services;

/// <summary>
/// Manages initial registration with cluster and periodic heartbeat
/// </summary>
public class RegistrationService(ILogger<RegistrationService> logger, IConfiguration configuration, AppDbConnection dbConnection, IHttpClientFactory httpFactory)
{
    private readonly ILogger<RegistrationService> _logger = logger;
    private readonly IConfiguration _configuration = configuration;
    private readonly AppDbConnection _dbConnection = dbConnection;
    private readonly IHttpClientFactory _httpFactory = httpFactory;
    private readonly string _clusterUrl = configuration["ClusterUrl"] ?? throw new InvalidOperationException("ClusterUrl is not configured");
    private readonly string _nodeId = configuration["NodeId"] ?? throw new InvalidOperationException("NodeId is not configured");
    private readonly string _apiKey = configuration["ApiKey"] ?? throw new InvalidOperationException("ApiKey is not configured");

    private static readonly JsonSerializerOptions _options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    public async Task<bool> RegisterWithClusterAsync(Dictionary<string, object>? capabilities = null)
    {
        _logger.LogInformation("Registering node with cluster");

        try
        {
            var httpClient = _httpFactory.CreateClient();
            httpClient.DefaultRequestHeaders.Add("X-API-Key", _apiKey);
            httpClient.DefaultRequestHeaders.Add("X-Node-Id", _nodeId);
            
            var envVars = _configuration.AsEnumerable().ToDictionary(kv => kv.Key, kv => kv.Value);
            var payload = new
            {
                EnvironmentTags = envVars,
                Capabilities = capabilities
            };

            var json = JsonSerializer.Serialize(payload, _options);

            _logger.LogInformation("Registration payload: {Payload}", json);
            var response = await httpClient.PostAsync($"{_clusterUrl}/api/Nodes/register", 
                            new StringContent(json, System.Text.Encoding.UTF8, "application/json"));

            var responseBody = await response.Content.ReadAsStringAsync();  
            var registrationData = JsonSerializer.Deserialize<RegistrationRequestResponse>(responseBody, _options);      

            _logger.LogInformation("Registration response status code: {StatusCode}", response.StatusCode);
            _logger.LogInformation("Registration response content: {Content}", registrationData);

            if (!response.IsSuccessStatusCode || registrationData == null || registrationData.NodeId.IsNullOrEmpty())
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

            command.Parameters.Add(new SqliteParameter("$1", registrationData.NodeId));
            command.Parameters.Add(new SqliteParameter("$2", registrationData.NodeName));
            command.Parameters.Add(new SqliteParameter("$3", registrationData.QueueParameters.QueueHost));
            command.Parameters.Add(new SqliteParameter("$4", registrationData.QueueParameters.QueuePort));
            command.Parameters.Add(new SqliteParameter("$5", registrationData.QueueParameters.QueueUserName));
            command.Parameters.Add(new SqliteParameter("$6", registrationData.QueueParameters.QueuePassword));
            command.ExecuteNonQuery();
            
            return response.IsSuccessStatusCode;
        } catch (Exception ex)
        {
            _logger.LogError(ex, "Error registering node with cluster");
            return false;
        }
    }
}
