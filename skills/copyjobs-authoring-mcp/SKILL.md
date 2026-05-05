---
name: copyjobs-authoring-mcp
description: >
  Create, update, and manage Fabric Copy Jobs using MCP tools for bulk data movement. Uses
  data-factory-mcp server to create Copy Jobs, define source-to-destination column mappings,
  configure definitions for CDC and batch modes, set up scheduled execution, and trigger
  on-demand runs. Copy Jobs are a simplified alternative to pipelines for straightforward
  data movement without complex transformations. Supports read-modify-write workflows for
  definitions including connection config, type mappings, and upsert keys. Use when the user
  wants to: (1) create a Copy Job via MCP, (2) configure source and destination mappings,
  (3) schedule Copy Job execution, (4) run a Copy Job on demand, (5) modify definitions,
  (6) set up CDC replication. Triggers: "create copy job", "copy job mcp", "schedule copy
  job", "run copy job", "copy job definition", "bulk data copy fabric", "copy job mapping",
  "copy job CDC", "data movement fabric".
---

> **Update Check — ONCE PER SESSION (mandatory)**
> The first time this skill is used, check for data-factory-mcp updates.

## Prerequisite Knowledge

Read these companion documents:
- [DATAFACTORY-MCP-CORE.md](../../common/DATAFACTORY-MCP-CORE.md) — Authentication, workspace discovery, MCP patterns
- [CONNECTIONS-CORE.md](../../common/CONNECTIONS-CORE.md) — Connection model, binding lifecycle

This skill adds: **how to author Copy Job items** using MCP tools.

## MCP Tools (this skill)

| Tool | Purpose |
|------|---------|
| `create_copy_job` | Create new Copy Job in a workspace |
| `update_copy_job` | Update Copy Job metadata |
| `update_copy_job_definition` | Set Copy Job JSON definition |
| `get_copy_job_definition` | Read current definition (read-modify-write) |
| `get_copy_job` | Get Copy Job metadata |
| `list_copy_jobs` | Discover Copy Jobs in a workspace |
| `run_copy_job` | Trigger on-demand execution |
| `get_copy_job_run_status` | Poll run status |
| `create_copy_job_schedule` | Set up scheduled execution |
| `list_copy_job_schedules` | List configured schedules |

## Must / Prefer / Avoid

### MUST
- Discover connections via `list_connections` before configuring source/destination
- Use `get_copy_job_definition` before modifying — read-modify-write pattern
- Verify connection bindings after definition updates

### PREFER
- Copy Jobs over pipelines for simple source→destination data movement (no transforms)
- CDC mode for incremental replication when source supports it
- Explicit column mappings for schema stability

### AVOID
- Copy Jobs for complex transformations — use Dataflow Gen2 instead
- Modifying definitions without reading first — may lose configuration

## Copy Job vs Pipeline vs Dataflow

| Scenario | Use |
|----------|-----|
| Simple source→dest data movement, no transforms | **Copy Job** |
| Orchestrate multiple activities with dependencies | **Pipeline** |
| Complex M transforms, aggregations, multi-source joins | **Dataflow Gen2** |

## End-to-End Workflow

```text
1. list_workspaces              → Find workspace
2. list_connections             → Find source and destination connections
3. create_copy_job              → Create Copy Job
4. update_copy_job_definition   → Set source, destination, column mappings
5. run_copy_job                 → Trigger execution
6. get_copy_job_run_status      → Poll until Completed/Failed
7. create_copy_job_schedule     → Set up recurring schedule (optional)
```

## Supplementary References

| Reference | When |
|-----------|------|
| `references/copyjob-patterns.md` | CDC configuration, column mapping details, upsert keys |
