using DataFactory.MCP.Infrastructure.McpApps;
using DataFactory.MCP.Resources.McpApps;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using System.ComponentModel;

namespace DataFactory.MCP.Tools;

/// <summary>
/// Simple POC tool that shows a UI for user input and returns it to the agent context.
/// </summary>
[McpServerToolType]
public class UserInputUITool
{
    /// <summary>
    /// Shows a simple form for user text input. The input is returned to the agent context.
    /// </summary>
    [McpServerTool(Name = "user_input_ui")]
    [McpMeta("ui", JsonValue = $$"""{"resourceUri": "{{UserInputResource.ResourceUri}}"}""")]
    [Description("Shows a simple form for user text input. Use this to get free-form input from the user.")]
    public static CallToolResult ShowUserInputForm()
    {
        return McpAppsExtensions.CreateToolResultWithUI(
            resourceUri: UserInputResource.ResourceUri,
            fallbackText: "User input form displayed. Waiting for user to submit their input."
        );
    }
}
