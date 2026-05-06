// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Serialization;
using DataFactory.MCP.Models.Pipeline;
using DataFactory.MCP.Models.Workspace;

namespace DataFactory.MCP.Fabric.Models;

[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(ListWorkspacesCommandResult))]
[JsonSerializable(typeof(ListPipelinesCommandResult))]
[JsonSerializable(typeof(CreatePipelineCommandResult))]
[JsonSerializable(typeof(GetPipelineCommandResult))]
[JsonSerializable(typeof(RunPipelineCommandResult))]
public partial class DataFactoryJsonContext : JsonSerializerContext
{
}

public sealed record ListWorkspacesCommandResult(List<Workspace> Workspaces, int TotalCount);
public sealed record ListPipelinesCommandResult(List<Pipeline> Pipelines, int TotalCount);
public sealed record CreatePipelineCommandResult(Pipeline Pipeline);
public sealed record GetPipelineCommandResult(Pipeline Pipeline);
public sealed record RunPipelineCommandResult(string? RunId);
