using DataFactory.MCP.Configuration;

namespace DataFactory.MCP.Infrastructure.Http;

/// <summary>
/// Centralized URL builder for Fabric API endpoints providing consistent URL construction
/// with proper encoding and query parameter handling.
/// </summary>
public class FabricUrlBuilder
{
    private readonly string _baseUrl;
    private readonly List<string> _pathSegments = new();
    private readonly List<(string Key, string Value)> _queryParams = new();

    /// <summary>
    /// Creates a new URL builder with the specified base URL
    /// </summary>
    /// <param name="baseUrl">The base URL (e.g., "https://api.fabric.microsoft.com/v1")</param>
    public FabricUrlBuilder(string baseUrl)
    {
        _baseUrl = baseUrl.TrimEnd('/');
    }

    /// <summary>
    /// Creates a URL builder using the default Fabric API base URL
    /// </summary>
    public static FabricUrlBuilder ForFabricApi() => new(ApiVersions.Fabric.V1BaseUrl);

    /// <summary>
    /// Creates a URL builder using the Azure Resource Manager base URL
    /// </summary>
    public static FabricUrlBuilder ForAzureResourceManager() => new("https://management.azure.com");

    /// <summary>
    /// Creates a URL builder using the Power BI API v2 base URL
    /// </summary>
    public static FabricUrlBuilder ForPowerBiV2Api() => new(ApiVersions.PowerBi.V2BaseUrl);

    /// <summary>
    /// Appends path segments to the URL. Segments are automatically URL-encoded.
    /// </summary>
    /// <param name="segments">Path segments to append</param>
    /// <returns>The builder for method chaining</returns>
    public FabricUrlBuilder WithPath(params string[] segments)
    {
        foreach (var segment in segments)
        {
            if (!string.IsNullOrWhiteSpace(segment))
            {
                _pathSegments.Add(Uri.EscapeDataString(segment.Trim('/')));
            }
        }
        return this;
    }

    /// <summary>
    /// Appends a literal path segment without URL encoding (for pre-defined API paths)
    /// </summary>
    /// <param name="path">The literal path to append</param>
    /// <returns>The builder for method chaining</returns>
    public FabricUrlBuilder WithLiteralPath(string path)
    {
        if (!string.IsNullOrWhiteSpace(path))
        {
            _pathSegments.Add(path.Trim('/'));
        }
        return this;
    }

    /// <summary>
    /// Adds a query parameter. Null or empty values are ignored.
    /// </summary>
    /// <param name="key">The parameter key</param>
    /// <param name="value">The parameter value (will be URL-encoded)</param>
    /// <returns>The builder for method chaining</returns>
    public FabricUrlBuilder WithQueryParam(string key, string? value)
    {
        if (!string.IsNullOrWhiteSpace(key) && !string.IsNullOrWhiteSpace(value))
        {
            _queryParams.Add((key, Uri.EscapeDataString(value)));
        }
        return this;
    }

    /// <summary>
    /// Adds a boolean query parameter. Null values are ignored.
    /// </summary>
    /// <param name="key">The parameter key</param>
    /// <param name="value">The boolean value</param>
    /// <returns>The builder for method chaining</returns>
    public FabricUrlBuilder WithQueryParam(string key, bool? value)
    {
        if (!string.IsNullOrWhiteSpace(key) && value.HasValue)
        {
            _queryParams.Add((key, value.Value.ToString().ToLowerInvariant()));
        }
        return this;
    }

    /// <summary>
    /// Adds an API version query parameter (common for Azure APIs)
    /// </summary>
    /// <param name="version">The API version string</param>
    /// <returns>The builder for method chaining</returns>
    public FabricUrlBuilder WithApiVersion(string version)
    {
        return WithQueryParam("api-version", version);
    }

    /// <summary>
    /// Adds a continuation token query parameter if provided
    /// </summary>
    /// <param name="continuationToken">The continuation token</param>
    /// <returns>The builder for method chaining</returns>
    public FabricUrlBuilder WithContinuationToken(string? continuationToken)
    {
        return WithQueryParam("continuationToken", continuationToken);
    }

    /// <summary>
    /// Builds the relative endpoint path (without base URL)
    /// </summary>
    /// <returns>The relative endpoint path</returns>
    public string BuildEndpoint()
    {
        var path = string.Join("/", _pathSegments);

        if (_queryParams.Count == 0)
        {
            return path;
        }

        var queryString = string.Join("&", _queryParams.Select(p => $"{p.Key}={p.Value}"));
        return $"{path}?{queryString}";
    }

    /// <summary>
    /// Builds the complete URL including base URL
    /// </summary>
    /// <returns>The complete URL</returns>
    public string Build()
    {
        var endpoint = BuildEndpoint();
        return string.IsNullOrEmpty(endpoint) ? _baseUrl : $"{_baseUrl}/{endpoint}";
    }

    /// <summary>
    /// Implicitly converts the builder to a string by calling Build()
    /// </summary>
    public static implicit operator string(FabricUrlBuilder builder) => builder.Build();

    public override string ToString() => Build();
}
