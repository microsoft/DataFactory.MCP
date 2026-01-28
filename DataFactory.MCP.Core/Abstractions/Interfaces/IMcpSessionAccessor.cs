using ModelContextProtocol;

namespace DataFactory.MCP.Abstractions.Interfaces;

/// <summary>
/// Provides access to the current MCP session.
/// Similar to IHttpContextAccessor pattern - allows services to access the session
/// without requiring it to be passed through every method call.
/// </summary>
public interface IMcpSessionAccessor
{
    /// <summary>
    /// Gets or sets the current MCP session.
    /// </summary>
    McpSession? CurrentSession { get; set; }
}
