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
