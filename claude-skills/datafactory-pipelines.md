# Data Factory Pipelines

## MCP Tools

- `create_pipeline` → create empty pipeline in a workspace
- `update_pipeline_definition` → set the pipeline JSON (activities, schedule)
- `get_pipeline_definition` → read current pipeline JSON
- `get_pipeline` → get pipeline metadata (name, description)
- `update_pipeline` → update pipeline metadata
- `list_pipelines` → list all pipelines in a workspace
- `create_pipeline_schedule` → create a schedule (Cron, Daily, Weekly, Monthly)
- `list_pipeline_schedules` → list a pipeline's schedules (returns `id` + `enabled` + `configuration`)
- `set_pipeline_schedule_enabled` → enable or disable (stop) an existing schedule, preserving its configuration

> To **stop** a schedule, use `set_pipeline_schedule_enabled` with `enabled=false`
> — see **Stopping schedules** below. There is still no MCP tool to *delete* a
> schedule; for deletion, fall back to the Fabric public REST API.

## Creating a Pipeline with a Dataflow Activity

### Step 0: Discover the dataflow ID

```python
list_dataflows(workspaceId="...")
# Returns array with objectId, name, description for each dataflow
# Use objectId as the DATAFLOW_ID in pipeline definitions
```

### Step 1: Create the pipeline

```python
create_pipeline(
  workspaceId="...",
  displayName="Monthly Refresh",
  description="Refreshes the store performance dataflow monthly"
)
# Returns pipelineId
```

### Step 2: Set the definition with a Dataflow activity

```python
update_pipeline_definition(
  workspaceId="...",
  pipelineId="...",
  definitionJson="""{ ... }"""  # See templates below
)
```

Use `templates/pipeline-single-dataflow.json` for the complete JSON structure. Replace `ACTIVITY_NAME`, `DATAFLOW_ID`, `WORKSPACE_ID`.

### Chaining Activities

Use `dependsOn` to sequence activities. Use `templates/pipeline-chained-dataflows.json` for a 3-activity chain template.

Dependency conditions: `Succeeded`, `Failed`, `Skipped`, `Completed` (any outcome).

## Scheduling

Schedules **can** be created via the MCP using `create_pipeline_schedule`
(Cron, Daily, Weekly, Monthly). An item supports up to 20 schedules. Use
`list_pipeline_schedules` to read existing ones and `set_pipeline_schedule_enabled`
to stop or re-enable one.

The MCP can **disable/enable** a schedule via `set_pipeline_schedule_enabled`,
but has no tool to **delete** a schedule. For deletion, use the Fabric public
REST API (see below).

## Stopping schedules (disable)

To stop a schedule, set its `enabled` flag to `false`. The schedule keeps
existing but no longer triggers runs. Re-enable it later by setting `enabled`
back to `true`.

### Preferred: use the MCP tool

```python
# Stop (disable) a schedule
set_pipeline_schedule_enabled(
  workspaceId="...",
  pipelineId="...",
  scheduleId="...",   # from list_pipeline_schedules
  enabled=False       # default; omit to disable
)

# Re-enable it later
set_pipeline_schedule_enabled(
  workspaceId="...", pipelineId="...", scheduleId="...", enabled=True
)
```

The tool reads the schedule's existing `configuration` and preserves it
automatically — you only pass the IDs and the `enabled` flag. It runs through
the MCP server's own authentication, so it works on any surface without a
separate `az login`.

### Bulk pattern: stop a bunch of schedules

1. For each pipeline, call `list_pipeline_schedules` to get every schedule `id`.
2. For each schedule where `enabled = true`, call
   `set_pipeline_schedule_enabled(..., enabled=False)`.
3. Re-run `list_pipeline_schedules` to confirm `enabled` is now `false`.

### Fallback: Fabric public REST API

If you need to **delete** a schedule, or are working outside the MCP, use the
Fabric [Update Item Schedule](https://learn.microsoft.com/rest/api/fabric/core/job-scheduler/update-item-schedule)
endpoint directly.

```
PATCH https://api.fabric.microsoft.com/v1/workspaces/{workspaceId}/items/{pipelineId}/jobs/Pipeline/schedules/{scheduleId}
Content-Type: application/json

{
  "enabled": false,
  "configuration": { ...existing configuration... }
}
```

The `configuration` block is required on update, so do **not** invent it —
read the current schedule first and reuse its `configuration` verbatim.

Use `az rest` (or any authenticated HTTP client) to issue the PATCH, e.g.:

```bash
az rest --method patch \
  --url "https://api.fabric.microsoft.com/v1/workspaces/$WS/items/$PIPE/jobs/Pipeline/schedules/$SCHED" \
  --headers "Content-Type=application/json" \
  --body '{"enabled": false, "configuration": <existing-config>}'
```

> **Auth caveat.** The MCP server holds its own Azure AD token internally; an
> `az rest` call uses a *separate* identity (`az login`). They may differ, so a
> caller who can list schedules through the MCP is not guaranteed to be
> authorized for the PATCH — expect possible `401`/`403` and confirm the
> `az` identity has write access to the workspace.

### Deleting vs. disabling

Disabling (`enabled: false`) is reversible — flip it back to `true` later.
Deleting a schedule (`DELETE` on the same URL) is permanent. Prefer disabling
unless the user explicitly wants the schedule removed.

## Common Pipeline Patterns

### Single Dataflow Refresh
One activity, one dataflow. Use for simple monthly/weekly refresh of a reporting dataflow.

### Multi-Dataflow Chain
Sequence multiple dataflows when downstream depends on upstream:
```
Refresh Sources → Refresh Aggregations → Refresh Reports
```
Each activity depends on the previous with `Succeeded` condition.

### Debugging Failed Activities

Pipeline runs surface activity-level status. When a Dataflow activity fails:
1. Check `refresh_dataflow_status` for the underlying dataflow error (credentials, M syntax, timeout)
2. The pipeline shows "Failed" but the root cause is always in the dataflow refresh status
3. Common causes: expired connection (re-bind), M syntax error (fix via `save_dataflow_definition`), timeout (chunk the query)

### Policy Settings

| Setting | Default | Notes |
|---------|---------|-------|
| `timeout` | `0.12:00:00` (12h) | Use `0.01:00:00` (1h) for dataflows; they rarely need more |
| `retry` | 0 | Set to 1 for transient auth/network failures |
| `retryIntervalInSeconds` | 30 | 60s is safer for token refresh timing |

### Template Field Notes

| Field | Required | Notes |
|-------|----------|-------|
| `dataflowId` | Yes | From `list_dataflows` → `objectId` |
| `workspaceId` | Yes | Workspace containing the dataflow |
| `lastModifiedByObjectId` | No | Set to zeros; platform overwrites on save |
| `lastPublishTime` | No | Set to any date; platform overwrites on save |
