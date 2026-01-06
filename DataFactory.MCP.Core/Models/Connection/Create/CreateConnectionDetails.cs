using System.Text.Json.Serialization;
using DataFactory.MCP.Models.Connection.Create.Parameters;

namespace DataFactory.MCP.Models.Connection.Create;

/// <summary>
/// The connection details input for create operations
/// </summary>
public class CreateConnectionDetails
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("creationMethod")]
    public string CreationMethod { get; set; } = string.Empty;

    [JsonPropertyName("parameters")]
    public List<ConnectionDetailsParameter> Parameters { get; set; } = new();
}