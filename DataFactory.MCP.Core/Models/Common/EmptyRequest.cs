// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace DataFactory.MCP.Models.Common;

/// <summary>
/// Empty request body for API calls that require a POST with empty JSON payload.
/// Used instead of anonymous types to support source-gen JSON serialization.
/// </summary>
public class EmptyRequest { }
