using ModelContextProtocol.Server;
using System.ComponentModel;
using DataFactory.MCP.Abstractions.Interfaces;
using DataFactory.MCP.Extensions;
using DataFactory.MCP.Models.Dataflow;
using DataFactory.MCP.Models.Dataflow.Definition;
using DataFactory.MCP.Models.Dataflow.BackgroundTask;
using DataFactory.MCP.Models.Connection;

namespace DataFactory.MCP.Tools;

[McpServerToolType]
public class DataflowTool
{
    private readonly IFabricDataflowService _dataflowService;
    private readonly IFabricConnectionService _connectionService;
    private readonly IValidationService _validationService;
    private readonly IDataflowRefreshService _dataflowRefreshService;

    public DataflowTool(
        IFabricDataflowService dataflowService,
        IFabricConnectionService connectionService,
        IValidationService validationService,
        IDataflowRefreshService dataflowRefreshService)
    {
        _dataflowService = dataflowService;
        _connectionService = connectionService;
        _validationService = validationService;
        _dataflowRefreshService = dataflowRefreshService;
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

    [McpServerTool, Description(@"Gets the decoded definition of a dataflow with human-readable content (queryMetadata.json, mashup.pq M code, and .platform metadata).")]
    public async Task<string> GetDecodedDataflowDefinitionAsync(
        [Description("The workspace ID containing the dataflow (required)")] string workspaceId,
        [Description("The dataflow ID to get the decoded definition for (required)")] string dataflowId)
    {
        try
        {
            _validationService.ValidateRequiredString(workspaceId, nameof(workspaceId));
            _validationService.ValidateRequiredString(dataflowId, nameof(dataflowId));

            var decoded = await _dataflowService.GetDecodedDataflowDefinitionAsync(workspaceId, dataflowId);

            var result = new
            {
                Success = true,
                DataflowId = dataflowId,
                WorkspaceId = workspaceId,
                QueryMetadata = decoded.QueryMetadata,
                MashupQuery = decoded.MashupQuery,
                PlatformMetadata = decoded.PlatformMetadata,
                RawPartsCount = decoded.RawParts.Count,
                RawParts = decoded.RawParts.Select(p => new
                {
                    Path = p.Path,
                    PayloadType = p.PayloadType.ToString(),
                    PayloadSize = p.Payload?.Length ?? 0
                })
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
            return ex.ToOperationError("getting decoded dataflow definition").ToMcpJson();
        }
    }

    [McpServerTool, Description(@"Adds one or more connections to an existing dataflow by updating its definition. Retrieves the current dataflow definition, gets connection details, and updates the queryMetadata.json to include the new connections.")]
    public async Task<string> AddConnectionToDataflowAsync(
        [Description("The workspace ID containing the dataflow (required)")] string workspaceId,
        [Description("The dataflow ID to update (required)")] string dataflowId,
        [Description("The connection ID(s) to add to the dataflow. Can be a single connection ID string or an array of connection IDs (required)")] object connectionIds)
    {
        try
        {
            _validationService.ValidateRequiredString(workspaceId, nameof(workspaceId));
            _validationService.ValidateRequiredString(dataflowId, nameof(dataflowId));

            // Parse connectionIds - can be a single string or an array
            var connectionIdList = ParseConnectionIds(connectionIds);
            if (connectionIdList.Count == 0)
            {
                var errorResponse = new
                {
                    Success = false,
                    DataflowId = dataflowId,
                    WorkspaceId = workspaceId,
                    Message = "At least one connection ID is required"
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

            var result = await _dataflowService.AddConnectionsToDataflowAsync(workspaceId, dataflowId, connectionsToAdd);

            var response = new
            {
                Success = result.Success,
                DataflowId = result.DataflowId,
                WorkspaceId = result.WorkspaceId,
                ConnectionIds = connectionIdList,
                ConnectionCount = connectionIdList.Count,
                Message = result.Success
                    ? $"Successfully added {connectionIdList.Count} connection(s) to dataflow {dataflowId}"
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
            return ex.ToOperationError("adding connection(s) to dataflow").ToMcpJson();
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

    [McpServerTool, Description(@"Start a dataflow refresh in the background. Returns immediately with task info.
You'll receive a notification (via MCP logging/message) when the refresh completes.
The user can continue chatting while the refresh runs in the background.

Use this for long-running refresh operations. For quick status checks, use RefreshDataflowStatus.")]
    public async Task<string> RefreshDataflowBackground(
        McpServer mcpServer,
        [Description("The workspace ID containing the dataflow (required)")] string workspaceId,
        [Description("The dataflow ID to refresh (required)")] string dataflowId,
        [Description("User-friendly name for notifications (optional, defaults to dataflow ID)")] string? displayName = null,
        [Description("Execute option: 'SkipApplyChanges' (default, faster) or 'ApplyChangesIfNeeded' (applies pending changes first)")] string executeOption = "SkipApplyChanges")
    {
        try
        {
            _validationService.ValidateRequiredString(workspaceId, nameof(workspaceId));
            _validationService.ValidateRequiredString(dataflowId, nameof(dataflowId));

            // Pass the MCP server (session) to the service for notifications
            var result = await _dataflowRefreshService.StartRefreshAsync(
                mcpServer,
                workspaceId,
                dataflowId,
                displayName,
                executeOption);

            var response = new
            {
                Success = !result.IsComplete || result.IsSuccess,
                Message = result.IsComplete
                    ? $"Refresh {result.Status}: {result.ErrorMessage}"
                    : $"Refresh started in background. You'll be notified when complete.",
                Status = result.Status,
                TaskInfo = result.Context != null ? new
                {
                    WorkspaceId = result.Context.WorkspaceId,
                    DataflowId = result.Context.DataflowId,
                    JobInstanceId = result.Context.JobInstanceId,
                    DisplayName = result.Context.DisplayName,
                    StartedAt = result.Context.StartedAtUtc.ToString("o"),
                    EstimatedPollInterval = $"{result.Context.RetryAfterSeconds} seconds"
                } : null,
                Hint = result.IsComplete
                    ? null
                    : "Continue chatting - you'll receive a notification when the refresh completes. " +
                      "Use RefreshDataflowStatus to manually check progress if needed."
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
            return ex.ToOperationError("starting dataflow refresh").ToMcpJson();
        }
    }

    [McpServerTool, Description(@"Check the status of a dataflow refresh operation.
Use this to manually poll for status if you started a refresh with RefreshDataflowBackground.
Returns the current status including whether it's complete and any error information.")]
    public async Task<string> RefreshDataflowStatus(
        [Description("The workspace ID containing the dataflow (required)")] string workspaceId,
        [Description("The dataflow ID being refreshed (required)")] string dataflowId,
        [Description("The job instance ID from RefreshDataflowBackground result (required)")] string jobInstanceId)
    {
        try
        {
            _validationService.ValidateRequiredString(workspaceId, nameof(workspaceId));
            _validationService.ValidateRequiredString(dataflowId, nameof(dataflowId));
            _validationService.ValidateRequiredString(jobInstanceId, nameof(jobInstanceId));

            var context = new DataflowRefreshContext
            {
                WorkspaceId = workspaceId,
                DataflowId = dataflowId,
                JobInstanceId = jobInstanceId
            };

            var result = await _dataflowRefreshService.GetStatusAsync(context);

            var response = new
            {
                IsComplete = result.IsComplete,
                IsSuccess = result.IsSuccess,
                Status = result.Status,
                WorkspaceId = workspaceId,
                DataflowId = dataflowId,
                JobInstanceId = jobInstanceId,
                EndTimeUtc = result.EndTimeUtc?.ToString("o"),
                Duration = result.DurationFormatted,
                FailureReason = result.FailureReason,
                Message = result.IsComplete
                    ? (result.IsSuccess
                        ? $"Refresh completed successfully in {result.DurationFormatted}"
                        : $"Refresh {result.Status}: {result.FailureReason}")
                    : $"Refresh still in progress (status: {result.Status})"
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
            return ex.ToOperationError("checking refresh status").ToMcpJson();
        }
    }

    [McpServerTool, Description(@"List all background tasks currently being tracked.
Shows status of all dataflow refreshes started with RefreshDataflowBackground.")]
    public Task<string> ListBackgroundTasks()
    {
        try
        {
            var tasks = _dataflowRefreshService.GetAllTasks();

            var response = new
            {
                TaskCount = tasks.Count,
                Tasks = tasks.Select(t => new
                {
                    t.TaskId,
                    t.JobType,
                    t.DisplayName,
                    t.Status,
                    StartedAt = t.StartedAt.ToString("o"),
                    CompletedAt = t.CompletedAt?.ToString("o"),
                    Duration = t.CompletedAt.HasValue
                        ? (t.CompletedAt.Value - t.StartedAt).ToString(@"hh\:mm\:ss")
                        : null,
                    t.FailureReason
                })
            };

            return Task.FromResult(response.ToMcpJson());
        }
        catch (Exception ex)
        {
            return Task.FromResult(ex.ToOperationError("listing background tasks").ToMcpJson());
        }
    }
}