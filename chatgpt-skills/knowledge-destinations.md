# Data Factory Destinations Knowledge

## Dataflow Gen2 Architecture (Lakehouse → Lakehouse)

For transforming data from source to destination Lakehouse:

1. **Use pure M for transformations** - define the data result
2. **Let Data Destination settings handle the write** - platform orchestrates

### Why Pure M (Not Action-Based)?

- Dataflow Gen2 generates an orchestration plan on refresh
- Your job: define the **data result** (the table)
- Platform's job: handle **load/write orchestration**
- Replace/Append are **destination concerns**, not M code concerns

## Three-Layer Structure

```
Source Query       → Read from source Lakehouse
         ↓
Transform Query    → Pure M transforms (Group By, joins, filters)
         ↓
Output Query       → Same as transform, with Data Destination attached
```

**Attach the Data Destination to your final aggregated query.**

## Automatic vs Manual Schema Settings

| Schema Behavior | Recommended Setting | Notes |
|-----------------|---------------------|-------|
| Schema changes frequently | **Automatic** | Managed mapping, drop/recreate table |
| Schema is stable | **Manual** | Explicit mapping, preserves relationships |
| New tables | **Automatic** or Manual with `AllowCreation = true` | |
| Existing tables with dependencies | **Manual** | Preserves downstream artifacts |

## Data Destination Components

Every data destination requires two M components:

### 1. Navigation Query (suffix `_DataDestination`)

Points to the target location with folding disabled:

```m
shared MyQuery_DataDestination = let
    Pattern = Lakehouse.Contents([CreateNavigationProperties = false, EnableFolding = false]),
    Navigation_1 = Pattern{[workspaceId = "your-workspace-id"]}[Data],
    Navigation_2 = Navigation_1{[lakehouseId = "your-lakehouse-id"]}[Data],
    TableNavigation = Navigation_2{[Id = "target_table", ItemKind = "Table"]}?[Data]?
in
    TableNavigation;
```

### 2. DataDestinations Attribute (on source query)

```m
[DataDestinations = {[
    Definition = [Kind = "Reference", QueryName = "MyQuery_DataDestination", IsNewTarget = true], 
    Settings = [Kind = "Automatic", TypeSettings = [Kind = "Table"]]
]}]
shared MyQuery = let
    // your transformation logic
in
    Result;
```

## New Table vs Existing Table

| Setting | `IsNewTarget = true` | `IsNewTarget = false` |
|---------|---------------------|----------------------|
| Table creation | Created on first refresh | Must exist already |
| If table deleted | Recreated on next refresh | Refresh fails |
| Navigation query | Use `?` null-safe operators | Direct navigation |

### Navigation for NEW tables (use `?` operators):
```m
TableNavigation = Navigation_2{[Id = "MyTable", ItemKind = "Table"]}?[Data]?
```

### Navigation for EXISTING tables (direct):
```m
TableNavigation = Navigation_2{[Id = "MyTable", ItemKind = "Table"]}[Data]
```

**Critical**: Using direct navigation `[Data]` for a non-existent table causes: "The key didn't match any rows in the table"

## Update Methods

| Method | Behavior |
|--------|----------|
| **Replace** | Data dropped and replaced each refresh |
| **Append** | Output appended to existing data |

```m
// Replace
Settings = [Kind = "Manual", UpdateMethod = [Kind = "Replace"], ...]

// Append
Settings = [Kind = "Manual", UpdateMethod = [Kind = "Append"], ...]
```

## Staging Requirements

| Destination | Staging |
|-------------|---------|
| **Warehouse** | REQUIRED - enable on query |
| **Lakehouse** | Disabled by default (for performance) |

## Programmatic Destination Configuration

Output destinations CAN be configured programmatically using `validate_and_save_m_document`.

### Complete M Section Document Example

```m
section Section1;

// Source query with DataDestinations attribute
[DataDestinations = {[
    Definition = [Kind = "Reference", QueryName = "SalesAggregated_DataDestination", IsNewTarget = true], 
    Settings = [
        Kind = "Manual", 
        AllowCreation = true, 
        ColumnSettings = [Mappings = {
            [SourceColumnName = "Category", DestinationColumnName = "Category"],
            [SourceColumnName = "TotalAmount", DestinationColumnName = "TotalAmount"],
            [SourceColumnName = "RecordCount", DestinationColumnName = "RecordCount"]
        }], 
        DynamicSchema = false, 
        UpdateMethod = [Kind = "Replace"], 
        TypeSettings = [Kind = "Table"]
    ]
]}]
shared SalesAggregated = let
    Source = Lakehouse.Contents(null),
    SourceLH = Source{[workspaceId = "source-workspace-id"]}[Data],
    SalesTable = SourceLH{[lakehouseId = "source-lakehouse-id"]}[Data]{[Id = "Sales", ItemKind = "Table"]}[Data],
    Grouped = Table.Group(SalesTable, {"Category"}, {
        {"TotalAmount", each List.Sum([Amount]), type number},
        {"RecordCount", each Table.RowCount(_), Int64.Type}
    })
in
    Grouped;

// Hidden destination query
shared SalesAggregated_DataDestination = let
    Pattern = Lakehouse.Contents([HierarchicalNavigation = null, CreateNavigationProperties = false, EnableFolding = false]),
    Navigation_1 = Pattern{[workspaceId = "target-workspace-id"]}[Data],
    Navigation_2 = Navigation_1{[lakehouseId = "target-lakehouse-id"]}[Data],
    TableNavigation = Navigation_2{[Id = "SalesAggregated", ItemKind = "Table"]}?[Data]?
in
    TableNavigation;
```

## Key Details

| Element | Requirement |
|---------|-------------|
| `_DataDestination` suffix | Required naming convention |
| `EnableFolding = false` | Required in destination query |
| Column mappings | Must match source query output exactly |
| Target table ID | Use source query name (if AllowCreation = true) |

## Discovering Lakehouse IDs

To find workspace and lakehouse IDs, query the Lakehouse contents:

```m
let
    Source = Lakehouse.Contents(null)
in
    Source
```

This returns a table with `workspaceId` for each lakehouse. Navigate further to get `lakehouseId`.

## Workflow for Programmatic Setup

1. **Create empty dataflow**: `create_dataflow`
2. **Attach connection**: `add_connection_to_dataflow` (use DatasourceId GUID)
3. **Discover target IDs**: `execute_query` to get workspace/lakehouse IDs
4. **Save M document**: `validate_and_save_m_document` with complete section document
5. **Verify**: `get_decoded_dataflow_definition` to confirm configuration

## Common Mistakes

| Mistake | Fix |
|---------|-----|
| Using `[Data]` for new tables | Use `?[Data]?` with null-safe operators |
| Missing `EnableFolding = false` | Add to Lakehouse.Contents options |
| Column mapping mismatch | Ensure mappings match source query output |
| Forgetting hidden query | Always create `_DataDestination` query |
| Wrong IsNewTarget setting | `true` for new tables, `false` for existing |
