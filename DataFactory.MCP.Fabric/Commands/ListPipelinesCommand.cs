// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using DataFactory.MCP.Abstractions.Interfaces;
using DataFactory.MCP.Fabric.Models;
using DataFactory.MCP.Fabric.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Mcp.Core.Commands;
using Microsoft.Mcp.Core.Extensions;
using Microsoft.Mcp.Core.Models.Command;
using Microsoft.Mcp.Core.Models.Option;
using Microsoft.Mcp.Core.Options;

namespace DataFactory.MCP.Fabric.Commands;

[CommandMetadata(
    Id = "a1b2c3d4-1001-4000-8000-000000000002",
    Name = "list-pipelines",
    Title = "List Pipelines",
    Description = "Lists all pipelines in a specified Microsoft Fabric workspace. Requires the workspace ID.",
    Destructive = false,
    Idempotent = true,
    ReadOnly = true,
    OpenWorld = false)]
public sealed class ListPipelinesCommand(
    ILogger<ListPipelinesCommand> logger,
    IFabricPipelineService pipelineService) : GlobalCommand<ListPipelinesOptions>()
{
    private readonly ILogger<ListPipelinesCommand> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly IFabricPipelineService _pipelineService = pipelineService ?? throw new ArgumentNullException(nameof(pipelineService));

    protected override void RegisterOptions(Command command)
    {
        base.RegisterOptions(command);
        command.Options.Add(DataFactoryOptionDefinitions.WorkspaceId.AsRequired());
    }

    protected override ListPipelinesOptions BindOptions(ParseResult parseResult)
    {
        var options = base.BindOptions(parseResult);
        options.WorkspaceId = parseResult.GetValueOrDefault<string>(DataFactoryOptionDefinitions.WorkspaceIdName) ?? string.Empty;
        return options;
    }

    public override async Task<CommandResponse> ExecuteAsync(CommandContext context, ParseResult parseResult, CancellationToken cancellationToken)
    {
        if (!Validate(parseResult.CommandResult, context.Response).IsValid)
        {
            return context.Response;
        }

        var options = BindOptions(parseResult);
        try
        {
            var response = await _pipelineService.ListPipelinesAsync(options.WorkspaceId);

            _logger.LogInformation("Successfully listed {Count} pipelines in workspace {WorkspaceId}",
                response.Value.Count, options.WorkspaceId);

            var result = new ListPipelinesCommandResult(response.Value, response.Value.Count);
            context.Response.Results = ResponseResult.Create(result, DataFactoryJsonContext.Default.ListPipelinesCommandResult);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing pipelines in workspace {WorkspaceId}", options.WorkspaceId);
            HandleException(context, ex);
        }

        return context.Response;
    }
}

public sealed class ListPipelinesOptions : GlobalOptions
{
    public string WorkspaceId { get; set; } = string.Empty;
}
