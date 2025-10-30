# Dataflow Management Guide

This guide covers how to use the Microsoft Data Factory MCP Server for managing Microsoft Fabric dataflows.

## Overview

The dataflow management tools allow you to:
- **Create** new dataflows in Microsoft Fabric workspaces
- **List** all dataflows within a specific workspace
- Access detailed dataflow information including properties and metadata
- Navigate paginated results for large dataflow collections
- Handle different dataflow types and configurations

## Available Operations

### Create Dataflow

Create a new dataflow in a specified Microsoft Fabric workspace. The workspace must be on a supported Fabric capacity.

#### Usage
```
create_dataflow(workspaceId: "12345678-1234-1234-1234-123456789012", displayName: "My New Dataflow")
```

#### With Optional Parameters
```
create_dataflow(
  workspaceId: "12345678-1234-1234-1234-123456789012", 
  displayName: "Sales ETL Pipeline",
  description: "Processes daily sales data from multiple sources",
  folderId: "11111111-1111-1111-1111-111111111111"
)
```

#### Parameters
- **workspaceId** (required): The workspace ID where the dataflow will be created
- **displayName** (required): The display name for the dataflow (max 256 characters)
- **description** (optional): Description of the dataflow's purpose (max 256 characters)
- **folderId** (optional): ID of the folder where the dataflow will be created (defaults to workspace root)

#### Response Format
```json
{
  "success": true,
  "message": "Dataflow 'Sales ETL Pipeline' created successfully",
  "dataflowId": "87654321-4321-4321-4321-210987654321",
  "displayName": "Sales ETL Pipeline",
  "description": "Processes daily sales data from multiple sources",
  "type": "Dataflow",
  "workspaceId": "12345678-1234-1234-1234-123456789012",
  "folderId": "11111111-1111-1111-1111-111111111111",
  "createdAt": "2025-10-30T10:30:00Z"
}
```

#### Requirements
- The workspace must be on a supported Fabric capacity
- User must have appropriate permissions in the target workspace
- Display name must follow Fabric naming conventions

### List Dataflows

Retrieve a list of all dataflows within a specified workspace with full details.

#### Usage
```
list_dataflows(workspaceId: "12345678-1234-1234-1234-123456789012")
```

#### With Pagination
```
list_dataflows(workspaceId: "12345678-1234-1234-1234-123456789012", continuationToken: "next-page-token")
```

#### Response Format
```json
{
  "workspaceId": "12345678-1234-1234-1234-123456789012",
  "dataflowCount": 5,
  "continuationToken": "eyJza2lwIjoyMCwidGFrZSI6MjB9",
  "continuationUri": "https://api.fabric.microsoft.com/v1/workspaces/12345/dataflows?continuationToken=abc123",
  "hasMoreResults": true,
  "dataflows": [
    {
      "id": "87654321-4321-4321-4321-210987654321",
      "displayName": "Sales Data ETL",
      "description": "Extracts, transforms and loads sales data from multiple sources",
      "type": "Dataflow",
      "workspaceId": "12345678-1234-1234-1234-123456789012",
      "folderId": "11111111-1111-1111-1111-111111111111",
      "tags": [
        {
          "id": "22222222-2222-2222-2222-222222222222",
          "displayName": "Sales"
        }
      ],
      "properties": {
        "isParametric": false
      }
    }
  ]
}
```

## Dataflow Properties

Dataflows in Microsoft Fabric include several key properties:

### Basic Properties
- **id**: Unique identifier for the dataflow
- **displayName**: Human-readable name of the dataflow
- **description**: Optional description of the dataflow's purpose
- **type**: Always "Dataflow" for dataflow items
- **workspaceId**: ID of the containing workspace

### Optional Properties
- **folderId**: ID of the folder containing the dataflow (if organized in folders)
- **tags**: Array of tags applied to the dataflow for categorization
- **properties**: Additional metadata about the dataflow

### Dataflow-Specific Properties
- **isParametric**: Boolean indicating if the dataflow uses parameters

## Usage Examples

### Dataflow Creation
```
# Create a basic dataflow
> create dataflow named "Customer Analytics" in workspace 12345678-1234-1234-1234-123456789012

# Create dataflow with description
> create dataflow "Sales Pipeline" with description "Daily sales data processing" in workspace 12345678-1234-1234-1234-123456789012

# Create dataflow in a specific folder
> create dataflow "Marketing Data" in folder 11111111-1111-1111-1111-111111111111 within workspace 12345678-1234-1234-1234-123456789012

# Create dataflow with all options
> create a new dataflow called "Comprehensive ETL" with description "Main data processing pipeline" in folder 11111111-1111-1111-1111-111111111111 of workspace 12345678-1234-1234-1234-123456789012
```

### Basic Dataflow Operations
```
# List all dataflows in a workspace
> list dataflows in workspace 12345678-1234-1234-1234-123456789012

# List dataflows with specific workspace
> show me all dataflows in my analytics workspace

# Get dataflows from a workspace with pagination
> list dataflows in workspace abc123 with continuation token xyz789
```

### Practical Scenarios
```
# Discovery - find all dataflows in a workspace
> what dataflows are available in workspace 12345678-1234-1234-1234-123456789012?

# Analysis - understand dataflow distribution
> show me how many dataflows are in workspace 12345678-1234-1234-1234-123456789012

# Navigation - browse through large collections
# get the next page of dataflows using token eyJza2lwIjoyMCwidGFrZSI6MjB9
```