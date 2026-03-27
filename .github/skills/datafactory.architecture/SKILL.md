---
name: datafactory.architecture
description: "DataFactory.MCP project architecture, component layers, design patterns, and extension points. Use when working on any code in this repo."
---

# DataFactory.MCP Architecture

For the full architecture reference, see [`docs/ARCHITECTURE.md`](../../../docs/ARCHITECTURE.md).

## Project Structure

```
DataFactory.MCP/             — Main MCP server (stdio transport, entry point)
DataFactory.MCP.Core/        — Core library (all business logic)
  ├── Abstractions/          — Interfaces and contracts
  ├── Configuration/         — App configuration classes
  ├── Extensions/            — Extension methods and DI registration
  ├── Infrastructure/        — Cross-cutting concerns (HTTP, logging)
  ├── Models/                — Data models and DTOs
  ├── Parsing/               — Input parsing utilities
  ├── Resources/             — Embedded resources
  ├── Services/              — Fabric API service clients
  ├── Tools/                 — MCP tool implementations
  └── Validation/            — Input validation
DataFactory.MCP.Http/        — HTTP transport layer (AspNetCore)
DataFactory.MCP.Tests/       — Test suite (xUnit)
DataFactory.WindowsMCP/      — Windows-specific MCP implementation
claude-skills/               — Claude skill definitions (RAG pattern)
docs/                        — Feature documentation
evals/                       — Evaluation test scenarios
```

## Layered Architecture

```
AI Chat Interface (VS Code, Visual Studio)
        │ MCP Protocol (JSON-RPC over stdio)
        ▼
┌─ MCP Tools Layer ──────────────────────────────┐
│  AuthenticationTool, GatewayTool,               │
│  ConnectionsTool, WorkspacesTool,               │
│  Dataflow/*, Pipeline/*, CopyJob/*,             │
│  CapacityTool, CreateConnectionUITool           │
├─ Core Services Layer ──────────────────────────┤
│  AuthenticationService, FabricGatewayService,   │
│  FabricConnectionService, FabricWorkspaceService│
│  FabricDataflowService, FabricPipelineService,  │
│  FabricCopyJobService, FabricCapacityService,   │
│  ValidationService, DataTransformationService   │
├─ Infrastructure Layer ─────────────────────────┤
│  AuthenticatedHttpHandler, ApiVersionConfig,    │
│  McpSessionAccessor, BackgroundTasks            │
├─ Abstractions Layer ───────────────────────────┤
│  Interfaces for all services                    │
├─ Models Layer ─────────────────────────────────┤
│  DTOs, request/response types, enums            │
└─────────────────────────────────────────────────┘
        │ Fabric REST APIs (with Azure AD tokens)
        ▼
    Microsoft Fabric / Power BI Service
```

## Key Design Principles

- **Separation of Concerns**: Tools handle MCP protocol; Services handle Fabric API calls
- **Dependency Injection**: All services registered via DI with proper lifetimes
- **Async-First**: All I/O operations use async/await
- **Configuration-Driven**: Feature flags control tool availability
- **Extensibility**: New tools/services can be added without modifying existing code

## Feature Documentation

| Topic | Doc |
|-------|-----|
| Authentication | [`docs/authentication.md`](../../../docs/authentication.md) |
| Gateways | [`docs/gateway-management.md`](../../../docs/gateway-management.md) |
| Connections | [`docs/connection-management.md`](../../../docs/connection-management.md) |
| Dataflows | [`docs/dataflow-management.md`](../../../docs/dataflow-management.md) |
| Pipelines | [`docs/pipeline-management.md`](../../../docs/pipeline-management.md) |
| Copy Jobs | [`docs/copyjob-management.md`](../../../docs/copyjob-management.md) |
| Workspaces | [`docs/workspace-management.md`](../../../docs/workspace-management.md) |
| Capacities | [`docs/capacity-management.md`](../../../docs/capacity-management.md) |
| Full architecture | [`docs/ARCHITECTURE.md`](../../../docs/ARCHITECTURE.md) |

## Domain Knowledge

For Data Factory / M language / Dataflow patterns, see `claude-skills/`:
- `datafactory-core.md` — M basics, MCP tools, connection discovery
- `datafactory-destinations.md` — Output destinations, DataDestination patterns
- `datafactory-performance.md` — Query tuning, chunking, query folding
- `datafactory-advanced.md` — Fast Copy, Action.Sequence, Modern Evaluator
