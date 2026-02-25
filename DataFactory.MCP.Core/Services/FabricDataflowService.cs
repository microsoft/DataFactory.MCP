using DataFactory.MCP.Abstractions;
using DataFactory.MCP.Abstractions.Interfaces;
using DataFactory.MCP.Abstractions.Interfaces.DMTSv2;
using DataFactory.MCP.Infrastructure.Http;
using DataFactory.MCP.Models.Dataflow;
using DataFactory.MCP.Models.Dataflow.Definition;
using DataFactory.MCP.Models.Dataflow.Query;
using DataFactory.MCP.Models.Connection;
using Microsoft.Extensions.Logging;

namespace DataFactory.MCP.Services;

/// <summary>
/// Service for interacting with Microsoft Fabric Dataflows API
/// </summary>
public class FabricDataflowService : FabricServiceBase, IFabricDataflowService
{
    private const string ArrowContentType = "application/octet-stream";

    private readonly IArrowDataReaderService _arrowDataReaderService;
    private readonly IGatewayClusterDatasourceService _cloudDatasourceService;
    private readonly IDataflowDefinitionProcessor _definitionProcessor;

    public FabricDataflowService(
        IHttpClientFactory httpClientFactory,
        ILogger<FabricDataflowService> logger,
        IValidationService validationService,
        IArrowDataReaderService arrowDataReaderService,
        IGatewayClusterDatasourceService cloudDatasourceService,
        IDataflowDefinitionProcessor definitionProcessor)
        : base(httpClientFactory, logger, validationService)
    {
        _arrowDataReaderService = arrowDataReaderService;
        _cloudDatasourceService = cloudDatasourceService;
        _definitionProcessor = definitionProcessor;
    }

    public async Task<ListDataflowsResponse> ListDataflowsAsync(
        string workspaceId,
        string? continuationToken = null)
    {
        try
        {
            ValidateGuids((workspaceId, nameof(workspaceId)));

            var endpoint = FabricUrlBuilder.ForFabricApi()
                .WithLiteralPath($"workspaces/{workspaceId}/dataflows")
                .BuildEndpoint();
            Logger.LogInformation("Fetching dataflows from workspace {WorkspaceId}", workspaceId);

            var dataflowsResponse = await GetAsync<ListDataflowsResponse>(endpoint, continuationToken);

            Logger.LogInformation("Successfully retrieved {Count} dataflows from workspace {WorkspaceId}",
                dataflowsResponse?.Value?.Count ?? 0, workspaceId);
            return dataflowsResponse ?? new ListDataflowsResponse();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error fetching dataflows from workspace {WorkspaceId}", workspaceId);
            throw;
        }
    }

