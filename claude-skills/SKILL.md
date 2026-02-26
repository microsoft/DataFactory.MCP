---
name: datafactory-tips
description: Operational tips and best practices for working with Microsoft Fabric Data Factory MCP tools. Use when executing queries, troubleshooting timeouts, creating dataflows end-to-end, or working with large datasets via the DataFactory.MCP tools.
---

# Data Factory Tips

Operational knowledge for working with Microsoft Fabric Data Factory.

## Must / Prefer / Avoid

### MUST DO
- Use `executeOption = "ApplyChangesIfNeeded"` on first refresh of API-created dataflows (default causes `DataflowNeverPublishedError`)
- Include both a `DataDestinations` annotation AND a companion `_DataDestination` query for Lakehouse output
- Name destination queries `[QueryName]_DataDestination` — e.g., query `Results` gets companion `Results_DataDestination`
- Use `Kind = "Automatic"` (not Manual) when creating new destination tables — Manual causes `DestinationColumnNotFound`
- Always use `IsNewTarget = true` with `?` null-safe operators in API-created dataflows — even when the table exists; `IsNewTarget = false` with direct `[Data]` fails on first refresh
- Add `[AllowCombine = true]` section attribute when combining multiple source types (Lakehouse + SharePoint, etc.)
- Use a fresh dataflow for multi-source — never transition a published single-source dataflow to multi-source
- Consolidate all Lakehouse table reads into a single source query (one `Lakehouse.Contents` call) in multi-source dataflows
- Re-add connections via `add_connection_to_dataflow` after `save_dataflow_definition` (save may wipe them)
- Use `clearExisting: true` to replace all connections atomically instead of manual removal
- Validate each connection with `execute_query` after adding
- Use `list_workspaces` and filter by name — there is no `find_workspace` tool
- Create a new dataflow instead of reverting a multi-source one to single-source — `save_dataflow_definition` does NOT remove stale connections, and there is no `remove_connection` tool

### PREFER
- Filter early in M queries to enable query folding (push filters to source)
- Expensive operations (sorting, aggregation) last — streaming operations (filter, select) first
- Native connectors (SQL Server, Lakehouse) over generic ones (ODBC/OLEDB) for better query folding
- Explicit data types on all columns, especially for unstructured sources (CSV, TXT)
- Modular queries — split large queries via "Extract Previous" for readability and reuse
- Standard engine for complex transforms; Fast Copy only for simple ingestion (select/rename/type change)
- Automatic schema settings for new tables; Manual for stable existing tables with downstream dependencies
- Validate both source and destination lakehouse access before configuring DataDestination
- `execute_query` with `customMashupDocument` to iterate on M code before saving

### AVOID
- `get_authoring_guidance` — deprecated, author M directly
- Fast Copy with `Table.Group`, `Table.NestedJoin`, or any schema-changing transform
- Manual column mappings (`ColumnSettings`) for new tables
- Default `SkipApplyChanges` on first refresh of API-created dataflows
- `Action.Sequence` for normal query transformations — only use for side-effecting writes
- Sorting early in query chains (requires reading all data before returning results)
- `SELECT *` equivalent without row limits during development — use "Keep First Rows" then remove
- Reusing a previously-published single-source dataflow for multi-source — stale connections persist and cause instant refresh failure; always create a fresh dataflow for multi-source
- Separate `Lakehouse.Contents` calls per query in multi-source dataflows — consolidate all Lakehouse table reads into a single query for reliable multi-source refresh

## Knowledge Files

| File | When to Use |
|------|-------------|
| `datafactory-core.md` | MCP tools, M basics, connection discovery, rolling dates, what-if queries |
| `datafactory-destinations.md` | Creating/configuring output destinations, DataDestination patterns, AllowCombine |
| `datafactory-performance.md` | Query timeouts, chunking, query folding, connector selection |
| `datafactory-advanced.md` | Fast Copy limits, Action.Sequence, Modern Evaluator |
| `datafactory-pipelines.md` | Creating pipelines, Dataflow activities, chaining, scheduling |
