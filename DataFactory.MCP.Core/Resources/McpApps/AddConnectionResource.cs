using DataFactory.MCP.Infrastructure.McpApps;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using System.ComponentModel;

namespace DataFactory.MCP.Resources.McpApps;

/// <summary>
/// MCP Apps resource for the Add Connection form.
/// </summary>
public class AddConnectionResource : McpAppResourceBase
{
    /// <summary>
    /// The URI for this resource.
    /// </summary>
    public const string ResourceUri = "ui://datafactory/add-connection";

    /// <inheritdoc />
    public override string Uri => ResourceUri;

    /// <inheritdoc />
    public override string Name => "Add Connection";

    /// <inheritdoc />
    public override string? Description => "Form to create a new data connection";

    /// <inheritdoc />
    public override string GetHtmlContent()
    {
        return McpAppResourceLoader.LoadAndBundle("AddConnection", "add-connection");
    }
}

/// <summary>
/// MCP Server resource handler for AddConnectionResource.
/// </summary>
[McpServerResourceType]
public class AddConnectionResourceHandler
{
    private static readonly AddConnectionResource _resource = new();

    [McpServerResource(
        UriTemplate = "ui://datafactory/add-connection",
        Name = "Add Connection",
        MimeType = "text/html;profile=mcp-app")]
    [McpMeta("ui", JsonValue = """{"csp": {}, "prefersBorder": false}""")]
    [Description("Form to create a new data connection")]
    public static ReadResourceResult GetAddConnection() => _resource.ToReadResourceResult();
}
