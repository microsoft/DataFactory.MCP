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

    [McpServerTool, Description(@"Executes a query against a dataflow and returns the complete results (all data) in Apache Arrow format. This allows you to run M (Power Query) language queries against data sources connected through the dataflow and get the full dataset.

FORMATTING INSTRUCTION: When displaying results to users, please format the 'table.rows' data as a markdown table using the column names from 'table.columns'. This provides immediate visual representation of the query results.")]
    public async Task<string> ExecuteQueryAsync(
        [Description("The workspace ID containing the dataflow (required)")] string workspaceId,
        [Description("The dataflow ID to execute the query against (required)")] string dataflowId,
        [Description("The name of the query to execute (required)")] string queryName,
        [Description("The M (Power Query) language query to execute. Can be either a raw M expression (which will be auto-wrapped) or a complete section document. Results will be returned as structured data - format the table.rows as a markdown table for user display.")] string customMashupDocument)
    {
        try
        {
            // Auto-wrap the query if it's not already in section format
            var wrappedQuery = WrapQueryIfNeeded(customMashupDocument, queryName);

            var request = new ExecuteDataflowQueryRequest
            {
                QueryName = queryName,
                CustomMashupDocument = wrappedQuery
            };

            var response = await _dataflowService.ExecuteQueryAsync(workspaceId, dataflowId, request);

            if (response.Success)
            {
                // Create comprehensive Arrow data report
                var result = response.CreateArrowDataReport();

                return JsonSerializer.Serialize(result, new JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });
            }
            else
            {
                var errorResult = new
                {
                    Success = false,
                    Error = response.Error,
                    Message = $"Failed to execute query '{queryName}' on dataflow {dataflowId}",
                    WorkspaceId = workspaceId,
                    DataflowId = dataflowId,
                    QueryName = queryName
                };

                return JsonSerializer.Serialize(errorResult, new JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });
            }
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
            return $"Error executing dataflow query: {ex.Message}";
        }
    }

    /// <summary>
    /// Wraps a raw M query in the proper section format if it's not already wrapped
    /// </summary>
    private static string WrapQueryIfNeeded(string query, string queryName)
    {
        if (string.IsNullOrWhiteSpace(query))
            return query;

        // Check if the query already starts with "section" (case-insensitive)
        var trimmedQuery = query.Trim();
        if (trimmedQuery.StartsWith("section ", StringComparison.OrdinalIgnoreCase))
        {
            // Already in section format, return as-is
            return query;
        }

        // Auto-wrap the raw M query in section format
        return $@"section Section1;

shared {queryName} = {query.TrimEnd()};";
    }
}