using DataFactory.MCP.Abstractions.Interfaces;
using DataFactory.MCP.Models;
using DataFactory.MCP.Models.Gateway;
using Microsoft.Extensions.Logging;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DataFactory.MCP.Services;

/// <summary>
/// Service for interacting with Microsoft Fabric Gateways API
/// </summary>
public class FabricGatewayService : IFabricGatewayService
{
    private const string BaseUrl = "https://api.fabric.microsoft.com/v1";
    private readonly HttpClient _httpClient;
    private readonly ILogger<FabricGatewayService> _logger;
    private readonly IAuthenticationService _authService;
    private readonly JsonSerializerOptions _jsonOptions;

    public FabricGatewayService(
        HttpClient httpClient,
        ILogger<FabricGatewayService> logger,
        IAuthenticationService authService)
    {
        _httpClient = httpClient;
        _logger = logger;
        _authService = authService;

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Converters = { new JsonStringEnumConverter() }
        };
    }

    public async Task<ListGatewaysResponse> ListGatewaysAsync(string? continuationToken = null)
    {
        try
        {
            await EnsureAuthenticationAsync();

            var url = $"{BaseUrl}/gateways";
            if (!string.IsNullOrEmpty(continuationToken))
            {
                url += $"?continuationToken={Uri.EscapeDataString(continuationToken)}";
            }

            _logger.LogInformation("Fetching gateways from: {Url}", url);

            var response = await _httpClient.GetAsync(url);

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var gatewaysResponse = JsonSerializer.Deserialize<ListGatewaysResponse>(content, _jsonOptions);

                _logger.LogInformation("Successfully retrieved {Count} gateways", gatewaysResponse?.Value?.Count ?? 0);
                return gatewaysResponse ?? new ListGatewaysResponse();
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Failed to fetch gateways. Status: {StatusCode}, Content: {Content}",
                    response.StatusCode, errorContent);

                throw new HttpRequestException($"Failed to fetch gateways: {response.StatusCode} - {errorContent}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching gateways");
            throw;
        }
    }

    public async Task<Gateway?> GetGatewayAsync(string gatewayId)
    {
        try
        {
            // The Fabric API doesn't have a direct get gateway by ID endpoint,
            // so we'll list all gateways and find the specific one
            var allGateways = await ListGatewaysAsync();
            return allGateways.Value.FirstOrDefault(g => g.Id.Equals(gatewayId, StringComparison.OrdinalIgnoreCase));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching gateway {GatewayId}", gatewayId);
            throw;
        }
    }

    private async Task EnsureAuthenticationAsync()
    {
        try
        {
            var tokenResult = await _authService.GetAccessTokenAsync();

            if (tokenResult.Contains("No valid authentication") || tokenResult.Contains("expired"))
            {
                throw new UnauthorizedAccessException("Valid authentication token is required. Please authenticate first.");
            }

            // Extract the actual token from the result
            // In a real implementation, you might want to modify the IAuthenticationService 
            // to return structured data instead of strings
            if (!tokenResult.StartsWith("eyJ")) // Basic JWT token validation
            {
                throw new UnauthorizedAccessException("Invalid access token format.");
            }

            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenResult);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set authentication for Fabric API");
            throw;
        }
    }
}
