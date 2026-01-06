using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using DataFactory.MCP.Abstractions.Interfaces;
using DataFactory.MCP.Abstractions.Interfaces.DMTSv2;
using DataFactory.MCP.Configuration;
using DataFactory.MCP.Infrastructure.Http;
using DataFactory.MCP.Models.Connection.Factories;
using DataFactory.MCP.Services;
using DataFactory.MCP.Services.DMTSv2;
using DataFactory.MCP.Tools;

namespace DataFactory.MCP.Extensions;

/// <summary>
/// Extension methods for registering DataFactory MCP services and tools
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers all DataFactory MCP core services including authentication, data services, and factories
    /// </summary>
    /// <param name="services">The service collection to register services with</param>
    /// <returns>The service collection for fluent chaining</returns>
    public static IServiceCollection AddDataFactoryMcpServices(this IServiceCollection services)
    {
        // Register authentication handlers as transient (DelegatingHandlers must be transient)
        services.AddTransient<FabricAuthenticationHandler>();
        services.AddTransient<AzureResourceManagerAuthenticationHandler>();

        // Register named HttpClients with authentication handlers
        services.AddHttpClient(HttpClientNames.FabricApi, client =>
        {
            client.BaseAddress = new Uri(ApiVersions.Fabric.V1BaseUrl + "/");
            client.Timeout = TimeSpan.FromSeconds(30);
        }).AddHttpMessageHandler<FabricAuthenticationHandler>();

        services.AddHttpClient(HttpClientNames.AzureResourceManager, client =>
        {
            client.BaseAddress = new Uri("https://management.azure.com/");
            client.Timeout = TimeSpan.FromSeconds(30);
        }).AddHttpMessageHandler<AzureResourceManagerAuthenticationHandler>();

        services.AddHttpClient(HttpClientNames.PowerBiV2Api, client =>
        {
            client.BaseAddress = new Uri(ApiVersions.PowerBi.V2BaseUrl + "/");
            client.Timeout = TimeSpan.FromSeconds(30);
        }).AddHttpMessageHandler<FabricAuthenticationHandler>();

        // Register core services
        services
            .AddSingleton<IValidationService, ValidationService>()
            .AddSingleton<IAuthenticationService, AuthenticationService>()
            .AddSingleton<IArrowDataReaderService, ArrowDataReaderService>()
            .AddSingleton<IGatewayClusterDatasourceService, GatewayClusterDatasourceService>()
            .AddSingleton<IDataTransformationService, DataTransformationService>()
            .AddSingleton<IDataflowDefinitionProcessor, DataflowDefinitionProcessor>()
            .AddSingleton<IFabricGatewayService, FabricGatewayService>()
            .AddSingleton<IFabricConnectionService, FabricConnectionService>()
            .AddSingleton<IFabricWorkspaceService, FabricWorkspaceService>()
            .AddSingleton<IFabricDataflowService, FabricDataflowService>()
            .AddSingleton<IFabricCapacityService, FabricCapacityService>()
            .AddSingleton<IAzureResourceDiscoveryService, AzureResourceDiscoveryService>()
            .AddSingleton<FabricDataSourceConnectionFactory>();

        return services;
    }

    /// <summary>
    /// Registers all core DataFactory MCP tools with the MCP server builder
    /// </summary>
    /// <param name="mcpBuilder">The MCP server builder to register tools with</param>
    /// <returns>The MCP server builder for fluent chaining</returns>
    public static IMcpServerBuilder AddDataFactoryMcpTools(this IMcpServerBuilder mcpBuilder)
    {
        return mcpBuilder
            .WithTools<AuthenticationTool>()
            .WithTools<GatewayTool>()
            .WithTools<ConnectionsTool>()
            .WithTools<WorkspacesTool>()
            .WithTools<DataflowTool>()
            .WithTools<CapacityTool>()
            .WithTools<AzureResourceDiscoveryTool>();
    }

    /// <summary>
    /// Registers optional DataFactory MCP tools based on feature flags
    /// </summary>
    /// <param name="mcpBuilder">The MCP server builder to register tools with</param>
    /// <param name="configuration">The application configuration containing feature flag values</param>
    /// <param name="args">Command line arguments to check for feature flags</param>
    /// <param name="logger">Logger for outputting registration status</param>
    /// <returns>The MCP server builder for fluent chaining</returns>
    public static IMcpServerBuilder AddDataFactoryMcpOptionalTools(
        this IMcpServerBuilder mcpBuilder,
        IConfiguration configuration,
        string[] args,
        ILogger logger)
    {
        // Conditionally enable DataflowQueryTool based on feature flag
        mcpBuilder.RegisterToolWithFeatureFlag<DataflowQueryTool>(
            configuration,
            args,
            FeatureFlags.DataflowQuery,
            nameof(DataflowQueryTool),
            logger);

        return mcpBuilder;
    }
}
