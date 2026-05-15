using System.Text.Json.Serialization;

namespace DataFactory.MCP.Models.AirflowJob.Definition;

/// <summary>
/// Internal class for HTTP response deserialization when fetching Airflow Job definitions
/// </summary>
internal sealed class GetAirflowJobDefinitionResponse
{
    [JsonPropertyName("definition")]
    public AirflowJobDefinition Definition { get; set; } = new();
}
