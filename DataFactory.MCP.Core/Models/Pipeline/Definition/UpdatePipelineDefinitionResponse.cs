namespace DataFactory.MCP.Models.Pipeline.Definition;

/// <summary>
/// Response model for pipeline definition update operations
/// </summary>
public class UpdatePipelineDefinitionResponse
{
    /// <summary>
    /// Indicates if the operation was successful
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Error message if the operation failed
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// The updated pipeline ID
    /// </summary>
    public string? PipelineId { get; set; }

    /// <summary>
    /// The workspace ID where the pipeline was updated
    /// </summary>
    public string? WorkspaceId { get; set; }
}
