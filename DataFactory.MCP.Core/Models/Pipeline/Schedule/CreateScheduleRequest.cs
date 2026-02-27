using System.Text.Json.Serialization;

namespace DataFactory.MCP.Models.Pipeline.Schedule;

/// <summary>
/// Request payload for creating a pipeline schedule
/// </summary>
public class CreateScheduleRequest
{
    /// <summary>
    /// Whether this schedule is enabled
    /// </summary>
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; }

    /// <summary>
    /// The schedule configuration (Cron, Daily, Weekly, or Monthly)
    /// </summary>
    [JsonPropertyName("configuration")]
    public object Configuration { get; set; } = null!;
}
