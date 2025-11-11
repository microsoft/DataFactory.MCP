using ModelContextProtocol.Server;
using System.ComponentModel;
using DataFactory.MCP.Abstractions.Interfaces;
using DataFactory.MCP.Extensions;
using DataFactory.MCP.Factories;
using DataFactory.MCP.Models;
using System.Text.Json;

namespace DataFactory.MCP.Tools;

[McpServerToolType]
public class WorkspacesTool
{
    private readonly IFabricWorkspaceService _workspaceService;

    public WorkspacesTool(IFabricWorkspaceService workspaceService)
    {
        _workspaceService = workspaceService;
    }

    [McpServerTool, Description(@"Lists all workspaces the user has permission for. Returns workspaces filtered by the specified roles if provided.")]
    public async Task<string> ListWorkspacesAsync(
        [Description("A list of roles. Separate values using a comma (e.g., 'Admin,Member,Contributor,Viewer'). If not provided, all workspaces are returned.")] string? roles = null,
        [Description("A token for retrieving the next page of results (optional)")] string? continuationToken = null,
        [Description("Include workspace-specific API endpoints in the response (true/false, optional)")] bool? preferWorkspaceSpecificEndpoints = null)
    {
        try
        {
            var response = await _workspaceService.ListWorkspacesAsync(roles, continuationToken, preferWorkspaceSpecificEndpoints);

            if (!response.Value.Any())
            {
                return Messages.NoWorkspacesFound;
            }

            var result = new
            {
                TotalCount = response.Value.Count,
                ContinuationToken = response.ContinuationToken,
                ContinuationUri = response.ContinuationUri,
                HasMoreResults = !string.IsNullOrEmpty(response.ContinuationToken),
                FilteredByRoles = !string.IsNullOrEmpty(roles),
                Roles = roles,
                IncludesApiEndpoints = preferWorkspaceSpecificEndpoints == true,
                Workspaces = response.Value.Select(w => w.ToFormattedInfo())
            };

            return result.ToMcpJson();
        }
        catch (UnauthorizedAccessException ex)
        {
            return ErrorResponseFactory.CreateAuthenticationError(ex.Message).ToMcpJson();
        }
        catch (HttpRequestException ex)
        {
            return ErrorResponseFactory.CreateHttpError(ex.Message).ToMcpJson();
        }
        catch (Exception ex)
        {
            return ErrorResponseFactory.CreateOperationError("listing workspaces", ex.Message).ToMcpJson();
        }
    }
}