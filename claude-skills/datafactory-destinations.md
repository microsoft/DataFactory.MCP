# Data Destinations & Architecture

## Dataflow Gen2 Architecture (Lakehouse → Lakehouse)

For aggregated tables from source Lakehouse to destination Lakehouse, use **pure M for transformations** and let **Data Destination settings handle the write**.

### Why Pure M (Not Action-Based)

- Dataflow Gen2 generates an orchestration plan on refresh
- Your job: define the **data result** (the table)
- Platform's job: handle load/write orchestration
- Replace/Append/Create are **destination concerns**, not M code concerns

### Three-Layer Structure

```
A) Source Query       → Read from source Lakehouse
         ↓
B) Aggregation Query  → Pure M transforms (Group By, joins, filters)
         ↓
C) Output Query       → Same as B, with Data Destination attached
```

**Attach the Data Destination to your final aggregated query.** The orchestration engine handles producing the destination table at refresh.

---

## Data Destinations in Dataflow Gen2

Data destinations have two M script components:

### 1. Navigation Query (suffix `_DataDestination`)
```m
shared MyQuery_DataDestination = let
  Pattern = Lakehouse.Contents([CreateNavigationProperties = false, EnableFolding = false]),
  Navigation_1 = Pattern{[workspaceId = "..."]}[Data],
  Navigation_2 = Navigation_1{[lakehouseId = "..."]}[Data],
  TableNavigation = Navigation_2{[Id = "target_table", ItemKind = "Table"]}?[Data]?
in
  TableNavigation;
```

### 2. DataDestinations Attribute (on source query)
```m
[DataDestinations = {[Definition = [Kind = "Reference", QueryName = "MyQuery_DataDestination", IsNewTarget = true], Settings = [Kind = "Automatic", TypeSettings = [Kind = "Table"]]]}]
shared MyQuery = let
  // source query logic
in
  Result;
```

### Automatic vs Manual Settings

| Setting | `Kind = "Automatic"` | `Kind = "Manual"` |
|---------|---------------------|-------------------|
| Mapping | Managed for you | Explicit `ColumnSettings` required |
| Schema changes | Allowed (table dropped/recreated) | Must match exactly |
| Use case | New tables, flexible schema | Existing tables, preserve relationships |

### New Table vs Existing Table

| Setting | `IsNewTarget = true` | `IsNewTarget = false` |
|---------|---------------------|----------------------|
| Table creation | Created on first refresh | Must exist already |
| If table deleted | Recreated on next refresh | Refresh fails |
| Navigation query | Use `?` null-safe operators | Direct navigation |

### Update Methods
- **Replace**: Data dropped and replaced each refresh
- **Append**: Output appended to existing data

### Staging Requirements
- **Warehouse destination**: Staging REQUIRED (enable on query)
- **Lakehouse destination**: Staging disabled by default for performance

---

## Programmatic Destination Configuration via MCP Tools

Output destinations CAN be configured programmatically using `save_dataflow_definition`. This requires constructing a complete M section document with all components.

### Required Components

#### 1. DataDestinations Annotation
Place immediately before the source query definition.

