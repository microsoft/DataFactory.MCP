using DataFactory.MCP.Abstractions.Interfaces;
using DataFactory.MCP.Extensions;
using DataFactory.MCP.Services;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

var builder = WebApplication.CreateBuilder(args);

// Create a logger for startup tracing
using var loggerFactory = LoggerFactory.Create(b =>
    b.AddConsole());
var logger = loggerFactory.CreateLogger("DataFactory.MCP.Http.Startup");

logger.LogInformation("Starting DataFactory MCP HTTP Server...");

// Register all DataFactory MCP services (shared with stdio version)
builder.Services.AddDataFactoryMcpServices();

// Register user notification service - HTTP uses MCP protocol notifications
builder.Services.AddSingleton<IUserNotificationService, McpUserNotificationService>();

// Configure MCP server capabilities - declare logging support for background notifications
builder.Services.Configure<McpServerOptions>(options =>
{
    options.Capabilities ??= new ServerCapabilities();
    options.Capabilities.Logging = new LoggingCapability();
});

// Configure MCP server with HTTP transport
logger.LogInformation("Registering MCP tools with HTTP transport...");
var mcpBuilder = builder.Services
    .AddMcpServer()
    .WithHttpTransport()
    .AddDataFactoryMcpTools();

// Register optional tools based on feature flags
mcpBuilder.AddDataFactoryMcpOptionalTools(
    builder.Configuration,
    args,
    logger);

var app = builder.Build();

// Map MCP endpoints
app.MapMcp();

// Add a simple health check endpoint
app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }));

logger.LogInformation("DataFactory MCP HTTP Server configured successfully");

app.Run();
