using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DataFactory.MCP.Configuration;

/// <summary>
/// Utility class for handling MCP tool registration based on feature flags
/// </summary>
public static class FeatureFlagRegistration
{
    /// <summary>
    /// Registers an MCP tool conditionally based on feature flag configuration and command line arguments
    /// </summary>
    /// <typeparam name="T">The MCP tool type to register</typeparam>
    /// <param name="mcpBuilder">The MCP server builder to register tools with</param>
    /// <param name="configuration">The application configuration containing feature flag values</param>
    /// <param name="args">Command line arguments to check for feature flags</param>
    /// <param name="featureFlag">The feature flag name to check</param>
    /// <param name="toolName">The name of the tool for logging purposes</param>
    /// <param name="logger">Logger for outputting registration status</param>
    /// <returns>The MCP server builder for fluent chaining</returns>
    public static IMcpServerBuilder RegisterToolWithFeatureFlag<T>(
        this IMcpServerBuilder mcpBuilder,
        IConfiguration configuration,
        string[] args,
        string featureFlag,
        string toolName,
        ILogger logger) where T : class
    {
        // Check both configuration parsing and direct args for flexibility
#pragma warning disable IL2026 // Members annotated with 'RequiresUnreferencedCodeAttribute'/'RequiresDynamicCodeAttribute' require dynamic access
        var isEnabled = configuration.GetValue<bool>(featureFlag) ||
                        args.Contains($"--{featureFlag}");
#pragma warning restore IL2026

        logger.LogInformation("Feature flag '{FeatureFlag}' is {Status}", featureFlag, isEnabled ? "ENABLED" : "DISABLED");

        if (isEnabled)
        {
            logger.LogInformation("Registering {ToolName}...", toolName);
#pragma warning disable IL2091 // Generic type argument does not satisfy 'DynamicallyAccessedMemberTypes' constraint
            mcpBuilder.WithTools<T>();
#pragma warning restore IL2091
            logger.LogInformation("{ToolName} registered successfully", toolName);
        }
        else
        {
            logger.LogInformation("{ToolName} registration skipped - feature flag disabled", toolName);
        }

        return mcpBuilder;
    }
}