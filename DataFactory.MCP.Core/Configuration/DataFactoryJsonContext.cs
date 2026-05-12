// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json;
using System.Text.Json.Serialization;
using DataFactory.MCP.Models.Capacity;
using DataFactory.MCP.Models.Common;
using DataFactory.MCP.Models.Connection;
using DataFactory.MCP.Models.CopyJob;
using DataFactory.MCP.Models.CopyJob.Definition;
using DataFactory.MCP.Models.Dataflow;
using DataFactory.MCP.Models.Dataflow.Definition;
using DataFactory.MCP.Models.Dataflow.Query;
using DataFactory.MCP.Models.Gateway;
using DataFactory.MCP.Models.Pipeline;
using DataFactory.MCP.Models.Pipeline.Definition;
using DataFactory.MCP.Models.Pipeline.Schedule;
using DataFactory.MCP.Models.Workspace;
using DataFactory.MCP.Services.DMTSv2;

namespace DataFactory.MCP.Configuration;

[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
// Connection types (concrete subtypes only - base type has custom converter)
[JsonSerializable(typeof(ShareableCloudConnection))]
[JsonSerializable(typeof(PersonalCloudConnection))]
[JsonSerializable(typeof(OnPremisesGatewayConnection))]
[JsonSerializable(typeof(OnPremisesGatewayPersonalConnection))]
[JsonSerializable(typeof(VirtualNetworkGatewayConnection))]
[JsonSerializable(typeof(ListConnectionsResponse))]
[JsonSerializable(typeof(CreateConnectionRequest))]
[JsonSerializable(typeof(ListSupportedConnectionTypesResponse))]
// Gateway types (concrete subtypes only - base type has custom converter)
[JsonSerializable(typeof(OnPremisesGateway))]
[JsonSerializable(typeof(OnPremisesGatewayPersonal))]
[JsonSerializable(typeof(VirtualNetworkGateway))]
[JsonSerializable(typeof(ListGatewaysResponse))]
[JsonSerializable(typeof(CreateVirtualnetworkGatewayRequest))]
[JsonSerializable(typeof(CreateVirtualnetworkGatewayResponse))]
// Capacity types
[JsonSerializable(typeof(Capacity))]
[JsonSerializable(typeof(ListCapacitiesResponse))]
// Workspace types
[JsonSerializable(typeof(Workspace))]
[JsonSerializable(typeof(ListWorkspacesResponse))]
// CopyJob types
[JsonSerializable(typeof(CopyJob))]
[JsonSerializable(typeof(CreateCopyJobRequest))]
[JsonSerializable(typeof(CreateCopyJobResponse))]
[JsonSerializable(typeof(UpdateCopyJobRequest))]
[JsonSerializable(typeof(ListCopyJobsResponse))]
// CopyJob Definition types
[JsonSerializable(typeof(CopyJobDefinition))]
[JsonSerializable(typeof(CopyJobDefinitionPart))]
[JsonSerializable(typeof(UpdateCopyJobDefinitionRequest))]
[JsonSerializable(typeof(GetCopyJobDefinitionResponse))]
// Pipeline types
[JsonSerializable(typeof(Pipeline))]
[JsonSerializable(typeof(CreatePipelineRequest))]
[JsonSerializable(typeof(CreatePipelineResponse))]
[JsonSerializable(typeof(UpdatePipelineRequest))]
[JsonSerializable(typeof(ListPipelinesResponse))]
[JsonSerializable(typeof(Models.Pipeline.ItemJobInstance), TypeInfoPropertyName = "PipelineItemJobInstance")]
// Pipeline Schedule types
[JsonSerializable(typeof(CreateScheduleRequest))]
[JsonSerializable(typeof(ItemSchedule))]
[JsonSerializable(typeof(ListSchedulesResponse))]
// Pipeline Definition types
[JsonSerializable(typeof(PipelineDefinition))]
[JsonSerializable(typeof(PipelineDefinitionPart))]
[JsonSerializable(typeof(UpdatePipelineDefinitionRequest))]
[JsonSerializable(typeof(UpdatePipelineDefinitionResponse))]
[JsonSerializable(typeof(GetPipelineDefinitionResponse))]
// Dataflow types
[JsonSerializable(typeof(Dataflow))]
[JsonSerializable(typeof(CreateDataflowRequest))]
[JsonSerializable(typeof(CreateDataflowResponse))]
[JsonSerializable(typeof(ListDataflowsResponse))]
// Dataflow Query types
[JsonSerializable(typeof(ExecuteDataflowQueryRequest))]
[JsonSerializable(typeof(ExecuteDataflowQueryResponse))]
// Dataflow BackgroundTask types
[JsonSerializable(typeof(DataFactory.MCP.Models.Dataflow.BackgroundTask.RunOnDemandExecuteRequest))]
[JsonSerializable(typeof(DataFactory.MCP.Models.Dataflow.BackgroundTask.ItemJobInstance), TypeInfoPropertyName = "DataflowItemJobInstance")]
// Dataflow Definition types
[JsonSerializable(typeof(DataflowDefinition))]
[JsonSerializable(typeof(DataflowDefinitionPart))]
[JsonSerializable(typeof(GetDataflowDefinitionHttpResponse))]
[JsonSerializable(typeof(UpdateDataflowDefinitionRequest))]
[JsonSerializable(typeof(UpdateDataflowDefinitionResponse))]
// Gateway Cluster Datasource types (DMTSv2)
[JsonSerializable(typeof(GatewayClusterDatasourceService.GatewayClusterDatasourcesResponse))]
[JsonSerializable(typeof(GatewayClusterDatasourceService.CloudDatasourceInfo))]
// Common types
[JsonSerializable(typeof(JsonElement))]
[JsonSerializable(typeof(EmptyRequest))]
[JsonSerializable(typeof(RunOnDemandRequest))]
internal sealed partial class DataFactoryJsonContext : JsonSerializerContext
{
}