    public async Task<CreateDataflowResponse> CreateDataflowAsync(
        string workspaceId,
        CreateDataflowRequest request)
    {
        try
        {
            ValidateGuids((workspaceId, nameof(workspaceId)));
            ValidationService.ValidateAndThrow(request, nameof(request));

            var endpoint = FabricUrlBuilder.ForFabricApi()
                .WithLiteralPath($"workspaces/{workspaceId}/dataflows")
                .BuildEndpoint();
            Logger.LogInformation("Creating dataflow '{DisplayName}' in workspace {WorkspaceId}",
                request.DisplayName, workspaceId);

            var createResponse = await PostAsync<CreateDataflowResponse>(endpoint, request);

            Logger.LogInformation("Successfully created dataflow '{DisplayName}' with ID {DataflowId} in workspace {WorkspaceId}",
                request.DisplayName, createResponse?.Id, workspaceId);

            return createResponse ?? new CreateDataflowResponse();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error creating dataflow '{DisplayName}' in workspace {WorkspaceId}",
                request?.DisplayName, workspaceId);
            throw;
        }
    }

    public async Task<ExecuteDataflowQueryResponse> ExecuteQueryAsync(
        string workspaceId,
        string dataflowId,
        ExecuteDataflowQueryRequest request)
    {
        try
        {
            ValidateGuids(
                (workspaceId, nameof(workspaceId)),
                (dataflowId, nameof(dataflowId)));
            ValidationService.ValidateAndThrow(request, nameof(request));

            var endpoint = FabricUrlBuilder.ForFabricApi()
                .WithLiteralPath($"workspaces/{workspaceId}/dataflows/{dataflowId}/executeQuery")
                .BuildEndpoint();

            Logger.LogInformation("Executing query '{QueryName}' on dataflow {DataflowId} in workspace {WorkspaceId}",
                request.QueryName, dataflowId, workspaceId);

            var responseData = await PostAsBytesAsync(endpoint, request);
            var contentLength = responseData.Length;

            Logger.LogInformation("Successfully executed query '{QueryName}' on dataflow {DataflowId}. Response: {ContentLength} bytes",
                request.QueryName, dataflowId, contentLength);

            // Delegate Arrow processing to specialized service
            var summary = await _arrowDataReaderService.ReadArrowStreamAsync(responseData);

            return new ExecuteDataflowQueryResponse
            {
                Data = responseData,
                ContentType = ArrowContentType,
                ContentLength = contentLength,
                Success = true,
                Summary = summary,
                Metadata = new Dictionary<string, object>
                {
                    { "executedAt", DateTime.UtcNow },
                    { "workspaceId", workspaceId },
                    { "dataflowId", dataflowId },
                    { "queryName", request.QueryName }
                }
            };
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error executing query '{QueryName}' on dataflow {DataflowId} in workspace {WorkspaceId}",
                request?.QueryName, dataflowId, workspaceId);

            return new ExecuteDataflowQueryResponse
            {
                Success = false,
                Error = $"Query execution error: {ex.Message}",
                ContentLength = 0
            };
        }
    }

    public async Task<DataflowDefinition> GetDataflowDefinitionAsync(
        string workspaceId,
        string dataflowId)
    {
        ValidateGuids(
            (workspaceId, nameof(workspaceId)),
            (dataflowId, nameof(dataflowId)));

        var response = await GetDataflowDefinitionResponseAsync(workspaceId, dataflowId);
        return response.Definition;
    }

    public async Task<DecodedDataflowDefinition> GetDecodedDataflowDefinitionAsync(
        string workspaceId,
        string dataflowId)
    {
        try
        {
            // Get raw definition via HTTP
            var rawDefinition = await GetDataflowDefinitionAsync(workspaceId, dataflowId);

            // Delegate decoding to processor service
            var decoded = _definitionProcessor.DecodeDefinition(rawDefinition);

            Logger.LogInformation("Successfully decoded definition for dataflow {DataflowId}", dataflowId);
            return decoded;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error decoding definition for dataflow {DataflowId} in workspace {WorkspaceId}",
                dataflowId, workspaceId);
            throw;
        }
    }

    private async Task<GetDataflowDefinitionHttpResponse> GetDataflowDefinitionResponseAsync(string workspaceId, string dataflowId)
    {
        var endpoint = FabricUrlBuilder.ForFabricApi()
            .WithLiteralPath($"workspaces/{workspaceId}/items/{dataflowId}/getDefinition")
            .BuildEndpoint();

        Logger.LogInformation("Getting definition for dataflow {DataflowId} in workspace {WorkspaceId}",
            dataflowId, workspaceId);

        // Use empty object as required by API
        var emptyRequest = new { };
        return await PostAsync<GetDataflowDefinitionHttpResponse>(endpoint, emptyRequest)
               ?? throw new InvalidOperationException("Failed to get dataflow definition response");
    }

    public async Task<UpdateDataflowDefinitionResponse> AddConnectionsToDataflowAsync(
        string workspaceId,
        string dataflowId,
        IEnumerable<(string ConnectionId, Connection Connection)> connections)
    {
        var connectionsList = connections.ToList();
        try
        {
            ValidateGuids(
                (workspaceId, nameof(workspaceId)),
                (dataflowId, nameof(dataflowId)));

            foreach (var (connectionId, _) in connectionsList)
            {
                ValidateGuids((connectionId, nameof(connectionId)));
            }

            Logger.LogInformation("Adding {Count} connection(s) to dataflow {DataflowId} in workspace {WorkspaceId}",
                connectionsList.Count, dataflowId, workspaceId);

            // Step 1: Get current dataflow definition via HTTP
            var currentDefinition = await GetDataflowDefinitionAsync(workspaceId, dataflowId);
            if (currentDefinition?.Parts == null)
            {
                return new UpdateDataflowDefinitionResponse
                {
                    Success = false,
                    ErrorMessage = "Failed to retrieve current dataflow definition",
                    DataflowId = dataflowId,
                    WorkspaceId = workspaceId
                };
            }

            // Step 2: Get the ClusterId for each connection from the Power BI v2.0 API
            // This is required for proper credential binding in the dataflow
            var connectionsWithClusterIds = new List<(Connection Connection, string ConnectionId, string? ClusterId)>();
            foreach (var (connectionId, connection) in connectionsList)
            {
                string? clusterId = await GetClusterId(connectionId);
                connectionsWithClusterIds.Add((connection, connectionId, clusterId));
            }

            // Step 3: Process connection additions via business logic service
            var updatedDefinition = _definitionProcessor.AddConnectionsToDefinition(
                currentDefinition,
                connectionsWithClusterIds);

            // Step 4: Update via HTTP
            await UpdateDataflowDefinitionAsync(workspaceId, dataflowId, updatedDefinition);

            var connectionIds = string.Join(", ", connectionsList.Select(c => c.ConnectionId));
            Logger.LogInformation("Successfully added connection(s) {ConnectionIds} to dataflow {DataflowId}",
                connectionIds, dataflowId);

            return new UpdateDataflowDefinitionResponse
            {
                Success = true,
                DataflowId = dataflowId,
                WorkspaceId = workspaceId
            };
        }
        catch (Exception ex)
        {
            var connectionIds = string.Join(", ", connectionsList.Select(c => c.ConnectionId));
            Logger.LogError(ex, "Error adding connection(s) {ConnectionIds} to dataflow {DataflowId} in workspace {WorkspaceId}",
                connectionIds, dataflowId, workspaceId);

            return new UpdateDataflowDefinitionResponse
            {
                Success = false,
                ErrorMessage = ex.Message,
                DataflowId = dataflowId,
                WorkspaceId = workspaceId
            };
        }
    }

    public async Task<UpdateDataflowDefinitionResponse> ClearConnectionsFromDataflowAsync(
        string workspaceId,
        string dataflowId)
    {
        try
        {
            ValidateGuids(
                (workspaceId, nameof(workspaceId)),
                (dataflowId, nameof(dataflowId)));

            Logger.LogInformation("Clearing all connections from dataflow {DataflowId} in workspace {WorkspaceId}",
                dataflowId, workspaceId);

            // Step 1: Get current dataflow definition
            var currentDefinition = await GetDataflowDefinitionAsync(workspaceId, dataflowId);
            if (currentDefinition?.Parts == null)
            {
                return new UpdateDataflowDefinitionResponse
                {
                    Success = false,
                    ErrorMessage = "Failed to retrieve current dataflow definition",
                    DataflowId = dataflowId,
                    WorkspaceId = workspaceId
                };
            }

            // Step 2: Clear connections via definition processor
            var updatedDefinition = _definitionProcessor.ClearConnectionsFromDefinition(currentDefinition);

            // Step 3: Update via HTTP
            await UpdateDataflowDefinitionAsync(workspaceId, dataflowId, updatedDefinition);

            Logger.LogInformation("Successfully cleared all connections from dataflow {DataflowId}", dataflowId);

            return new UpdateDataflowDefinitionResponse
            {
                Success = true,
                DataflowId = dataflowId,
                WorkspaceId = workspaceId
            };
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error clearing connections from dataflow {DataflowId} in workspace {WorkspaceId}",
                dataflowId, workspaceId);

            return new UpdateDataflowDefinitionResponse
            {
                Success = false,
                ErrorMessage = ex.Message,
                DataflowId = dataflowId,
                WorkspaceId = workspaceId
            };
        }
    }

    private async Task<string?> GetClusterId(string connectionId)
    {
        try
        {
            var clusterId = await _cloudDatasourceService.GetClusterIdForConnectionAsync(connectionId);

            if (clusterId == null)
            {
                Logger.LogWarning("Could not find ClusterId for connection {ConnectionId}. " +
                    "Credential binding may not work correctly.", connectionId);
                return null;
            }

            Logger.LogInformation("Successfully retrieved ClusterId {ClusterId} for connection {ConnectionId}",
                clusterId, connectionId);
            return clusterId;
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Failed to get ClusterId for connection {ConnectionId}. " +
                "Continuing without ClusterId - credential binding may not work correctly.", connectionId);
            return null;
        }
    }

    public async Task UpdateDataflowDefinitionAsync(string workspaceId, string dataflowId, DataflowDefinition definition)
    {
        ValidateGuids(
            (workspaceId, nameof(workspaceId)),
            (dataflowId, nameof(dataflowId)));

        var endpoint = FabricUrlBuilder.ForFabricApi()
            .WithLiteralPath($"workspaces/{workspaceId}/items/{dataflowId}/updateDefinition")
            .BuildEndpoint();
        var request = new UpdateDataflowDefinitionRequest { Definition = definition };

        Logger.LogInformation("Updating dataflow definition for {DataflowId}", dataflowId);

        var success = await PostNoContentAsync(endpoint, request);

        if (!success)
        {
            throw new HttpRequestException($"Failed to update dataflow definition for {dataflowId}");
        }

        Logger.LogInformation("Successfully updated dataflow definition for {DataflowId}", dataflowId);
    }

    public async Task<UpdateDataflowDefinitionResponse> AddOrUpdateQueryAsync(
        string workspaceId,
        string dataflowId,
        string queryName,
        string mCode,
        string? attribute = null,
        string? sectionAttribute = null)
    {
        try
        {
            ValidateGuids(
                (workspaceId, nameof(workspaceId)),
                (dataflowId, nameof(dataflowId)));

            ValidationService.ValidateRequiredString(queryName, nameof(queryName));
            ValidationService.ValidateRequiredString(mCode, nameof(mCode));

            Logger.LogInformation("Adding/updating query '{QueryName}' in dataflow {DataflowId} in workspace {WorkspaceId}",
                queryName, dataflowId, workspaceId);

            // Step 1: Get current dataflow definition via HTTP
            var currentDefinition = await GetDataflowDefinitionAsync(workspaceId, dataflowId);
            if (currentDefinition?.Parts == null)
            {
                return new UpdateDataflowDefinitionResponse
                {
                    Success = false,
                    ErrorMessage = "Failed to retrieve current dataflow definition",
                    DataflowId = dataflowId,
                    WorkspaceId = workspaceId
                };
            }

            // Step 2: Process query addition/update via business logic service
            var updatedDefinition = _definitionProcessor.AddOrUpdateQueryInDefinition(
                currentDefinition,
                queryName,
                mCode,
                attribute,
                sectionAttribute);

            // Step 3: Update via HTTP
            await UpdateDataflowDefinitionAsync(workspaceId, dataflowId, updatedDefinition);

            Logger.LogInformation("Successfully added/updated query '{QueryName}' in dataflow {DataflowId}",
                queryName, dataflowId);

            return new UpdateDataflowDefinitionResponse
            {
                Success = true,
                DataflowId = dataflowId,
                WorkspaceId = workspaceId
            };
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error adding/updating query '{QueryName}' in dataflow {DataflowId} in workspace {WorkspaceId}",
                queryName, dataflowId, workspaceId);

            return new UpdateDataflowDefinitionResponse
            {
                Success = false,
                ErrorMessage = ex.Message,
                DataflowId = dataflowId,
                WorkspaceId = workspaceId
            };
        }
    }

    public async Task<UpdateDataflowDefinitionResponse> SyncMashupDocumentAsync(
        string workspaceId,
        string dataflowId,
        string newMashupDocument,
        IList<(string QueryName, string MCode, string? Attribute)> parsedQueries)
    {
        try
        {
            ValidateGuids(
                (workspaceId, nameof(workspaceId)),
                (dataflowId, nameof(dataflowId)));
            ValidationService.ValidateRequiredString(newMashupDocument, nameof(newMashupDocument));

            if (parsedQueries == null || parsedQueries.Count == 0)
            {
                return new UpdateDataflowDefinitionResponse
                {
                    Success = false,
                    ErrorMessage = "No queries provided in the document",
                    DataflowId = dataflowId,
                    WorkspaceId = workspaceId
                };
            }

            Logger.LogInformation("Syncing mashup document with {QueryCount} queries in dataflow {DataflowId} in workspace {WorkspaceId}",
                parsedQueries.Count, dataflowId, workspaceId);

            // Step 1: Get current dataflow definition
            var currentDefinition = await GetDataflowDefinitionAsync(workspaceId, dataflowId);
            if (currentDefinition?.Parts == null)
            {
                return new UpdateDataflowDefinitionResponse
                {
                    Success = false,
                    ErrorMessage = "Failed to retrieve current dataflow definition",
                    DataflowId = dataflowId,
                    WorkspaceId = workspaceId
                };
            }

            // Step 2: Sync the mashup document (replace entire mashup + sync metadata)
            var updatedDefinition = _definitionProcessor.SyncMashupInDefinition(
                currentDefinition,
                newMashupDocument,
                parsedQueries);

            // Step 3: Save the updated definition
            await UpdateDataflowDefinitionAsync(workspaceId, dataflowId, updatedDefinition);

            Logger.LogInformation("Successfully synced mashup document with {QueryCount} queries in dataflow {DataflowId}",
                parsedQueries.Count, dataflowId);

            return new UpdateDataflowDefinitionResponse
            {
                Success = true,
                DataflowId = dataflowId,
                WorkspaceId = workspaceId
            };
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error syncing mashup document in dataflow {DataflowId} in workspace {WorkspaceId}",
                dataflowId, workspaceId);

            return new UpdateDataflowDefinitionResponse
            {
                Success = false,
                ErrorMessage = ex.Message,
                DataflowId = dataflowId,
                WorkspaceId = workspaceId
            };
        }
    }
}
