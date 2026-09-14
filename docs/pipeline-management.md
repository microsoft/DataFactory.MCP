# Pipeline Management Guide

This guide covers how to use the Microsoft Data Factory MCP Server for managing Microsoft Fabric pipelines.

## Overview

The pipeline management tools allow you to:
- **List** all pipelines within a specific workspace
- **Create** new pipelines in Microsoft Fabric workspaces
- **Get** pipeline metadata by ID
- **Update** pipeline metadata (display name and description)
- **Get** pipeline definitions with decoded base64 content
- **Update** pipeline definitions with JSON content
- **Upsert** a single top-level activity by name (append or whole-object replacement)
- **Remove** a single top-level activity by name (checking top-level dependents)
- **Run** pipelines on demand (with optional execution data)
- **Check** pipeline run status by job instance ID
- **Create** pipeline schedules (Cron, Daily, Weekly, Monthly)
- **List** schedules configured for a pipeline
- **Enable or disable** an existing schedule (stop a schedule without deleting it)
- Navigate paginated results for large pipeline collections

## MCP Tools

### list_pipelines

Returns a list of Pipelines from the specified workspace. This API supports pagination.

#### Usage
```
list_pipelines(workspaceId: "12345678-1234-1234-1234-123456789012")
```

#### With Pagination
```
list_pipelines(
  workspaceId: "12345678-1234-1234-1234-123456789012",
  continuationToken: "next-page-token"
)
```

#### Parameters

| Parameter | Required | Description |
|-----------|----------|-------------|
| `workspaceId` | Yes | The workspace ID to list pipelines from |
| `continuationToken` | No | A token for retrieving the next page of results |

#### Response Format
```json
{
  "workspaceId": "12345678-1234-1234-1234-123456789012",
  "pipelineCount": 3,
  "continuationToken": "eyJza2lwIjoyMCwidGFrZSI6MjB9",
  "continuationUri": "https://api.fabric.microsoft.com/v1/workspaces/12345/dataPipelines?continuationToken=abc123",
  "hasMoreResults": true,
  "pipelines": [
    {
      "id": "87654321-4321-4321-4321-210987654321",
      "displayName": "Sales Data Pipeline",
      "description": "Orchestrates daily sales data processing",
      "type": "DataPipeline",
      "workspaceId": "12345678-1234-1234-1234-123456789012",
      "folderId": "11111111-1111-1111-1111-111111111111"
    }
  ]
}
```

### create_pipeline

Creates a Pipeline in the specified workspace.

#### Usage
```
create_pipeline(
  workspaceId: "12345678-1234-1234-1234-123456789012",
  displayName: "My New Pipeline"
)
```

#### With Optional Parameters
```
create_pipeline(
  workspaceId: "12345678-1234-1234-1234-123456789012",
  displayName: "Sales ETL Pipeline",
  description: "Orchestrates daily sales data processing",
  folderId: "11111111-1111-1111-1111-111111111111"
)
```

#### Parameters

| Parameter | Required | Description |
|-----------|----------|-------------|
| `workspaceId` | Yes | The workspace ID where the pipeline will be created |
| `displayName` | Yes | The Pipeline display name (max 256 characters) |
| `description` | No | The Pipeline description (max 256 characters) |
| `folderId` | No | The folder ID where the pipeline will be created (defaults to workspace root) |

#### Response Format
```json
{
  "success": true,
  "message": "Pipeline 'Sales ETL Pipeline' created successfully",
  "pipelineId": "87654321-4321-4321-4321-210987654321",
  "displayName": "Sales ETL Pipeline",
  "description": "Orchestrates daily sales data processing",
  "type": "DataPipeline",
  "workspaceId": "12345678-1234-1234-1234-123456789012",
  "folderId": "11111111-1111-1111-1111-111111111111",
  "createdAt": "2026-02-12T10:30:00Z"
}
```

### get_pipeline

Gets the metadata of a Pipeline by ID.

#### Usage
```
get_pipeline(
  workspaceId: "12345678-1234-1234-1234-123456789012",
  pipelineId: "87654321-4321-4321-4321-210987654321"
)
```

#### Parameters

