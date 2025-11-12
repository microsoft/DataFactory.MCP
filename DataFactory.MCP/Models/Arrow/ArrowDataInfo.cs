namespace DataFactory.MCP.Models.Arrow;

/// <summary>
/// Information extracted from Apache Arrow data
/// </summary>
public class ArrowDataInfo
{
    public bool Success { get; set; }
    public string? Error { get; set; }
    public ArrowSchemaInfo? Schema { get; set; }
    public Dictionary<string, List<object>>? AllData { get; set; }
    public int TotalRows { get; set; }
    public int BatchCount { get; set; }
}