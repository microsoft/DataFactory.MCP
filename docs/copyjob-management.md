# Copy Job Management Guide

This guide covers how to use the Microsoft Data Factory MCP Server for managing Microsoft Fabric copy jobs.

## Overview

The copy job management tools allow you to:
- **List** all copy jobs within a specific workspace
- **Create** new copy jobs in Microsoft Fabric workspaces
- **Get** copy job metadata by ID
- **Update** copy job metadata (display name and description)
- **Get** copy job definitions with decoded base64 content
- **Update** copy job definitions with JSON content
- **Run** copy jobs on demand (with optional execution data)
- **Check** copy job run status by job instance ID
- **Create** copy job schedules (Cron, Daily, Weekly, Monthly)
- **List** schedules configured for a copy job
- Navigate paginated results for large copy job collections

## MCP Tools

### list_copy_jobs

Returns a list of Copy Jobs from the specified workspace. This API supports pagination.

#### Usage
```
list_copy_jobs(workspaceId: "12345678-1234-1234-1234-123456789012")
```

#### With Pagination
```
list_copy_jobs(
  workspaceId: "12345678-1234-1234-1234-123456789012",
  continuationToken: "next-page-token"
)
```

#### Parameters

| Parameter | Required | Description |
|-----------|----------|-------------|
| `workspaceId` | Yes | The workspace ID to list copy jobs from |
| `continuationToken` | No | A token for retrieving the next page of results |

#### Response Format
```json
{
  "workspaceId": "12345678-1234-1234-1234-123456789012",
  "copyJobCount": 3,
  "continuationToken": "eyJza2lwIjoyMCwidGFrZSI6MjB9",
  "continuationUri": "https://api.fabric.microsoft.com/v1/workspaces/12345/copyJobs?continuationToken=abc123",
  "hasMoreResults": true,
  "copyJobs": [
    {
      "id": "87654321-4321-4321-4321-210987654321",
      "displayName": "Sales Data Copy Job",
      "description": "Copies daily sales data between lakehouses",
      "type": "CopyJob",
      "workspaceId": "12345678-1234-1234-1234-123456789012",
      "folderId": "11111111-1111-1111-1111-111111111111"
    }
  ]
}
```

### create_copy_job

Creates a Copy Job in the specified workspace.

#### Usage
```
create_copy_job(
  workspaceId: "12345678-1234-1234-1234-123456789012",
  displayName: "My New Copy Job"
)
```

#### With Optional Parameters
```
create_copy_job(
  workspaceId: "12345678-1234-1234-1234-123456789012",
  displayName: "Sales Data Copy Job",
  description: "Copies daily sales data between lakehouses",
  folderId: "11111111-1111-1111-1111-111111111111"
)
```

#### Parameters

| Parameter | Required | Description |
|-----------|----------|-------------|
| `workspaceId` | Yes | The workspace ID where the copy job will be created |
| `displayName` | Yes | The Copy Job display name |
| `description` | No | The Copy Job description (max 256 characters) |
| `folderId` | No | The folder ID where the copy job will be created (defaults to workspace root) |

#### Response Format
```json
{
  "success": true,
  "message": "Copy job 'Sales Data Copy Job' created successfully",
  "copyJobId": "87654321-4321-4321-4321-210987654321",
  "displayName": "Sales Data Copy Job",
  "description": "Copies daily sales data between lakehouses",
  "type": "CopyJob",
  "workspaceId": "12345678-1234-1234-1234-123456789012",
  "folderId": "11111111-1111-1111-1111-111111111111",
  "createdAt": "2026-02-12T10:30:00Z"
}
```

### get_copy_job

Gets the metadata of a Copy Job by ID.

#### Usage
```
get_copy_job(
  workspaceId: "12345678-1234-1234-1234-123456789012",
  copyJobId: "87654321-4321-4321-4321-210987654321"
)
```

#### Parameters

| Parameter | Required | Description |
|-----------|----------|-------------|
| `workspaceId` | Yes | The workspace ID containing the copy job |
| `copyJobId` | Yes | The copy job ID to retrieve |

#### Response Format
```json
{
  "id": "87654321-4321-4321-4321-210987654321",
  "displayName": "Sales Data Copy Job",
  "description": "Copies daily sales data between lakehouses",
  "type": "CopyJob",
  "workspaceId": "12345678-1234-1234-1234-123456789012",
  "folderId": "11111111-1111-1111-1111-111111111111"
}
```

### update_copy_job

Updates the metadata (displayName and/or description) of a Copy Job.

#### Usage
```
update_copy_job(
  workspaceId: "12345678-1234-1234-1234-123456789012",
  copyJobId: "87654321-4321-4321-4321-210987654321",
  displayName: "Updated Copy Job Name"
)
```

#### With Both Fields
```
update_copy_job(
  workspaceId: "12345678-1234-1234-1234-123456789012",
  copyJobId: "87654321-4321-4321-4321-210987654321",
  displayName: "Renamed Copy Job",
  description: "Updated description for the copy job"
)
```

