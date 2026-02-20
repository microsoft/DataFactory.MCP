using DataFactory.MCP.Abstractions.Interfaces;
using DataFactory.MCP.Infrastructure.McpApps;
using DataFactory.MCP.Models;
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
    private readonly IAuthenticationService _authService;

    public CreateConnectionUITool(IAuthenticationService authService)
    {
        _authService = authService;
    }

    /// <summary>
    /// Shows a form for creating a new data source connection.
    /// Validates that the user is authenticated before displaying the UI.
    /// </summary>
    [McpServerTool(Name = "create_connection_ui")]
    [McpMeta("ui", JsonValue = $$"""{"resourceUri": "{{CreateConnectionResource.ResourceUri}}"}""")]
    [Description("Shows a form for creating a new data source connection. Use this when the user wants to add a new connection to a data source.")]
    public async Task<CallToolResult> ShowCreateConnectionForm()
    {
        // Validate authentication before showing the UI
        var token = await _authService.GetAccessTokenAsync();

        var failureMessages = new[]
        {
            Messages.NoAuthenticationFound,
            Messages.NotAuthenticated,
            Messages.TokenNotAvailable,
            Messages.AccessTokenExpired,
            Messages.AccessTokenExpiredCannotRefresh
        };

        if (string.IsNullOrEmpty(token) || failureMessages.Any(msg => token.StartsWith(msg, StringComparison.OrdinalIgnoreCase)))
        {
            return new CallToolResult
            {
                Content = [new TextContentBlock { Text = "Authentication required. Please sign in first using the interactive or device code authentication tool before creating a connection." }],
                IsError = true,
                Meta = new JsonObject
                {
                    ["ui"] = new JsonObject
                    {
                        ["resourceUri"] = CreateConnectionResource.ResourceUri
                    }
                }
            };
        }

        return new CallToolResult
        {
            Content = [new TextContentBlock { Text = "Create connection form displayed. Waiting for user to configure the connection." }],
            Meta = new JsonObject
            {
                ["ui"] = new JsonObject
                {
                    ["resourceUri"] = CreateConnectionResource.ResourceUri
                }
            }
        };
    }
}

