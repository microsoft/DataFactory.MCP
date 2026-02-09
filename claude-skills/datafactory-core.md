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
- `get_decoded_dataflow_definition` → read M code
- `create_dataflow` → create new dataflow
- `validate_and_save_m_document` → save M code (including destination config)
- `execute_query` → run M, get results
- `add_connection_to_dataflow` → attach connections
- `add_or_update_query_in_dataflow` → add/update queries

## Load Additional Knowledge

| When | Load |
|------|------|
| Query timeout, slow performance, chunking | `datafactory-performance.md` |
| Output destinations, new tables, staging, programmatic config | `datafactory-destinations.md` |
| Fast Copy, Action.Sequence, Modern Evaluator | `datafactory-advanced.md` |
