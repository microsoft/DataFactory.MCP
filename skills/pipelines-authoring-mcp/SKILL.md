---
name: pipelines-authoring-mcp
description: >
  Create, update, and orchestrate Fabric Data Pipelines using MCP tools. Uses data-factory-mcp
  server tools to create pipelines, define activity chains with dependsOn conditions, configure
  Dataflow activities within pipelines, set up Cron/Daily/Weekly/Monthly schedules via
  create_pipeline_schedule, and trigger on-demand runs with run_pipeline. Supports read-modify-
  write workflows for pipeline JSON definitions including activity parameters, timeout policies,
  retry configuration, and multi-dataflow orchestration patterns. Use when the user wants to:
  (1) create a new pipeline via MCP, (2) add or modify pipeline activities, (3) chain dataflow
  activities with dependencies, (4) schedule pipeline execution, (5) run a pipeline on demand
  and monitor status, (6) update pipeline definitions programmatically.
  Triggers: "create pipeline mcp", "pipeline activities", "schedule pipeline", "run pipeline",
  "pipeline definition", "chain dataflows pipeline", "pipeline dependsOn", "orchestrate
  dataflows", "pipeline schedule mcp".
---

> **Update Check — ONCE PER SESSION (mandatory)**
> The first time this skill is used, check for data-factory-mcp updates.

## Prerequisite Knowledge

Read these companion documents:
- [DATAFACTORY-MCP-CORE.md](../../common/DATAFACTORY-MCP-CORE.md) — Authentication, workspace discovery, MCP patterns

This skill adds: **how to create and orchestrate Data Pipelines** using MCP tools.

## MCP Tools (this skill)

| Tool | Purpose |
|------|---------|
| `create_pipeline` | Create empty pipeline in a workspace |
| `update_pipeline` | Update pipeline metadata (name, description) |
| `update_pipeline_definition` | Set pipeline JSON (activities, parameters) |
| `get_pipeline_definition` | Read current pipeline JSON (read-modify-write) |
| `get_pipeline` | Get pipeline metadata |
| `list_pipelines` | Discover pipelines in a workspace |
| `run_pipeline` | Trigger on-demand pipeline execution |
| `get_pipeline_run_status` | Poll run status until terminal state |
| `create_pipeline_schedule` | Set up scheduled execution (Cron/Daily/Weekly/Monthly) |
| `list_pipeline_schedules` | List configured schedules |

## Must / Prefer / Avoid

### MUST
- Discover the dataflow objectId via `list_dataflows` before referencing in pipeline activities
- Use `get_pipeline_definition` before `update_pipeline_definition` for read-modify-write
- Set appropriate timeout policies (default 12h is excessive for dataflow activities)

### PREFER
- `timeout = "0.01:00:00"` (1h) for Dataflow activities — they rarely need more
- `retry = 1` with `retryIntervalInSeconds = 60` for transient auth/network failures
- `dependsOn` with `Succeeded` condition for sequential activity chains

### AVOID
- Hardcoding dataflow or workspace IDs — always discover via list tools
- Creating activities without `dependsOn` unless they're truly independent

## Creating a Pipeline with a Dataflow Activity

### Step 1: Discover the dataflow

```text
list_dataflows(workspaceId="...") → extract objectId
```

### Step 2: Create the pipeline

```text
create_pipeline(workspaceId="...", displayName="Monthly Refresh", description="...")
→ returns pipelineId
```

### Step 3: Set the definition

```text
update_pipeline_definition(workspaceId="...", pipelineId="...", definitionJson="...")
```

Use `templates/pipeline-single-dataflow.json` for the complete JSON structure. Replace `ACTIVITY_NAME`, `DATAFLOW_ID`, `WORKSPACE_ID`.

### Step 4: Schedule (optional)

```text
create_pipeline_schedule(workspaceId="...", pipelineId="...", ...)
```

Supports Cron, Daily, Weekly, and Monthly frequency.

### Step 5: Run on demand (optional)

```text
run_pipeline(workspaceId="...", pipelineId="...")
→ returns runId
get_pipeline_run_status(workspaceId="...", pipelineId="...", runId="...")
→ poll until Completed/Failed
```

## Chaining Activities

Use `dependsOn` to sequence activities:

```json
{
  "name": "RefreshAggregations",
  "type": "DataflowActivity",
  "dependsOn": [
    {
      "activity": "RefreshSources",
      "dependencyConditions": ["Succeeded"]
    }
  ]
}
```

Dependency conditions: `Succeeded`, `Failed`, `Skipped`, `Completed` (any outcome).

## Policy Settings

| Setting | Default | Recommended |
|---------|---------|-------------|
| `timeout` | `0.12:00:00` (12h) | `0.01:00:00` (1h) for dataflows |
| `retry` | 0 | 1 for transient failures |
| `retryIntervalInSeconds` | 30 | 60 for token refresh timing |

## Debugging Failed Activities

Pipeline runs surface activity-level status. When a Dataflow activity fails:
1. Check `refresh_dataflow_status` for the underlying dataflow error
2. The pipeline shows "Failed" but the root cause is always in the dataflow refresh
3. Common causes: expired connection (re-bind), M syntax error, timeout (chunk query)

## Supplementary References

| Reference | When |
|-----------|------|
| `references/activity-patterns.md` | Complex chaining, multi-dataflow orchestration |
