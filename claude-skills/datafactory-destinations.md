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

### When to Use Action.Sequence Instead

Only for unusual side-effect requirements:
- Custom external writes (blob/ADLS files)
- Multi-step writes that must be sequenced
- Parallel file outputs

For standard Lakehouse → Lakehouse, the platform already provides Replace/Append via destination settings.

### Automatic vs Manual: Schema Stability Decision

| Schema Behavior | Recommended Setting |
|-----------------|---------------------|
| Schema changes frequently | Automatic (managed mapping, drop/recreate) |
| Schema is stable, downstream artifacts exist | Manual (explicit mapping, preserves relationships) |

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

### Validation Before Running
1. Validate source connection with `execute_query`
2. Validate destination lakehouse access with `execute_query`
3. After `add_connection_to_dataflow`, verify with `execute_query`

---

## Programmatic Destination Configuration via MCP Tools

Output destinations CAN be configured programmatically using `validate_and_save_m_document`. This requires constructing a complete M section document with all components.

### Required Components

#### 1. DataDestinations Annotation
Place immediately before the source query definition.

**For NEW tables (don't exist yet):**
```m
[DataDestinations = {[
  Definition = [Kind = "Reference", QueryName = "MyQuery_DataDestination", IsNewTarget = true], 
  Settings = [
    Kind = "Manual", 
    AllowCreation = true, 
    ColumnSettings = [Mappings = {
      [SourceColumnName = "Col1", DestinationColumnName = "Col1"],
      [SourceColumnName = "Col2", DestinationColumnName = "Col2"]
    }], 
    DynamicSchema = false, 
    UpdateMethod = [Kind = "Replace"], 
    TypeSettings = [Kind = "Table"]
  ]
]}]
shared MyQuery = let ... in ...;
```

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
Submit the full document via `validate_and_save_m_document`:
```m
section Section1;
[DataDestinations = {...}]
shared MyQuery = let ... in ...;
shared MyQuery_DataDestination = let ... in ...;
```

### MCP Tool Workflow

```
1. create_dataflow              → Create empty dataflow
2. add_connection_to_dataflow   → Attach Lakehouse connection (use DatasourceId GUID)
3. execute_query                → Discover target lakehouse ID
4. validate_and_save_m_document → Save complete M document with destination config
5. get_decoded_dataflow_definition → Verify configuration
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

# 2. Add connection (use DatasourceId GUID)
add_connection_to_dataflow(connectionIds="97b68bdf-...", dataflowId="...", workspaceId="...")

# 3. Save M document with destination (new table)
validate_and_save_m_document(
  dataflowId="...",
  workspaceId="...",
  mDocument="""
section Section1;
[DataDestinations = {[Definition = [Kind = "Reference", QueryName = "Results_DataDestination", IsNewTarget = true], Settings = [Kind = "Manual", AllowCreation = true, ColumnSettings = [Mappings = {[SourceColumnName = "Year", DestinationColumnName = "Year"], [SourceColumnName = "Count", DestinationColumnName = "Count"]}], DynamicSchema = false, UpdateMethod = [Kind = "Replace"], TypeSettings = [Kind = "Table"]]]}]
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
```

### New Table vs Existing Table Summary

| Scenario | `IsNewTarget` | `AllowCreation` | Navigation |
|----------|---------------|-----------------|------------|
| Table doesn't exist yet | `true` | `true` | `?[Data]?` (null-safe) |
| Table already exists | `false` | `false` | `[Data]` (direct) |
