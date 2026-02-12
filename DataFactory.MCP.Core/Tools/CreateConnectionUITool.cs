using DataFactory.MCP.Infrastructure.McpApps;
using DataFactory.MCP.Resources.McpApps;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using System.ComponentModel;

namespace DataFactory.MCP.Tools;

/// <summary>
/// Tool that shows a UI for creating data source connections.
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
    public static CallToolResult ShowCreateConnectionForm()
    {
        return McpAppsExtensions.CreateToolResultWithUI(
            resourceUri: CreateConnectionResource.ResourceUri,
            fallbackText: "Create connection form displayed. Waiting for user to configure the connection."
        );
    }
}
