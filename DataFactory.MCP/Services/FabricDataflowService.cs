using DataFactory.MCP.Abstractions;
using DataFactory.MCP.Abstractions.Interfaces;
using DataFactory.MCP.Extensions;
using DataFactory.MCP.Models.Dataflow;
using DataFactory.MCP.Models.Dataflow.Query;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;

namespace DataFactory.MCP.Services;

/// <summary>
/// Service for interacting with Microsoft Fabric Dataflows API
/// </summary>
public class FabricDataflowService : FabricServiceBase, IFabricDataflowService
{
    private readonly IValidationService _validationService;
    private readonly IArrowDataReaderService _arrowDataReaderService;

    public FabricDataflowService(
        ILogger<FabricDataflowService> logger,
        IAuthenticationService authService,
        IValidationService validationService,
        IArrowDataReaderService arrowDataReaderService)
        : base(logger, authService)
    {
        _validationService = validationService;
        _arrowDataReaderService = arrowDataReaderService;
    }

    public async Task<ListDataflowsResponse> ListDataflowsAsync(
        string workspaceId,
        string? continuationToken = null)
    {
        try
        {
            _validationService.ValidateGuid(workspaceId, nameof(workspaceId));

            await EnsureAuthenticationAsync();

            var url = BuildDataflowsUrl(workspaceId, continuationToken);

            Logger.LogInformation("Fetching dataflows from workspace {WorkspaceId}: {Url}", workspaceId, url);

            var response = await HttpClient.GetAsync(url);

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var dataflowsResponse = System.Text.Json.JsonSerializer.Deserialize<ListDataflowsResponse>(content, JsonOptions);

                Logger.LogInformation("Successfully retrieved {Count} dataflows from workspace {WorkspaceId}",
                    dataflowsResponse?.Value?.Count ?? 0, workspaceId);
                return dataflowsResponse ?? new ListDataflowsResponse();
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                Logger.LogError("API request failed. Status: {StatusCode}, Content: {Content}",
                    response.StatusCode, errorContent);

                throw new HttpRequestException($"API request failed: {response.StatusCode} - {errorContent}");
            }
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
            _validationService.ValidateGuid(workspaceId, nameof(workspaceId));
            _validationService.ValidateAndThrow(request, nameof(request));

            await EnsureAuthenticationAsync();

            var url = $"{BaseUrl}/workspaces/{workspaceId}/dataflows";
            var jsonContent = JsonSerializer.Serialize(request, JsonOptions);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            Logger.LogInformation("Creating dataflow '{DisplayName}' in workspace {WorkspaceId}: {Url}",
                request.DisplayName, workspaceId, url);

            var response = await HttpClient.PostAsync(url, content);

            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                var createResponse = JsonSerializer.Deserialize<CreateDataflowResponse>(responseContent, JsonOptions);

                Logger.LogInformation("Successfully created dataflow '{DisplayName}' with ID {DataflowId} in workspace {WorkspaceId}",
                    request.DisplayName, createResponse?.Id, workspaceId);

                return createResponse ?? new CreateDataflowResponse();
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                Logger.LogError("Failed to create dataflow. Status: {StatusCode}, Content: {Content}",
                    response.StatusCode, errorContent);

                throw new HttpRequestException($"Failed to create dataflow: {response.StatusCode} - {errorContent}");
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error creating dataflow '{DisplayName}' in workspace {WorkspaceId}",
                request?.DisplayName, workspaceId);
            throw;
        }
    }

    private static string BuildDataflowsUrl(string workspaceId, string? continuationToken)
    {
        var url = new StringBuilder($"{BaseUrl}/workspaces/{workspaceId}/dataflows");
        var queryParams = new List<string>();

        if (!string.IsNullOrEmpty(continuationToken))
        {
            queryParams.Add($"continuationToken={Uri.EscapeDataString(continuationToken)}");
        }

        if (queryParams.Any())
        {
            url.Append("?");
            url.Append(string.Join("&", queryParams));
        }

        return url.ToString();
    }

    public async Task<ExecuteDataflowQueryResponse> ExecuteQueryAsync(
        string workspaceId,
        string dataflowId,
        ExecuteDataflowQueryRequest request)
    {
        try
        {
            _validationService.ValidateGuid(workspaceId, nameof(workspaceId));
            _validationService.ValidateGuid(dataflowId, nameof(dataflowId));
            _validationService.ValidateAndThrow(request, nameof(request));

            await EnsureAuthenticationAsync();

            var url = $"{BaseUrl}/workspaces/{workspaceId}/dataflows/{dataflowId}/executeQuery";
            var jsonContent = JsonSerializer.Serialize(request, JsonOptions);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            Logger.LogInformation("Executing query '{QueryName}' on dataflow {DataflowId} in workspace {WorkspaceId}: {Url}",
                request.QueryName, dataflowId, workspaceId, url);

            var response = await HttpClient.PostAsync(url, content);

            if (response.IsSuccessStatusCode)
            {
                // Read the binary response data
                var responseData = await response.Content.ReadAsByteArrayAsync();
                var contentType = response.Content.Headers.ContentType?.MediaType;
                var contentLength = responseData.Length;

                Logger.LogInformation("Successfully executed query '{QueryName}' on dataflow {DataflowId}. Response: {ContentLength} bytes, Content-Type: {ContentType}",
                    request.QueryName, dataflowId, contentLength, contentType);

                // Extract summary information from the Arrow data using the enhanced reader
                var arrowInfo = await _arrowDataReaderService.ReadArrowStreamAsync(responseData);
                var summary = arrowInfo.ToQueryResultSummary();

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
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                Logger.LogError("Failed to execute query '{QueryName}' on dataflow {DataflowId}. Status: {StatusCode}, Content: {Content}",
                    request.QueryName, dataflowId, response.StatusCode, errorContent);

                return new ExecuteDataflowQueryResponse
                {
                    Success = false,
                    Error = $"Query execution failed: {response.StatusCode} - {errorContent}",
                    ContentLength = 0
                };
            }
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
}