using ModelContextProtocol.Protocol;
using System.Text.Json.Nodes;

namespace DataFactory.MCP.Infrastructure.McpApps;

/// <summary>
/// Base class for MCP Apps UI resources.
/// Provides common functionality for creating HTML UI resources.
/// </summary>
public abstract class McpAppResourceBase : IMcpAppResource
{
    /// <inheritdoc />
    public abstract string Uri { get; }

    /// <inheritdoc />
    public abstract string Name { get; }

    /// <inheritdoc />
    public virtual string? Description => null;

    /// <summary>
    /// Gets the UI rendering options for this resource.
    /// Override to customize CSP or border preferences.
    /// </summary>
    protected virtual McpAppUIOptions UIOptions => McpAppUIOptions.Default;

    /// <inheritdoc />
    public abstract string GetHtmlContent();

    /// <inheritdoc />
    public ReadResourceResult ToReadResourceResult()
    {
        return new ReadResourceResult
        {
            Contents =
            [
                new TextResourceContents
                {
                    Uri = Uri,
                    Text = GetHtmlContent(),
                    MimeType = McpAppsConstants.HtmlMimeType,
                    Meta = new JsonObject
                    {
                        ["ui"] = UIOptions.ToMetaUIObject()
                    }
                }
            ]
        };
    }

    /// <inheritdoc />
    public Resource ToResourceMetadata()
    {
        return new Resource
        {
            Uri = Uri,
            Name = Name,
            Description = Description,
            MimeType = McpAppsConstants.HtmlMimeType
        };
    }

    /// <summary>
    /// Helper to build a URI for this server.
    /// </summary>
    /// <param name="resourceName">The resource name (e.g., "test-ui")</param>
    /// <returns>Full URI (e.g., "ui://datafactory/test-ui")</returns>
    protected static string BuildUri(string resourceName)
    {
        return $"{McpAppsConstants.UriScheme}{McpAppsConstants.DefaultServerName}/{resourceName}";
    }
}