**For NEW tables (don't exist yet) — use Automatic:**
```m
[DataDestinations = {[
  Definition = [Kind = "Reference", QueryName = "MyQuery_DataDestination", IsNewTarget = true],
  Settings = [Kind = "Automatic", TypeSettings = [Kind = "Table"]]
]}]
shared MyQuery = let ... in ...;
```

**CRITICAL:** Do NOT use `Kind = "Manual"` with `ColumnSettings` for new tables. Manual validates column mappings against the destination table *before* creating it, causing `DestinationColumnNotFound` errors. Use `Automatic` to let the engine infer and create the schema on first refresh.

**For EXISTING tables:**
```m
[DataDestinations = {[
  Definition = [Kind = "Reference", QueryName = "MyQuery_DataDestination", IsNewTarget = false],
  Settings = [
    Kind = "Manual",
    AllowCreation = false,
    ColumnSettings = [Mappings = {...}],
    DynamicSchema = false,
    UpdateMethod = [Kind = "Replace"],
    TypeSettings = [Kind = "Table"]
  ]
]}]
shared MyQuery = let ... in ...;
```

#### 2. Hidden Destination Query
Create a query pointing to the target lakehouse with folding disabled.

**For NEW tables — use null-safe `?` operators:**
```m
shared MyQuery_DataDestination = let
  Pattern = Lakehouse.Contents([HierarchicalNavigation = null, CreateNavigationProperties = false, EnableFolding = false]),
  Navigation_1 = Pattern{[workspaceId = "your-workspace-id"]}[Data],
  Navigation_2 = Navigation_1{[lakehouseId = "your-target-lakehouse-id"]}[Data],
  TableNavigation = Navigation_2{[Id = "MyQuery", ItemKind = "Table"]}?[Data]?
in
  TableNavigation;
```

**For EXISTING tables — direct navigation:**
```m
shared MyQuery_DataDestination = let
  Pattern = Lakehouse.Contents([HierarchicalNavigation = null, CreateNavigationProperties = false, EnableFolding = false]),
  Navigation_1 = Pattern{[workspaceId = "your-workspace-id"]}[Data],
  Navigation_2 = Navigation_1{[lakehouseId = "your-target-lakehouse-id"]}[Data],
  TableNavigation = Navigation_2{[Id = "MyQuery", ItemKind = "Table"]}[Data]
in
  TableNavigation;
```

**Critical:** Using direct navigation `[Data]` for a non-existent table causes error: "The key didn't match any rows in the table"

#### 3. Complete Section Document
Submit the full document via `save_dataflow_definition`:
```m
section Section1;
[DataDestinations = {...}]
shared MyQuery = let ... in ...;
shared MyQuery_DataDestination = let ... in ...;
```

### Multi-Source Privacy: AllowCombine

When a dataflow combines multiple source types (e.g. Lakehouse + SharePoint, Lakehouse + Web), the mashup engine's privacy firewall blocks cross-source queries by default. This causes instant "Validation failure" on refresh with no detailed error.

**Fix:** Add `[AllowCombine = true]` as a section-level attribute in the M document:

```m
[AllowCombine = true]
section Section1;

shared LakehouseQuery = let ... in ...;
shared SharePointQuery = let ... in ...;
shared JoinedOutput = let ... in ...;
```

| Symptom | Cause | Fix |
|---------|-------|-----|
| Instant refresh failure (0-3 seconds) | Privacy firewall blocking cross-source | Add `[AllowCombine = true]` before `section` |
| "Validation failure" with no details | Same — pre-execution metadata check | Same |
| Works in UI but fails via MCP | UI auto-prompts for privacy; API does not | Must set explicitly in M document |

**Critical:** This is REQUIRED for any dataflow that mixes source types. Single-source dataflows (e.g. Lakehouse-only) do not need it.

### Multi-Source via API: Requirements

Multi-source dataflows (e.g. Lakehouse + SharePoint/Web) **work via API** with `[AllowCombine = true]`, but require specific conditions:

1. **Fresh dataflow** — Always create a new dataflow for multi-source. Never transition a previously-published single-source dataflow to multi-source; stale connection metadata persists and causes instant refresh failure.
2. **Consolidated Lakehouse reads** — Read all Lakehouse tables in a single source query (one `Lakehouse.Contents` call), not separate queries per table. This reduces the number of distinct data source contexts the privacy firewall must reconcile.
3. **All connections bound** — Add all connections (Lakehouse, Web/SharePoint) via `add_connection_to_dataflow` before and after `save_dataflow_definition`.
4. **`ApplyChangesIfNeeded`** — Required on first refresh of any API-created dataflow.

**Pattern that works:**
```
StoreActuals     → Single query: Lakehouse.Contents → reads orders, items, stores, products, categories → aggregates
StoreTargets     → Excel.Workbook(Web.Contents("...sharepoint.com/.../file.xlsx"))
StoreAnnotations → Excel.Workbook(Web.Contents("...sharepoint.com/.../file.xlsx"))
OutputQuery      → Joins StoreActuals + StoreTargets + StoreAnnotations, with DataDestination attached
```

**What causes failure:**
| Scenario | Result |
|----------|--------|
| Fresh dataflow + consolidated reads + AllowCombine | Works |
| Previously-published single-source dataflow converted to multi-source | Instant failure — stale metadata |
| Separate `Lakehouse.Contents` calls per source query | May fail — multiple data source contexts |

### MCP Tool Workflow

```
1. create_dataflow              → Create empty dataflow
2. add_connection_to_dataflow   → Attach ALL source connections (one call per connection)
3. execute_query                → Discover target lakehouse ID
4. save_dataflow_definition     → Save complete M document with destination config
                                   Include [AllowCombine = true] if multi-source
5. add_connection_to_dataflow   → Re-add connections (save_dataflow_definition may wipe them)
6. get_dataflow_definition      → Verify connections + destination config
7. refresh_dataflow_background  → Materialize the table
                                   MUST use executeOption="ApplyChangesIfNeeded" on first refresh
```

### Key Details

| Element | Requirement |
|---------|-------------|
| `_DataDestination` suffix | Required naming convention for hidden query |
| `EnableFolding = false` | Required in destination query pattern |
| `isHidden = true` | Automatically set in queryMetadata for destination queries |
| Column mappings | Must match source query output columns exactly |
| Target table ID | Use source query name (table created if `AllowCreation = true`) |

### Discovering Lakehouse IDs

Use `execute_query` to list available lakehouses in workspace:
```m
let
  Source = Lakehouse.Contents(null),
  Navigation = Source{[workspaceId = "your-workspace-id"]}[Data]
in
  Navigation
```

Returns: `lakehouseId`, `lakehouseName`, `databaseId` for each lakehouse.

### Connection ID Format

When using `add_connection_to_dataflow`:
- Use the **DatasourceId GUID** from the connection, NOT the composite format
- Find via `list_connections` or extract from existing dataflow's queryMetadata
- Composite format (`{"ClusterId":"...","DatasourceId":"..."}`) appears in definitions but tool requires plain GUID

### Example: Complete Programmatic Setup (New Table)

```python
# 1. Create dataflow
create_dataflow(displayName="My Dataflow", workspaceId="...")

# 2. Add connections (use DatasourceId GUID, one per call)
add_connection_to_dataflow(connectionIds="97b68bdf-...", dataflowId="...", workspaceId="...")
add_connection_to_dataflow(connectionIds="58699886-...", dataflowId="...", workspaceId="...")  # if multi-source

# 3. Save M document with destination (new table)
#    Include [AllowCombine = true] if mixing source types
save_dataflow_definition(
  dataflowId="...",
  workspaceId="...",
  mDocument="""
[AllowCombine = true]
section Section1;
[DataDestinations = {[Definition = [Kind = "Reference", QueryName = "Results_DataDestination", IsNewTarget = true], Settings = [Kind = "Automatic", TypeSettings = [Kind = "Table"]]]}]
shared Results = let
  Source = Lakehouse.Contents(null),
  // ... query logic
in
  FinalTable;
shared Results_DataDestination = let
  Pattern = Lakehouse.Contents([HierarchicalNavigation = null, CreateNavigationProperties = false, EnableFolding = false]),
  Navigation_1 = Pattern{[workspaceId = "..."]}[Data],
  Navigation_2 = Navigation_1{[lakehouseId = "target-lakehouse-id"]}[Data],
  TableNavigation = Navigation_2{[Id = "Results", ItemKind = "Table"]}?[Data]?
in
  TableNavigation;
"""
)

# 4. Re-add connections (save_dataflow_definition may have wiped them)
add_connection_to_dataflow(connectionIds="97b68bdf-...", dataflowId="...", workspaceId="...")
add_connection_to_dataflow(connectionIds="58699886-...", dataflowId="...", workspaceId="...")

# 5. Verify
get_dataflow_definition(dataflowId="...", workspaceId="...")

# 6. Refresh (MUST use ApplyChangesIfNeeded on first refresh of API-created dataflow)
refresh_dataflow_background(dataflowId="...", workspaceId="...", executeOption="ApplyChangesIfNeeded")
```

### Why `ApplyChangesIfNeeded` Is Required (Technical Detail)

API-created dataflows start in an unpublished draft state. Additionally, `save_dataflow_definition` sets `loadEnabled: false` in queryMetadata — this controls whether data is staged during transformations, not destination configuration. The platform reconciles both issues on refresh ONLY when using `ApplyChangesIfNeeded`.

| Refresh Option | Behavior on MCP-created dataflow |
|---|---|
| `SkipApplyChanges` (default) | **Instant failure** — stale metadata, draft never published |
| `ApplyChangesIfNeeded` | Re-parses M annotations, publishes draft, reconciles metadata, then executes |

After the first successful refresh, subsequent refreshes can use either option.

---

## SharePoint Excel Source Pattern

To read an Excel file from SharePoint, use a Web connection (not SharePoint connector):

```m
Targets_WB = Excel.Workbook(Web.Contents("https://<tenant>.sharepoint.com/teams/<site>/Shared Documents/<file>.xlsx"), null, true),
Targets_Sheet = Targets_WB{[Item = "<SheetName>", Kind = "Sheet"]}[Data],
Targets_Headers = Table.PromoteHeaders(Targets_Sheet, [PromoteAllScalars = true]),
Targets_Typed = Table.TransformColumnTypes(Targets_Headers, {{"col1", type text}, {"col2", type number}})
```

Requires a Web connection to the SharePoint site URL. Find via `list_connections` filtering for `type: "Web"` with a SharePoint path.

---

## Common Pitfalls Quick Reference

| Symptom | Cause | Fix |
|---------|-------|-----|
| `DataflowNeverPublishedError` | Default `SkipApplyChanges` on first run | `executeOption = "ApplyChangesIfNeeded"` |
| `DestinationColumnNotFound` | Manual mappings for new table | Use `Kind = "Automatic"`, no `ColumnSettings` |
| Credentials error on Lakehouse | Connection not bound | `add_connection_to_dataflow` then validate |
| FastCopy fails with transforms | `Table.Group`, `NestedJoin`, etc. | Remove `[StagingDefinition]` |
| Instant refresh fail (0-3s) | Privacy firewall or unpublished | `[AllowCombine = true]` and/or `ApplyChangesIfNeeded` |
| `loadEnabled: false` in metadata | Controls data staging during transforms, not destinations | Not a problem with `DataDestinations` + `ApplyChangesIfNeeded` |
| Multi-source instant fail via API | Dirty dataflow (converted from single-source) or separate Lakehouse.Contents calls | Create fresh dataflow; consolidate Lakehouse reads into single query |
| Lakehouse-only fails after multi-source revert | `save_dataflow_definition` does NOT remove stale connections from queryMetadata | Create a new dataflow instead of reverting; no `remove_connection` tool exists |
| `IsNewTarget = false` fails on API-created dataflow | Direct `[Data]` navigation + `IsNewTarget = false` fails on first refresh | Always use `IsNewTarget = true` with `?[Data]?` null-safe operators, even for existing tables |

---

### New Table vs Existing Table Summary

| Scenario | `IsNewTarget` | `AllowCreation` | Navigation |
|----------|---------------|-----------------|------------|
| Table doesn't exist yet | `true` | `true` | `?[Data]?` (null-safe) |
| Table already exists | `false` | `false` | `[Data]` (direct) |
