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
