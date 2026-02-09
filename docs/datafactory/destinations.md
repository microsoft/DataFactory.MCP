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
