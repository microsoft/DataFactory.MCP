namespace DataFactory.MCP.Configuration;

/// <summary>
/// Constants for feature flag names used throughout the application
/// </summary>
public static class FeatureFlags
{
    /// <summary>
    /// Feature flag for enabling the DataflowQueryTool
    /// Command line: --dataflow-query
    /// </summary>
    public const string DataflowQuery = "dataflow-query";

    /// <summary>
    /// Feature flag for enabling the DeviceCodeAuthenticationTool
    /// Command line: --device-code-auth
    /// Only enabled for HTTP version
    /// </summary>
    public const string DeviceCodeAuth = "device-code-auth";

    /// <summary>
    /// Feature flag for enabling the InteractiveAuthenticationTool
    /// Command line: --interactive-auth
    /// Enabled by default for stdio, disabled by default for HTTP
    /// </summary>
    public const string InteractiveAuth = "interactive-auth";

    /// <summary>
    /// Feature flag for enabling the PipelineTool
    /// Command line: --pipeline
    /// </summary>
    public const string Pipeline = "pipeline";
}