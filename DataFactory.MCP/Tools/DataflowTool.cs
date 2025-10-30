using ModelContextProtocol.Server;
using System.ComponentModel;
using DataFactory.MCP.Abstractions.Interfaces;
using DataFactory.MCP.Extensions;
using DataFactory.MCP.Models;
using DataFactory.MCP.Models.Dataflow;
using System.Text.Json;

namespace DataFactory.MCP.Tools;

[McpServerToolType]
public class DataflowTool
{
    private readonly IFabricDataflowService _dataflowService;

    public DataflowTool(IFabricDataflowService dataflowService)
    {
        _dataflowService = dataflowService;
    }

    [McpServerTool, Description(@"Returns a list of Dataflows from the specified workspace. This API supports pagination.")]
    public async Task<string> ListDataflowsAsync(
        [Description("The workspace ID to list dataflows from (required)")] string workspaceId,
        [Description("A token for retrieving the next page of results (optional)")] string? continuationToken = null)
    {
        try
        {
            if (string.IsNullOrEmpty(workspaceId))
            {
                return "Error: Workspace ID is required.";
            }

            var response = await _dataflowService.ListDataflowsAsync(workspaceId, continuationToken);

            if (!response.Value.Any())
            {
                return $"No dataflows found in workspace '{workspaceId}'.";
            }

            var result = new
            {
                WorkspaceId = workspaceId,
                DataflowCount = response.Value.Count,
                ContinuationToken = response.ContinuationToken,
                ContinuationUri = response.ContinuationUri,
                HasMoreResults = !string.IsNullOrEmpty(response.ContinuationToken),
                Dataflows = response.Value.Select(d => d.ToFormattedInfo())
            };

            return JsonSerializer.Serialize(result, new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
        }
        catch (ArgumentException ex)
        {
            return $"Error: {ex.Message}";
        }
        catch (UnauthorizedAccessException ex)
        {
            return string.Format(Messages.AuthenticationErrorTemplate, ex.Message);
        }
        catch (HttpRequestException ex)
        {
            return string.Format(Messages.ApiRequestFailedTemplate, ex.Message);
        }
        catch (Exception ex)
        {
            return $"Error listing dataflows: {ex.Message}";
        }
    }

    [McpServerTool, Description(@"Creates a Dataflow in the specified workspace. The workspace must be on a supported Fabric capacity.")]
    public async Task<string> CreateDataflowAsync(
        [Description("The workspace ID where the dataflow will be created (required)")] string workspaceId,
        [Description("The Dataflow display name (required)")] string displayName,
        [Description("The Dataflow description (optional, max 256 characters)")] string? description = null,
        [Description("The folder ID where the dataflow will be created (optional, defaults to workspace root)")] string? folderId = null)
    {
        try
        {
            var request = new CreateDataflowRequest
            {
                DisplayName = displayName,
                Description = description,
                FolderId = folderId
            };

            var response = await _dataflowService.CreateDataflowAsync(workspaceId, request);

            var result = new
            {
                Success = true,
                Message = $"Dataflow '{displayName}' created successfully",
                DataflowId = response.Id,
                DisplayName = response.DisplayName,
                Description = response.Description,
                Type = response.Type,
                WorkspaceId = response.WorkspaceId,
                FolderId = response.FolderId,
                CreatedAt = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")
            };

            return JsonSerializer.Serialize(result, new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
        }
        catch (ArgumentException ex)
        {
            return $"Error: {ex.Message}";
        }
        catch (UnauthorizedAccessException ex)
        {
            return string.Format(Messages.AuthenticationErrorTemplate, ex.Message);
        }
        catch (HttpRequestException ex) when (ex.Message.Contains("403") || ex.Message.Contains("Forbidden"))
        {
            return $"Error: Access denied or feature not available. The workspace must be on a supported Fabric capacity to create dataflows. Details: {ex.Message}";
        }
        catch (HttpRequestException ex)
        {
            return string.Format(Messages.ApiRequestFailedTemplate, ex.Message);
        }
        catch (Exception ex)
        {
            return $"Error creating dataflow: {ex.Message}";
        }
    }
}