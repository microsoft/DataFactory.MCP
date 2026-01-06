using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using DataFactory.MCP.Extensions;

var builder = Host.CreateApplicationBuilder(args);

// Configure all logs to go to stderr (stdout is used for the MCP protocol messages).
builder.Logging.AddConsole(o => o.LogToStandardErrorThreshold = LogLevel.Trace);

// Create a logger for startup tracing
using var loggerFactory = LoggerFactory.Create(b =>
    b.AddConsole(o => o.LogToStandardErrorThreshold = LogLevel.Trace));
var logger = loggerFactory.CreateLogger("DataFactory.MCP.Startup");

// Register all DataFactory MCP services (shared with HTTP version)
builder.Services.AddDataFactoryMcpServices();

// Configure MCP server with stdio transport and register tools
logger.LogInformation("Registering core MCP tools...");
var mcpBuilder = builder.Services
    .AddMcpServer()
    .WithStdioServerTransport()
    .AddDataFactoryMcpTools();

// Register optional tools based on feature flags
mcpBuilder.AddDataFactoryMcpOptionalTools(
    builder.Configuration,
    args,
    logger);

await builder.Build().RunAsync();
