---
name: dataflows-consumption-mcp
description: >
  Inspect and monitor Fabric Dataflows Gen2 using MCP tools for read-only exploration. Uses
  data-factory-mcp server tools to list dataflows across workspaces, decode base64 mashup.pq
  definitions to examine M query logic, inspect connection bindings and staging settings in
  queryMetadata.json, discover typed parameters with defaults, and poll refresh job status for
  completion timing and error details. Provides structured discovery: list workspaces, enumerate
  dataflows, inspect properties, decode definitions, check refresh history. Use when the user
  wants to: (1) browse dataflows in a workspace, (2) inspect M query logic without modifying,
  (3) check refresh status or failure reasons, (4) discover dataflow parameters and settings,
  (5) analyze staging and connection configuration, (6) audit dataflow definitions.
  Triggers: "list dataflows", "inspect dataflow definition", "dataflow refresh status",
  "dataflow parameters", "browse dataflows", "dataflow monitoring mcp", "decode mashup".
---

> **Update Check — ONCE PER SESSION (mandatory)**
> The first time this skill is used, check for data-factory-mcp updates.

## Prerequisite Knowledge

Read these companion documents:
- [DATAFACTORY-MCP-CORE.md](../../common/DATAFACTORY-MCP-CORE.md) — Authentication, workspace discovery, MCP patterns

This skill adds: **how to inspect and monitor Dataflow Gen2 items** using MCP tools (read-only).

## MCP Tools (this skill)

| Tool | Purpose |
|------|---------|
| `list_dataflows` | Discover dataflows in a workspace (name, ID, description) |
| `get_dataflow_definition` | Read M section document — decode base64 mashup.pq |
| `refresh_dataflow_status` | Poll refresh job status (Completed, Failed, InProgress) |

These are the same tools available in `dataflows-authoring-mcp`. This skill teaches **inspection and monitoring patterns** rather than creation and modification.

## Must / Prefer / Avoid

### MUST
- Authenticate and discover workspace before any inspection
- Use `get_dataflow_definition` to read definitions — do not guess M code structure

### PREFER
- Structured discovery sequence: workspace → dataflows → definition → refresh status
- Examining queryMetadata.json alongside mashup.pq for full picture
- Checking refresh status before inspecting definitions (stale definition if refresh in progress)

### AVOID
- Modifying definitions through consumption tools — use `dataflows-authoring-mcp` for writes
- Assuming refresh status without polling — always check explicitly

## Discovery Sequence

```text
1. list_workspaces          → Find target workspace
2. list_dataflows           → Enumerate dataflows (name, objectId, description)
3. get_dataflow_definition  → Decode M document + queryMetadata
4. refresh_dataflow_status  → Check last refresh outcome
```

## What You Can Learn from Definitions

The `get_dataflow_definition` response contains:

| Component | What It Reveals |
|-----------|----------------|
| `mashup.pq` (M code) | Source queries, transforms, destination annotations |
| `queryMetadata.json` | Connection bindings, staging settings, load status, parameters |
| `DataDestinations` annotations | Target lakehouse/warehouse, IsNewTarget, update method |
| `AllowCombine` attribute | Whether multi-source privacy bypass is enabled |
| `StagingDefinition` | Whether Fast Copy is enabled |

## Refresh Status Interpretation

| Status | Meaning |
|--------|---------|
| `Completed` | Refresh succeeded |
| `Failed` | Refresh failed — check error details |
| `InProgress` | Still running |
| `Cancelled` | User or system cancelled |
| `NotStarted` | Queued but not yet running |

When a refresh fails, the status response includes error details. Common causes:
- Credentials error → connection binding issue (see CONNECTIONS-CORE.md)
- M syntax error → definition problem
- Timeout → query performance issue
- `DataflowNeverPublishedError` → missing `ApplyChangesIfNeeded` on first refresh
