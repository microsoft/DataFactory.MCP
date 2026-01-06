using System.Text.Json.Serialization;

namespace DataFactory.MCP.Models.Connection.Create.Parameters;

/// <summary>
/// Base class for connection parameters
/// </summary>
public abstract class ConnectionDetailsParameter
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("dataType")]
    public string DataType { get; set; } = string.Empty;
}