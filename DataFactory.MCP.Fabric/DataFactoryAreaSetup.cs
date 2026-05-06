// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using DataFactory.MCP.Extensions;
using DataFactory.MCP.Fabric.Commands.Pipeline;
using DataFactory.MCP.Fabric.Commands.Workspace;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Mcp.Core.Areas;
using Microsoft.Mcp.Core.Commands;

namespace DataFactory.MCP.Fabric;

public class DataFactoryAreaSetup : IAreaSetup
{
    public string Name => "datafactory";
    public string Title => "Microsoft Fabric Data Factory";

    public void ConfigureServices(IServiceCollection services)
    {
        // Register DataFactory.MCP.Core services (auth, HttpClients, all service implementations)
        services.AddDataFactoryMcpServices();

        // Register command instances
        services.AddSingleton<ListWorkspacesCommand>();
        services.AddSingleton<ListPipelinesCommand>();
        services.AddSingleton<CreatePipelineCommand>();
        services.AddSingleton<GetPipelineCommand>();
        services.AddSingleton<RunPipelineCommand>();
    }

    public CommandGroup RegisterCommands(IServiceProvider serviceProvider)
    {
        var group = new CommandGroup(Name,
            """
            Microsoft Fabric Data Factory Operations - Manage pipelines, dataflows, and workspaces.
            Use this tool when you need to:
            - List and manage workspaces
            - Create, get, list, and run pipelines
            - Work with dataflows and data transformations
            """);

        group.AddCommand<ListWorkspacesCommand>(serviceProvider);
        group.AddCommand<ListPipelinesCommand>(serviceProvider);
        group.AddCommand<CreatePipelineCommand>(serviceProvider);
        group.AddCommand<GetPipelineCommand>(serviceProvider);
        group.AddCommand<RunPipelineCommand>(serviceProvider);

        return group;
    }
}
