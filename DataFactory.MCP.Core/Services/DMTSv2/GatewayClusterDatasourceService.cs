using DataFactory.MCP.Abstractions.Interfaces.DMTSv2;
using DataFactory.MCP.Configuration;
using DataFactory.MCP.Extensions;
using DataFactory.MCP.Infrastructure.Http;
using Microsoft.Extensions.Logging;
using System.Net.Http;
using System.Text.Json.Serialization;

namespace DataFactory.MCP.Services.DMTSv2;

/// <summary>
/// Service for retrieving ClusterId from the Power BI v2.0 API for cloud datasources.
/// The ClusterId is required for proper credential binding when adding connections to dataflows.
/// Authentication is handled automatically by the FabricAuthenticationHandler in the HTTP pipeline.
/// 
/// API Endpoint: GET https://api.powerbi.com/v2.0/myorg/me/gatewayClusterDatasources
/// </summary>
public class GatewayClusterDatasourceService : IGatewayClusterDatasourceService
{
    private static readonly string GatewayClusterDatasourcesUrl = FabricUrlBuilder.ForPowerBiV2Api()
        .WithLiteralPath("myorg/me/gatewayClusterDatasources")
        .Build();

    private readonly ILogger<GatewayClusterDatasourceService> _logger;
    private readonly HttpClient _httpClient;

    // Cache for gateway cluster datasources to avoid repeated API calls
    private List<CloudDatasourceInfo>? _cachedDatasources;
    private DateTime _cacheExpiration = DateTime.MinValue;
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

    public GatewayClusterDatasourceService(
        IHttpClientFactory httpClientFactory,
        ILogger<GatewayClusterDatasourceService> logger)
    {
        _httpClient = httpClientFactory.CreateClient(HttpClientNames.PowerBiV2Api);
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<string?> GetClusterIdForConnectionAsync(string connectionId)
    {
        try
        {
            _logger.LogInformation("Looking up ClusterId for connectionId: {ConnectionId}", connectionId);

            var datasources = await GetCloudDatasourcesAsync();

            var matchingDatasource = datasources
                .FirstOrDefault(ds => ds.Id.Equals(connectionId, StringComparison.OrdinalIgnoreCase));

            if (matchingDatasource != null)
            {
                _logger.LogInformation("Found ClusterId {ClusterId} for connectionId {ConnectionId}",
                    matchingDatasource.ClusterId, connectionId);
                return matchingDatasource.ClusterId;
            }

            _logger.LogWarning("No ClusterId found for connectionId: {ConnectionId}. " +
                "The connection may not be a cloud datasource.", connectionId);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error looking up ClusterId for connectionId: {ConnectionId}", connectionId);
            throw;
        }
    }

    private async Task<List<CloudDatasourceInfo>> GetCloudDatasourcesAsync()
    {
        // Check cache first
        if (_cachedDatasources != null && DateTime.UtcNow < _cacheExpiration)
        {
            _logger.LogDebug("Returning cached cloud datasources ({Count} items)", _cachedDatasources.Count);
            return _cachedDatasources;
        }

        _logger.LogDebug("Fetching cloud datasources from Power BI v2.0 API");

        var response = await _httpClient.GetAsync(GatewayClusterDatasourcesUrl);
        var result = await response.ReadAsJsonAsync<GatewayClusterDatasourcesResponse>(JsonSerializerOptionsProvider.FabricApi);

        var datasources = result?.Value ?? new List<CloudDatasourceInfo>();

        _logger.LogInformation("Retrieved {Count} cloud datasources from Power BI v2.0 API", datasources.Count);

        // Update cache
        _cachedDatasources = datasources;
        _cacheExpiration = DateTime.UtcNow.Add(CacheDuration);

        return datasources;
    }

    #region Internal Models

    /// <summary>
    /// Minimal model for cloud datasource info from the v2 API
    /// </summary>
    private sealed class CloudDatasourceInfo
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("clusterId")]
        public string ClusterId { get; set; } = string.Empty;
    }

    /// <summary>
    /// Response wrapper for the gatewayClusterDatasources API
    /// </summary>
    private sealed class GatewayClusterDatasourcesResponse
    {
        [JsonPropertyName("value")]
        public List<CloudDatasourceInfo> Value { get; set; } = new();
    }

    #endregion
}
