using DataFactory.WindowsMCP.Abstractions.Interfaces;
using DataFactory.WindowsMCP.Services;      
using DataFactory.WindowsMCP.Tools;
using Microsoft.Extensions.DependencyInjection;

namespace DataFactory.WindowsMCP.Extensions;

/// <summary>
/// Extension methods for registering Windows-specific MCP tools
/// </summary>
public static class WindowsServiceCollectionExtensions
{
    /// <summary>
    /// Registers all Windows-specific MCP tools with the MCP server builder
    /// </summary>
    /// <param name="mcpBuilder">The MCP server builder to register tools with</param>
    /// <returns>The MCP server builder for fluent chaining</returns>
    public static IMcpServerBuilder AddWindowsMcpTools(this IMcpServerBuilder mcpBuilder)
    {
        return mcpBuilder
            .WithTools<WindowsSystemInfoTool>()
            .WithTools<ExcelTool>();
    }

    public static IServiceCollection AddWindowsMcpServices(this IServiceCollection services)
    {
        // Register Excel service
        services
            .AddSingleton<IExcelService, ExcelService>();

        return services;
    }
}
