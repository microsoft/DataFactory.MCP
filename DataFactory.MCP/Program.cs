using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using DataFactory.MCP.Tools;
using DataFactory.MCP.Abstractions.Interfaces;
using DataFactory.MCP.Services;

var builder = Host.CreateApplicationBuilder(args);

// Configure all logs to go to stderr (stdout is used for the MCP protocol messages).
builder.Logging.AddConsole(o => o.LogToStandardErrorThreshold = LogLevel.Trace);

// Add the MCP services: the transport to use (stdio) and the tools to register.
builder.Services
    .AddSingleton<IAuthenticationService, AuthenticationService>()
    .AddSingleton<IFabricGatewayService, FabricGatewayService>()
    .AddSingleton<IFabricConnectionService, FabricConnectionService>()
    .AddSingleton<IFabricWorkspaceService, FabricWorkspaceService>()
    .AddSingleton<IFabricDataflowService, FabricDataflowService>()
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithTools<AuthenticationTool>()
    .WithTools<GatewayTool>()
    .WithTools<ConnectionsTool>()
    .WithTools<WorkspacesTool>()
    .WithTools<DataflowTool>();

await builder.Build().RunAsync();
