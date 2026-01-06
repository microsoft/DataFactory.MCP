using System.Text.Json;
using System.Text.Json.Serialization;
using DataFactory.MCP.Models.Connection;
using DataFactory.MCP.Models.Gateway;

namespace DataFactory.MCP.Configuration;

/// <summary>
/// Provides centralized, reusable JsonSerializerOptions instances for consistent JSON serialization.
/// Using static instances is recommended by Microsoft for performance - creating JsonSerializerOptions 
/// is expensive and instances are thread-safe for reading.
/// </summary>
public static class JsonSerializerOptionsProvider
{
    /// <summary>
    /// Options for serializing/deserializing Fabric API payloads.
    /// Uses camelCase naming, ignores null values, and supports enum serialization as strings.
    /// Includes all polymorphic converters for Fabric types (Connection, Gateway, Credentials, etc.).
    /// </summary>
    public static JsonSerializerOptions FabricApi { get; } = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters =
        {
            new JsonStringEnumConverter(),
            new ConnectionJsonConverter(),
            new CredentialsJsonConverter(),
            new ConnectionDetailsParameterJsonConverter(),
            new GatewayJsonConverter()
        }
    };

    /// <summary>
    /// Options for deserializing responses from external APIs (Azure, etc.) where property names may vary.
    /// Uses case-insensitive property matching for maximum compatibility.
    /// </summary>
    public static JsonSerializerOptions CaseInsensitive { get; } = new()
    {
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    /// Options for MCP tool responses with human-readable formatting.
    /// Uses indented output and camelCase naming.
    /// </summary>
    public static JsonSerializerOptions McpResponse { get; } = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <summary>
    /// Options for pretty-printing JSON (indented output only).
    /// Useful for logging or debugging purposes.
    /// </summary>
    public static JsonSerializerOptions Indented { get; } = new()
    {
        WriteIndented = true
    };
}
