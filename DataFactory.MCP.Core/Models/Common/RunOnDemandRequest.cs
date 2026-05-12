// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json;
using System.Text.Json.Serialization;

namespace DataFactory.MCP.Models.Common;

/// <summary>
/// Request payload for running an item job on demand with optional execution data.
/// Used by both Pipeline and CopyJob run operations.
/// </summary>
public class RunOnDemandRequest
{
    /// <summary>
    /// Optional execution data for the job run
    /// </summary>
    [JsonPropertyName("executionData")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public JsonElement? ExecutionData { get; set; }
}
