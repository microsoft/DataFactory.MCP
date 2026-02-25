using ModelContextProtocol.Server;
using System.ComponentModel;
using DataFactory.MCP.Abstractions.Interfaces;
using DataFactory.MCP.Extensions;
using DataFactory.MCP.Models.Dataflow;

namespace DataFactory.MCP.Tools.Dataflow;

/// <summary>
/// MCP Tool for managing Microsoft Fabric Dataflows.
/// Handles CRUD operations and definition management.
/// </summary>
[McpServerToolType]
public class DataflowTool
{
    private readonly IFabricDataflowService _dataflowService;
    private readonly IFabricConnectionService _connectionService;
    private readonly IValidationService _validationService;

    public DataflowTool(
        IFabricDataflowService dataflowService,
        IFabricConnectionService connectionService,
        IValidationService validationService)
    {
        _dataflowService = dataflowService;
        _connectionService = connectionService;
        _validationService = validationService;
    }

    [McpServerTool, Description(@"Returns a list of Dataflows from the specified workspace. This API supports pagination.")]
    public async Task<string> ListDataflowsAsync(
        [Description("The workspace ID to list dataflows from (required)")] string workspaceId,
        [Description("A token for retrieving the next page of results (optional)")] string? continuationToken = null)
    {
        try
        {
            _validationService.ValidateRequiredString(workspaceId, nameof(workspaceId));

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

            return result.ToMcpJson();
        }
        catch (ArgumentException ex)
        {
            return ex.ToValidationError().ToMcpJson();
        }
        catch (UnauthorizedAccessException ex)
        {
            return ex.ToAuthenticationError().ToMcpJson();
        }
        catch (HttpRequestException ex)
        {
            return ex.ToHttpError().ToMcpJson();
        }
        catch (Exception ex)
        {
            return ex.ToOperationError("listing dataflows").ToMcpJson();
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

            return result.ToMcpJson();
        }
        catch (ArgumentException ex)
        {
            return ex.ToValidationError().ToMcpJson();
        }
        catch (UnauthorizedAccessException ex)
        {
            return ex.ToAuthenticationError().ToMcpJson();
        }
        catch (HttpRequestException ex) when (ex.Message.Contains("403") || ex.Message.Contains("Forbidden"))
        {
            return new HttpRequestException("Access denied or feature not available. The workspace must be on a supported Fabric capacity to create dataflows.")
                .ToHttpError().ToMcpJson();
        }
        catch (HttpRequestException ex)
        {
            return ex.ToHttpError().ToMcpJson();
        }
        catch (Exception ex)
        {
            return ex.ToOperationError("creating dataflow").ToMcpJson();
        }
    }

    [McpServerTool, Description(@"Adds or replaces connections in an existing dataflow, or clears all connections. When clearExisting is true with no connectionIds, all connections are removed. When clearExisting is true with connectionIds, existing connections are replaced. When clearExisting is false (default), connections are appended.")]
    public async Task<string> AddConnectionToDataflowAsync(
        [Description("The workspace ID containing the dataflow (required)")] string workspaceId,
        [Description("The dataflow ID to update (required)")] string dataflowId,
        [Description("The connection ID(s) to add to the dataflow. Can be a single connection ID string or an array of connection IDs. Optional when clearExisting is true (to clear all connections).")] object? connectionIds = null,
        [Description("When true, clears existing connections before adding new ones. If no connectionIds are provided, all connections are removed. Defaults to false.")] bool clearExisting = false)
    {
        try
        {
            _validationService.ValidateRequiredString(workspaceId, nameof(workspaceId));
            _validationService.ValidateRequiredString(dataflowId, nameof(dataflowId));

            // Parse connectionIds - can be a single string, an array, or null
            var connectionIdList = connectionIds != null ? ParseConnectionIds(connectionIds) : new List<string>();

            if (connectionIdList.Count == 0 && !clearExisting)
            {
                var errorResponse = new
                {
                    Success = false,
                    DataflowId = dataflowId,
                    WorkspaceId = workspaceId,
                    Message = "At least one connection ID is required when not clearing connections"
                };
                return errorResponse.ToMcpJson();
            }

            // Get connection details for all connection IDs
            var connectionsToAdd = new List<(string ConnectionId, Models.Connection.Connection Connection)>();
            var notFoundIds = new List<string>();

            foreach (var connectionId in connectionIdList)
            {
                var connection = await _connectionService.GetConnectionAsync(connectionId);
                if (connection == null)
                {
                    notFoundIds.Add(connectionId);
                }
                else
                {
                    connectionsToAdd.Add((connectionId, connection));
                }
            }

            if (notFoundIds.Count > 0)
            {
                var errorResponse = new
                {
                    Success = false,
                    DataflowId = dataflowId,
                    WorkspaceId = workspaceId,
                    ConnectionIds = notFoundIds,
                    Message = $"Connection(s) not found: {string.Join(", ", notFoundIds)}"
                };
                return errorResponse.ToMcpJson();
            }

            // Single call: clearExisting flag is passed through so clear+add is atomic
            var result = await _dataflowService.AddConnectionsToDataflowAsync(workspaceId, dataflowId, connectionsToAdd, clearExisting);

            var response = new
            {
                Success = result.Success,
                DataflowId = result.DataflowId,
                WorkspaceId = result.WorkspaceId,
                ConnectionIds = connectionIdList,
                ConnectionCount = connectionIdList.Count,
                ClearedExisting = clearExisting,
                Message = result.Success
                    ? clearExisting && connectionIdList.Count == 0
                        ? $"Successfully cleared all connections from dataflow {dataflowId}"
                        : clearExisting
                            ? $"Successfully replaced connections with {connectionIdList.Count} new connection(s) in dataflow {dataflowId}"
                            : $"Successfully added {connectionIdList.Count} connection(s) to dataflow {dataflowId}"
                    : result.ErrorMessage
            };

            return response.ToMcpJson();
        }
        catch (ArgumentException ex)
        {
            return ex.ToValidationError().ToMcpJson();
        }
        catch (UnauthorizedAccessException ex)
        {
            return ex.ToAuthenticationError().ToMcpJson();
        }
        catch (HttpRequestException ex)
        {
            return ex.ToHttpError().ToMcpJson();
        }
        catch (Exception ex)
        {
            return ex.ToOperationError("adding/clearing connection(s) to dataflow").ToMcpJson();
        }
    }

