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
    Id = "a1b2c3d4-1001-4000-8000-000000000001",
    Name = "list-workspaces",
    Title = "List Workspaces",
    Description = "Lists all Microsoft Fabric workspaces accessible to the current user. Optionally filter by role (Admin, Member, Contributor, Viewer).",
    Destructive = false,
    Idempotent = true,
    ReadOnly = true,
    OpenWorld = false)]
public sealed class ListWorkspacesCommand(
    ILogger<ListWorkspacesCommand> logger,
    IFabricWorkspaceService workspaceService) : GlobalCommand<ListWorkspacesOptions>()
{
    private readonly ILogger<ListWorkspacesCommand> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly IFabricWorkspaceService _workspaceService = workspaceService ?? throw new ArgumentNullException(nameof(workspaceService));

    protected override void RegisterOptions(Command command)
    {
        base.RegisterOptions(command);
        command.Options.Add(DataFactoryOptionDefinitions.Roles.AsOptional());
    }

    protected override ListWorkspacesOptions BindOptions(ParseResult parseResult)
    {
        var options = base.BindOptions(parseResult);
        options.Roles = parseResult.GetValueOrDefault<string>(DataFactoryOptionDefinitions.RolesName);
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
            var response = await _workspaceService.ListWorkspacesAsync(options.Roles);

            _logger.LogInformation("Successfully listed {Count} workspaces", response.Value.Count);

            var result = new ListWorkspacesCommandResult(response.Value, response.Value.Count);
            context.Response.Results = ResponseResult.Create(result, DataFactoryJsonContext.Default.ListWorkspacesCommandResult);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing workspaces");
            HandleException(context, ex);
        }

        return context.Response;
    }
}

public sealed class ListWorkspacesOptions : GlobalOptions
{
    public string? Roles { get; set; }
}
