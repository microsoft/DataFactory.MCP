using DataFactory.MCP.Abstractions;
using DataFactory.MCP.Abstractions.Interfaces;
using DataFactory.MCP.Abstractions.Interfaces.DMTSv2;
using DataFactory.MCP.Models.Dataflow;
using DataFactory.MCP.Models.Dataflow.Query;
using DataFactory.MCP.Models.Connection;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DataFactory.MCP.Services;

/// <summary>
/// Internal class for HTTP response deserialization
/// </summary>
internal class GetDataflowDefinitionHttpResponse
{
    [JsonPropertyName("definition")]
    public DataflowDefinition Definition { get; set; } = new();
}

/// <summary>
/// Service for interacting with Microsoft Fabric Dataflows API
/// </summary>
public class FabricDataflowService : FabricServiceBase, IFabricDataflowService
{
    private readonly IArrowDataReaderService _arrowDataReaderService;
    private readonly IGatewayClusterDatasourceService _cloudDatasourceService;
    private readonly IDataflowDefinitionProcessor _definitionProcessor;

    public FabricDataflowService(
        ILogger<FabricDataflowService> logger,
        IAuthenticationService authService,
        IValidationService validationService,
        IArrowDataReaderService arrowDataReaderService,
        IGatewayClusterDatasourceService cloudDatasourceService,
        IDataflowDefinitionProcessor definitionProcessor)
        : base(logger, authService, validationService)
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
            await ValidateAndAuthenticateAsync((workspaceId, nameof(workspaceId)));

            var endpoint = BuildDataflowsEndpoint(workspaceId);
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
            await ValidateAndAuthenticateAsync((workspaceId, nameof(workspaceId)));
            ValidationService.ValidateAndThrow(request, nameof(request));

            var endpoint = $"workspaces/{workspaceId}/dataflows";
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

    private static string BuildDataflowsEndpoint(string workspaceId)
    {
        return $"workspaces/{workspaceId}/dataflows";
    }

