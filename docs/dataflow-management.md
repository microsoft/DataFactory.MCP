# Dataflow Management Guide

This guide covers how to use the Microsoft Data Factory MCP Server for managing Microsoft Fabric dataflows.

## Overview

The dataflow management tools allow you to:
- **Create** new dataflows in Microsoft Fabric workspaces
- **List** all dataflows within a specific workspace
- **Get** decoded dataflow definitions with M code and metadata
- **Execute** M (Power Query) queries against dataflows
- **Add** connections to existing dataflows
- **Add or update** queries in existing dataflows
- Navigate paginated results for large dataflow collections

## MCP Tools

### list_dataflows

Returns a list of Dataflows from the specified workspace. This API supports pagination.

#### Usage
```
list_dataflows(workspaceId: "12345678-1234-1234-1234-123456789012")
```

#### With Pagination
```
list_dataflows(
  workspaceId: "12345678-1234-1234-1234-123456789012", 
  continuationToken: "next-page-token"
)
```

#### Parameters

| Parameter | Required | Description |
|-----------|----------|-------------|
| `workspaceId` | Yes | The workspace ID to list dataflows from |
| `continuationToken` | No | A token for retrieving the next page of results |

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
      "folderId": "11111111-1111-1111-1111-111111111111"
    }
  ]
}
```

### create_dataflow

Creates a Dataflow in the specified workspace. The workspace must be on a supported Fabric capacity.

#### Usage
```
create_dataflow(
  workspaceId: "12345678-1234-1234-1234-123456789012", 
  displayName: "My New Dataflow"
)
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

| Parameter | Required | Description |
|-----------|----------|-------------|
| `workspaceId` | Yes | The workspace ID where the dataflow will be created |
| `displayName` | Yes | The Dataflow display name |
| `description` | No | The Dataflow description (max 256 characters) |
| `folderId` | No | The folder ID where the dataflow will be created (defaults to workspace root) |

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

### get_decoded_dataflow_definition

Gets the decoded definition of a dataflow with human-readable content (queryMetadata.json, mashup.pq M code, and .platform metadata).

#### Usage
```
get_decoded_dataflow_definition(
  workspaceId: "12345678-1234-1234-1234-123456789012",
  dataflowId: "87654321-4321-4321-4321-210987654321"
)
```

#### Parameters

| Parameter | Required | Description |
|-----------|----------|-------------|
| `workspaceId` | Yes | The workspace ID containing the dataflow |
| `dataflowId` | Yes | The dataflow ID to get the decoded definition for |

#### Response Format
```json
{
  "success": true,
  "dataflowId": "87654321-4321-4321-4321-210987654321",
  "workspaceId": "12345678-1234-1234-1234-123456789012",
  "queryMetadata": { ... },
  "mashupQuery": "section Section1; shared Query1 = let Source = ...",
  "platformMetadata": { ... },
  "rawPartsCount": 3,
  "rawParts": [
    {
      "path": "mashup.pq",
      "payloadType": "Text",
      "payloadSize": 1024
    }
  ]
}
```

### execute_query

Executes a query against a dataflow and returns the complete results (all data) in Apache Arrow format. This allows you to run M (Power Query) language queries against data sources connected through the dataflow and get the full dataset.

#### Usage
```
execute_query(
  workspaceId: "12345678-1234-1234-1234-123456789012",
  dataflowId: "87654321-4321-4321-4321-210987654321",
  queryName: "MyQuery",
  customMashupDocument: "let Source = Sql.Database(\"server\", \"db\") in Source"
)
```

#### Parameters

| Parameter | Required | Description |
|-----------|----------|-------------|
| `workspaceId` | Yes | The workspace ID containing the dataflow |
| `dataflowId` | Yes | The dataflow ID to execute the query against |
| `queryName` | Yes | The name of the query to execute |
| `customMashupDocument` | Yes | The M (Power Query) language query to execute. Can be either a raw M expression (which will be auto-wrapped) or a complete section document. |

**Note**: When displaying results to users, format the `table.rows` data as a markdown table using the column names from `table.columns` for immediate visual representation.

