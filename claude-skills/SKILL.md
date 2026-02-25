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
- Use `Kind = "Automatic"` (not Manual) when creating new destination tables — Manual causes `DestinationColumnNotFound`
- Use `?` null-safe operators in the `_DataDestination` query for new tables (table doesn't exist yet)
- Add `[AllowCombine = true]` section attribute when combining multiple source types (Lakehouse + SharePoint, etc.)
- Re-add connections via `add_connection_to_dataflow` after `save_dataflow_definition` (save may wipe them)
- Use `clearExisting: true` to replace all connections atomically instead of manual removal
- Validate each connection with `execute_query` after adding
- Use `list_workspaces` and filter by name — there is no `find_workspace` tool

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

## Knowledge Files

| File                          | When to Use                                                                      |
| ----------------------------- | -------------------------------------------------------------------------------- |
| `datafactory-core.md`         | MCP tools, M basics, first-refresh rule, deprecated tools                        |
| `datafactory-destinations.md` | Creating/configuring output destinations, DataDestination patterns, AllowCombine |
| `datafactory-performance.md`  | Query timeouts, chunking, query folding, connector selection                     |
| `datafactory-advanced.md`     | Fast Copy limits, Action.Sequence, Modern Evaluator                              |
