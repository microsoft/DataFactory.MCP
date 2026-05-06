// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Mcp.Core.Options;

namespace DataFactory.MCP.Fabric.Options.Pipeline;

public sealed class GetPipelineOptions : GlobalOptions
{
    public string WorkspaceId { get; set; } = string.Empty;
    public string PipelineId { get; set; } = string.Empty;
}