| Parameter | Required | Description |
|-----------|----------|-------------|
| `workspaceId` | Yes | The workspace ID containing the pipeline |
| `pipelineId` | Yes | The pipeline ID to retrieve |

#### Response Format
```json
{
  "id": "87654321-4321-4321-4321-210987654321",
  "displayName": "Sales Data Pipeline",
  "description": "Orchestrates daily sales data processing",
  "type": "DataPipeline",
  "workspaceId": "12345678-1234-1234-1234-123456789012",
  "folderId": "11111111-1111-1111-1111-111111111111"
}
```

### update_pipeline

Updates the metadata (displayName and/or description) of a Pipeline.

#### Usage
```
update_pipeline(
  workspaceId: "12345678-1234-1234-1234-123456789012",
  pipelineId: "87654321-4321-4321-4321-210987654321",
  displayName: "Updated Pipeline Name"
)
```

#### With Both Fields
```
update_pipeline(
  workspaceId: "12345678-1234-1234-1234-123456789012",
  pipelineId: "87654321-4321-4321-4321-210987654321",
  displayName: "Renamed Pipeline",
  description: "Updated description for the pipeline"
)
```

#### Parameters

| Parameter | Required | Description |
|-----------|----------|-------------|
| `workspaceId` | Yes | The workspace ID containing the pipeline |
| `pipelineId` | Yes | The pipeline ID to update |
| `displayName` | No* | The new display name (max 256 characters) |
| `description` | No* | The new description (max 256 characters) |

*At least one of `displayName` or `description` must be provided.

#### Response Format
```json
{
  "success": true,
  "message": "Pipeline 'Renamed Pipeline' updated successfully",
  "pipeline": {
    "id": "87654321-4321-4321-4321-210987654321",
    "displayName": "Renamed Pipeline",
    "description": "Updated description for the pipeline",
    "type": "DataPipeline",
    "workspaceId": "12345678-1234-1234-1234-123456789012",
    "folderId": "11111111-1111-1111-1111-111111111111"
  }
}
```

### get_pipeline_definition

Gets the definition of a Pipeline. The definition contains the pipeline JSON configuration with base64-encoded parts, which are automatically decoded for readability.

#### Usage
```
get_pipeline_definition(
  workspaceId: "12345678-1234-1234-1234-123456789012",
  pipelineId: "87654321-4321-4321-4321-210987654321"
)
```

#### Parameters

| Parameter | Required | Description |
|-----------|----------|-------------|
| `workspaceId` | Yes | The workspace ID containing the pipeline |
| `pipelineId` | Yes | The pipeline ID to get the definition for |

#### Response Format
```json
{
  "success": true,
  "pipelineId": "87654321-4321-4321-4321-210987654321",
  "workspaceId": "12345678-1234-1234-1234-123456789012",
  "partsCount": 1,
  "parts": [
    {
      "path": "pipeline-content.json",
      "payloadType": "InlineBase64",
      "decodedPayload": "{\"properties\":{\"activities\":[{\"name\":\"CopyData\",\"type\":\"Copy\"}]}}"
    }
  ]
}
```

### update_pipeline_definition

Updates the definition of a Pipeline with the provided JSON content. The JSON will be base64-encoded and sent to the API.

#### Usage
```
update_pipeline_definition(
  workspaceId: "12345678-1234-1234-1234-123456789012",
  pipelineId: "87654321-4321-4321-4321-210987654321",
  definitionJson: "{\"properties\":{\"activities\":[{\"name\":\"CopyData\",\"type\":\"Copy\"}]}}"
)
```

#### Parameters

| Parameter | Required | Description |
|-----------|----------|-------------|
| `workspaceId` | Yes | The workspace ID containing the pipeline |
| `pipelineId` | Yes | The pipeline ID to update |
| `definitionJson` | Yes | The pipeline definition JSON content (must be valid JSON) |

#### Response Format
```json
{
  "success": true,
  "pipelineId": "87654321-4321-4321-4321-210987654321",
  "workspaceId": "12345678-1234-1234-1234-123456789012",
  "message": "Pipeline definition updated successfully"
}
```

### upsert_pipeline_activity

