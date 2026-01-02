using DataFactory.MCP.Abstractions;
using DataFactory.MCP.Abstractions.Interfaces;
using DataFactory.MCP.Abstractions.Interfaces.DMTSv2;
using DataFactory.MCP.Models;
using Microsoft.Extensions.Logging;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DataFactory.MCP.Services.DMTSv2;

/// <summary>
/// Service for retrieving ClusterId from the Power BI v2.0 API for cloud datasources.
/// The ClusterId is required for proper credential binding when adding connections to dataflows.
/// 
/// API Endpoint: GET https://api.powerbi.com/v2.0/myorg/me/gatewayClusterDatasources
/// </summary>
public class GatewayClusterDatasourceService : IGatewayClusterDatasourceService, IDisposable
{
    private static readonly string GatewayClusterDatasourcesUrl = FabricUrlBuilder.ForPowerBiV2Api()
        .WithLiteralPath("myorg/me/gatewayClusterDatasources")
        .Build();

    private readonly ILogger<GatewayClusterDatasourceService> _logger;
    private readonly IAuthenticationService _authService;
    private readonly HttpClient _httpClient;
    private readonly JsonSerializerOptions _jsonOptions;

    // Cache for gateway cluster datasources to avoid repeated API calls
    private List<CloudDatasourceInfo>? _cachedDatasources;
    private DateTime _cacheExpiration = DateTime.MinValue;
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

    public GatewayClusterDatasourceService(
        ILogger<GatewayClusterDatasourceService> logger,
        IAuthenticationService authService)
    {
        _logger = logger;
        _authService = authService;
        _httpClient = new HttpClient();
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Converters = { new JsonStringEnumConverter() }
        };
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

        await EnsureAuthenticationAsync();

        _logger.LogDebug("Fetching cloud datasources from Power BI v2.0 API");

        var response = await _httpClient.GetAsync(GatewayClusterDatasourcesUrl);

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            _logger.LogError("Failed to get cloud datasources. Status: {StatusCode}, Content: {Content}",
                response.StatusCode, errorContent);
            throw new HttpRequestException($"Failed to get cloud datasources: {response.StatusCode} - {errorContent}");
        }

        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<GatewayClusterDatasourcesResponse>(content, _jsonOptions);

        var datasources = result?.Value ?? new List<CloudDatasourceInfo>();

        _logger.LogInformation("Retrieved {Count} cloud datasources from Power BI v2.0 API", datasources.Count);

        // Update cache
        _cachedDatasources = datasources;
        _cacheExpiration = DateTime.UtcNow.Add(CacheDuration);

        return datasources;
    }

    private async Task EnsureAuthenticationAsync()
    {
        var tokenResult = await _authService.GetAccessTokenAsync();

        if (tokenResult.Contains("No valid authentication") || tokenResult.Contains("expired"))
        {
            throw new UnauthorizedAccessException(Messages.AuthenticationRequired);
        }

        if (!tokenResult.StartsWith("eyJ")) // Basic JWT token validation
        {
            throw new UnauthorizedAccessException(Messages.InvalidTokenFormat);
        }

        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenResult);
    }

    public void Dispose()
    {
        _httpClient.Dispose();
        GC.SuppressFinalize(this);
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
