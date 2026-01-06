using DataFactory.MCP.Abstractions.Interfaces;
using DataFactory.MCP.Configuration;
using DataFactory.MCP.Extensions;
using DataFactory.MCP.Infrastructure.Http;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;

namespace DataFactory.MCP.Abstractions;

/// <summary>
/// Abstract base class for Microsoft Fabric API services providing common functionality.
/// Uses IHttpClientFactory for proper HttpClient lifecycle management.
/// Authentication is handled automatically by FabricAuthenticationHandler in the HTTP pipeline.
/// </summary>
public abstract class FabricServiceBase
{
    protected readonly HttpClient HttpClient;
    protected readonly ILogger Logger;
    protected readonly IValidationService ValidationService;
    protected static JsonSerializerOptions JsonOptions => JsonSerializerOptionsProvider.FabricApi;

    protected FabricServiceBase(
        IHttpClientFactory httpClientFactory,
        ILogger logger,
        IValidationService validationService)
    {
        HttpClient = httpClientFactory.CreateClient(HttpClientNames.FabricApi);
        Logger = logger;
        ValidationService = validationService;
    }

    /// <summary>
    /// Validates GUIDs before making API calls
    /// </summary>
    protected void ValidateGuids(params (string value, string name)[] guids)
    {
        foreach (var (value, name) in guids)
        {
            ValidationService.ValidateGuid(value, name);
        }
    }

    protected async Task<T?> GetAsync<T>(string endpoint, string? continuationToken = null) where T : class
    {
        var url = FabricUrlBuilder.ForFabricApi()
            .WithLiteralPath(endpoint)
            .WithContinuationToken(continuationToken)
            .Build();

        Logger.LogInformation("Fetching from: {Url}", url);

        var response = await HttpClient.GetAsync(url);
        return await response.ReadAsJsonAsync<T>(JsonOptions);
    }

    protected async Task<T?> PostAsync<T>(string endpoint, object request) where T : class
    {
        var url = FabricUrlBuilder.ForFabricApi()
            .WithLiteralPath(endpoint)
            .Build();
        Logger.LogInformation("Posting to: {Url}", url);

        var jsonContent = JsonSerializer.Serialize(request, JsonOptions);
        var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

        var response = await HttpClient.PostAsync(url, content);
        return await response.ReadAsJsonAsync<T>(JsonOptions);
    }

    /// <summary>
    /// Posts a request and returns the response as a byte array (for binary responses like Arrow format)
    /// </summary>
    protected async Task<byte[]> PostAsBytesAsync(string endpoint, object request)
    {
        var url = FabricUrlBuilder.ForFabricApi()
            .WithLiteralPath(endpoint)
            .Build();
        Logger.LogInformation("Posting to: {Url}", url);

        var jsonContent = JsonSerializer.Serialize(request, JsonOptions);
        var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

        var response = await HttpClient.PostAsync(url, content);
        return await response.ReadAsBytesAsync();
    }

    /// <summary>
    /// Posts a request expecting no content response (204). Returns true on success, false on failure.
    /// Logs errors but does not throw on failure.
    /// </summary>
    protected async Task<bool> PostNoContentAsync(string endpoint, object request)
    {
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

        var (_, _, error) = await response.TryReadAsJsonAsync<object>(JsonOptions);
        Logger.LogError("API POST request failed. Status: {StatusCode}, Content: {Content}",
            error?.StatusCode, error?.ResponseContent);
        return false;
    }
}
