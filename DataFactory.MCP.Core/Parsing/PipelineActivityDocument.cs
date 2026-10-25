using System.Text.Json;
using System.Text.Json.Nodes;

namespace DataFactory.MCP.Parsing;

/// <summary>
/// Retains a pipeline activity as a JSON DOM without imposing an activity-type schema.
/// </summary>
public sealed class PipelineActivityDocument
{
    private PipelineActivityDocument(JsonObject content)
    {
        Content = content;
    }

    /// <summary>
    /// Gets the live activity JSON object.
    /// </summary>
    public JsonObject Content { get; }

    /// <summary>
    /// Gets the activity name, or <see langword="null"/> when it is absent or JSON null.
    /// </summary>
    public string? Name => GetOptionalString("name");

    /// <summary>
    /// Gets the activity type, or <see langword="null"/> when it is absent or JSON null.
    /// </summary>
    public string? Type => GetOptionalString("type");

    /// <summary>
    /// Parses a strict JSON activity object.
    /// </summary>
    public static PipelineActivityDocument Parse(string activityJson)
    {
        JsonNode? activityNode;
        try
        {
            activityNode = JsonNode.Parse(activityJson);
        }
        catch (JsonException ex)
        {
            throw new ArgumentException($"Invalid activityJson format: {ex.Message}", ex);
        }

        return activityNode is JsonObject activity
            ? new PipelineActivityDocument(activity)
            : throw new ArgumentException("activityJson must be a JSON object");
    }

    /// <summary>
    /// Replaces the activity's dependencies when an override is supplied.
    /// </summary>
    public void ApplyDependsOnOverride(string? dependsOnJson)
    {
        if (string.IsNullOrEmpty(dependsOnJson))
            return;

        JsonNode? dependsOnNode;
        try
        {
            dependsOnNode = JsonNode.Parse(dependsOnJson);
        }
        catch (JsonException ex)
        {
            throw new ArgumentException($"Invalid dependsOnJson format: {ex.Message}", ex);
        }

        if (dependsOnNode is not JsonArray dependsOn)
            throw new ArgumentException("dependsOnJson must be a JSON array");

        Content["dependsOn"] = dependsOn.DeepClone();
    }

    private string? GetOptionalString(string propertyName)
    {
        var value = Content[propertyName];
        if (value is null)
            return null;

        return value is JsonValue jsonValue && jsonValue.TryGetValue<string>(out var result)
            ? result
            : throw new InvalidOperationException($"Activity property '{propertyName}' must be a string or null");
    }
}