#### Parameters

| Parameter | Required | Description |
|-----------|----------|-------------|
| `workspaceId` | Yes | The workspace ID containing the copy job |
| `copyJobId` | Yes | The copy job ID to update |
| `displayName` | No* | The new display name |
| `description` | No* | The new description |

*At least one of `displayName` or `description` must be provided.

#### Response Format
```json
{
  "success": true,
  "message": "Copy job 'Renamed Copy Job' updated successfully",
  "copyJob": {
    "id": "87654321-4321-4321-4321-210987654321",
    "displayName": "Renamed Copy Job",
    "description": "Updated description for the copy job",
    "type": "CopyJob",
    "workspaceId": "12345678-1234-1234-1234-123456789012",
    "folderId": "11111111-1111-1111-1111-111111111111"
  }
}
```

### get_copy_job_definition

Gets the definition of a Copy Job. The definition contains the copy job JSON configuration with base64-encoded parts, which are automatically decoded for readability.

#### Usage
```
get_copy_job_definition(
  workspaceId: "12345678-1234-1234-1234-123456789012",
  copyJobId: "87654321-4321-4321-4321-210987654321"
)
```

#### Parameters

| Parameter | Required | Description |
|-----------|----------|-------------|
| `workspaceId` | Yes | The workspace ID containing the copy job |
| `copyJobId` | Yes | The copy job ID to get the definition for |

#### Response Format
```json
{
  "success": true,
  "copyJobId": "87654321-4321-4321-4321-210987654321",
  "workspaceId": "12345678-1234-1234-1234-123456789012",
  "partsCount": 1,
  "parts": [
    {
      "path": "copyjob-content.json",
      "payloadType": "InlineBase64",
      "decodedPayload": "{\"source\":{\"type\":\"Lakehouse\"},\"destination\":{\"type\":\"Lakehouse\"}}"
    }
  ]
}
```

### update_copy_job_definition

Updates the definition of a Copy Job with the provided JSON content. The JSON will be base64-encoded and sent to the API.

#### Usage
```
update_copy_job_definition(
  workspaceId: "12345678-1234-1234-1234-123456789012",
  copyJobId: "87654321-4321-4321-4321-210987654321",
  definitionJson: "{\"source\":{\"type\":\"Lakehouse\"},\"destination\":{\"type\":\"Lakehouse\"}}"
)
```

#### Parameters

| Parameter | Required | Description |
|-----------|----------|-------------|
| `workspaceId` | Yes | The workspace ID containing the copy job |
| `copyJobId` | Yes | The copy job ID to update |
| `definitionJson` | Yes | The copy job definition JSON content (must be valid JSON) |

#### Response Format
```json
{
  "success": true,
  "copyJobId": "87654321-4321-4321-4321-210987654321",
  "workspaceId": "12345678-1234-1234-1234-123456789012",
  "message": "Copy job definition updated successfully"
}
```

### run_copy_job

Runs a Copy Job on demand. Returns a job instance ID that can be used to track the run status.

#### Usage
```
run_copy_job(
  workspaceId: "12345678-1234-1234-1234-123456789012",
  copyJobId: "87654321-4321-4321-4321-210987654321"
)
```

#### With Optional Execution Data
```
run_copy_job(
  workspaceId: "12345678-1234-1234-1234-123456789012",
  copyJobId: "87654321-4321-4321-4321-210987654321",
  executionDataJson: "{\"parameters\":{\"loadDate\":\"2026-02-24\"}}"
)
```

#### Parameters

| Parameter | Required | Description |
|-----------|----------|-------------|
| `workspaceId` | Yes | The workspace ID containing the copy job |
| `copyJobId` | Yes | The copy job ID to run |
| `executionDataJson` | No | Optional execution data as JSON string |

#### Response Format
```json
{
  "success": true,
  "message": "Copy job run triggered successfully",
  "copyJobId": "87654321-4321-4321-4321-210987654321",
  "workspaceId": "12345678-1234-1234-1234-123456789012",
  "jobInstanceId": "34147f60-c8f1-4bb7-8b7e-24557a6bfeed",
  "locationUrl": "https://api.fabric.microsoft.com/v1/workspaces/.../jobs/instances/34147f60-c8f1-4bb7-8b7e-24557a6bfeed",
  "hint": "Use get_copy_job_run_status with the jobInstanceId to check the run status"
}
```

### get_copy_job_run_status

Gets the status of a copy job run (job instance).

#### Usage
```
get_copy_job_run_status(
  workspaceId: "12345678-1234-1234-1234-123456789012",
  copyJobId: "87654321-4321-4321-4321-210987654321",
  jobInstanceId: "34147f60-c8f1-4bb7-8b7e-24557a6bfeed"
)
```

#### Parameters

| Parameter | Required | Description |
|-----------|----------|-------------|
| `workspaceId` | Yes | The workspace ID containing the copy job |
| `copyJobId` | Yes | The copy job ID |
| `jobInstanceId` | Yes | The job instance ID returned by `run_copy_job` |

