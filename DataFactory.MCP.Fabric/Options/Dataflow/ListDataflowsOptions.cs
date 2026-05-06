// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Mcp.Core.Options;

namespace DataFactory.MCP.Fabric.Options.Dataflow;

public sealed class ListDataflowsOptions : GlobalOptions
{
    public string WorkspaceId { get; set; } = string.Empty;
}
