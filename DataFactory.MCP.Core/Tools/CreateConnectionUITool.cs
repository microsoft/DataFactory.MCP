using DataFactory.MCP.Infrastructure.McpApps;
using DataFactory.MCP.Resources.McpApps;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Text.Json.Nodes;

namespace DataFactory.MCP.Tools;

/// <summary>
/// Tool that shows a UI for creating data source connections.
/// No token is passed to the UI — all API calls happen server-side via callServerTool,
/// which uses FabricAuthenticationHandler for auth.
/// </summary>
[McpServerToolType]
public class CreateConnectionUITool
{
    /// <summary>
    /// Shows a form for creating a new data source connection.
    /// </summary>
    [McpServerTool(Name = "create_connection_ui")]
    [McpMeta("ui", JsonValue = $$"""{"resourceUri": "{{CreateConnectionResource.ResourceUri}}"}""")]
    [Description("Shows a form for creating a new data source connection. Use this when the user wants to add a new connection to a data source.")]
    public Task<CallToolResult> ShowCreateConnectionForm()
    {
        return Task.FromResult(new CallToolResult
        {
            Content = [new TextContentBlock { Text = "Create connection form displayed. Waiting for user to configure the connection." }],
            Meta = new JsonObject
            {
                ["ui"] = new JsonObject
                {
                    ["resourceUri"] = CreateConnectionResource.ResourceUri
                }
            }
        });
    }
}

