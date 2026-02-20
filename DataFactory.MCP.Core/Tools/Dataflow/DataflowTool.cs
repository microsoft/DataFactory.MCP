using ModelContextProtocol.Server;
using System.ComponentModel;
using DataFactory.MCP.Abstractions.Interfaces;
using DataFactory.MCP.Extensions;
using DataFactory.MCP.Models.Dataflow;
using DataFactory.MCP.Models.Dataflow.Definition;
using Apache.Arrow;
using System.Text.Json;

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

    [McpServerTool, Description(@"Patch arbitrary fields in queryMetadata.json for a specific query within a dataflow. Think of it as jq for dataflow metadata — a general-purpose wrench, not a single-purpose screwdriver.")]
    public async Task<string> UpdateQueryMetadata(
        [Description("The workspace ID containing the dataflow (required)")] string workspaceId,
        [Description("The dataflow ID to update the metadata for (required)")] string dataflowId,
        [Description("The name of the query to update (required)")] string queryName,
        [Description("JSON to deep-merge into the query's metadata")] object metadataPatchObject)
    {
        try
        {
            _validationService.ValidateRequiredString(workspaceId, nameof(workspaceId));
            _validationService.ValidateRequiredString(dataflowId, nameof(dataflowId));
            _validationService.ValidateRequiredString(queryName, nameof(queryName));

            MetadataPatch? metadataPatch = ParseMetadataPatch(metadataPatchObject);
            if (metadataPatch == null)
            {
                var errorResponse = new
                {
                    Success = false,
                    DataflowId = dataflowId,
                    WorkspaceId = workspaceId,
                    QueryName = queryName,
                    Message = "Invalid metadata patch format. Must be a JSON object with the fields to update."
                };
                return errorResponse.ToMcpJson();
            }

            // Step 1: Get the raw dataflow definition
            var definition = await _dataflowService.GetDataflowDefinitionAsync(workspaceId, dataflowId);
            var queryMetadataPart = definition.Parts?.FirstOrDefault(p =>
                p.Path?.Equals("querymetadata.json", StringComparison.OrdinalIgnoreCase) == true);

            if (queryMetadataPart?.Payload == null)
            {
                var errorResponse = new
                {
                    Success = false,
                    DataflowId = dataflowId,
                    WorkspaceId = workspaceId,
                    QueryName = queryName,
                    Message = "Dataflow definition does not contain queryMetadata.json"
                };
                return errorResponse.ToMcpJson();
            }

            // Step 2: Decode the queryMetadata.json from Base64
            var decodedBytes = Convert.FromBase64String(queryMetadataPart.Payload);
            var metadataJson = System.Text.Encoding.UTF8.GetString(decodedBytes);

            using var document = JsonDocument.Parse(metadataJson);
            var metadataDict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(metadataJson)
                ?? new Dictionary<string, JsonElement>();

            // Step 3: Find and update the query entry in queriesMetadata
            if (!metadataDict.TryGetValue("queriesMetadata", out var queriesMetadataElement))
            {
                var errorResponse = new
                {
                    Success = false,
                    DataflowId = dataflowId,
                    WorkspaceId = workspaceId,
                    QueryName = queryName,
                    Message = "queryMetadata.json does not contain a queriesMetadata section"
                };
                return errorResponse.ToMcpJson();
            }

            var queriesMetadata = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, object>>>(
                queriesMetadataElement.GetRawText()) ?? new Dictionary<string, Dictionary<string, object>>();

            if (!queriesMetadata.TryGetValue(queryName, out var queryEntry))
            {
                var errorResponse = new
                {
                    Success = false,
                    DataflowId = dataflowId,
                    WorkspaceId = workspaceId,
                    QueryName = queryName,
                    Message = $"Query '{queryName}' not found in queriesMetadata"
                };
                return errorResponse.ToMcpJson();
            }

            // Step 4: Apply the patch
            if (metadataPatch.LoadEnabled.HasValue)
            {
                queryEntry["loadEnabled"] = metadataPatch.LoadEnabled.Value;
            }

            if (metadataPatch.IsHidden.HasValue)
            {
                queryEntry["isHidden"] = metadataPatch.IsHidden.Value;
            }

            if (metadataPatch.DestinationSettings != null)
            {
                queryEntry["destinationSettings"] = metadataPatch.DestinationSettings;
            }

            queriesMetadata[queryName] = queryEntry;

            // Step 5: Reconstruct the full queryMetadata JSON
            var fullMetadata = JsonSerializer.Deserialize<Dictionary<string, object>>(metadataJson)
                ?? new Dictionary<string, object>();
            fullMetadata["queriesMetadata"] = queriesMetadata;

            var updatedMetadataJson = JsonSerializer.Serialize(fullMetadata, new JsonSerializerOptions { WriteIndented = true });
            var updatedBytes = System.Text.Encoding.UTF8.GetBytes(updatedMetadataJson);
            queryMetadataPart.Payload = Convert.ToBase64String(updatedBytes);

            // Step 6: Save the updated definition
            await _dataflowService.UpdateDataflowDefinitionAsync(workspaceId, dataflowId, definition);

            var result = new
            {
                Success = true,
                DataflowId = dataflowId,
                WorkspaceId = workspaceId,
                QueryName = queryName,
                Message = $"Successfully updated metadata for query '{queryName}' in dataflow {dataflowId}",
                AppliedPatch = new
                {
                    metadataPatch.LoadEnabled
                }
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
            return ex.ToOperationError("updating query metadata").ToMcpJson();
        }
    }

    private static readonly JsonSerializerOptions _patchDeserializationOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private static MetadataPatch? ParseMetadataPatch(object metadataPatch)
    {
        if (metadataPatch is System.Text.Json.JsonElement jsonElement)
        {
            if (jsonElement.ValueKind == System.Text.Json.JsonValueKind.Object)
            {
                return jsonElement.Deserialize<MetadataPatch>(_patchDeserializationOptions);
            }
            else if (jsonElement.ValueKind == System.Text.Json.JsonValueKind.String)
            {
                var value = jsonElement.GetString();
                if (!string.IsNullOrWhiteSpace(value))
                {
                    return System.Text.Json.JsonSerializer.Deserialize<MetadataPatch>(value, _patchDeserializationOptions);
                }
            }
        }
        else if (metadataPatch is string jsonString && !string.IsNullOrWhiteSpace(jsonString))
        {
            return System.Text.Json.JsonSerializer.Deserialize<MetadataPatch>(jsonString, _patchDeserializationOptions);
        }

        return null;
    }

}
