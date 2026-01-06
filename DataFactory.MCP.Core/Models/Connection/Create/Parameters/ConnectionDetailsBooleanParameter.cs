using System.Text.Json.Serialization;

namespace DataFactory.MCP.Models.Connection.Create.Parameters;

/// <summary>
/// ConnectionDetailsParameter for boolean dataType
/// </summary>
public class ConnectionDetailsBooleanParameter : ConnectionDetailsParameter
{
    [JsonPropertyName("value")]
    public bool Value { get; set; }

    public ConnectionDetailsBooleanParameter()
    {
        DataType = "Boolean";
    }
}