    private static List<string> ParseConnectionIds(object connectionIds)
    {
        var result = new List<string>();

        if (connectionIds is string singleId)
        {
            if (!string.IsNullOrWhiteSpace(singleId))
            {
                result.Add(singleId);
            }
        }
        else if (connectionIds is System.Text.Json.JsonElement jsonElement)
        {
            if (jsonElement.ValueKind == System.Text.Json.JsonValueKind.String)
            {
                var value = jsonElement.GetString();
                if (!string.IsNullOrWhiteSpace(value))
                {
                    result.Add(value);
                }
            }
            else if (jsonElement.ValueKind == System.Text.Json.JsonValueKind.Array)
            {
                foreach (var element in jsonElement.EnumerateArray())
                {
                    if (element.ValueKind == System.Text.Json.JsonValueKind.String)
                    {
                        var value = element.GetString();
                        if (!string.IsNullOrWhiteSpace(value))
                        {
                            result.Add(value);
                        }
                    }
                }
            }
        }
        else if (connectionIds is IEnumerable<string> stringArray)
        {
            result.AddRange(stringArray.Where(s => !string.IsNullOrWhiteSpace(s)));
        }
        else if (connectionIds is IEnumerable<object> objArray)
        {
            foreach (var obj in objArray)
            {
                if (obj is string str && !string.IsNullOrWhiteSpace(str))
                {
                    result.Add(str);
                }
            }
        }

        return result;
    }

    [McpServerTool, Description(@"Adds or updates a query in an existing dataflow by updating its definition. The query will be added to the mashup.pq file and registered in queryMetadata.json.")]
    public async Task<string> AddOrUpdateQueryInDataflowAsync(
        [Description("The workspace ID containing the dataflow (required)")] string workspaceId,
        [Description("The dataflow ID to update (required)")] string dataflowId,
        [Description("The name of the query to add or update (required)")] string queryName,
        [Description("The M (Power Query) code for the query (required). Can be a full 'let...in' expression or a simple expression that will be wrapped automatically.")] string mCode)
    {
        try
        {
            _validationService.ValidateRequiredString(workspaceId, nameof(workspaceId));
            _validationService.ValidateRequiredString(dataflowId, nameof(dataflowId));
            _validationService.ValidateRequiredString(queryName, nameof(queryName));
            _validationService.ValidateRequiredString(mCode, nameof(mCode));

            var result = await _dataflowService.AddOrUpdateQueryAsync(workspaceId, dataflowId, queryName, mCode);

            var response = new
            {
                Success = result.Success,
                DataflowId = result.DataflowId,
                WorkspaceId = result.WorkspaceId,
                QueryName = queryName,
                Message = result.Success
                    ? $"Successfully added/updated query '{queryName}' in dataflow {dataflowId}"
                    : result.ErrorMessage
            };

            return response.ToMcpJson();
        }
        catch (ArgumentException ex)
        {
            return ex.ToValidationError().ToMcpJson();
        }
        catch (UnauthorizedAccessException ex)
        {
            return ex.ToAuthenticationError().ToMcpJson();
        }
        catch (HttpRequestException ex)
        {
            return ex.ToHttpError().ToMcpJson();
        }
        catch (Exception ex)
        {
            return ex.ToOperationError("adding/updating query in dataflow").ToMcpJson();
        }
    }
}
