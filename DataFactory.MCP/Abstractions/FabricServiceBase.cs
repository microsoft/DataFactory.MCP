using DataFactory.MCP.Abstractions.Interfaces;
using DataFactory.MCP.Models;
using Microsoft.Extensions.Logging;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DataFactory.MCP.Abstractions;

/// <summary>
/// Abstract base class for Microsoft Fabric API services providing common functionality
/// </summary>
public abstract class FabricServiceBase : IDisposable
{
    protected const string BaseUrl = "https://api.fabric.microsoft.com/v1";
    protected readonly HttpClient HttpClient;
    protected readonly ILogger Logger;
    protected readonly IAuthenticationService AuthService;
    protected readonly JsonSerializerOptions JsonOptions;

    protected FabricServiceBase(
        ILogger logger,
        IAuthenticationService authService)
    {
        HttpClient = new HttpClient();
        Logger = logger;
        AuthService = authService;

        JsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Converters = { new JsonStringEnumConverter() }
        };
    }

    protected async Task EnsureAuthenticationAsync()
    {
        try
        {
            var tokenResult = await AuthService.GetAccessTokenAsync();

            if (tokenResult.Contains("No valid authentication") || tokenResult.Contains("expired"))
            {
                throw new UnauthorizedAccessException(Messages.AuthenticationRequired);
            }

            if (!tokenResult.StartsWith("eyJ")) // Basic JWT token validation
            {
                throw new UnauthorizedAccessException(Messages.InvalidTokenFormat);
            }

            HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenResult);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to set authentication for Fabric API");
            throw;
        }
    }

    protected async Task<T?> GetAsync<T>(string endpoint, string? continuationToken = null) where T : class
    {
        await EnsureAuthenticationAsync();

        var url = $"{BaseUrl}/{endpoint}";
        if (!string.IsNullOrEmpty(continuationToken))
        {
            url += $"?continuationToken={Uri.EscapeDataString(continuationToken)}";
        }

        Logger.LogInformation("Fetching from: {Url}", url);

        var response = await HttpClient.GetAsync(url);

        if (response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<T>(content, JsonOptions);
        }
        else
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            Logger.LogError("API request failed. Status: {StatusCode}, Content: {Content}",
                response.StatusCode, errorContent);

            throw new HttpRequestException($"API request failed: {response.StatusCode} - {errorContent}");
        }
    }

    public void Dispose()
    {
        HttpClient?.Dispose();
    }
}