    public async Task<ExecuteDataflowQueryResponse> ExecuteQueryAsync(
        string workspaceId,
        string dataflowId,
        ExecuteDataflowQueryRequest request)
    {
        try
        {
            await ValidateAndAuthenticateAsync(
                (workspaceId, nameof(workspaceId)),
                (dataflowId, nameof(dataflowId)));
            ValidationService.ValidateAndThrow(request, nameof(request));

            var endpoint = $"workspaces/{workspaceId}/dataflows/{dataflowId}/executeQuery";

            Logger.LogInformation("Executing query '{QueryName}' on dataflow {DataflowId} in workspace {WorkspaceId}",
                request.QueryName, dataflowId, workspaceId);

            // Handle binary response directly since base PostAsync doesn't support byte[] return type
            var jsonContent = JsonSerializer.Serialize(request, JsonOptions);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
            var url = $"{BaseUrl}/{endpoint}";
            Logger.LogInformation("Posting to: {Url}", url);

            var response = await HttpClient.PostAsync(url, content);
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                Logger.LogError("Failed to execute query. Status: {StatusCode}, Content: {Content}",
                    response.StatusCode, errorContent);
                throw new HttpRequestException($"Failed to execute query: {response.StatusCode} - {errorContent}");
            }

            var responseData = await response.Content.ReadAsByteArrayAsync();
            var contentType = "application/octet-stream"; // Arrow format
            var contentLength = responseData.Length;

            Logger.LogInformation("Successfully executed query '{QueryName}' on dataflow {DataflowId}. Response: {ContentLength} bytes",
                request.QueryName, dataflowId, contentLength);

            // Delegate Arrow processing to specialized service
            var summary = await _arrowDataReaderService.ReadArrowStreamAsync(responseData);

            return new ExecuteDataflowQueryResponse
            {
                Data = responseData,
                ContentType = contentType,
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
        await ValidateAndAuthenticateAsync(
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
        var endpoint = $"workspaces/{workspaceId}/items/{dataflowId}/getDefinition";

        Logger.LogInformation("Getting definition for dataflow {DataflowId} in workspace {WorkspaceId}",
            dataflowId, workspaceId);

        // Use empty object as required by API
        var emptyRequest = new { };
        return await PostAsync<GetDataflowDefinitionHttpResponse>(endpoint, emptyRequest)
               ?? throw new InvalidOperationException("Failed to get dataflow definition response");
    }

    public async Task<UpdateDataflowDefinitionResponse> AddConnectionToDataflowAsync(
        string workspaceId,
        string dataflowId,
        string connectionId,
        Connection connection)
    {
        try
        {
            await ValidateAndAuthenticateAsync(
                (workspaceId, nameof(workspaceId)),
                (dataflowId, nameof(dataflowId)),
                (connectionId, nameof(connectionId)));

            Logger.LogInformation("Adding connection {ConnectionId} to dataflow {DataflowId} in workspace {WorkspaceId}",
                connectionId, dataflowId, workspaceId);

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

            // Step 2: Get the ClusterId for this connection from the Power BI v2.0 API
            // This is required for proper credential binding in the dataflow
            string? clusterId = await GetClusterId(connectionId);

            // Step 3: Process connection addition via business logic service
            var updatedDefinition = _definitionProcessor.AddConnectionToDefinition(
                currentDefinition,
                connection,
                connectionId,
                clusterId);

            // Step 3: Update via HTTP
            var updateResult = await UpdateDataflowDefinitionAsync(workspaceId, dataflowId, updatedDefinition);

            Logger.LogInformation("Successfully added connection {ConnectionId} to dataflow {DataflowId}",
                connectionId, dataflowId);

            return new UpdateDataflowDefinitionResponse
            {
                Success = updateResult,
                DataflowId = dataflowId,
                WorkspaceId = workspaceId,
                ErrorMessage = updateResult ? null : "Failed to update dataflow definition"
            };
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error adding connection {ConnectionId} to dataflow {DataflowId} in workspace {WorkspaceId}",
                connectionId, dataflowId, workspaceId);

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
        string? clusterId = null;
        try
        {
            clusterId = await _cloudDatasourceService.GetClusterIdForConnectionAsync(connectionId);
            if (clusterId != null)
            {
                Logger.LogInformation("Successfully retrieved ClusterId {ClusterId} for connection {ConnectionId}",
                    clusterId, connectionId);
            }
            else
            {
                Logger.LogWarning("Could not find ClusterId for connection {ConnectionId}. " +
                    "Credential binding may not work correctly.", connectionId);
            }
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Failed to get ClusterId for connection {ConnectionId}. " +
                "Continuing without ClusterId - credential binding may not work correctly.", connectionId);
        }

        return clusterId;
    }

    public async Task<bool> UpdateDataflowDefinitionAsync(string workspaceId, string dataflowId, DataflowDefinition definition)
    {
        try
        {
            await ValidateAndAuthenticateAsync(
                (workspaceId, nameof(workspaceId)),
                (dataflowId, nameof(dataflowId)));

            var endpoint = $"workspaces/{workspaceId}/items/{dataflowId}/updateDefinition";
            var request = new UpdateDataflowDefinitionRequest { Definition = definition };

            Logger.LogInformation("Updating dataflow definition for {DataflowId}", dataflowId);

            try
            {

                var jsonContent = JsonSerializer.Serialize(request, JsonOptions);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                var url = $"{BaseUrl}/{endpoint}";
                Logger.LogInformation("Posting to: {Url}", url);

                var response = await HttpClient.PostAsync(url, content);

                if (response.IsSuccessStatusCode)
                {
                    // Fabric API returns 204 No Content on successful update - no response body to deserialize
                    Logger.LogInformation("Successfully updated dataflow definition for {DataflowId}", dataflowId);
                    return true;
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    Logger.LogError("Failed to update dataflow definition. Status: {StatusCode}, Content: {Content}",
                        response.StatusCode, errorContent);
                    return false;
                }
            }
            catch (HttpRequestException ex)
            {
                Logger.LogError("Failed to update dataflow definition: {Error}", ex.Message);
                return false;
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error updating dataflow definition for {DataflowId}", dataflowId);
            return false;
        }
    }
}