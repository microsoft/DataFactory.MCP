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
