using ModelContextProtocol.Protocol;

namespace DataFactory.MCP.Infrastructure.McpApps;

/// <summary>
/// Defines the contract for an MCP Apps UI resource.
/// Implementations provide HTML content that can be rendered in MCP clients.
/// </summary>
public interface IMcpAppResource
{
    /// <summary>
    /// Gets the unique URI for this resource (e.g., "ui://datafactory/my-resource").
    /// </summary>
    string Uri { get; }

    /// <summary>
    /// Gets the display name for this resource.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the description of this resource.
    /// </summary>
    string? Description { get; }

    /// <summary>
    /// Gets the HTML content for this resource.
    /// </summary>
    string GetHtmlContent();

    /// <summary>
    /// Creates a ReadResourceResult for this resource.
    /// </summary>
    ReadResourceResult ToReadResourceResult();

    /// <summary>
    /// Creates a Resource metadata object for listing.
    /// </summary>
    Resource ToResourceMetadata();
}
