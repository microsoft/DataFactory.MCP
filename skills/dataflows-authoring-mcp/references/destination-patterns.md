# New Table Destination Pattern

Creating a new output table in Lakehouse via MCP tools. This is the most common programmatic destination path.

## Required Components

### 1. DataDestinations Annotation

Place immediately before the source query. **Must use `Automatic`** — Manual validates column mappings against a table that doesn't exist yet, causing `DestinationColumnNotFound`.

```m
[DataDestinations = {[
  Definition = [Kind = "Reference", QueryName = "MyQuery_DataDestination", IsNewTarget = true],
  Settings = [Kind = "Automatic", TypeSettings = [Kind = "Table"]]
]}]
shared MyQuery = let
  // source query logic
in
  Result;
```

### 2. Hidden Destination Query

**Must use `?` null-safe operators** — the table doesn't exist on first refresh, so direct `[Data]` navigation fails with "The key didn't match any rows in the table."

```m
shared MyQuery_DataDestination = let
  Pattern = Lakehouse.Contents([HierarchicalNavigation = null, CreateNavigationProperties = false, EnableFolding = false]),
  Navigation_1 = Pattern{[workspaceId = "your-workspace-id"]}[Data],
  Navigation_2 = Navigation_1{[lakehouseId = "your-target-lakehouse-id"]}[Data],
  TableNavigation = Navigation_2{[Id = "MyQuery", ItemKind = "Table"]}?[Data]?
in
  TableNavigation;
```

### 3. Complete Section Document

Submit via `save_dataflow_definition`:

```m
section Section1;

[DataDestinations = {[
  Definition = [Kind = "Reference", QueryName = "MyQuery_DataDestination", IsNewTarget = true],
  Settings = [Kind = "Automatic", TypeSettings = [Kind = "Table"]]
]}]
shared MyQuery = let
  Source = Lakehouse.Contents(null),
  Nav = Source{[workspaceId = "..."}]}[Data],
  DB = Nav{[lakehouseId = "..."]}[Data],
  // transform logic
  Result = DB
in
  Result;

shared MyQuery_DataDestination = let
  Pattern = Lakehouse.Contents([HierarchicalNavigation = null, CreateNavigationProperties = false, EnableFolding = false]),
  Navigation_1 = Pattern{[workspaceId = "..."]}[Data],
  Navigation_2 = Navigation_1{[lakehouseId = "..."]}[Data],
  TableNavigation = Navigation_2{[Id = "MyQuery", ItemKind = "Table"]}?[Data]?
in
  TableNavigation;
```

## MCP Tool Workflow (7 Steps)

```
1. create_dataflow              → Create empty dataflow
2. add_connection_to_dataflow   → Attach ALL source connections
3. execute_query                → Discover target lakehouse ID
4. save_dataflow_definition     → Save complete M document with destination config
5. add_connection_to_dataflow   → Re-add connections (save may wipe them)
6. get_dataflow_definition      → Verify connections + destination config
7. refresh_dataflow_background  → Materialize the table
                                   MUST use executeOption="ApplyChangesIfNeeded"
```

## Why `ApplyChangesIfNeeded` Is Required

API-created dataflows start in an unpublished draft state. `save_dataflow_definition` sets `loadEnabled: false` in queryMetadata. The platform reconciles both issues on refresh ONLY when using `ApplyChangesIfNeeded`.

| Refresh Option | Behavior on MCP-created dataflow |
|---|---|
| `SkipApplyChanges` (default) | **Instant failure** — stale metadata, draft never published |
| `ApplyChangesIfNeeded` | Re-parses M annotations, publishes draft, reconciles metadata, then executes |

After the first successful refresh, subsequent refreshes can use either option.

## Discovering Lakehouse IDs

Use `execute_query` to list available lakehouses in a workspace:

```m
let
  Source = Lakehouse.Contents(null),
  Navigation = Source{[workspaceId = "your-workspace-id"]}[Data]
in
  Navigation
```

Returns: `lakehouseId`, `lakehouseName`, `databaseId` for each lakehouse.

## Key Details

| Element | Requirement |
|---------|-------------|
| `_DataDestination` suffix | Required naming convention for hidden query |
| `EnableFolding = false` | Required in destination query pattern |
| `isHidden = true` | Automatically set in queryMetadata for destination queries |
| Target table ID | Uses source query name (table created on first refresh) |

For connection binding workflow and connection ID format → see `datafactory-connections.md`.

## `IsNewTarget = true` Always for API-Created Dataflows

Even when targeting a table that already exists, use `IsNewTarget = true` with `?[Data]?` null-safe operators. `IsNewTarget = false` with direct `[Data]` navigation fails on first refresh of API-created dataflows.

---

# Existing Table Destination Pattern

Writing to a Lakehouse table that already exists. Use when you need precise control over column mappings and update behavior.

> **API-created dataflows (via MCP tools):** Use `dest-new-table.md` instead — even for existing tables. `IsNewTarget = true` with `?[Data]?` null-safe operators is required for first refresh of API-created dataflows. The pattern below is for dataflows created or already published through the Fabric UI.

## DataDestinations Annotation

```m
[DataDestinations = {[
  Definition = [Kind = "Reference", QueryName = "MyQuery_DataDestination", IsNewTarget = false],
  Settings = [
    Kind = "Manual",
    AllowCreation = false,
    DynamicSchema = false,
    UpdateMethod = [Kind = "Replace"],
    TypeSettings = [Kind = "Table"]
  ]
]}]
shared MyQuery = let
  // source query logic
in
  Result;
```

## Hidden Destination Query

Uses direct `[Data]` navigation (no null-safe operators) — the table must exist.

```m
shared MyQuery_DataDestination = let
  Pattern = Lakehouse.Contents([HierarchicalNavigation = null, CreateNavigationProperties = false, EnableFolding = false]),
  Navigation_1 = Pattern{[workspaceId = "your-workspace-id"]}[Data],
  Navigation_2 = Navigation_1{[lakehouseId = "your-target-lakehouse-id"]}[Data],
  TableNavigation = Navigation_2{[Id = "MyQuery", ItemKind = "Table"]}[Data]
in
  TableNavigation;
```

## Automatic vs Manual Settings

| Setting | `Kind = "Automatic"` | `Kind = "Manual"` |
|---------|---------------------|-------------------|
| Mapping | Managed for you | Explicit `ColumnSettings` required |
| Schema changes | Allowed (table dropped/recreated) | Must match exactly |
| Use case | New tables, flexible schema | Existing tables, preserve relationships |

## Update Methods

- **Replace**: Data dropped and replaced each refresh
- **Append**: Output appended to existing data

## Staging Requirements

- **Warehouse destination**: Staging REQUIRED (enable on query)
- **Lakehouse destination**: Staging disabled by default for performance

## API-Created Dataflows

Do not use this pattern for API-created dataflows. Use `dest-new-table.md` with `IsNewTarget = true` and `?[Data]?` null-safe operators — even when the table already exists. After the first successful refresh, you can switch to this pattern if needed.

---

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
