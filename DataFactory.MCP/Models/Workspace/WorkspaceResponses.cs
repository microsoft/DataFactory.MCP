using System.Text.Json.Serialization;

namespace DataFactory.MCP.Models.Workspace;

/// <summary>
/// Response containing a list of workspaces with optional pagination
/// </summary>
public class ListWorkspacesResponse
{
    /// <summary>
    /// A list of workspaces
    /// </summary>
    [JsonPropertyName("value")]
    public List<Workspace> Value { get; set; } = new();

    /// <summary>
    /// The token for the next result set batch. If there are no more records, it's removed from the response.
    /// </summary>
    [JsonPropertyName("continuationToken")]
    public string? ContinuationToken { get; set; }

    /// <summary>
    /// The URI of the next result set batch. If there are no more records, it's removed from the response.
    /// </summary>
    [JsonPropertyName("continuationUri")]
    public string? ContinuationUri { get; set; }
}