using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using DataFactory.MCP.Tools;
using DataFactory.MCP.Abstractions.Interfaces;
using DataFactory.MCP.Services;
using DataFactory.MCP.Models.Connection.Factories;

var builder = Host.CreateApplicationBuilder(args);

// Configure all logs to go to stderr (stdout is used for the MCP protocol messages).
builder.Logging.AddConsole(o => o.LogToStandardErrorThreshold = LogLevel.Trace);

// Add the MCP services: the transport to use (stdio) and the tools to register.
builder.Services
    .AddSingleton<IValidationService, ValidationService>()
    .AddSingleton<IAuthenticationService, AuthenticationService>()
    .AddSingleton<IFabricGatewayService, FabricGatewayService>()
    .AddSingleton<IFabricConnectionService, FabricConnectionService>()
    .AddSingleton<IFabricWorkspaceService, FabricWorkspaceService>()
    .AddSingleton<IFabricDataflowService, FabricDataflowService>()
    .AddSingleton<IFabricCapacityService, FabricCapacityService>()
    .AddSingleton<IAzureResourceDiscoveryService, AzureResourceDiscoveryService>()
    .AddSingleton<FabricDataSourceConnectionFactory>()
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithTools<AuthenticationTool>()
    .WithTools<GatewayTool>()
    .WithTools<ConnectionsTool>()
    .WithTools<WorkspacesTool>()
    .WithTools<DataflowTool>()
    .WithTools<CapacityTool>()
    .WithTools<AzureResourceDiscoveryTool>();

await builder.Build().RunAsync();
