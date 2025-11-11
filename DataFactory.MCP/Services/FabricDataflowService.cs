using DataFactory.MCP.Abstractions;
using DataFactory.MCP.Abstractions.Interfaces;
using DataFactory.MCP.Models.Dataflow;
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

    public FabricDataflowService(
        ILogger<FabricDataflowService> logger,
        IAuthenticationService authService,
        IValidationService validationService)
        : base(logger, authService)
    {
        _validationService = validationService;
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
                var summary = ExtractArrowDataSummary(responseData);

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



    private static QueryResultSummary ExtractArrowDataSummary(byte[] arrowData)
    {
        // Use the enhanced Arrow reader service with full data retrieval
        var arrowInfo = ArrowDataReaderService.ReadArrowStream(arrowData, returnAllData: true);

        var summary = new QueryResultSummary
        {
            ArrowParsingSuccess = arrowInfo.Success,
            ArrowParsingError = arrowInfo.Error,
            EstimatedRowCount = arrowInfo.TotalRows,
            BatchCount = arrowInfo.BatchCount
        };

        if (arrowInfo.Schema != null)
        {
            summary.Columns = arrowInfo.Schema.Columns?.Select(c => c.Name).ToList() ?? new List<string>();

            summary.ArrowSchema = new ArrowSchemaDetails
            {
                FieldCount = arrowInfo.Schema.FieldCount,
                Columns = arrowInfo.Schema.Columns?.Select(c => new ArrowColumnDetails
                {
                    Name = c.Name,
                    DataType = c.DataType,
                    IsNullable = c.IsNullable,
                    Metadata = c.Metadata
                }).ToList()
            };
        }

        // Use AllData if available (when returnAllData=true), otherwise use SampleData
        var dataToUse = arrowInfo.AllData ?? arrowInfo.SampleData;

        if (dataToUse != null)
        {
            // Convert structured data to string format for backward compatibility
            summary.SampleData = dataToUse.ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value.Select(v => v?.ToString() ?? "null").ToList()
            );

            // Also provide the structured data
            summary.StructuredSampleData = dataToUse;
        }

        // If Arrow parsing failed, fall back to basic text extraction
        if (!arrowInfo.Success)
        {
            var fallbackSummary = ExtractBasicTextSummary(arrowData);
            summary.Columns = fallbackSummary.Columns ?? new List<string>();
            summary.SampleData = fallbackSummary.SampleData ?? new Dictionary<string, List<string>>();
            summary.EstimatedRowCount = fallbackSummary.EstimatedRowCount;
        }

        return summary;
    }

    private static QueryResultSummary ExtractBasicTextSummary(byte[] arrowData)
    {
        var summary = new QueryResultSummary();

        try
        {
            // Convert bytes to string to look for readable patterns
            var stringContent = System.Text.Encoding.UTF8.GetString(arrowData);

            // Look for common column names
            var columns = new List<string>();
            var commonColumns = new[] { "RoleInstance", "ProcessName", "Message", "Timestamp", "Level" };
            foreach (var column in commonColumns)
            {
                if (stringContent.Contains(column))
                {
                    columns.Add(column);
                }
            }
            summary.Columns = columns;

            // Extract sample data
            var sampleData = new Dictionary<string, List<string>>();

            // Look for role instances
            var rolePattern = @"vmback_\d+";
            var roleMatches = System.Text.RegularExpressions.Regex.Matches(stringContent, rolePattern);
            if (roleMatches.Count > 0)
            {
                sampleData["RoleInstance"] = roleMatches.Cast<System.Text.RegularExpressions.Match>()
                    .Select(m => m.Value).Distinct().Take(3).ToList();
            }

            // Look for service names
            var servicePattern = @"Microsoft\.Mashup\.Web\.[A-Za-z.]+";
            var serviceMatches = System.Text.RegularExpressions.Regex.Matches(stringContent, servicePattern);
            if (serviceMatches.Count > 0)
            {
                sampleData["ProcessName"] = serviceMatches.Cast<System.Text.RegularExpressions.Match>()
                    .Select(m => m.Value).Distinct().Take(3).ToList();
            }

            // Look for common error messages
            var errorMessages = new[] { "Invalid workload hostname", "DataSource requested unhandled application property", "A generic MashupException was caught" };
            var foundMessages = errorMessages.Where(msg => stringContent.Contains(msg)).Take(3).ToList();
            if (foundMessages.Any())
            {
                sampleData["Message"] = foundMessages;
            }

            summary.SampleData = sampleData;

            // Rough estimate of row count based on pattern matches
            var estimatedRows = Math.Max(roleMatches.Count, serviceMatches.Count);
            if (estimatedRows > 0)
            {
                summary.EstimatedRowCount = Math.Min(estimatedRows, 1000); // Cap at reasonable number
            }
        }
        catch (Exception ex)
        {
            // If extraction fails, just return basic summary
            summary.Columns = new List<string> { "Data extraction failed" };
            summary.SampleData = new Dictionary<string, List<string>>
            {
                { "Error", new List<string> { ex.Message } }
            };
        }

        return summary;
    }
}