### add_connection_to_dataflow

Adds a connection to an existing dataflow by updating its definition. Retrieves the current dataflow definition, gets connection details, and updates the queryMetadata.json to include the new connection.

#### Usage
```
add_connection_to_dataflow(
  workspaceId: "12345678-1234-1234-1234-123456789012",
  dataflowId: "87654321-4321-4321-4321-210987654321",
  connectionId: "a0b9fa12-60f5-4f95-85ca-565d34abcea1"
)
```

#### Parameters

| Parameter | Required | Description |
|-----------|----------|-------------|
| `workspaceId` | Yes | The workspace ID containing the dataflow |
| `dataflowId` | Yes | The dataflow ID to update |
| `connectionId` | Yes | The connection ID to add to the dataflow |

### add_or_update_query_in_dataflow

Adds or updates a query in an existing dataflow by updating its definition. The query will be added to the mashup.pq file and registered in queryMetadata.json.

#### Usage
```
add_or_update_query_in_dataflow(
  workspaceId: "12345678-1234-1234-1234-123456789012",
  dataflowId: "87654321-4321-4321-4321-210987654321",
  queryName: "MyQuery",
  mCode: "let Source = Sql.Database(\"server\", \"db\") in Source"
)
```

#### Parameters

| Parameter | Required | Description |
|-----------|----------|-------------|
| `workspaceId` | Yes | The workspace ID containing the dataflow |
| `dataflowId` | Yes | The dataflow ID to update |
| `queryName` | Yes | The name of the query to add or update |
| `mCode` | Yes | The M (Power Query) code for the query. Can be a full 'let...in' expression or a simple expression that will be wrapped automatically. |

#### Response Format
```json
{
  "success": true,
  "dataflowId": "87654321-4321-4321-4321-210987654321",
  "workspaceId": "12345678-1234-1234-1234-123456789012",
  "queryName": "MyQuery",
  "message": "Successfully added/updated query 'MyQuery' in dataflow 87654321-4321-4321-4321-210987654321"
}
```

#### Examples

**Add a simple query:**
```
add_or_update_query_in_dataflow(
  workspaceId: "12345678-1234-1234-1234-123456789012",
  dataflowId: "87654321-4321-4321-4321-210987654321",
  queryName: "Customers",
  mCode: "let\n    Source = Sql.Database(\"server.database.windows.net\", \"mydb\"),\n    Customers = Source{[Schema=\"dbo\", Item=\"Customers\"]}[Data]\nin\n    Customers"
)
```

**Update an existing query with transformations:**
```
add_or_update_query_in_dataflow(
  workspaceId: "12345678-1234-1234-1234-123456789012",
  dataflowId: "87654321-4321-4321-4321-210987654321",
  queryName: "FilteredOrders",
  mCode: "let\n    Source = Sql.Database(\"server\", \"db\"),\n    Orders = Source{[Schema=\"dbo\", Item=\"Orders\"]}[Data],\n    FilteredRows = Table.SelectRows(Orders, each [Status] = \"Active\"),\n    SortedRows = Table.Sort(FilteredRows, {{\"OrderDate\", Order.Descending}})\nin\n    SortedRows"
)
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
```

### Basic Dataflow Operations
```
# List all dataflows in a workspace
> list dataflows in workspace 12345678-1234-1234-1234-123456789012

# Get decoded dataflow definition
> show me the M code for dataflow 87654321-4321-4321-4321-210987654321 in workspace 12345678-1234-1234-1234-123456789012
```

### Query Execution
```
# Execute a simple M query
> run query against dataflow to get all customers from the SQL database

# Execute a custom M expression
> execute M query "let Source = Sql.Database(\"server\", \"db\"), Customers = Source{[Schema=\"dbo\",Item=\"Customers\"]}[Data] in Customers"
```

### Adding Connections
```
# Add a connection to a dataflow
> add connection a0b9fa12-60f5-4f95-85ca-565d34abcea1 to dataflow 87654321-4321-4321-4321-210987654321 in workspace 12345678-1234-1234-1234-123456789012
```