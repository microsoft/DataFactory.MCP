using System.Text.Json;

namespace DataFactory.MCP.Abstractions.Interfaces;

/// <summary>
/// Service for data transformation operations (JSON, Base64, etc.)
/// </summary>
public interface IDataTransformationService
{
    /// <summary>
    /// Converts a JsonElement to a Dictionary for easier manipulation
    /// </summary>
    /// <param name="element">The JsonElement to convert</param>
    /// <returns>Dictionary representation of the JsonElement</returns>
    Dictionary<string, object> JsonElementToDictionary(JsonElement element);

    /// <summary>
    /// Converts a JsonElement value to appropriate object type
    /// </summary>
    /// <param name="element">The JsonElement to convert</param>
    /// <returns>Converted object</returns>
    object JsonElementToObject(JsonElement element);

    /// <summary>
    /// Parses JSON content safely and returns JsonElement
    /// </summary>
    /// <param name="jsonContent">The JSON string to parse</param>
    /// <returns>JsonElement if successful, null if parsing fails</returns>
    JsonElement? ParseJsonContent(string jsonContent);
}