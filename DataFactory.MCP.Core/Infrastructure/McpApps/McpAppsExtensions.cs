using ModelContextProtocol.Protocol;
using System.Text.Json.Nodes;

namespace DataFactory.MCP.Infrastructure.McpApps;

/// <summary>
/// Extension methods for creating MCP Apps tool responses.
/// </summary>
public static class McpAppsExtensions
{
    /// <summary>
    /// Creates a CallToolResult that opens a UI resource.
    /// </summary>
    /// <param name="resource">The UI resource to open</param>
    /// <param name="fallbackText">Text shown if client doesn't support MCP Apps</param>
    public static CallToolResult CreateToolResultWithUI(
        this IMcpAppResource resource,
        string? fallbackText = null)
    {
        return CreateToolResultWithUI(resource.Uri, fallbackText ?? $"Opening {resource.Name}...");
    }

    /// <summary>
    /// Creates a CallToolResult that opens a UI resource by URI.
    /// </summary>
    /// <param name="resourceUri">The UI resource URI (e.g., "ui://datafactory/my-ui")</param>
    /// <param name="fallbackText">Text shown if client doesn't support MCP Apps</param>
    public static CallToolResult CreateToolResultWithUI(
        string resourceUri,
        string fallbackText = "Opening UI...")
    {
        return new CallToolResult
        {
            Content = [new TextContentBlock { Text = fallbackText }],
            Meta = new JsonObject
            {
                ["ui"] = new JsonObject
                {
                    ["resourceUri"] = resourceUri
                }
            }
        };
    }

    /// <summary>
    /// Creates a CallToolResult with UI and additional content blocks.
    /// </summary>
    /// <param name="resourceUri">The UI resource URI</param>
    /// <param name="content">Additional content blocks to include</param>
    public static CallToolResult CreateToolResultWithUI(
        string resourceUri,
        params ContentBlock[] content)
    {
        return new CallToolResult
        {
            Content = content,
            Meta = new JsonObject
            {
                ["ui"] = new JsonObject
                {
                    ["resourceUri"] = resourceUri
                }
            }
        };
    }

    /// <summary>
    /// Gets the McpMeta JSON value string for a UI resource URI.
    /// Use with [McpMeta("ui", JsonValue = ...)] attribute.
    /// </summary>
    /// <param name="resourceUri">The UI resource URI</param>
    /// <returns>JSON string for McpMeta attribute</returns>
    public static string GetMcpMetaJsonValue(string resourceUri)
    {
        return $$"""{"resourceUri": "{{resourceUri}}"}""";
    }
}
