using System.Text;
using System.Text.Json.Nodes;
using DataFactory.MCP.Models.Pipeline.Definition;

namespace DataFactory.MCP.Parsing;

/// <summary>
/// Retains pipeline content as a JSON DOM and creates the Fabric update envelope.
/// </summary>
public sealed class PipelineContentDocument
{
    private const string ContentPath = "pipeline-content.json";
    private readonly JsonObject _properties;

    private PipelineContentDocument(JsonObject content, JsonObject properties)
    {
        Content = content;
        _properties = properties;
    }

    /// <summary>
    /// Gets the live pipeline-content root object.
    /// </summary>
    public JsonObject Content { get; }

    /// <summary>
    /// Parses the exact pipeline content part from a Fabric definition.
    /// </summary>
    public static PipelineContentDocument Parse(PipelineDefinition definition)
    {
        if (definition is null)
            throw new InvalidOperationException("Pipeline definition is null");

        if (definition.Parts is null)
            throw new InvalidOperationException("Pipeline definition parts are null");

        PipelineDefinitionPart? contentPart = null;
        foreach (var part in definition.Parts)
        {
            if (part is null)
                throw new InvalidOperationException("Pipeline definition contains a null part");

            if (contentPart is null && string.Equals(part.Path, ContentPath, StringComparison.Ordinal))
                contentPart = part;
        }

        if (contentPart is null)
        {
            throw new ArgumentException(
                "Pipeline definition does not contain a 'pipeline-content.json' part");
        }

        if (contentPart.Payload is null)
            throw new InvalidOperationException("Pipeline content part payload is null");

        var contentJson = Encoding.UTF8.GetString(Convert.FromBase64String(contentPart.Payload));
        var contentNode = JsonNode.Parse(contentJson)
            ?? throw new ArgumentException("Failed to parse pipeline content JSON");
        if (contentNode is not JsonObject content)
            throw new InvalidOperationException("Pipeline content JSON must be an object");

        var propertiesNode = content["properties"];
        if (propertiesNode is null)
            throw new ArgumentException("Pipeline content missing 'properties' object");
        if (propertiesNode is not JsonObject properties)
            throw new InvalidOperationException("Pipeline content 'properties' must be an object");

        return new PipelineContentDocument(content, properties);
    }

    /// <summary>
    /// Gets the live top-level activities array, optionally creating it when absent or null.
    /// </summary>
    public JsonArray GetActivities(bool createIfMissing)
    {
        var activitiesNode = _properties["activities"];
        if (activitiesNode is JsonArray activities)
            return activities;

        if (activitiesNode is not null)
            throw new InvalidOperationException("Pipeline content 'activities' must be an array");

        if (!createIfMissing)
            throw new ArgumentException("Pipeline has no activities array");

        activities = new JsonArray();
        _properties["activities"] = activities;
        return activities;
    }

    /// <summary>
    /// Serializes the retained DOM into the current one-part update envelope.
    /// </summary>
    public PipelineDefinition ToDefinition()
    {
        var payload = Convert.ToBase64String(Encoding.UTF8.GetBytes(Content.ToJsonString()));
        return new PipelineDefinition
        {
            Parts =
            [
                new PipelineDefinitionPart
                {
                    Path = ContentPath,
                    Payload = payload,
                    PayloadType = "InlineBase64"
                }
            ]
        };
    }
}
