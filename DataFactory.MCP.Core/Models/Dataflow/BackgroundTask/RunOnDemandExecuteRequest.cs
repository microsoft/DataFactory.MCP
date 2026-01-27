using System.Text.Json.Serialization;

namespace DataFactory.MCP.Models.Dataflow.BackgroundTask;

/// <summary>
/// Request body for running an on-demand dataflow execute job
/// POST /workspaces/{workspaceId}/dataflows/{dataflowId}/jobs/Execute/instances
/// </summary>
public class RunOnDemandExecuteRequest
{
    /// <summary>
    /// Execution data payload (optional - only needed for parameterized dataflows)
    /// </summary>
    [JsonPropertyName("executionData")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public DataflowExecutionPayload? ExecutionData { get; set; }
}

/// <summary>
/// Execution payload for dataflow refresh
/// </summary>
public class DataflowExecutionPayload
{
    /// <summary>
    /// Execute option: SkipApplyChanges or ApplyChangesIfNeeded
    /// </summary>
    [JsonPropertyName("executeOption")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ExecuteOption { get; set; }

    /// <summary>
    /// Parameters to override during execution
    /// </summary>
    [JsonPropertyName("parameters")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<ItemJobParameter>? Parameters { get; set; }
}

/// <summary>
/// Parameter override for dataflow execution
/// </summary>
public class ItemJobParameter
{
    /// <summary>
    /// Name of the parameter
    /// </summary>
    [JsonPropertyName("parameterName")]
    public required string ParameterName { get; set; }

    /// <summary>
    /// Parameter type (must be "Automatic")
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; set; } = "Automatic";

    /// <summary>
    /// Value to override
    /// </summary>
    [JsonPropertyName("value")]
    public object? Value { get; set; }
}

/// <summary>
/// Execute options for dataflow refresh
/// </summary>
public static class ExecuteOptions
{
    /// <summary>
    /// Default - skips applying changes, faster execution
    /// </summary>
    public const string SkipApplyChanges = "SkipApplyChanges";

    /// <summary>
    /// Applies pending changes before executing
    /// </summary>
    public const string ApplyChangesIfNeeded = "ApplyChangesIfNeeded";
}
