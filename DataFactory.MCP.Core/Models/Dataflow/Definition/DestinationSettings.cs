using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace DataFactory.MCP.Models.Dataflow.Definition;

/// <summary>
/// Destination settings for updating query metadata in a dataflow definition
/// </summary>
public class DestinationSettings
{
    /// <summary>
    /// The Lakehouse ID for the destination
    /// </summary>
    [JsonPropertyName("lakehouseId")]
    [Required(ErrorMessage = "Lakehouse ID is required")]
    public string LakehouseId { get; set; } = string.Empty;

    /// <summary>
    /// The workspace ID for the destination
    /// </summary>
    [JsonPropertyName("workspaceId")]
    [Required(ErrorMessage = "Workspace ID is required")]
    public string WorkspaceId { get; set; } = string.Empty;

    /// <summary>
    /// The table name for the destination
    /// </summary>
    [JsonPropertyName("tableName")]
    [Required(ErrorMessage = "Table Name is required")]
    public string TableName { get; set; } = string.Empty;

    /// <summary>
    /// The update method for the destination
    /// </summary>
    [JsonPropertyName("updateMethod")]
    [Required(ErrorMessage = "Update Method is required")]
    public string UpdateMethod { get; set; } = "Replace";

    /// <summary>
    /// The schema mapping for the destination
    /// </summary>
    [JsonPropertyName("schemaMapping")]
    [Required(ErrorMessage = "Schema Mapping is required")]
    public string SchemaMapping { get; set; } = "Automatic";

    /// <summary>
    /// The schema mapping for the destination
    /// </summary>
    [JsonPropertyName("isNewTable")]
    [Required(ErrorMessage = "Is New Table is required")]
    public bool IsNewTable { get; set; } = true;
}
