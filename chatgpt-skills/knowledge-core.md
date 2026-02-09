# Data Factory Core Knowledge

## What is M (Power Query Formula Language)

M is a functional, case-sensitive, lazy-evaluated language for data transformation. It's used in:
- Power BI Desktop
- Excel Power Query
- Microsoft Fabric Dataflow Gen2

### Key Characteristics
- **Functional**: Operations are expressions that return values
- **Case-sensitive**: `Table` and `table` are different
- **Lazy-evaluated**: Operations only execute when results are needed
- **Immutable**: Data isn't modified; new values are created

## Basic M Pattern

```m
let
    // Step 1: Connect to data source
    Source = Lakehouse.Contents(null),
    
    // Step 2: Navigate to table
    MyTable = Source{[workspaceId="..."]}[Data]{[lakehouseId="..."]}[Data]{[Id="tablename", ItemKind="Table"]}[Data],
    
    // Step 3: Transform
    Filtered = Table.SelectRows(MyTable, each [Status] = "Active"),
    
    // Step 4: Select columns
    Result = Table.SelectColumns(Filtered, {"ID", "Name", "Status"})
in
    Result
```

## What is Dataflow Gen2

Dataflow Gen2 is Microsoft Fabric's cloud ETL tool:
- Runs M queries in the cloud
- Writes results to Lakehouse, Warehouse, or other destinations
- You define the **data transformations**
- The platform handles **orchestration and execution**

### Key Concepts
- **Queries**: M code that transforms data
- **Data Destinations**: Where transformed data is written
- **Refresh**: Executing the dataflow to process data
- **Staging**: Intermediate storage for complex transforms

## Common M Functions

### Filtering
```m
Table.SelectRows(table, each [Column] = "Value")
Table.SelectRows(table, each [Amount] > 100)
Table.SelectRows(table, each [Date] >= #date(2024, 1, 1))
```

### Column Selection
```m
Table.SelectColumns(table, {"Col1", "Col2", "Col3"})
Table.RemoveColumns(table, {"UnwantedCol"})
Table.RenameColumns(table, {{"OldName", "NewName"}})
```

### Aggregation
```m
Table.Group(table, {"GroupByColumn"}, {
    {"Sum", each List.Sum([Amount]), type number},
    {"Count", each Table.RowCount(_), Int64.Type},
    {"Average", each List.Average([Value]), type number}
})
```

### Joins
```m
Table.NestedJoin(table1, "Key1", table2, "Key2", "Joined", JoinKind.LeftOuter)
Table.ExpandTableColumn(previousStep, "Joined", {"Col1", "Col2"})
```

### Type Conversion
```m
Table.TransformColumnTypes(table, {
    {"TextCol", type text},
    {"NumCol", type number},
    {"DateCol", type date}
})
```

## Dataflow Gen2 Architecture

### Three-Layer Pattern
```
Source Query       → Read from source (Lakehouse, SQL, etc.)
         ↓
Transform Query    → Apply M transformations
         ↓
Output Query       → Same as transform, with Data Destination attached
```

### Why This Pattern?
- Separates concerns: source, transform, output
- Enables reuse of transform queries
- Platform handles write orchestration
- You focus on the data logic, not the plumbing

## Common Connectors

| Connector | Use Case |
|-----------|----------|
| `Lakehouse.Contents` | Microsoft Fabric Lakehouse |
| `Sql.Database` | Azure SQL, SQL Server |
| `AzureStorage.BlobContents` | Azure Blob Storage |
| `Web.Contents` | REST APIs, web data |
| `Csv.Document` | CSV files |
| `Json.Document` | JSON data |

## Error Handling

```m
// Try-otherwise pattern
let
    Result = try riskyOperation() otherwise defaultValue
in
    Result

// Check for errors
let
    MaybeError = try riskyOperation(),
    Result = if MaybeError[HasError] then "Error occurred" else MaybeError[Value]
in
    Result
```

## Best Practices Summary

1. **Filter early** - reduce data volume as soon as possible
2. **Use native connectors** - better query folding and performance
3. **Set data types explicitly** - especially for CSV/text sources
4. **Name steps clearly** - future you will thank present you
5. **Use parameters** - for reusable values like server names
6. **Test with subsets** - use "Keep First Rows" during development
