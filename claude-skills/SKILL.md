---
name: datafactory-tips
description: Operational tips and best practices for working with Microsoft Fabric Data Factory MCP tools. Use when executing queries, troubleshooting timeouts, creating dataflows end-to-end, or working with large datasets via the DataFactory.MCP tools.
---

# Data Factory Tips

Operational knowledge for working with Microsoft Fabric Data Factory.

## Must / Prefer / Avoid

### MUST DO
- Use `executeOption = "ApplyChangesIfNeeded"` on first refresh of API-created dataflows
- Re-add connections via `add_connection_to_dataflow` after `save_dataflow_definition` (save may wipe them)
- Use `list_workspaces` and filter by name — there is no `find_workspace` tool
- Validate each connection with `execute_query` after adding
- Create a new dataflow instead of reverting a multi-source one to single-source — there is no `remove_connection` tool

### PREFER
- Filter early in M queries to enable query folding (push filters to source)
- Expensive operations (sorting, aggregation) last — streaming operations (filter, select) first
- `execute_query` with `customMashupDocument` to iterate on M code before saving
- Standard engine for complex transforms; Fast Copy only for simple ingestion

### AVOID
- `get_authoring_guidance` — deprecated, author M directly
- Sorting early in query chains (requires reading all data before returning results)
- `SELECT *` equivalent without row limits during development — use "Keep First Rows" then remove

## Symptom Triage

| Symptom | Likely Cause | Read |
|---------|-------------|------|
| `DataflowNeverPublishedError` | Default SkipApplyChanges on first run | `destinations/dest-new-table.md` |
| `DestinationColumnNotFound` | Manual mappings for new table | `destinations/dest-new-table.md` |
| Credentials error on Lakehouse | Connection not bound | `datafactory-connections.md` (Binding) |
| FastCopy fails with transforms | Unsupported transform in fast copy | `datafactory-advanced.md` (Fast Copy) |
| Instant refresh fail (0-3s) | Privacy firewall or unpublished draft | `sources/multi-source.md` |
| Multi-source instant fail via API | Dirty dataflow or separate Lakehouse.Contents calls | `sources/multi-source.md` |
| `IsNewTarget = false` fails | Direct navigation on API-created dataflow | `destinations/dest-new-table.md` |
| Stale connections after revert | save_dataflow_definition doesn't remove connections | `datafactory-connections.md` (Troubleshooting) |

## Knowledge Files

| File | When to Read |
|------|--------------|
| `datafactory-core.md` | MCP tools, M basics, rolling dates, what-if queries |
| `datafactory-connections.md` | Connection discovery, creation, binding, gateways, troubleshooting connection errors |
| `datafactory-performance.md` | Query timeouts, chunking, query folding, connector selection |
| `datafactory-advanced.md` | Fast Copy limits, Action.Sequence, Modern Evaluator |
| `datafactory-pipelines.md` | Pipeline creation, Dataflow activities, chaining, scheduling |

### Destination Files (read only the one you need)

| File | When to Read |
|------|--------------|
| `destinations/dest-new-table.md` | Creating a new output table via MCP (most common path) |
| `destinations/dest-existing-table.md` | Writing to a table that already exists |
| `destinations/dest-troubleshooting.md` | Diagnosing refresh failures, connection issues, silent errors |

### Source Files

| File | When to Read |
|------|--------------|
| `sources/multi-source.md` | Combining Lakehouse + SharePoint/Web sources, AllowCombine |
| `sources/sharepoint-excel.md` | Reading Excel files from SharePoint via Web.Contents |

### M Templates (copy-paste-ready, no prose)

| File | When to Read |
|------|--------------|
| `templates/m-new-table-destination.m` | Need the complete M section document for a new table |
| `templates/m-existing-table-destination.m` | Need the M section document for an existing table |
| `templates/m-multi-source-section.m` | Need the M section document for multi-source with AllowCombine |
| `templates/m-sharepoint-excel-source.m` | Need the M snippet for SharePoint Excel via Web.Contents |
| `templates/pipeline-single-dataflow.json` | Need pipeline JSON for a single Dataflow activity |
| `templates/pipeline-chained-dataflows.json` | Need pipeline JSON for chained Dataflow activities |
