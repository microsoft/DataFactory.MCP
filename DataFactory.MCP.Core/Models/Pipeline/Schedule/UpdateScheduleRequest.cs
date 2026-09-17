using System.Text.Json;
using System.Text.Json.Serialization;

namespace DataFactory.MCP.Models.Pipeline.Schedule;

/// <summary>
/// Request payload for updating a pipeline schedule (e.g., enabling or disabling)
/// </summary>
public class UpdateScheduleRequest
{
    /// <summary>
    /// Whether this schedule is enabled
    /// </summary>
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; }

    /// <summary>
    /// The schedule configuration (required by Fabric API on update)
    /// </summary>
    [JsonPropertyName("configuration")]
    public JsonElement Configuration { get; set; }
}
