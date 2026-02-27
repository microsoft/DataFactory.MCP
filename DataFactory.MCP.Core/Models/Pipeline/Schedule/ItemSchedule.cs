using System.Text.Json.Serialization;

namespace DataFactory.MCP.Models.Pipeline.Schedule;

/// <summary>
/// Represents a schedule for a pipeline item in Microsoft Fabric
/// </summary>
public class ItemSchedule
{
    /// <summary>
    /// The schedule ID
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Whether this schedule is enabled
    /// </summary>
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; }

    /// <summary>
    /// The created timestamp of this schedule in UTC
    /// </summary>
    [JsonPropertyName("createdDateTime")]
    public string? CreatedDateTime { get; set; }

    /// <summary>
    /// The schedule configuration (Cron, Daily, Weekly, or Monthly)
    /// </summary>
    [JsonPropertyName("configuration")]
    public object? Configuration { get; set; }

    /// <summary>
    /// The owner principal who created or last modified this schedule
    /// </summary>
    [JsonPropertyName("owner")]
    public object? Owner { get; set; }
}
