using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using DataFactory.MCP.Abstractions.Interfaces;
using DataFactory.MCP.Services;
using DataFactory.MCP.Tools;
using DataFactory.MCP.Models.Connection.Factories;

namespace DataFactory.MCP.Tests.Infrastructure;

/// <summary>
/// Test fixture that provides a configured dependency injection container
/// for integration testing without mocking
/// </summary>
public class McpTestFixture : IDisposable
{
    public IServiceProvider ServiceProvider { get; private set; }
    private readonly IHost _host;

    public McpTestFixture()
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.test.json", optional: true)
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AzureAd:ClientId"] = Environment.GetEnvironmentVariable("AZURE_CLIENT_ID") ?? "#{AZURE_CLIENT_ID}#",
                ["AzureAd:TenantId"] = Environment.GetEnvironmentVariable("AZURE_TENANT_ID") ?? "#{AZURE_TENANT_ID}#",
                ["AzureAd:Instance"] = "https://login.microsoftonline.com/",
                ["AzureAd:ClientSecret"] = Environment.GetEnvironmentVariable("AZURE_CLIENT_SECRET") ?? "#{AZURE_CLIENT_SECRET}#"
            })
            .AddEnvironmentVariables()
            .Build();

        _host = Host.CreateDefaultBuilder()
            .ConfigureAppConfiguration(config =>
            {
                config.AddConfiguration(configuration);
            })
            .ConfigureServices((context, services) =>
            {
                // Configure logging
                services.AddLogging(builder => builder.AddConsole());

                // Configure HTTP client
                services.AddHttpClient();


                // Register services
                services.AddScoped<IValidationService, ValidationService>();
                services.AddScoped<IAuthenticationService, AuthenticationService>();
                services.AddScoped<IArrowDataReaderService, ArrowDataReaderService>();
                services.AddScoped<IFabricGatewayService, FabricGatewayService>();
                services.AddScoped<IFabricConnectionService, FabricConnectionService>();
                services.AddScoped<IFabricWorkspaceService, FabricWorkspaceService>();
                services.AddScoped<IFabricDataflowService, FabricDataflowService>();
                services.AddScoped<IFabricCapacityService, FabricCapacityService>();
                services.AddScoped<FabricDataSourceConnectionFactory>();

                // Register tools
                services.AddScoped<AuthenticationTool>();
                services.AddScoped<GatewayTool>();
                services.AddScoped<ConnectionsTool>();
                services.AddScoped<WorkspacesTool>();
                services.AddScoped<DataflowTool>();
                services.AddScoped<CapacityTool>();
                services.AddScoped<DataflowQueryTool>();
            })
            .Build();

        ServiceProvider = _host.Services;
    }

    public T GetService<T>() where T : notnull
    {
        return ServiceProvider.GetRequiredService<T>();
    }

    public void Dispose()
    {
        _host?.Dispose();
        GC.SuppressFinalize(this);
    }
}
