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

    public DataflowDefinition AddConnectionsToDefinition(
        DataflowDefinition definition,
        IEnumerable<(Connection Connection, string ConnectionId, string? ClusterId)> connections)
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

            // Create updated metadata with all connections
            var updatedMetadata = CreateUpdatedQueryMetadataWithConnections(metadata, connections);

            // Encode updated metadata back to Base64
            var updatedMetadataJson = JsonSerializer.Serialize(updatedMetadata, JsonSerializerOptionsProvider.Indented);
            var updatedBytes = Encoding.UTF8.GetBytes(updatedMetadataJson);
            queryMetadataPart.Payload = Convert.ToBase64String(updatedBytes);
        }

        return definition;
    }

    private Dictionary<string, object> CreateUpdatedQueryMetadataWithConnections(
        JsonElement currentMetadata,
        IEnumerable<(Connection Connection, string ConnectionId, string? ClusterId)> connections)
    {
        // Convert the entire JsonElement to a Dictionary for easier manipulation
        var metadataDict = _dataTransformationService.JsonElementToDictionary(currentMetadata);

        // Ensure documentLocale is set (required for proper date/number parsing)
        if (!metadataDict.ContainsKey("documentLocale"))
        {
            metadataDict["documentLocale"] = "en-US";
        }

        // Handle connections array
        if (!metadataDict.ContainsKey("connections"))
        {
            metadataDict["connections"] = new List<object>();
        }

        var connectionsList = metadataDict["connections"] as List<object> ?? new List<object>();

        foreach (var (connection, connectionId, clusterId) in connections)
        {
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
                // Add new connection
                var newConnection = new Dictionary<string, object>
                {
                    ["connectionId"] = connectionIdValue,
                    ["kind"] = connection.ConnectionDetails.Type,
                    ["path"] = connection.ConnectionDetails.Path
                };
                connectionsList.Add(newConnection);
            }
        }

        metadataDict["connections"] = connectionsList;
        return metadataDict;
    }

    public DataflowDefinition AddOrUpdateQueryInDefinition(
        DataflowDefinition definition,
        string queryName,
        string mCode,
        string? attribute = null,
        string? sectionAttribute = null)
    {
        // Step 1: Update the mashup.pq file with the new/updated query
        var mashupPart = definition.Parts?.FirstOrDefault(p =>
            p.Path?.Equals("mashup.pq", StringComparison.OrdinalIgnoreCase) == true);

        if (mashupPart?.Payload != null)
        {
            var decodedBytes = Convert.FromBase64String(mashupPart.Payload);
            var currentMashup = Encoding.UTF8.GetString(decodedBytes);

            var updatedMashup = UpdateMashupWithQuery(currentMashup, queryName, mCode, attribute, sectionAttribute);

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

            // Extract destination query name from [DataDestinations] attribute if present
            var referencedDestinationQuery = ExtractDestinationQueryNameFromAttribute(attribute);

            var updatedMetadata = CreateUpdatedQueryMetadataWithQuery(metadata, queryName, referencedDestinationQuery);

            var updatedMetadataJson = JsonSerializer.Serialize(updatedMetadata, JsonSerializerOptionsProvider.Indented);
            var updatedBytes = Encoding.UTF8.GetBytes(updatedMetadataJson);
            queryMetadataPart.Payload = Convert.ToBase64String(updatedBytes);
        }

        return definition;
    }

    /// <summary>
    /// Extracts the destination query name from a [DataDestinations] attribute.
    /// Example: [DataDestinations = {[Definition = [Kind = "Reference", QueryName = "Customers_DataDestination", ...]]}]
    /// Returns: "Customers_DataDestination"
    /// </summary>
    private string? ExtractDestinationQueryNameFromAttribute(string? attribute)
    {
        if (string.IsNullOrEmpty(attribute) || !attribute.Contains("[DataDestinations"))
            return null;

        // Parse QueryName from the attribute using regex
        var match = System.Text.RegularExpressions.Regex.Match(
            attribute,
            @"QueryName\s*=\s*""([^""]+)""",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        return match.Success ? match.Groups[1].Value : null;
    }

    private string UpdateMashupWithQuery(string currentMashup, string queryName, string mCode, string? attribute = null, string? sectionAttribute = null)
    {
        // Normalize query name for M code (handle special characters)
        var normalizedQueryName = NormalizeQueryName(queryName);
        var sharedDeclaration = $"shared {normalizedQueryName} =";

        // Check if query already exists - also check for attribute prefix
        var lines = currentMashup.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None).ToList();
        var queryStartIndex = -1;
        var queryEndIndex = -1;

        for (int i = 0; i < lines.Count; i++)
        {
            var trimmedLine = lines[i].TrimStart();

            // Check if this line contains the shared declaration directly
            if (trimmedLine.StartsWith(sharedDeclaration, StringComparison.OrdinalIgnoreCase))
            {
                // Look back to see if there's an attribute on a previous line
                queryStartIndex = i;
                for (int k = i - 1; k >= 0; k--)
                {
                    var prevTrimmed = lines[k].TrimStart();
                    // If we hit another shared declaration or section, stop looking back
                    if (prevTrimmed.StartsWith("shared ", StringComparison.OrdinalIgnoreCase) ||
                        prevTrimmed.StartsWith("section ", StringComparison.OrdinalIgnoreCase) ||
                        string.IsNullOrWhiteSpace(prevTrimmed))
                    {
                        break;
                    }
                    // If we find an attribute (starts with [ and not [StagingDefinition), include it
                    if (prevTrimmed.StartsWith("[") && !prevTrimmed.StartsWith("[StagingDefinition", StringComparison.OrdinalIgnoreCase))
                    {
                        queryStartIndex = k;
                        // Continue looking back in case of multi-line attributes
                    }
                    // Also handle continuation lines of multi-line attributes (lines that don't start with [ or shared)
                    else if (prevTrimmed.Contains("]]]}]") || prevTrimmed.Contains("]]") ||
                             prevTrimmed.StartsWith("Settings") || prevTrimmed.StartsWith("Definition"))
                    {
                        queryStartIndex = k;
                    }
                }

                // Find the end of this query (next attribute/shared or end of file)
                for (int j = i + 1; j < lines.Count; j++)
                {
                    var nextTrimmed = lines[j].TrimStart();
                    if (nextTrimmed.StartsWith("shared ", StringComparison.OrdinalIgnoreCase) ||
                        (nextTrimmed.StartsWith("[") && !nextTrimmed.StartsWith("[StagingDefinition", StringComparison.OrdinalIgnoreCase)))
                    {
                        queryEndIndex = j - 1;
                        break;
                    }
                }
                if (queryEndIndex == -1) queryEndIndex = lines.Count - 1;
                break;
            }

            // Also check if this line starts an attribute and a subsequent line has the shared declaration
            // (to handle multi-line attributes where shared is not on line i+1)
            if (trimmedLine.StartsWith("[") && !trimmedLine.StartsWith("[StagingDefinition", StringComparison.OrdinalIgnoreCase))
            {
                // Look ahead for the shared declaration (up to 10 lines for multi-line attributes)
                for (int lookAhead = 1; lookAhead <= 10 && i + lookAhead < lines.Count; lookAhead++)
                {
                    var aheadTrimmed = lines[i + lookAhead].TrimStart();
                    if (aheadTrimmed.StartsWith(sharedDeclaration, StringComparison.OrdinalIgnoreCase))
                    {
                        queryStartIndex = i;

                        // Find the end of this query
                        for (int j = i + lookAhead + 1; j < lines.Count; j++)
                        {
                            var nextTrimmed = lines[j].TrimStart();
                            if (nextTrimmed.StartsWith("shared ", StringComparison.OrdinalIgnoreCase) ||
                                (nextTrimmed.StartsWith("[") && !nextTrimmed.StartsWith("[StagingDefinition", StringComparison.OrdinalIgnoreCase)))
                            {
                                queryEndIndex = j - 1;
                                break;
                            }
                        }
                        if (queryEndIndex == -1) queryEndIndex = lines.Count - 1;
                        break;
                    }
                    // If we hit another shared declaration, stop looking
                    if (aheadTrimmed.StartsWith("shared ", StringComparison.OrdinalIgnoreCase))
                    {
                        break;
                    }
                }
                if (queryStartIndex >= 0) break;
            }
        }

        // Build the new query declaration with attribute
        var newQueryCode = BuildQueryDeclaration(normalizedQueryName, mCode, attribute);

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
                var sectionDecl = !string.IsNullOrEmpty(sectionAttribute)
                    ? $"{sectionAttribute}\r\nsection Section1;"
                    : "section Section1;";
                return $"{sectionDecl}\r\n{newQueryCode}";
            }
            lines.Add(newQueryCode);
        }

        // Handle section-level attribute if provided and not already present
        var result = string.Join("\r\n", lines);
        if (!string.IsNullOrEmpty(sectionAttribute) && !result.Contains("[StagingDefinition"))
        {
            // Insert section attribute before "section Section1;"
            result = result.Replace("section Section1;", $"{sectionAttribute}\r\nsection Section1;");
        }

        return result;
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

    private string BuildQueryDeclaration(string normalizedQueryName, string mCode, string? attribute = null)
    {
        // Ensure the M code starts with "let" and ends with proper structure
        var trimmedCode = mCode.Trim();

        // If the code doesn't start with "let", wrap it
        if (!trimmedCode.StartsWith("let", StringComparison.OrdinalIgnoreCase))
        {
            trimmedCode = $"let\n    Source = {trimmedCode}\nin\n    Source";
        }

        // Build declaration with optional attribute
        if (!string.IsNullOrEmpty(attribute))
        {
            return $"{attribute}\r\nshared {normalizedQueryName} = {trimmedCode};";
        }

        // Ensure it ends with a semicolon for the shared declaration
        return $"shared {normalizedQueryName} = {trimmedCode};";
    }

    private Dictionary<string, object> CreateUpdatedQueryMetadataWithQuery(
        JsonElement currentMetadata,
        string queryName,
        string? referencedDestinationQuery = null)
    {
        var metadataDict = _dataTransformationService.JsonElementToDictionary(currentMetadata);

        // Ensure documentLocale is set (required for proper date/number parsing)
        if (!metadataDict.ContainsKey("documentLocale"))
        {
            metadataDict["documentLocale"] = "en-US";
        }

        // Handle queriesMetadata object
        if (!metadataDict.ContainsKey("queriesMetadata"))
        {
            metadataDict["queriesMetadata"] = new Dictionary<string, object>();
        }

        var queriesMetadata = metadataDict["queriesMetadata"] as Dictionary<string, object>
            ?? new Dictionary<string, object>();

        // Add/update the current query
        if (!queriesMetadata.ContainsKey(queryName))
        {
            var queryId = Guid.NewGuid().ToString();
            var queryMetadataEntry = new Dictionary<string, object>
            {
                ["queryId"] = queryId,
                ["queryName"] = queryName,
                ["loadEnabled"] = false
            };
            queriesMetadata[queryName] = queryMetadataEntry;
        }
        else if (queriesMetadata[queryName] is Dictionary<string, object> existingEntry)
        {
            if (!existingEntry.ContainsKey("loadEnabled"))
            {
                existingEntry["loadEnabled"] = false;
            }
        }

        // If this query has a [DataDestinations] attribute referencing another query,
        // mark that referenced query as hidden (it's a destination helper query)
        if (!string.IsNullOrEmpty(referencedDestinationQuery))
        {
            if (queriesMetadata.ContainsKey(referencedDestinationQuery) &&
                queriesMetadata[referencedDestinationQuery] is Dictionary<string, object> destEntry)
            {
                destEntry["isHidden"] = true;
            }
            // If the destination query doesn't exist yet, create a placeholder that will be hidden
            // (it will be fully populated when the destination query is added)
            else if (!queriesMetadata.ContainsKey(referencedDestinationQuery))
            {
                queriesMetadata[referencedDestinationQuery] = new Dictionary<string, object>
                {
                    ["queryId"] = Guid.NewGuid().ToString(),
                    ["queryName"] = referencedDestinationQuery,
                    ["isHidden"] = true,
                    ["loadEnabled"] = false
                };
            }
        }

        metadataDict["queriesMetadata"] = queriesMetadata;
        return metadataDict;
    }

    public DataflowDefinition SyncMashupInDefinition(
        DataflowDefinition definition,
        string newMashupDocument,
        IList<(string QueryName, string MCode, string? Attribute)> parsedQueries)
    {
        // Step 1: Replace the entire mashup.pq with the new document
        var mashupPart = definition.Parts?.FirstOrDefault(p =>
            p.Path?.Equals("mashup.pq", StringComparison.OrdinalIgnoreCase) == true);

        if (mashupPart != null)
        {
            var updatedBytes = Encoding.UTF8.GetBytes(newMashupDocument);
            mashupPart.Payload = Convert.ToBase64String(updatedBytes);
            _logger.LogDebug("Replaced mashup.pq with new document ({Length} bytes)", newMashupDocument.Length);
        }

        // Step 2: Sync queryMetadata.json to match the queries in the new document
        var queryMetadataPart = definition.Parts?.FirstOrDefault(p =>
            p.Path?.Equals("querymetadata.json", StringComparison.OrdinalIgnoreCase) == true);

        if (queryMetadataPart?.Payload != null)
        {
            var decodedBytes = Convert.FromBase64String(queryMetadataPart.Payload);
            var currentMetadataJson = Encoding.UTF8.GetString(decodedBytes);

            using var document = JsonDocument.Parse(currentMetadataJson);
            var currentMetadata = document.RootElement;

            var updatedMetadata = SyncQueryMetadata(currentMetadata, parsedQueries);

            var updatedMetadataJson = JsonSerializer.Serialize(updatedMetadata, JsonSerializerOptionsProvider.Indented);
            var updatedMetadataBytes = Encoding.UTF8.GetBytes(updatedMetadataJson);
            queryMetadataPart.Payload = Convert.ToBase64String(updatedMetadataBytes);

            _logger.LogDebug("Synced queryMetadata.json with {QueryCount} queries", parsedQueries.Count);
        }

        return definition;
    }

    private Dictionary<string, object> SyncQueryMetadata(
        JsonElement currentMetadata,
        IList<(string QueryName, string MCode, string? Attribute)> parsedQueries)
    {
        var metadataDict = _dataTransformationService.JsonElementToDictionary(currentMetadata);

        // Ensure documentLocale is set
        if (!metadataDict.ContainsKey("documentLocale"))
        {
            metadataDict["documentLocale"] = "en-US";
        }

        // Build a set of query names that reference DataDestinations
        // These referenced queries should be marked as hidden
        var destinationQueries = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var (_, _, attribute) in parsedQueries)
        {
            if (!string.IsNullOrEmpty(attribute))
            {
                var referencedQuery = ExtractDestinationQueryNameFromAttribute(attribute);
                if (!string.IsNullOrEmpty(referencedQuery))
                {
                    destinationQueries.Add(referencedQuery);
                }
            }
        }

        // Build new queriesMetadata from the parsed queries
        var newQueriesMetadata = new Dictionary<string, object>();

        // Get existing queriesMetadata to preserve queryIds where possible
        var existingQueriesMetadata = metadataDict.ContainsKey("queriesMetadata")
            ? metadataDict["queriesMetadata"] as Dictionary<string, object> ?? new Dictionary<string, object>()
            : new Dictionary<string, object>();

        foreach (var (queryName, _, _) in parsedQueries)
        {
            // Try to preserve existing queryId if the query already exists
            string queryId;
            if (existingQueriesMetadata.TryGetValue(queryName, out var existingEntry) &&
                existingEntry is Dictionary<string, object> existingDict &&
                existingDict.TryGetValue("queryId", out var existingId))
            {
                queryId = existingId?.ToString() ?? Guid.NewGuid().ToString();
            }
            else
            {
                queryId = Guid.NewGuid().ToString();
            }

            var queryMetadataEntry = new Dictionary<string, object>
            {
                ["queryId"] = queryId,
                ["queryName"] = queryName,
                ["loadEnabled"] = false
            };

            // Mark destination queries as hidden
            if (destinationQueries.Contains(queryName))
            {
                queryMetadataEntry["isHidden"] = true;
            }

            newQueriesMetadata[queryName] = queryMetadataEntry;
        }

        metadataDict["queriesMetadata"] = newQueriesMetadata;

        _logger.LogDebug("Synced query metadata: {QueryCount} queries, {HiddenCount} hidden",
            parsedQueries.Count, destinationQueries.Count);

        return metadataDict;
    }
}