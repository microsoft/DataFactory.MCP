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

    public DataflowDefinition AddOrUpdateQueryInDefinition(
        DataflowDefinition definition,
        string queryName,
        string mCode)
    {
        // Step 1: Update the mashup.pq file with the new/updated query
        var mashupPart = definition.Parts?.FirstOrDefault(p =>
            p.Path?.Equals("mashup.pq", StringComparison.OrdinalIgnoreCase) == true);

        if (mashupPart?.Payload != null)
        {
            var decodedBytes = Convert.FromBase64String(mashupPart.Payload);
            var currentMashup = Encoding.UTF8.GetString(decodedBytes);

            var updatedMashup = UpdateMashupWithQuery(currentMashup, queryName, mCode);

            var updatedBytes = Encoding.UTF8.GetBytes(updatedMashup);
            mashupPart.Payload = Convert.ToBase64String(updatedBytes);
        }

        // Step 2: Update the queryMetadata.json file with the query metadata
        var queryMetadataPart = definition.Parts?.FirstOrDefault(p =>
            p.Path?.Equals("querymetadata.json", StringComparison.OrdinalIgnoreCase) == true);

        if (queryMetadataPart?.Payload != null)
        {
            var decodedBytes = Convert.FromBase64String(queryMetadataPart.Payload);
            var currentMetadataJson = Encoding.UTF8.GetString(decodedBytes);

            using var document = JsonDocument.Parse(currentMetadataJson);
            var metadata = document.RootElement;

            var updatedMetadata = CreateUpdatedQueryMetadataWithQuery(metadata, queryName);

            var updatedMetadataJson = JsonSerializer.Serialize(updatedMetadata, JsonSerializerOptionsProvider.Indented);
            var updatedBytes = Encoding.UTF8.GetBytes(updatedMetadataJson);
            queryMetadataPart.Payload = Convert.ToBase64String(updatedBytes);
        }

        return definition;
    }

    private string UpdateMashupWithQuery(string currentMashup, string queryName, string mCode)
    {
        // Normalize query name for M code (handle special characters)
        var normalizedQueryName = NormalizeQueryName(queryName);
        var sharedDeclaration = $"shared {normalizedQueryName} =";

        // Check if query already exists
        var lines = currentMashup.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None).ToList();
        var queryStartIndex = -1;
        var queryEndIndex = -1;

        for (int i = 0; i < lines.Count; i++)
        {
            if (lines[i].TrimStart().StartsWith(sharedDeclaration, StringComparison.OrdinalIgnoreCase))
            {
                queryStartIndex = i;
                // Find the end of this query (next "shared" or end of file)
                for (int j = i + 1; j < lines.Count; j++)
                {
                    if (lines[j].TrimStart().StartsWith("shared ", StringComparison.OrdinalIgnoreCase))
                    {
                        queryEndIndex = j - 1;
                        break;
                    }
                }
                if (queryEndIndex == -1) queryEndIndex = lines.Count - 1;
                break;
            }
        }

        // Build the new query declaration
        var newQueryCode = BuildQueryDeclaration(normalizedQueryName, mCode);

        if (queryStartIndex >= 0)
        {
            // Replace existing query
            lines.RemoveRange(queryStartIndex, queryEndIndex - queryStartIndex + 1);
            lines.Insert(queryStartIndex, newQueryCode);
        }
        else
        {
            // Add new query at the end
            // Ensure there's a section declaration if the mashup is empty or only has section
            if (string.IsNullOrWhiteSpace(currentMashup) || currentMashup.Trim() == "section Section1;")
            {
                return $"section Section1;\r\n{newQueryCode}";
            }
            lines.Add(newQueryCode);
        }

        return string.Join("\r\n", lines);
    }

    private string NormalizeQueryName(string queryName)
    {
        // If query name contains spaces or special characters, wrap in #""
        if (queryName.Contains(' ') || queryName.Contains('(') || queryName.Contains(')'))
        {
            return $"#\"{queryName}\"";
        }
        return queryName;
    }

    private string BuildQueryDeclaration(string normalizedQueryName, string mCode)
    {
        // Ensure the M code starts with "let" and ends with proper structure
        var trimmedCode = mCode.Trim();

        // If the code doesn't start with "let", wrap it
        if (!trimmedCode.StartsWith("let", StringComparison.OrdinalIgnoreCase))
        {
            trimmedCode = $"let\n    Source = {trimmedCode}\nin\n    Source";
        }

        // Ensure it ends with a semicolon for the shared declaration
        return $"shared {normalizedQueryName} = {trimmedCode};";
    }

    private Dictionary<string, object> CreateUpdatedQueryMetadataWithQuery(
        JsonElement currentMetadata,
        string queryName)
    {
        var metadataDict = _dataTransformationService.JsonElementToDictionary(currentMetadata);

        // Handle queriesMetadata object
        if (!metadataDict.ContainsKey("queriesMetadata"))
        {
            metadataDict["queriesMetadata"] = new Dictionary<string, object>();
        }

        var queriesMetadata = metadataDict["queriesMetadata"] as Dictionary<string, object>
            ?? new Dictionary<string, object>();

        // Check if query already exists
        if (!queriesMetadata.ContainsKey(queryName))
        {
            // Add new query metadata with a generated GUID
            var queryId = Guid.NewGuid().ToString();
            queriesMetadata[queryName] = new Dictionary<string, object>
            {
                ["queryId"] = queryId,
                ["queryName"] = queryName
            };
        }

        metadataDict["queriesMetadata"] = queriesMetadata;
        return metadataDict;
    }
}