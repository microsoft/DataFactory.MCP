using System.Text.Json;

namespace DataFactory.MCP.Extensions;

/// <summary>
/// Extension methods for consistent JSON serialization across MCP tools
/// </summary>
public static class JsonExtensions
{
    /// <summary>
    /// Standard JSON serialization options for MCP tool responses
    /// </summary>
    private static readonly JsonSerializerOptions McpJsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <summary>
    /// Serializes an object to JSON using consistent MCP formatting
    /// </summary>
    /// <param name="obj">The object to serialize</param>
    /// <returns>The JSON string representation</returns>
    public static string ToMcpJson(this object obj)
    {
        return JsonSerializer.Serialize(obj, McpJsonOptions);
    }
}