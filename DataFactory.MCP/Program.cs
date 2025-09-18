using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using DataFactory.MCP.Tools;
using DataFactory.MCP.Abstractions.Interfaces;
using DataFactory.MCP.Services;

var builder = Host.CreateApplicationBuilder(args);

// Configure all logs to go to stderr (stdout is used for the MCP protocol messages).
builder.Logging.AddConsole(o => o.LogToStandardErrorThreshold = LogLevel.Trace);

// Add authentication services directly
builder.Services.AddSingleton<IAuthenticationService, AuthenticationService>();
builder.Services.AddTransient<AuthenticationTool>();

// Add HTTP client and Fabric Gateway services
builder.Services.AddHttpClient<IFabricGatewayService, FabricGatewayService>();
builder.Services.AddTransient<GatewayTool>();

// Add HTTP client and Fabric Connection services
builder.Services.AddHttpClient<IFabricConnectionService, FabricConnectionService>();
builder.Services.AddTransient<ConnectionsTool>();

// Add HTTP client and Fabric Workspace services
builder.Services.AddHttpClient<IFabricWorkspaceService, FabricWorkspaceService>();
builder.Services.AddTransient<WorkspacesTool>();

// Add the MCP services: the transport to use (stdio) and the tools to register.
builder.Services
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithTools<AuthenticationTool>()
    .WithTools<GatewayTool>()
    .WithTools<ConnectionsTool>()
    .WithTools<WorkspacesTool>();

await builder.Build().RunAsync();