#### Response Format
```json
{
  "success": true,
  "jobInstanceId": "34147f60-c8f1-4bb7-8b7e-24557a6bfeed",
  "copyJobId": "87654321-4321-4321-4321-210987654321",
  "workspaceId": "12345678-1234-1234-1234-123456789012",
  "jobType": "CopyJob",
  "invokeType": "Manual",
  "status": "Completed",
  "startTimeUtc": "2026-02-24T08:15:00Z",
  "endTimeUtc": "2026-02-24T08:16:32Z",
  "failureReason": null
}
```

Possible `status` values include: `NotStarted`, `InProgress`, `Completed`, `Failed`, `Cancelled`, `Deduped`.

### create_copy_job_schedule

Creates a schedule for a copy job.

#### Usage
```
create_copy_job_schedule(
  workspaceId: "12345678-1234-1234-1234-123456789012",
  copyJobId: "87654321-4321-4321-4321-210987654321",
  enabled: true,
  configurationJson: "{\"type\":\"Cron\",\"startDateTime\":\"2026-02-24T00:00:00\",\"endDateTime\":\"2026-03-24T23:59:59\",\"localTimeZoneId\":\"UTC\",\"interval\":30}"
)
```

#### Parameters

| Parameter | Required | Description |
|-----------|----------|-------------|
| `workspaceId` | Yes | The workspace ID containing the copy job |
| `copyJobId` | Yes | The copy job ID to schedule |
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
  "message": "Copy job schedule created successfully",
  "scheduleId": "f36bc1bb-7007-4c15-b175-f63101609f95",
  "copyJobId": "87654321-4321-4321-4321-210987654321",
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

### list_copy_job_schedules

Lists all schedules configured for a copy job. This API supports pagination.

#### Usage
```
list_copy_job_schedules(
  workspaceId: "12345678-1234-1234-1234-123456789012",
  copyJobId: "87654321-4321-4321-4321-210987654321"
)
```

#### With Pagination
```
list_copy_job_schedules(
  workspaceId: "12345678-1234-1234-1234-123456789012",
  copyJobId: "87654321-4321-4321-4321-210987654321",
  continuationToken: "next-page-token"
)
```

#### Parameters

| Parameter | Required | Description |
|-----------|----------|-------------|
| `workspaceId` | Yes | The workspace ID containing the copy job |
| `copyJobId` | Yes | The copy job ID to list schedules for |
| `continuationToken` | No | A token for retrieving the next page of results |

#### Response Format
```json
{
  "copyJobId": "87654321-4321-4321-4321-210987654321",
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

## Copy Job Properties

Copy jobs in Microsoft Fabric include several key properties:

### Basic Properties
- **id**: Unique identifier for the copy job
- **displayName**: Human-readable name of the copy job
- **description**: Optional description of the copy job's purpose
- **type**: Always "CopyJob" for copy job items
- **workspaceId**: ID of the containing workspace

### Optional Properties
- **folderId**: ID of the folder containing the copy job (if organized in folders)

## Usage Examples

### Copy Job Creation
```
# Create a basic copy job
> create a copy job named "Customer Data Copy" in workspace 12345678-1234-1234-1234-123456789012

# Create copy job with description
> create copy job "Sales Copy Job" with description "Daily sales data copy between lakehouses" in workspace 12345678-1234-1234-1234-123456789012

# Create copy job in a specific folder
> create copy job "Marketing Data Copy" in folder 11111111-1111-1111-1111-111111111111 within workspace 12345678-1234-1234-1234-123456789012
```

### Basic Copy Job Operations
```
# List all copy jobs in a workspace
> list copy jobs in workspace 12345678-1234-1234-1234-123456789012

# Get copy job details
> show me copy job 87654321-4321-4321-4321-210987654321 in workspace 12345678-1234-1234-1234-123456789012

# Update copy job name
> rename copy job 87654321-4321-4321-4321-210987654321 to "New Copy Job Name"
```

### Copy Job Definition Operations
```
# Get copy job definition
> show me the definition of copy job 87654321-4321-4321-4321-210987654321 in workspace 12345678-1234-1234-1234-123456789012

# Update copy job definition
> update the definition of copy job 87654321-4321-4321-4321-210987654321 with the following JSON configuration
```

### Copy Job Run and Schedule Operations
```
# Trigger a copy job run
> run copy job 87654321-4321-4321-4321-210987654321 in workspace 12345678-1234-1234-1234-123456789012

# Check copy job run status
> get run status for copy job 87654321-4321-4321-4321-210987654321 with job instance 34147f60-c8f1-4bb7-8b7e-24557a6bfeed

# Create a copy job schedule
> create a daily schedule for copy job 87654321-4321-4321-4321-210987654321 in workspace 12345678-1234-1234-1234-123456789012

# List copy job schedules
> list schedules for copy job 87654321-4321-4321-4321-210987654321 in workspace 12345678-1234-1234-1234-123456789012
```
