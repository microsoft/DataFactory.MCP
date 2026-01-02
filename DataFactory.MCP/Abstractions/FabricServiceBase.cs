using DataFactory.MCP.Abstractions.Interfaces;
using DataFactory.MCP.Models;
using Microsoft.Extensions.Logging;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DataFactory.MCP.Abstractions;

/// <summary>
/// Abstract base class for Microsoft Fabric API services providing common functionality
/// </summary>
public abstract class FabricServiceBase : IDisposable
{
    protected readonly HttpClient HttpClient;
    protected readonly ILogger Logger;
    protected readonly IAuthenticationService AuthService;
    protected readonly IValidationService ValidationService;
    protected readonly JsonSerializerOptions JsonOptions;

    protected FabricServiceBase(
        ILogger logger,
        IAuthenticationService authService,
        IValidationService validationService)
    {
        HttpClient = new HttpClient();
        Logger = logger;
        AuthService = authService;
        ValidationService = validationService;

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

    /// <summary>
    /// Validates GUIDs and ensures authentication - common pattern across all Fabric services
    /// </summary>
    protected async Task ValidateGuidsAndAuthenticateAsync(params (string value, string name)[] guids)
    {
        foreach (var (value, name) in guids)
        {
            ValidationService.ValidateGuid(value, name);
        }
        await EnsureAuthenticationAsync();
    }

    protected async Task<T?> GetAsync<T>(string endpoint, string? continuationToken = null) where T : class
    {
        await EnsureAuthenticationAsync();

        var url = FabricUrlBuilder.ForFabricApi()
            .WithLiteralPath(endpoint)
            .WithContinuationToken(continuationToken)
            .Build();

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

    protected async Task<T?> PostAsync<T>(string endpoint, object request) where T : class
    {
        await EnsureAuthenticationAsync();

        var url = FabricUrlBuilder.ForFabricApi()
            .WithLiteralPath(endpoint)
            .Build();
        Logger.LogInformation("Posting to: {Url}", url);

        // Serialize object to JSON content
        var jsonContent = JsonSerializer.Serialize(request, JsonOptions);
        var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

        var response = await HttpClient.PostAsync(url, content);

        if (response.IsSuccessStatusCode)
        {
            var responseContent = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<T>(responseContent, JsonOptions);
        }
        else
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            Logger.LogError("API POST request failed. Status: {StatusCode}, Content: {Content}",
                response.StatusCode, errorContent);

            throw new HttpRequestException($"API POST request failed: {response.StatusCode} - {errorContent}");
        }
    }

    /// <summary>
    /// Posts a request and returns the response as a byte array (for binary responses like Arrow format)
    /// </summary>
    protected async Task<byte[]> PostAsBytesAsync(string endpoint, object request)
    {
        await EnsureAuthenticationAsync();

        var url = FabricUrlBuilder.ForFabricApi()
            .WithLiteralPath(endpoint)
            .Build();
        Logger.LogInformation("Posting to: {Url}", url);

        var jsonContent = JsonSerializer.Serialize(request, JsonOptions);
        var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

        var response = await HttpClient.PostAsync(url, content);

        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadAsByteArrayAsync();
        }
        else
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            Logger.LogError("API POST request failed. Status: {StatusCode}, Content: {Content}",
                response.StatusCode, errorContent);

            throw new HttpRequestException($"API POST request failed: {response.StatusCode} - {errorContent}");
        }
    }

    /// <summary>
    /// Posts a request expecting no content response (204). Returns true on success, false on failure.
    /// </summary>
    protected async Task<bool> PostNoContentAsync(string endpoint, object request)
    {
        await EnsureAuthenticationAsync();

        var url = FabricUrlBuilder.ForFabricApi()
            .WithLiteralPath(endpoint)
            .Build();
        Logger.LogInformation("Posting to: {Url}", url);

        var jsonContent = JsonSerializer.Serialize(request, JsonOptions);
        var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

        var response = await HttpClient.PostAsync(url, content);

        if (response.IsSuccessStatusCode)
        {
            return true;
        }
        else
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            Logger.LogError("API POST request failed. Status: {StatusCode}, Content: {Content}",
                response.StatusCode, errorContent);

            return false;
        }
    }

    public void Dispose()
    {
        HttpClient?.Dispose();
    }
}
