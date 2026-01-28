using DataFactory.MCP.Abstractions.Interfaces;
using ModelContextProtocol;

namespace DataFactory.MCP.Services;

/// <summary>
/// Provides access to the current MCP session using AsyncLocal for thread-safety.
/// </summary>
public class McpSessionAccessor : IMcpSessionAccessor
{
    private static readonly AsyncLocal<McpSession?> _currentSession = new();

    public McpSession? CurrentSession
    {
        get => _currentSession.Value;
        set => _currentSession.Value = value;
    }
}
