// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Mcp.Core.Options;

namespace DataFactory.MCP.Fabric.Options.Workspace;

public sealed class ListWorkspacesOptions : GlobalOptions
{
    public string? Roles { get; set; }
}
