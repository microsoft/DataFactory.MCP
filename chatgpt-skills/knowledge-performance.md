# Data Factory Performance Knowledge

## Handling Query Timeouts

When a query times out, the solution is **chunking** - processing data in smaller batches.

### Chunking Strategy

1. **Identify a partitioning column**: Dates work best, but IDs or regions also work
2. **Run queries for smaller ranges**: One month, one week, or batches of IDs
3. **Aggregate results**: Combine chunked results as needed

### Example: Date-Based Chunking

Instead of:
```m
// This might timeout on large datasets
let
    Source = Sql.Database("server", "database"),
    AllData = Source{[Schema="dbo", Item="LargeTable"]}[Data],
    Aggregated = Table.Group(AllData, {"Category"}, {{"Total", each List.Sum([Amount])}})
in
    Aggregated
```

Use chunked approach:
```m
// Process one month at a time
let
    Source = Sql.Database("server", "database"),
    Data = Source{[Schema="dbo", Item="LargeTable"]}[Data],
    FilteredMonth = Table.SelectRows(Data, each [Date] >= #date(2024, 1, 1) and [Date] < #date(2024, 2, 1)),
    Aggregated = Table.Group(FilteredMonth, {"Category"}, {{"Total", each List.Sum([Amount])}})
in
    Aggregated
```

## Filter Early

Apply filters as early as possible in your query pipeline.

### Why?
- **Query folding**: Filters pushed to the data source reduce data transfer
- **Memory efficiency**: Less data to process in subsequent steps
- **Faster execution**: Smaller datasets = faster operations

### Example
```m
let
    Source = Sql.Database("server", "database"),
    Table = Source{[Schema="dbo", Item="Sales"]}[Data],
    
    // ✅ Filter early - before any transformations
    Filtered = Table.SelectRows(Table, each [Year] = 2024 and [Region] = "West"),
    
    // Then transform the smaller dataset
    Grouped = Table.Group(Filtered, {"Product"}, {{"Total", each List.Sum([Amount])}}),
    Sorted = Table.Sort(Grouped, {{"Total", Order.Descending}})
in
    Sorted
```

## Expensive Operations Last

Some operations must read ALL data before returning results. Put these at the end.

### Expensive Operations (put last)
- `Table.Sort` - must read all rows to sort
- `Table.Distinct` - must scan all rows for duplicates
- `Table.Group` - must process all rows for aggregation
- `Table.Buffer` - loads entire table into memory

### Streaming Operations (can go first)
- `Table.SelectRows` - filters row by row
- `Table.SelectColumns` - processes column by column
- `Table.TransformColumns` - transforms row by row
- `Table.RenameColumns` - metadata change only

## Choose the Right Connector

| Instead of... | Use... | Why? |
|---------------|--------|------|
| Generic ODBC | Native SQL connector | Better query folding |
| Generic OLEDB | Native connector | Optimized protocols |
| Web.Contents for files | AzureStorage.BlobContents | Direct access |

### Query Folding Benefits
- Filters pushed to source database
- Aggregations computed on server
- Less data transferred over network
- Faster overall execution

## Work on Subsets During Development

When building queries, limit data to speed up iteration:

```m
let
    Source = Lakehouse.Contents(null),
    Table = ...,
    
    // Add during development, remove for production
    Subset = Table.FirstN(Table, 1000),
    
    // Your transformations
    Transformed = Table.TransformColumns(Subset, ...)
in
    Transformed
```

## Query Organization

### Modular Queries
Split large queries into smaller, referenced queries:

```m
// Query 1: Source
shared Source_Sales = let
    Source = Lakehouse.Contents(null),
    Data = ...
in
    Data;

// Query 2: Transform (references Source)
shared Transformed_Sales = let
    Source = Source_Sales,
    Filtered = Table.SelectRows(Source, ...),
    Grouped = Table.Group(...)
in
    Grouped;

// Query 3: Output (references Transform, has Data Destination)
shared Output_Sales = Transformed_Sales;
```

### Benefits
- Easier debugging (test each step)
- Reusability (share source queries)
- Clarity (see the data flow)

## Parameters for Reusability

Store configurable values as parameters:

```m
// Parameter definition
shared StartDate = #date(2024, 1, 1) meta [IsParameterQuery = true];
shared EndDate = #date(2024, 12, 31) meta [IsParameterQuery = true];

// Usage in queries
shared FilteredData = let
    Source = ...,
    Filtered = Table.SelectRows(Source, each [Date] >= StartDate and [Date] <= EndDate)
in
    Filtered;
```

## Handle Dynamic Data

### Remove Bottom Rows (for footers)
```m
// Better than row index filters
Table.RemoveLastN(table, 2)
```

### Select Specific Columns (survives new columns)
```m
// Won't break if source adds new columns
Table.SelectColumns(table, {"ID", "Name", "Amount"})
```

### Unpivot Selected Columns Only
```m
// Won't break if new columns added
Table.UnpivotOtherColumns(table, {"ID", "Category"}, "Attribute", "Value")
```

## Error Handling

### Remove Error Rows
```m
Table.RemoveRowsWithErrors(table, {"ColumnThatMightError"})
```

### Replace Errors with Defaults
```m
Table.ReplaceErrorValues(table, {{"Amount", 0}, {"Name", "Unknown"}})
```

## Performance Checklist

- [ ] Filters applied early in the query
- [ ] Using native connectors (not ODBC/OLEDB)
- [ ] Expensive operations (sort, group) at the end
- [ ] Large datasets chunked by date/ID
- [ ] Development subset removed before production
- [ ] Data types set explicitly
- [ ] Queries organized modularly
