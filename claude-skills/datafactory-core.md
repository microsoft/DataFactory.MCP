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

**Fabric-source connections** (Lakehouse, Warehouse, Eventhouse): All Fabric-native artifact connections require workspace context (workspace ID) to resolve the target. Artifact names are not globally unique — they are scoped to their parent workspace. When listing or creating connections to Fabric sources, always expect/require the workspace ID where the source artifact resides. This applies uniformly across all Fabric item types.

If no matching connection exists, use `create_connection` (or the `create_connection_ui` MCP App) to create one.

## Connection Management

`add_connection_to_dataflow` accepts a single GUID string, an array of GUIDs, or no connections (when clearing).

- **Add**: pass `connectionIds` (string or array) — appends to existing connections
- **Replace**: pass `connectionIds` + `clearExisting: true` — atomically clears and adds
- **Clear**: pass `clearExisting: true` with no `connectionIds` — removes all connections

## Rolling Date Windows

Use relative dates instead of hardcoded ranges for dataflows that refresh on a schedule:

```m
// Rolling 3-month window: 3 months ago through end of last month
today = DateTime.Date(DateTime.LocalNow()),
window_start = Date.StartOfMonth(Date.AddMonths(today, -3)),
window_end = Date.EndOfMonth(Date.AddMonths(today, -1)),
filtered = Table.SelectRows(source, each [date_col] >= window_start and [date_col] <= window_end)
```

Common patterns:
| Window | Start | End |
|--------|-------|-----|
| Last N months | `Date.StartOfMonth(Date.AddMonths(today, -N))` | `Date.EndOfMonth(Date.AddMonths(today, -1))` |
| Current quarter | `Date.StartOfQuarter(today)` | `today` |
| Year to date | `#date(Date.Year(today), 1, 1)` | `today` |

Use rolling windows when a pipeline will refresh the dataflow on a schedule. Use hardcoded dates for one-off analysis.

## What-If Query Pattern

Create derivative queries that reference a materialized output query to model scenarios without duplicating source logic:

```m
// Base query (has DataDestination, materializes to Lakehouse)
shared StorePerformance = let ... in final_table;

// What-if: reclassify a dimension and re-aggregate
shared WhatIf_TacomaToTierC = let
  Source = StorePerformance,
  Reclassed = Table.AddColumn(Source, "new_tier",
    each if [store_name] = "Tacoma" then "C" else [tier], type text),
  Dropped = Table.RemoveColumns(Reclassed, {"tier"}),
  Renamed = Table.RenameColumns(Dropped, {{"new_tier", "tier"}}),
  Grouped = Table.Group(Renamed, {"tier"}, {
    {"total_revenue", each List.Sum([revenue]), type number}
  })
in
  Grouped;
```

What-if queries can also have their own `DataDestination` to persist as Lakehouse tables. This pattern keeps source logic in one place — all scenarios derive from the same base.
