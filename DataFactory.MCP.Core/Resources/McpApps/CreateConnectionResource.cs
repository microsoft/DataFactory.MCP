using DataFactory.MCP.Infrastructure.McpApps;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using System.ComponentModel;

namespace DataFactory.MCP.Resources.McpApps;

/// <summary>
/// MCP Apps resource for the Create Connection form.
/// </summary>
public class CreateConnectionResource : McpAppResourceBase
{
    /// <summary>
    /// The URI for this resource.
    /// </summary>
    public const string ResourceUri = "ui://datafactory/create-connection";

    /// <inheritdoc />
    public override string Uri => ResourceUri;

    /// <inheritdoc />
    public override string Name => "Create Connection";

    /// <inheritdoc />
    public override string? Description => "Form to create a new data source connection";

    /// <inheritdoc />
    public override string GetHtmlContent()
    {
        return McpAppResourceLoader.LoadFromMonorepo("create-connection");
    }
}

/// <summary>
/// MCP Server resource handler for CreateConnectionResource.
/// </summary>
[McpServerResourceType]
public class CreateConnectionResourceHandler
{
    private static readonly CreateConnectionResource _resource = new();

    [McpServerResource(
        UriTemplate = "ui://datafactory/create-connection",
        Name = "Create Connection",
        MimeType = "text/html;profile=mcp-app")]
    [McpMeta("ui", JsonValue = """{"csp": {}, "prefersBorder": false}""")]
    [Description("Form to create a new data source connection")]
    public static ReadResourceResult GetCreateConnection() => _resource.ToReadResourceResult();
}