Adds or replaces a single **top-level** activity in `properties.activities` in an existing pipeline definition. Names are matched exactly and case-sensitively. A matching top-level activity is **replaced as a whole object**, not merged; otherwise the activity is appended.

Targeting is **not recursive**. If a name exists only inside a ForEach, Switch, or other container, upsert appends a new top-level activity instead of changing that child. If both a top-level activity and a child have that name, only the top-level activity is targeted.

To change nested children, read the current definition and supply the **complete top-level container**, including all children and properties to retain. Omitted content is not automatically retained. Editing unrelated top-level siblings leaves existing container content in `pipeline-content.json` unchanged.

Dependencies reference **top-level activities only**, by case-sensitive name; a nested-only target is rejected. Before saving, the tool checks top-level references for missing targets, self-dependency and supported conditions (`Succeeded`, `Failed`, `Skipped`, `Completed`). It does not detect cycles, validate nested dependencies, or validate the full graph.

#### Local Validation Limits

Activities are **not fully validated locally**. Required inputs and the top-level dependency guards are checked, but activity-type-specific payloads are opaque: even the shape and contents of `typeProperties` are not locally schema-validated. No activity-type warnings are returned. A successful tool response is **not a guarantee of runtime validity** or comprehensive synchronous Fabric validation; the [definition update API](https://learn.microsoft.com/en-us/rest/api/fabric/core/items/update-item-definition) can accept work asynchronously.

#### Usage
```
upsert_pipeline_activity(
  workspaceId: "12345678-1234-1234-1234-123456789012",
  pipelineId: "87654321-4321-4321-4321-210987654321",
  activityJson: "{\"name\":\"LoadNotebook\",\"type\":\"TridentNotebook\",\"dependsOn\":[],\"typeProperties\":{\"notebookId\":\"NOTEBOOK_ID\",\"workspaceId\":\"NOTEBOOK_WORKSPACE_ID\"}}"
)
```

#### With Dependency Wiring
```
upsert_pipeline_activity(
  workspaceId: "12345678-1234-1234-1234-123456789012",
  pipelineId: "87654321-4321-4321-4321-210987654321",
  activityJson: "{\"name\":\"Transform\",\"type\":\"TridentNotebook\",\"dependsOn\":[],\"typeProperties\":{\"notebookId\":\"NB_ID\",\"workspaceId\":\"NOTEBOOK_WORKSPACE_ID\"}}",
  dependsOnJson: "[{\"activity\":\"Extract\",\"dependencyConditions\":[\"Succeeded\"]}]"
)
```

#### Parameters

| Parameter | Required | Description |
|-----------|----------|-------------|
| `workspaceId` | Yes | The workspace ID containing the pipeline |
| `pipelineId` | Yes | The pipeline ID to update |
| `activityJson` | Yes | Complete top-level activity JSON object with non-empty `name` and `type` fields. Case-sensitive name matching; whole-object replacement, not a merge. To edit children, supply the complete top-level container and all children to retain. Type-specific payloads are not locally schema-validated. Use strict JSON without comments. |
| `dependsOnJson` | No | JSON array of dependsOn entries targeting top-level activities only, by case-sensitive name. Nested-only targets are not resolved. Overrides any dependsOn in activityJson when provided. |

#### Activity Templates and Schema

The notebook examples require both `typeProperties.notebookId` and `typeProperties.workspaceId` per the [TridentNotebook schema](https://learn.microsoft.com/en-us/rest/api/fabric/articles/item-management/definitions/datapipeline-definition#tridentnotebook-activity-type-properties). The outer MCP `workspaceId` identifies the **pipeline's** workspace and does not populate the **notebook's** workspace property, even when both workspaces are the same. See the [notebook template](../claude-skills/templates/activity-notebook.json).

For Web activities, use `WebActivity`, `typeProperties.relativeUrl`, `typeProperties.method`, and activity-level `externalReferences.connection`. The [Web template](../claude-skills/templates/activity-web.json) requires an existing appropriate Fabric connection ID and the request's relative URL. See the documented [Web activity properties](https://learn.microsoft.com/en-us/rest/api/fabric/articles/item-management/definitions/datapipeline-definition#web-activity-properties), [Web type properties](https://learn.microsoft.com/en-us/rest/api/fabric/articles/item-management/definitions/datapipeline-definition#web-activity-type-properties), and [connection reference](https://learn.microsoft.com/en-us/rest/api/fabric/articles/item-management/definitions/datapipeline-definition#external-references).

Replace template placeholders and submit only the JSON object as `activityJson`, without the leading guidance comments. These templates provide authoring guidance, not local schema validation.

#### Response Format
```json
{
  "success": true,
  "message": "Activity 'LoadNotebook' added successfully",
  "activityName": "LoadNotebook",
  "activityType": "TridentNotebook",
  "operation": "Added",
  "totalActivityCount": 3,
  "pipelineId": "87654321-4321-4321-4321-210987654321",
  "workspaceId": "12345678-1234-1234-1234-123456789012"
}
```

### remove_pipeline_activity

Removes a single **top-level** activity from `properties.activities` by exact, case-sensitive name. A nested-only name is **not found**; containers are not searched. If a top-level activity and a child share a name, only the top-level entry is removed.

Removal is refused if another **top-level** activity references it via `dependsOn`; update or remove those dependents first. Nested dependencies are not checked, and these guards are not full graph or activity-schema validation. To remove a nested child, use `upsert_pipeline_activity` with the complete top-level container including all children and properties to retain. Removing an unrelated top-level sibling leaves existing container content in `pipeline-content.json` unchanged.

#### Usage
```
remove_pipeline_activity(
  workspaceId: "12345678-1234-1234-1234-123456789012",
  pipelineId: "87654321-4321-4321-4321-210987654321",
  activityName: "OldStep"
)
```

#### Parameters

| Parameter | Required | Description |
|-----------|----------|-------------|
| `workspaceId` | Yes | The workspace ID containing the pipeline |
| `pipelineId` | Yes | The pipeline ID to update |
| `activityName` | Yes | Exact, case-sensitive name of the top-level activity to remove. Nested child names are not searched; a nested-only name is not found. |

#### Response Format
```json
{
  "success": true,
  "message": "Activity 'OldStep' removed successfully",
  "removedActivity": "OldStep",
  "remainingActivityCount": 2,
  "pipelineId": "87654321-4321-4321-4321-210987654321",
  "workspaceId": "12345678-1234-1234-1234-123456789012"
}
```

### run_pipeline

Runs a Pipeline on demand. Returns a job instance ID that can be used to track the run status.

#### Usage
```
run_pipeline(
  workspaceId: "12345678-1234-1234-1234-123456789012",
  pipelineId: "87654321-4321-4321-4321-210987654321"
)
```

#### With Optional Execution Data
```
run_pipeline(
  workspaceId: "12345678-1234-1234-1234-123456789012",
  pipelineId: "87654321-4321-4321-4321-210987654321",
  executionDataJson: "{\"parameters\":{\"loadDate\":\"2026-02-24\"}}"
)
```

#### Parameters

| Parameter | Required | Description |
|-----------|----------|-------------|
| `workspaceId` | Yes | The workspace ID containing the pipeline |
| `pipelineId` | Yes | The pipeline ID to run |
| `executionDataJson` | No | Optional execution data as JSON string |

#### Response Format
```json
{
  "success": true,
  "message": "Pipeline run triggered successfully",
  "pipelineId": "87654321-4321-4321-4321-210987654321",
  "workspaceId": "12345678-1234-1234-1234-123456789012",
  "jobInstanceId": "34147f60-c8f1-4bb7-8b7e-24557a6bfeed",
  "locationUrl": "https://api.fabric.microsoft.com/v1/workspaces/.../jobs/instances/34147f60-c8f1-4bb7-8b7e-24557a6bfeed",
  "hint": "Use get_pipeline_run_status with the jobInstanceId to check the run status"
}
```

### get_pipeline_run_status

Gets the status of a pipeline run (job instance).

#### Usage
```
get_pipeline_run_status(
  workspaceId: "12345678-1234-1234-1234-123456789012",
  pipelineId: "87654321-4321-4321-4321-210987654321",
  jobInstanceId: "34147f60-c8f1-4bb7-8b7e-24557a6bfeed"
)
```

#### Parameters

| Parameter | Required | Description |
|-----------|----------|-------------|
| `workspaceId` | Yes | The workspace ID containing the pipeline |
| `pipelineId` | Yes | The pipeline ID |
| `jobInstanceId` | Yes | The job instance ID returned by `run_pipeline` |

#### Response Format
```json
{
  "success": true,
  "jobInstanceId": "34147f60-c8f1-4bb7-8b7e-24557a6bfeed",
  "pipelineId": "87654321-4321-4321-4321-210987654321",
  "workspaceId": "12345678-1234-1234-1234-123456789012",
  "jobType": "Pipeline",
  "invokeType": "Manual",
  "status": "Completed",
  "startTimeUtc": "2026-02-24T08:15:00Z",
  "endTimeUtc": "2026-02-24T08:16:32Z",
  "failureReason": null
}
```

Possible `status` values include: `NotStarted`, `InProgress`, `Completed`, `Failed`, `Cancelled`, `Deduped`.

### create_pipeline_schedule

Creates a schedule for a pipeline.

#### Usage
```
create_pipeline_schedule(
  workspaceId: "12345678-1234-1234-1234-123456789012",
  pipelineId: "87654321-4321-4321-4321-210987654321",
  enabled: true,
  configurationJson: "{\"type\":\"Cron\",\"startDateTime\":\"2026-02-24T00:00:00\",\"endDateTime\":\"2026-03-24T23:59:59\",\"localTimeZoneId\":\"UTC\",\"interval\":30}"
)
```

#### Parameters

| Parameter | Required | Description |
|-----------|----------|-------------|
| `workspaceId` | Yes | The workspace ID containing the pipeline |
| `pipelineId` | Yes | The pipeline ID to schedule |
| `enabled` | Yes | Whether the schedule is enabled |
| `configurationJson` | Yes | Schedule configuration as JSON |

#### Supported Schedule Types

- `Cron` (interval-based)
- `Daily`
- `Weekly`
- `Monthly`

#### Response Format
```json
{
  "success": true,
  "message": "Pipeline schedule created successfully",
  "scheduleId": "f36bc1bb-7007-4c15-b175-f63101609f95",
  "pipelineId": "87654321-4321-4321-4321-210987654321",
  "workspaceId": "12345678-1234-1234-1234-123456789012",
  "enabled": true,
  "createdDateTime": "2026-02-24T08:20:11Z",
  "configuration": {
    "type": "Cron",
    "interval": 30
  },
  "owner": {
    "id": "owner-id"
  }
}
```

### list_pipeline_schedules

Lists all schedules configured for a pipeline. This API supports pagination.

#### Usage
```
list_pipeline_schedules(
  workspaceId: "12345678-1234-1234-1234-123456789012",
  pipelineId: "87654321-4321-4321-4321-210987654321"
)
```

#### With Pagination
```
list_pipeline_schedules(
  workspaceId: "12345678-1234-1234-1234-123456789012",
  pipelineId: "87654321-4321-4321-4321-210987654321",
  continuationToken: "next-page-token"
)
```

#### Parameters

| Parameter | Required | Description |
|-----------|----------|-------------|
| `workspaceId` | Yes | The workspace ID containing the pipeline |
| `pipelineId` | Yes | The pipeline ID to list schedules for |
| `continuationToken` | No | A token for retrieving the next page of results |

#### Response Format
```json
{
  "pipelineId": "87654321-4321-4321-4321-210987654321",
  "workspaceId": "12345678-1234-1234-1234-123456789012",
  "scheduleCount": 2,
  "continuationToken": null,
  "continuationUri": null,
  "hasMoreResults": false,
  "schedules": [
    {
      "id": "f36bc1bb-7007-4c15-b175-f63101609f95",
      "enabled": true,
      "createdDateTime": "2026-02-24T08:20:11Z",
      "configuration": {
        "type": "Cron",
        "interval": 30
      },
      "owner": {
        "id": "owner-id"
      }
    }
  ]
}
```

### set_pipeline_schedule_enabled

Enables or disables an existing pipeline schedule, preserving its current configuration. Use `enabled=false` to **stop** (disable) a schedule without deleting it, and `enabled=true` to re-enable it later. Use the `scheduleId` returned from `list_pipeline_schedules`.

#### Usage
```
# Disable (stop) a schedule
set_pipeline_schedule_enabled(
  workspaceId: "12345678-1234-1234-1234-123456789012",
  pipelineId: "87654321-4321-4321-4321-210987654321",
  scheduleId: "f36bc1bb-7007-4c15-b175-f63101609f95",
  enabled: false
)

# Re-enable a schedule
set_pipeline_schedule_enabled(
  workspaceId: "12345678-1234-1234-1234-123456789012",
  pipelineId: "87654321-4321-4321-4321-210987654321",
  scheduleId: "f36bc1bb-7007-4c15-b175-f63101609f95",
  enabled: true
)
```

#### Parameters

| Parameter | Required | Description |
|-----------|----------|-------------|
| `workspaceId` | Yes | The workspace ID containing the pipeline |
| `pipelineId` | Yes | The pipeline ID whose schedule is being changed |
| `scheduleId` | Yes | The schedule ID to enable or disable |
| `enabled` | No | Whether the schedule should be enabled. Defaults to `false` (disable/stop the schedule) |

#### Response Format
```json
{
  "success": true,
  "message": "Pipeline schedule disabled (stopped) successfully",
  "scheduleId": "f36bc1bb-7007-4c15-b175-f63101609f95",
  "pipelineId": "87654321-4321-4321-4321-210987654321",
  "workspaceId": "12345678-1234-1234-1234-123456789012",
  "enabled": false
}
```

## Pipeline Properties

Pipelines in Microsoft Fabric include several key properties:

### Basic Properties
- **id**: Unique identifier for the pipeline
- **displayName**: Human-readable name of the pipeline
- **description**: Optional description of the pipeline's purpose
- **type**: Always "DataPipeline" for pipeline items
- **workspaceId**: ID of the containing workspace

### Optional Properties
- **folderId**: ID of the folder containing the pipeline (if organized in folders)

## Usage Examples

### Pipeline Creation
```
# Create a basic pipeline
> create a pipeline named "Customer ETL" in workspace 12345678-1234-1234-1234-123456789012

# Create pipeline with description
> create pipeline "Sales Pipeline" with description "Daily sales data orchestration" in workspace 12345678-1234-1234-1234-123456789012

# Create pipeline in a specific folder
> create pipeline "Marketing Data" in folder 11111111-1111-1111-1111-111111111111 within workspace 12345678-1234-1234-1234-123456789012
```

### Basic Pipeline Operations
```
# List all pipelines in a workspace
> list pipelines in workspace 12345678-1234-1234-1234-123456789012

# Get pipeline details
> show me pipeline 87654321-4321-4321-4321-210987654321 in workspace 12345678-1234-1234-1234-123456789012

# Update pipeline name
> rename pipeline 87654321-4321-4321-4321-210987654321 to "New Pipeline Name"
```

### Pipeline Definition Operations
```
# Get pipeline definition
> show me the definition of pipeline 87654321-4321-4321-4321-210987654321 in workspace 12345678-1234-1234-1234-123456789012

# Update pipeline definition
> update the definition of pipeline 87654321-4321-4321-4321-210987654321 with the following JSON activities configuration
```

### Pipeline Run and Schedule Operations
```
# Trigger a pipeline run
> run pipeline 87654321-4321-4321-4321-210987654321 in workspace 12345678-1234-1234-1234-123456789012

# Check pipeline run status
> get run status for pipeline 87654321-4321-4321-4321-210987654321 with job instance 34147f60-c8f1-4bb7-8b7e-24557a6bfeed

# Create a pipeline schedule
> create a daily schedule for pipeline 87654321-4321-4321-4321-210987654321 in workspace 12345678-1234-1234-1234-123456789012

# List pipeline schedules
> list schedules for pipeline 87654321-4321-4321-4321-210987654321 in workspace 12345678-1234-1234-1234-123456789012

# Stop (disable) a schedule without deleting it
> stop schedule f36bc1bb-7007-4c15-b175-f63101609f95 on pipeline 87654321-4321-4321-4321-210987654321 in workspace 12345678-1234-1234-1234-123456789012
```
