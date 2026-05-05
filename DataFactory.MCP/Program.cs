using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using DataFactory.MCP.Abstractions.Interfaces;
using DataFactory.MCP.Extensions;
using DataFactory.MCP.Notifications;
using DataFactory.MCP.Services;

var builder = Host.CreateApplicationBuilder(args);

// Configure all logs to go to stderr (stdout is used for the MCP protocol messages).
builder.Logging.AddConsole(o => o.LogToStandardErrorThreshold = LogLevel.Trace);

// Create a logger for startup tracing
using var loggerFactory = LoggerFactory.Create(b =>
    b.AddConsole(o => o.LogToStandardErrorThreshold = LogLevel.Trace));
var logger = loggerFactory.CreateLogger("DataFactory.MCP.Startup");

// Register all DataFactory MCP services (shared with HTTP version)
builder.Services.AddDataFactoryMcpServices();

// Register platform-specific notification providers (stdio host only - HTTP uses MCP protocol)
builder.Services.AddSingleton<IPlatformNotificationProvider, WindowsToastNotificationProvider>();
builder.Services.AddSingleton<IPlatformNotificationProvider, MacOsNotificationProvider>();
builder.Services.AddSingleton<IPlatformNotificationProvider, LinuxNotificationProvider>();

// Register user notification service - stdio uses OS toast notifications
builder.Services.AddSingleton<IUserNotificationService, SystemToastNotificationService>();

// Configure MCP server with stdio transport and register tools
logger.LogInformation("Registering core MCP tools...");
var mcpBuilder = builder.Services
    .AddMcpServer()
    .WithStdioServerTransport()
    .AddDataFactoryMcpTools();

// Register optional tools based on feature flags
mcpBuilder.AddDataFactoryMcpOptionalTools(
    builder.Configuration,
    args.Concat(["--interactive-auth"]).ToArray(),  // Enable interactive auth by default for stdio
    logger);

// Initialize skill registry for SEP-2640 skill:// resources
DataFactory.MCP.Resources.Skills.SkillRegistry.Initialize();

await builder.Build().RunAsync();
