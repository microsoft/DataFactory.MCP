using DataFactory.MCP.Infrastructure.McpApps;
using DataFactory.MCP.Resources.McpApps;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using System.ComponentModel;

namespace DataFactory.MCP.Tools;

/// <summary>
/// MCP Apps tool for adding a new connection via interactive UI.
/// </summary>
[McpServerToolType]
public class AddConnectionUITool
{
    /// <summary>
    /// Opens the Add Connection form UI.
    /// </summary>
    [McpServerTool(Name = "add_connection_ui")]
    [McpMeta("ui", JsonValue = """{"resourceUri": "ui://datafactory/add-connection"}""")]
    [Description("Opens an interactive form to add a new data connection")]
    public static CallToolResult AddConnectionUI()
    {
        return McpAppsExtensions.CreateToolResultWithUI(
            AddConnectionResource.ResourceUri,
            "Opening Add Connection form...");
    }
}
