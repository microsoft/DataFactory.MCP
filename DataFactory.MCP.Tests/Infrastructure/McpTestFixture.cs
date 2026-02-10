using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using DataFactory.MCP.Abstractions.Interfaces;
using DataFactory.MCP.Abstractions.Interfaces.DMTSv2;
using DataFactory.MCP.Configuration;
using DataFactory.MCP.Infrastructure.Http;
using DataFactory.MCP.Services;
using DataFactory.MCP.Services.Authentication;
using DataFactory.MCP.Services.BackgroundTasks;
using DataFactory.MCP.Services.DMTSv2;
using DataFactory.MCP.Services.Notifications;
using DataFactory.MCP.Tools;
using DataFactory.MCP.Tools.Dataflow;
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

                // Register authentication handlers
                services.AddTransient<FabricAuthenticationHandler>();
                services.AddTransient<AzureResourceManagerAuthenticationHandler>();

                // Register named HttpClients with authentication handlers (matching Program.cs)
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


                // Register services
                // Authentication system with providers (must be Singleton to persist tokens across scopes/requests)
                services.AddSingleton<IAuthenticationStateManager, AuthenticationStateManager>();
                services.AddSingleton<IAuthenticationProvider, InteractiveAuthenticationProvider>();
                services.AddSingleton<IAuthenticationProvider, DeviceCodeAuthenticationProvider>();
                services.AddSingleton<IAuthenticationProvider, ServicePrincipalAuthenticationProvider>();
                services.AddSingleton<IAuthenticationService, AuthenticationService>();
                services.AddScoped<IValidationService, ValidationService>();
                services.AddScoped<IArrowDataReaderService, ArrowDataReaderService>();
                services.AddScoped<IDataTransformationService, DataTransformationService>();
                services.AddScoped<IDataflowDefinitionProcessor, DataflowDefinitionProcessor>();
                services.AddScoped<IGatewayClusterDatasourceService, GatewayClusterDatasourceService>();
                services.AddScoped<IFabricGatewayService, FabricGatewayService>();
                services.AddScoped<IFabricConnectionService, FabricConnectionService>();
                services.AddScoped<IFabricWorkspaceService, FabricWorkspaceService>();
                services.AddScoped<IFabricDataflowService, FabricDataflowService>();
                services.AddScoped<IFabricCapacityService, FabricCapacityService>();
                services.AddScoped<FabricDataSourceConnectionFactory>();

                // Register background task services
                services.AddSingleton<IMcpSessionAccessor, McpSessionAccessor>();
                services.AddSingleton<IUserNotificationService, SystemToastNotificationService>();
                services.AddSingleton<INotificationQueue, NotificationQueue>();
                services.AddSingleton<IBackgroundJobMonitor, BackgroundJobMonitor>();
                services.AddScoped<IDataflowRefreshService, DataflowRefreshService>();

                // Register tools
                services.AddScoped<AuthenticationTool>();
                services.AddScoped<GatewayTool>();
                services.AddScoped<ConnectionsTool>();
                services.AddScoped<WorkspacesTool>();
                services.AddScoped<DataflowTool>();
                services.AddScoped<DataflowRefreshTool>();
                services.AddScoped<CapacityTool>();
                services.AddScoped<DataflowQueryTool>();
                services.AddScoped<MDocumentTool>();
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
