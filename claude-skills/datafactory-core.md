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

## Connection Management

`add_connection_to_dataflow` accepts a single GUID string, an array of GUIDs, or no connections (when clearing).

- **Add**: pass `connectionIds` (string or array) — appends to existing connections
- **Replace**: pass `connectionIds` + `clearExisting: true` — atomically clears and adds
- **Clear**: pass `clearExisting: true` with no `connectionIds` — removes all connections
