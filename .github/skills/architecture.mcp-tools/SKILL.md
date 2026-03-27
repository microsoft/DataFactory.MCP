---
name: architecture.mcp-tools
description: "Patterns for adding new MCP tools, services, and models to DataFactory.MCP. Use when adding new Fabric API integrations or MCP capabilities."
---

# Adding MCP Tools — Patterns

For the full architecture, see [`docs/ARCHITECTURE.md`](../../../docs/ARCHITECTURE.md).

## Overview

Every new MCP capability follows the **Tool → Service → Model** pattern:

```
1. Define Models       (DataFactory.MCP.Core/Models/)
2. Define Interface    (DataFactory.MCP.Core/Abstractions/)
3. Implement Service   (DataFactory.MCP.Core/Services/)
4. Implement Tool      (DataFactory.MCP.Core/Tools/)
5. Register in DI      (DataFactory.MCP.Core/Extensions/)
6. Add Tests           (DataFactory.MCP.Tests/)
```

## Step-by-Step

### 1. Models — Define data shapes

Create request/response DTOs in `Models/`. Keep them as pure data containers — no business logic.

### 2. Interface — Define the service contract

Add to `Abstractions/`. The interface defines what operations are available.

### 3. Service — Implement Fabric API calls

Create in `Services/`. Follow existing services as examples:
- `FabricGatewayService.cs` — Gateway CRUD operations
- `FabricConnectionService.cs` — Connection management
- `FabricDataflowService.cs` — Dataflow operations

Key patterns:
- Inject `HttpClient` (configured with auth handler)
- Use `AuthenticationService` for token management
- Return strongly-typed models
- Handle HTTP errors gracefully

### 4. Tool — Implement MCP interface

Create in `Tools/`. Follow existing tools as examples:
- `GatewayTool.cs` — Simple CRUD tools
- `ConnectionsTool.cs` — Tools with UI resources
- `Dataflow/` — Complex multi-tool directory

Key patterns:
- Define tool name, description, and JSON schema for parameters
- Validate inputs before calling service
- Format responses for MCP protocol
- Handle auth-not-ready state

### 5. DI Registration

Register new services and tools in `Extensions/` so they're available at runtime.

### 6. Tests

Add xUnit tests in `DataFactory.MCP.Tests/`:
- Test parameter validation
- Test service response parsing
- Test error handling paths

## Existing Tools Reference

| Tool File | Service | Capability |
|-----------|---------|-----------|
| `AuthenticationTool.cs` | `AuthenticationService` | Azure AD auth flows |
| `GatewayTool.cs` | `FabricGatewayService` | Gateway CRUD |
| `ConnectionsTool.cs` | `FabricConnectionService` | Connection management |
| `WorkspacesTool.cs` | `FabricWorkspaceService` | Workspace listing |
| `CapacityTool.cs` | `FabricCapacityService` | Capacity management |
| `Dataflow/` | `FabricDataflowService` | Dataflow operations |
| `Pipeline/` | `FabricPipelineService` | Pipeline management |
| `CopyJob/` | `FabricCopyJobService` | Copy job management |
| `CreateConnectionUITool.cs` | `FabricConnectionService` | UI wizard for connections |

## Feature Flags

Some tools are gated behind feature flags (e.g., Pipeline, CopyJob, DataflowQuery). Check `Configuration/` for how feature flags control tool registration.
