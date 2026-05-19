---
name: pipelines-consumption-mcp
description: >
  Inspect and monitor Fabric Data Pipelines using MCP tools for read-only exploration and run
  tracking. Uses data-factory-mcp server tools to list pipelines across workspaces, decode base64
  pipeline JSON definitions to examine activity configurations and dependency chains, poll run
  status for on-demand and scheduled executions (NotStarted, InProgress, Completed, Failed,
  Cancelled, Deduped), and list configured schedules. Provides structured discovery: list
  workspaces, enumerate pipelines, inspect metadata and definitions, check run history and
  schedule configuration. Use when the user wants to: (1) browse pipelines in a workspace,
  (2) inspect activity definitions without modifying, (3) check pipeline run status or failure
  reasons, (4) list pipeline schedules, (5) audit pipeline configurations,
  (6) understand activity dependencies in a pipeline.
  Triggers: "list pipelines", "pipeline run status", "inspect pipeline definition",
  "pipeline schedules", "browse pipelines mcp", "pipeline monitoring".
---

> **Update Check — ONCE PER SESSION (mandatory)**
> The first time this skill is used, check for data-factory-mcp updates.

## Prerequisite Knowledge

Read these companion documents:
- [DATAFACTORY-MCP-CORE.md](../../common/DATAFACTORY-MCP-CORE.md) — Authentication, workspace discovery, MCP patterns

This skill adds: **how to inspect and monitor Data Pipelines** using MCP tools (read-only).

## MCP Tools (this skill)

| Tool | Purpose |
|------|---------|
| `list_pipelines` | Discover pipelines in a workspace |
| `get_pipeline` | Read pipeline metadata (name, description) |
| `get_pipeline_definition` | Read pipeline JSON definition (activities, parameters) |
| `get_pipeline_run_status` | Poll run status |
| `list_pipeline_schedules` | List configured schedules |

## Must / Prefer / Avoid

### MUST
- Authenticate and discover workspace before inspection
- Use `get_pipeline_definition` to read definitions — do not guess activity structure

### PREFER
- Discovery sequence: workspace → pipelines → definition → run status
- Checking run status before inspecting definitions (stale if run in progress)
- Reading schedule configuration alongside run history for full operational picture

### AVOID
- Modifying definitions — use `pipelines-authoring-mcp` for writes
- Assuming run status without polling

## Discovery Sequence

```text
1. list_workspaces            → Find target workspace
2. list_pipelines             → Enumerate pipelines
3. get_pipeline               → Read metadata
4. get_pipeline_definition    → Decode activity configuration
5. get_pipeline_run_status    → Check last run outcome
6. list_pipeline_schedules    → See configured schedules
```

## Run Status Interpretation

| Status | Meaning |
|--------|---------|
| `Completed` | All activities succeeded |
| `Failed` | One or more activities failed — check activity-level details |
| `InProgress` | Still running |
| `Cancelled` | User or system cancelled |
| `NotStarted` | Queued but not yet running |
| `Deduped` | Skipped because another run was already in progress |

When a run fails, dig into the underlying item:
- Dataflow activity failure → check `refresh_dataflow_status` for the dataflow error
- Copy Job activity failure → check Copy Job run status
