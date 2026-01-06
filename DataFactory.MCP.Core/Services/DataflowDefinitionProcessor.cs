using DataFactory.MCP.Abstractions;
using DataFactory.MCP.Abstractions.Interfaces;
using DataFactory.MCP.Configuration;
using DataFactory.MCP.Models.Connection;
using DataFactory.MCP.Models.Dataflow;
using DataFactory.MCP.Models.Dataflow.Definition;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;

namespace DataFactory.MCP.Services;

/// <summary>
/// Service for processing and transforming dataflow definitions
/// </summary>
public class DataflowDefinitionProcessor : IDataflowDefinitionProcessor
{
    private readonly ILogger<DataflowDefinitionProcessor> _logger;
    private readonly IDataTransformationService _dataTransformationService;

    public DataflowDefinitionProcessor(
        ILogger<DataflowDefinitionProcessor> logger,
        IDataTransformationService dataTransformationService)
    {
        _logger = logger;
        _dataTransformationService = dataTransformationService;
    }

    public DecodedDataflowDefinition DecodeDefinition(DataflowDefinition rawDefinition)
    {
        var decoded = new DecodedDataflowDefinition
        {
            RawParts = rawDefinition?.Parts ?? new List<DataflowDefinitionPart>()
        };

        foreach (var part in decoded.RawParts)
        {
            if (string.IsNullOrEmpty(part.Payload)) continue;

            try
            {
                var decodedBytes = Convert.FromBase64String(part.Payload);
                var decodedText = Encoding.UTF8.GetString(decodedBytes);

                switch (part.Path.ToLowerInvariant())
                {
                    case "querymetadata.json":
                        decoded.QueryMetadata = _dataTransformationService.ParseJsonContent(decodedText);
                        break;
                    case "mashup.pq":
                        decoded.MashupQuery = decodedText;
                        break;
                    case ".platform":
                        decoded.PlatformMetadata = _dataTransformationService.ParseJsonContent(decodedText);
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to decode part {PartPath}", part.Path);
            }
        }

        return decoded;
    }

    public DataflowDefinition AddConnectionToDefinition(
        DataflowDefinition definition,
        Connection connection,
        string connectionId,
        string? clusterId)
    {
        // Find and update the queryMetadata.json part
        var queryMetadataPart = definition.Parts?.FirstOrDefault(p =>
            p.Path?.Equals("querymetadata.json", StringComparison.OrdinalIgnoreCase) == true);

        if (queryMetadataPart?.Payload != null)
        {
            // Decode current queryMetadata
            var decodedBytes = Convert.FromBase64String(queryMetadataPart.Payload);
            var currentMetadataJson = Encoding.UTF8.GetString(decodedBytes);

            using var document = JsonDocument.Parse(currentMetadataJson);
            var metadata = document.RootElement;

            // Create updated metadata with new connection
            var updatedMetadata = CreateUpdatedQueryMetadata(metadata, connection, connectionId, clusterId);

            // Encode updated metadata back to Base64
            var updatedMetadataJson = JsonSerializer.Serialize(updatedMetadata, JsonSerializerOptionsProvider.Indented);
            var updatedBytes = Encoding.UTF8.GetBytes(updatedMetadataJson);
            queryMetadataPart.Payload = Convert.ToBase64String(updatedBytes);
        }

        return definition;
    }

    private Dictionary<string, object> CreateUpdatedQueryMetadata(
        JsonElement currentMetadata,
        Connection connection,
        string connectionId,
        string? clusterId)
    {
        // Convert the entire JsonElement to a Dictionary for easier manipulation
        var metadataDict = _dataTransformationService.JsonElementToDictionary(currentMetadata);

        // Handle connections array
        if (!metadataDict.ContainsKey("connections"))
        {
            metadataDict["connections"] = new List<object>();
        }

        var connectionsList = metadataDict["connections"] as List<object> ?? new List<object>();

        // Build the connectionId value for the metadata
        // If we have a ClusterId, use the format: {"ClusterId":"...","DatasourceId":"..."}
        // This format is required for proper credential binding in Fabric dataflows
        string connectionIdValue;
        if (!string.IsNullOrEmpty(clusterId))
        {
            // Use the ClusterId + DatasourceId format that Fabric UI uses
            // This enables proper credential binding for query execution
            var connectionIdObj = new Dictionary<string, string>
            {
                ["ClusterId"] = clusterId,
                ["DatasourceId"] = connectionId
            };
            connectionIdValue = JsonSerializer.Serialize(connectionIdObj);
        }
        else
        {
            // Fall back to plain connectionId if ClusterId is not available
            connectionIdValue = connectionId;
        }

        // Add new connection if it doesn't already exist
        var newConnection = new Dictionary<string, object>
        {
            ["connectionId"] = connectionIdValue,
            ["kind"] = connection.ConnectionDetails.Type,
            ["path"] = connection.ConnectionDetails.Path
        };

        // Check if connection already exists (check for both old format and new Fabric format)
        bool connectionExists = connectionsList.Any(conn =>
        {
            if (conn is Dictionary<string, object> dict && dict.ContainsKey("connectionId"))
            {
                var existingConnectionId = dict["connectionId"]?.ToString();
                // Check if it matches the plain connectionId or the Fabric format
                return existingConnectionId == connectionId ||
                       existingConnectionId == connectionIdValue ||
                       existingConnectionId?.Contains($"\"DatasourceId\":\"{connectionId}\"") == true;
            }
            return false;
        });

        if (!connectionExists)
        {
            connectionsList.Add(newConnection);
        }

        metadataDict["connections"] = connectionsList;
        return metadataDict;
    }
}