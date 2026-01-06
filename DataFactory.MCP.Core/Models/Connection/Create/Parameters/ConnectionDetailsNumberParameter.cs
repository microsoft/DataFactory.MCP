using System.Text.Json.Serialization;

namespace DataFactory.MCP.Models.Connection.Create.Parameters;

/// <summary>
/// ConnectionDetailsParameter for number dataType
/// </summary>
public class ConnectionDetailsNumberParameter : ConnectionDetailsParameter
{
    [JsonPropertyName("value")]
    public double Value { get; set; }

    public ConnectionDetailsNumberParameter()
    {
        DataType = "Number";
    }
}