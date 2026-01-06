using System.Text.Json.Serialization;

namespace DataFactory.MCP.Models.Connection.Create.Parameters;

/// <summary>
/// ConnectionDetailsParameter for text dataType
/// </summary>
public class ConnectionDetailsTextParameter : ConnectionDetailsParameter
{
    [JsonPropertyName("value")]
    public string Value { get; set; } = string.Empty;

    public ConnectionDetailsTextParameter()
    {
        DataType = "Text";
    }
}