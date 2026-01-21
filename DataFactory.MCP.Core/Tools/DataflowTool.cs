using ModelContextProtocol.Server;
using System.ComponentModel;
using DataFactory.MCP.Abstractions.Interfaces;
using DataFactory.MCP.Extensions;
using DataFactory.MCP.Models.Dataflow;
using DataFactory.MCP.Models.Dataflow.Definition;
using DataFactory.MCP.Models.Connection;

namespace DataFactory.MCP.Tools;

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

    [McpServerTool, Description(@"Adds a connection to an existing dataflow by updating its definition. Retrieves the current dataflow definition, gets connection details, and updates the queryMetadata.json to include the new connection.")]
    public async Task<string> AddConnectionToDataflowAsync(
        [Description("The workspace ID containing the dataflow (required)")] string workspaceId,
        [Description("The dataflow ID to update (required)")] string dataflowId,
        [Description("The connection ID to add to the dataflow (required)")] string connectionId)
    {
        try
        {
            _validationService.ValidateRequiredString(workspaceId, nameof(workspaceId));
            _validationService.ValidateRequiredString(dataflowId, nameof(dataflowId));
            _validationService.ValidateRequiredString(connectionId, nameof(connectionId));

            // Get connection details using the dedicated connection service
            var connection = await _connectionService.GetConnectionAsync(connectionId);
            if (connection == null)
            {
                var errorResponse = new
                {
                    Success = false,
                    DataflowId = dataflowId,
                    WorkspaceId = workspaceId,
                    ConnectionId = connectionId,
                    Message = $"Connection with ID '{connectionId}' not found"
                };
                return errorResponse.ToMcpJson();
            }

            var result = await _dataflowService.AddConnectionToDataflowAsync(workspaceId, dataflowId, connectionId, connection);

            var response = new
            {
                Success = result.Success,
                DataflowId = result.DataflowId,
                WorkspaceId = result.WorkspaceId,
                ConnectionId = connectionId,
                Message = result.Success
                    ? $"Successfully added connection {connectionId} to dataflow {dataflowId}"
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
            return ex.ToOperationError("adding connection to dataflow").ToMcpJson();
        }
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