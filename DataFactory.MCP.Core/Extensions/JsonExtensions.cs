using DataFactory.MCP.Configuration;
using System.Text.Json;

namespace DataFactory.MCP.Extensions;

/// <summary>
/// Extension methods for consistent JSON serialization across MCP tools
/// </summary>
public static class JsonExtensions
{
    /// <summary>
    /// Serializes an object to JSON using consistent MCP formatting
    /// </summary>
    /// <param name="obj">The object to serialize</param>
    /// <returns>The JSON string representation</returns>
    public static string ToMcpJson(this object obj)
    {
        return JsonSerializer.Serialize(obj, JsonSerializerOptionsProvider.McpResponse);
    }
}