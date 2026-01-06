using DataFactory.MCP.Abstractions.Interfaces;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace DataFactory.MCP.Services;

/// <summary>
/// Service for data transformation operations (JSON, Base64, etc.)
/// </summary>
public class DataTransformationService : IDataTransformationService
{
    private readonly ILogger<DataTransformationService> _logger;

    public DataTransformationService(ILogger<DataTransformationService> logger)
    {
        _logger = logger;
    }

    public Dictionary<string, object> JsonElementToDictionary(JsonElement element)
    {
        var dict = new Dictionary<string, object>();

        foreach (var property in element.EnumerateObject())
        {
            dict[property.Name] = JsonElementToObject(property.Value);
        }

        return dict;
    }

    public object JsonElementToObject(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String => element.GetString() ?? "",
            JsonValueKind.Number => element.TryGetInt32(out var intValue) ? intValue : element.GetDouble(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => null!,
            JsonValueKind.Array => element.EnumerateArray().Select(JsonElementToObject).ToList(),
            JsonValueKind.Object => JsonElementToDictionary(element),
            _ => element.GetRawText()
        };
    }

    public JsonElement? ParseJsonContent(string jsonContent)
    {
        try
        {
            // Parse JSON content into JsonElement for structured access
            using var document = JsonDocument.Parse(jsonContent);
            return document.RootElement.Clone();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse JSON content");
            return null;
        }
    }
}