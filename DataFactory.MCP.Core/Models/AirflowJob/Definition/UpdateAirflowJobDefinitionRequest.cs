using System.Text.Json.Serialization;

namespace DataFactory.MCP.Models.AirflowJob.Definition;

/// <summary>
/// Request model for updating an Apache Airflow Job definition
/// </summary>
public class UpdateAirflowJobDefinitionRequest
{
    /// <summary>
    /// The definition to update
    /// </summary>
    [JsonPropertyName("definition")]
    public AirflowJobDefinition Definition { get; set; } = new();
}
