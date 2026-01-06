using System.Net;
using System.Text.Json;
using DataFactory.MCP.Configuration;

namespace DataFactory.MCP.Extensions;

/// <summary>
/// Custom exception for API errors with detailed information
/// </summary>
public class FabricApiException : HttpRequestException
{
    public new HttpStatusCode StatusCode { get; }
    public string? ResponseContent { get; }
    public TimeSpan? RetryAfter { get; }

    public FabricApiException(HttpStatusCode statusCode, string message, string? responseContent = null, TimeSpan? retryAfter = null)
        : base(message)
    {
        StatusCode = statusCode;
        ResponseContent = responseContent;
        RetryAfter = retryAfter;
    }

    public bool IsRateLimited => StatusCode == HttpStatusCode.TooManyRequests;
    public bool IsUnauthorized => StatusCode == HttpStatusCode.Unauthorized;
    public bool IsForbidden => StatusCode == HttpStatusCode.Forbidden;
    /// <summary>
    /// Returns true if the error is an authentication/authorization error (401 or 403)
    /// </summary>
    public bool IsAuthenticationError => IsUnauthorized || IsForbidden;
    public bool IsNotFound => StatusCode == HttpStatusCode.NotFound;
    public bool IsTransient => StatusCode is HttpStatusCode.TooManyRequests
        or HttpStatusCode.ServiceUnavailable
        or HttpStatusCode.GatewayTimeout
        or HttpStatusCode.BadGateway;
}

/// <summary>
/// Extension methods for HttpResponseMessage to standardize response handling across all services.
/// Eliminates duplicated deserialization and error handling patterns.
/// </summary>
public static class HttpResponseMessageExtensions
{
    /// <summary>
    /// Reads and deserializes the response content as JSON, throwing on failure.
    /// </summary>
    /// <typeparam name="T">The type to deserialize to</typeparam>
    /// <param name="response">The HTTP response</param>
    /// <param name="options">JSON serializer options (uses FabricApi options if null)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The deserialized object</returns>
    /// <exception cref="FabricApiException">Thrown when the response indicates failure</exception>
    public static async Task<T?> ReadAsJsonAsync<T>(
        this HttpResponseMessage response,
        JsonSerializerOptions? options = null,
        CancellationToken cancellationToken = default) where T : class
    {
        await response.EnsureSuccessOrThrowAsync(cancellationToken);

        var content = await response.Content.ReadAsStringAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(content))
        {
            return null;
        }

        return JsonSerializer.Deserialize<T>(content, options ?? JsonSerializerOptionsProvider.FabricApi);
    }

    /// <summary>
    /// Reads and deserializes the response content as JSON, returning a default value on failure.
    /// Useful for methods that should return empty collections instead of throwing.
    /// </summary>
    /// <typeparam name="T">The type to deserialize to</typeparam>
    /// <param name="response">The HTTP response</param>
    /// <param name="defaultValue">Value to return on failure</param>
    /// <param name="options">JSON serializer options (uses FabricApi options if null)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The deserialized object or default value on failure</returns>
    public static async Task<T> ReadAsJsonOrDefaultAsync<T>(
        this HttpResponseMessage response,
        T defaultValue,
        JsonSerializerOptions? options = null,
        CancellationToken cancellationToken = default) where T : class
    {
        if (!response.IsSuccessStatusCode)
        {
            return defaultValue;
        }

        var content = await response.Content.ReadAsStringAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(content))
        {
            return defaultValue;
        }

        return JsonSerializer.Deserialize<T>(content, options ?? JsonSerializerOptionsProvider.FabricApi)
            ?? defaultValue;
    }

    /// <summary>
    /// Ensures the response is successful, throwing a detailed FabricApiException on failure.
    /// Handles rate limiting by extracting Retry-After header.
    /// </summary>
    /// <param name="response">The HTTP response</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <exception cref="FabricApiException">Thrown when the response indicates failure</exception>
    public static async Task EnsureSuccessOrThrowAsync(
        this HttpResponseMessage response,
        CancellationToken cancellationToken = default)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
        var retryAfter = GetRetryAfter(response);

        var message = response.StatusCode switch
        {
            HttpStatusCode.TooManyRequests => $"Rate limited. Retry after {retryAfter?.TotalSeconds ?? 60} seconds. {errorContent}",
            HttpStatusCode.Unauthorized => $"Authentication failed. Please re-authenticate. {errorContent}",
            HttpStatusCode.Forbidden => $"Access denied. Check permissions. {errorContent}",
            HttpStatusCode.NotFound => $"Resource not found. {errorContent}",
            HttpStatusCode.BadRequest => $"Invalid request. {errorContent}",
            HttpStatusCode.ServiceUnavailable => $"Service temporarily unavailable. {errorContent}",
            _ => $"API request failed with status {(int)response.StatusCode} ({response.StatusCode}). {errorContent}"
        };

        throw new FabricApiException(response.StatusCode, message, errorContent, retryAfter);
    }

    /// <summary>
    /// Tries to read the response as JSON, returning success/failure result.
    /// </summary>
    public static async Task<(bool Success, T? Value, FabricApiException? Error)> TryReadAsJsonAsync<T>(
        this HttpResponseMessage response,
        JsonSerializerOptions? options = null,
        CancellationToken cancellationToken = default) where T : class
    {
        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
            var retryAfter = GetRetryAfter(response);
            var error = new FabricApiException(
                response.StatusCode,
                $"API request failed: {response.StatusCode}",
                errorContent,
                retryAfter);
            return (false, null, error);
        }

        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        var value = JsonSerializer.Deserialize<T>(content, options ?? JsonSerializerOptionsProvider.FabricApi);
        return (true, value, null);
    }

    /// <summary>
    /// Checks if the response indicates a transient failure that could be retried.
    /// </summary>
    public static bool IsTransientFailure(this HttpResponseMessage response)
    {
        return response.StatusCode is HttpStatusCode.TooManyRequests
            or HttpStatusCode.ServiceUnavailable
            or HttpStatusCode.GatewayTimeout
            or HttpStatusCode.BadGateway
            or HttpStatusCode.RequestTimeout;
    }

    /// <summary>
    /// Checks if the response indicates rate limiting (429).
    /// </summary>
    public static bool IsRateLimited(this HttpResponseMessage response)
    {
        return response.StatusCode == HttpStatusCode.TooManyRequests;
    }

    /// <summary>
    /// Gets the Retry-After header value if present.
    /// </summary>
    public static TimeSpan? GetRetryAfter(this HttpResponseMessage response)
    {
        if (response.Headers.RetryAfter?.Delta.HasValue == true)
        {
            return response.Headers.RetryAfter.Delta.Value;
        }

        if (response.Headers.RetryAfter?.Date.HasValue == true)
        {
            var retryDate = response.Headers.RetryAfter.Date.Value;
            var delay = retryDate - DateTimeOffset.UtcNow;
            return delay > TimeSpan.Zero ? delay : TimeSpan.FromSeconds(1);
        }

        // Default retry delay for rate limiting
        if (response.StatusCode == HttpStatusCode.TooManyRequests)
        {
            return TimeSpan.FromSeconds(60);
        }

        return null;
    }

    /// <summary>
    /// Reads response content as bytes, throwing on failure.
    /// </summary>
    public static async Task<byte[]> ReadAsBytesAsync(
        this HttpResponseMessage response,
        CancellationToken cancellationToken = default)
    {
        await response.EnsureSuccessOrThrowAsync(cancellationToken);
        return await response.Content.ReadAsByteArrayAsync(cancellationToken);
    }
}
