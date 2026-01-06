using DataFactory.MCP.Configuration;
using DataFactory.MCP.Extensions;
using DataFactory.MCP.Tools;

var builder = WebApplication.CreateBuilder(args);

// Create a logger for startup tracing
using var loggerFactory = LoggerFactory.Create(b =>
    b.AddConsole());
var logger = loggerFactory.CreateLogger("DataFactory.MCP.Http.Startup");

logger.LogInformation("Starting DataFactory MCP HTTP Server...");

// Register all DataFactory MCP services (shared with stdio version)
builder.Services.AddDataFactoryMcpServices();

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

// Default port is 3001, can be overridden via command line or configuration
var urls = builder.Configuration["urls"] ?? "http://localhost:3001";
app.Run(urls);
