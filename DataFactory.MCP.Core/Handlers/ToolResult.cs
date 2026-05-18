namespace DataFactory.MCP.Handlers;

/// <summary>
/// Represents the result of a tool handler execution.
/// Framework-agnostic — both SDK tools and Fabric commands consume this.
/// </summary>
public class ToolResult<T>
{
    public bool IsSuccess { get; init; }
    public T? Value { get; init; }
    public string? Error { get; init; }
    public string? ErrorType { get; init; } // "validation", "auth", "http", "operation"

    public static ToolResult<T> Success(T value) => new() { IsSuccess = true, Value = value };
    public static ToolResult<T> Failure(string error, string errorType = "operation")
        => new() { IsSuccess = false, Error = error, ErrorType = errorType };
}
