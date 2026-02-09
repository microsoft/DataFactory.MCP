# Data Factory Knowledge

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

## Additional Knowledge

When these topics come up, reference the corresponding file in `docs/datafactory/`:

| Topic | File |
|-------|------|
| Query timeout, slow performance, chunking | `performance.md` |
| Output destinations, new tables, staging | `destinations.md` |
| Fast Copy, Action.Sequence, Modern Evaluator | `advanced.md` |
