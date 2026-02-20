using System.Text.Json.Nodes;

namespace DataFactory.MCP.Infrastructure.McpApps;

/// <summary>
/// Options for MCP Apps UI rendering behavior.
/// </summary>
public sealed class McpAppUIOptions
{
    /// <summary>
    /// Gets or sets the Content Security Policy options.
    /// Default is an empty object allowing default CSP.
    /// </summary>
    public JsonObject Csp { get; set; } = [];

    /// <summary>
    /// Gets or sets whether the UI prefers a border around the iframe.
    /// Default is false.
    /// </summary>
    public bool PrefersBorder { get; set; } = false;

    /// <summary>
    /// Creates a default UI options instance.
    /// </summary>
    public static McpAppUIOptions Default => new();

    /// <summary>
    /// Converts the options to a JsonObject for the _meta.ui property.
    /// </summary>
    public JsonObject ToMetaUIObject()
    {
        return new JsonObject
        {
            ["csp"] = Csp.DeepClone(),
            ["prefersBorder"] = PrefersBorder
        };
    }
}
