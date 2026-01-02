namespace DataFactory.MCP.Models.Dataflow.Definition;

/// <summary>
/// Response model for dataflow definition update operations
/// </summary>
public class UpdateDataflowDefinitionResponse
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
    /// The updated dataflow ID
    /// </summary>
    public string? DataflowId { get; set; }

    /// <summary>
    /// The workspace ID where the dataflow was updated
    /// </summary>
    public string? WorkspaceId { get; set; }
}
