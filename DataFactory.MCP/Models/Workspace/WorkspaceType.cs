using System.Text.Json.Serialization;

namespace DataFactory.MCP.Models.Workspace;

/// <summary>
/// A workspace type. Additional workspace types may be added over time.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum WorkspaceType
{
    /// <summary>
    /// My folder or My workspace used to manage user items.
    /// </summary>
    Personal,

    /// <summary>
    /// Workspace used to manage the Fabric items.
    /// </summary>
    Workspace,

    /// <summary>
    /// Admin monitoring workspace. Contains admin reports such as the audit report and the usage and adoption report.
    /// </summary>
    AdminWorkspace
}