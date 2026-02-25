# Data Factory Core Knowledge

## What is M

M (Power Query Formula Language) is a functional, case-sensitive, lazy-evaluated language for data transformation. Used in Power BI, Excel Power Query, and Dataflow Gen2.

## What is Dataflow Gen2

Fabric's cloud ETL tool. Runs M queries, writes to Lakehouse/Warehouse. You define data transformations; platform handles orchestration.

## Basic M Pattern

```m
let
    Source = Lakehouse.Contents(null),
    Filtered = Table.SelectRows(Source, each [Status] = "Active"),
    Result = Table.SelectColumns(Filtered, {"ID", "Name"})
in
    Result
```

## MCP Tools

- `list_workspaces`, `list_dataflows` → discover resources
- `get_dataflow_definition` → read M code
- `create_dataflow` → create new dataflow
- `save_dataflow_definition` → save M code (including destination config)
- `execute_query` → run M, get results
- `add_connection_to_dataflow` → attach connections
- `add_or_update_query_in_dataflow` → add/update queries
- `refresh_dataflow_background` → trigger refresh
- `refresh_dataflow_status` → poll for completion
- `list_connections` → find connection GUIDs

## Connection Discovery

When a user mentions a data source — by name, type, server, path, or any description — use `list_connections` to find the matching connection:

1. Call `list_connections` to get all available connections
2. Match by `connectionType` and connection parameters (server name, URL, path, account)
3. Extract the `DatasourceId` GUID from the matching connection

**Common mappings:**

| User describes | connectionType | Match on |
|----------------|----------------|----------|
| SQL database, SQL Server, "contoso-db" | `SQL` | server/database in parameters |
| SharePoint file, Excel on SharePoint | `Web` | URL containing `.sharepoint.com` |
| Lakehouse, "sales Lakehouse" | `Lakehouse` | workspace/lakehouse name |
| Blob storage, Azure storage account | `AzureBlobs` | account name |
| Web API, REST endpoint | `Web` | base URL |

Users rarely use exact source names. Treat any mention of a data source, database, file location, or storage account as a trigger to call `list_connections` and match.

If no matching connection exists, use `create_connection` (or the `create_connection_ui` MCP App) to create one.

## Connection GUIDs

`add_connection_to_dataflow` accepts a single GUID string, not arrays or JSON.
