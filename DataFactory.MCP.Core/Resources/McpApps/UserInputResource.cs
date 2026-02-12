using DataFactory.MCP.Infrastructure.McpApps;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using System.ComponentModel;

namespace DataFactory.MCP.Resources.McpApps;

/// <summary>
/// MCP Apps resource for the User Input POC form.
/// </summary>
public class UserInputResource : McpAppResourceBase
{
    /// <summary>
    /// The URI for this resource.
    /// </summary>
    public const string ResourceUri = "ui://datafactory/user-input";

    /// <inheritdoc />
    public override string Uri => ResourceUri;

    /// <inheritdoc />
    public override string Name => "User Input";

    /// <inheritdoc />
    public override string? Description => "Simple form to capture user input";

    /// <inheritdoc />
    public override string GetHtmlContent()
    {
        return McpAppResourceLoader.LoadFromMonorepo("user-input");
    }
}

/// <summary>
/// MCP Server resource handler for UserInputResource.
/// </summary>
[McpServerResourceType]
public class UserInputResourceHandler
{
    private static readonly UserInputResource _resource = new();

    [McpServerResource(
        UriTemplate = "ui://datafactory/user-input",
        Name = "User Input",
        MimeType = "text/html;profile=mcp-app")]
    [McpMeta("ui", JsonValue = """{"csp": {}, "prefersBorder": false}""")]
    [Description("Simple form to capture user input")]
    public static ReadResourceResult GetUserInput() => _resource.ToReadResourceResult();
}
