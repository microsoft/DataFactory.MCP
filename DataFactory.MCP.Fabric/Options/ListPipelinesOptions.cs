// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Mcp.Core.Options;

namespace DataFactory.MCP.Fabric.Options;

public sealed class ListPipelinesOptions : GlobalOptions
{
    public string WorkspaceId { get; set; } = string.Empty;
}
