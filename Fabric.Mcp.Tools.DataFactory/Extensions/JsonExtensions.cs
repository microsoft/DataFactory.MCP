using Fabric.Mcp.Tools.DataFactory.Configuration;
using System.Text.Json;

namespace Fabric.Mcp.Tools.DataFactory.Extensions;

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
#pragma warning disable IL2026, IL3050
        return JsonSerializer.Serialize(obj, JsonSerializerOptionsProvider.McpResponse);
#pragma warning restore IL2026, IL3050
    }
}