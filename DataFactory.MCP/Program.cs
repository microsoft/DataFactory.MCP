using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using DataFactory.MCP.Tools;
using DataFactory.MCP.Abstractions.Interfaces;
using DataFactory.MCP.Abstractions.Interfaces.DMTSv2;
using DataFactory.MCP.Services;
using DataFactory.MCP.Services.DMTSv2;
using DataFactory.MCP.Models.Connection.Factories;
using DataFactory.MCP.Configuration;

var builder = Host.CreateApplicationBuilder(args);

// Configure all logs to go to stderr (stdout is used for the MCP protocol messages).
builder.Logging.AddConsole(o => o.LogToStandardErrorThreshold = LogLevel.Trace);

// Create a logger for startup tracing
using var loggerFactory = LoggerFactory.Create(builder =>
    builder.AddConsole(o => o.LogToStandardErrorThreshold = LogLevel.Trace));
var logger = loggerFactory.CreateLogger("DataFactory.MCP.Startup");

// Register named HttpClients to avoid socket exhaustion
builder.Services.AddHttpClient("FabricApi", client =>
{
    client.BaseAddress = new Uri("https://api.fabric.microsoft.com/v1/");
    client.Timeout = TimeSpan.FromSeconds(30);
});

builder.Services.AddHttpClient("AzureResourceManager", client =>
{
    client.BaseAddress = new Uri("https://management.azure.com/");
    client.Timeout = TimeSpan.FromSeconds(30);
});

builder.Services.AddHttpClient("PowerBiApi", client =>
{
    client.BaseAddress = new Uri("https://api.powerbi.com/v2.0/");
    client.Timeout = TimeSpan.FromSeconds(30);
});

// Add the MCP services: the transport to use (stdio) and the tools to register.
builder.Services
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

// Configure MCP server with conditional tool registration
logger.LogInformation("Registering core MCP tools...");
var mcpBuilder = builder.Services
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithTools<AuthenticationTool>()
    .WithTools<GatewayTool>()
    .WithTools<ConnectionsTool>()
    .WithTools<WorkspacesTool>()
    .WithTools<DataflowTool>()
    .WithTools<CapacityTool>()
    .WithTools<AzureResourceDiscoveryTool>();

// Conditionally enable DataflowQueryTool based on feature flag
mcpBuilder.RegisterToolWithFeatureFlag<DataflowQueryTool>(
    builder.Configuration,
    args,
    FeatureFlags.DataflowQuery,
    nameof(DataflowQueryTool),
    logger);

await builder.Build().RunAsync();
