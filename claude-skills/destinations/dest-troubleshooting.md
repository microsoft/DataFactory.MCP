# Destination Troubleshooting

## Common Pitfalls — Expanded

| Symptom | Cause | Fix |
|---------|-------|-----|
| `DataflowNeverPublishedError` | Default `SkipApplyChanges` on first run | `executeOption = "ApplyChangesIfNeeded"` |
| `DestinationColumnNotFound` | Manual mappings for new table | Use `Kind = "Automatic"`, no `ColumnSettings` |
| Credentials error on Lakehouse | Connection not bound | `add_connection_to_dataflow` then validate |
| FastCopy fails with transforms | `Table.Group`, `NestedJoin`, etc. | Remove `[StagingDefinition]` |
| Instant refresh fail (0-3s) | Privacy firewall or unpublished | `[AllowCombine = true]` and/or `ApplyChangesIfNeeded` |
| `loadEnabled: false` in metadata | Controls data staging, not destinations | Not a problem with `DataDestinations` + `ApplyChangesIfNeeded` |
| Multi-source instant fail via API | Dirty dataflow or separate Lakehouse.Contents | Create fresh dataflow; consolidate reads |
| Lakehouse-only fails after revert | `save_dataflow_definition` doesn't remove stale connections | Create new dataflow; no `remove_connection` tool |
| `IsNewTarget = false` fails on API-created | Direct `[Data]` navigation fails on first refresh | Use `IsNewTarget = true` with `?[Data]?` null-safe operators |

## Detailed Explanations

### `DataflowNeverPublishedError`
API-created dataflows start in an unpublished draft state. The default refresh option (`SkipApplyChanges`) skips metadata reconciliation, so the draft never gets published. Always use `ApplyChangesIfNeeded` on the first refresh — it re-parses M annotations, publishes the draft, and reconciles metadata before executing.

### `DestinationColumnNotFound`
When `Kind = "Manual"` is set, the engine validates column mappings against the destination table BEFORE creating it. Since the table doesn't exist yet, every column fails validation. Use `Kind = "Automatic"` for new tables — it infers and creates the schema on first refresh.

### Stale Connections After Revert
`save_dataflow_definition` does NOT remove connections from queryMetadata — it only adds/updates them. If you convert a multi-source dataflow back to single-source, the old connections remain as ghost metadata, causing failures. There is no `remove_connection` tool. Create a new dataflow instead.

### `loadEnabled: false`
`save_dataflow_definition` sets `loadEnabled: false` in queryMetadata. This controls whether data is staged during transformations, not whether the destination is configured. With `DataDestinations` annotations and `ApplyChangesIfNeeded`, this is reconciled automatically.

### `IsNewTarget = false` on API-Created Dataflows
Direct `[Data]` navigation (without `?` null-safe operators) combined with `IsNewTarget = false` fails on first refresh of API-created dataflows. The table navigation step runs before the platform reconciles metadata, finding no table. Always use `IsNewTarget = true` with `?[Data]?` for first-time API setup, even for existing tables.

## New Table vs Existing Table Summary

| Scenario | `IsNewTarget` | `AllowCreation` | Navigation |
|----------|---------------|-----------------|------------|
| Table doesn't exist yet | `true` | `true` | `?[Data]?` (null-safe) |
| Table already exists (UI-created dataflow) | `false` | `false` | `[Data]` (direct) |
| Table already exists (API-created dataflow) | `true` | `true` | `?[Data]?` (null-safe) — use `dest-new-table.md` pattern |

## Edge Cases

- **Connection binding and ID format**: See `datafactory-connections.md` for binding workflow, GUID format, and `clearExisting` usage.
- **Warehouse destinations**: Staging is REQUIRED. Lakehouse destinations have staging disabled by default for performance.
