# Data Factory Pipelines

## MCP Tools

- `create_pipeline` → create empty pipeline in a workspace
- `update_pipeline_definition` → set the pipeline JSON (activities, schedule)
- `get_pipeline_definition` → read current pipeline JSON
- `get_pipeline` → get pipeline metadata (name, description)
- `update_pipeline` → update pipeline metadata
- `list_pipelines` → list all pipelines in a workspace

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

Pipeline schedules cannot be set via the MCP API. After creating the pipeline, the user must configure the schedule in Fabric Studio:

1. Open pipeline in the workspace
2. Click **Schedule** on the toolbar
3. Set frequency (hourly, daily, weekly, monthly)

Always inform the user of this manual step after creating a pipeline.

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
