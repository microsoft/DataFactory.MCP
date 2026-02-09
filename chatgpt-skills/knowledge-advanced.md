# Data Factory Advanced Knowledge

## Action.Sequence (Side Effects / Writes)

`Action.Sequence` is for **side-effecting operations** (writes, mutations) - NOT for normal query transformations.

### When to Use Action.Sequence

| Scenario | Use Action.Sequence? |
|----------|---------------------|
| Filters, joins, transforms | ❌ NO - use normal `let ... in` |
| Write files to blob/ADLS | ✅ YES |
| Parallel file outputs | ✅ YES |
| Calculated columns, type changes | ❌ NO |
| Database table writes | ❌ NO - use Data Destinations |

### Key Primitives

| Primitive | Purpose |
|-----------|---------|
| `Action.Sequence({...})` | Forces ordered execution of actions |
| `Action.DoNothing` | No-op (often ends a sequence) |
| `ValueAction.Replace(target, source)` | Write/replace to storage |
| `ListAction.ParallelExecute({...})` | Run action units concurrently |

### Pattern: Parallel File Writes

```m
// Function that writes a single file
shared writeFile = (index) => Action.Sequence({
    ValueAction.Replace(
        AzureStorage.BlobContents(
            "https://mystorageaccount.blob.core.windows.net/container/output" 
            & Text.From(index) & ".csv"
        ),
        Csv.FromValue(sourceData)
    ),
    Action.DoNothing
});

// Execute 4 writes in parallel
shared writeAllFiles = ListAction.ParallelExecute({
    () => writeFile(0),
    () => writeFile(1),
    () => writeFile(2),
    () => writeFile(3)
});
```

### Mental Model

- **Parallelism across units** → `ListAction.ParallelExecute`
- **Ordering within a unit** → `Action.Sequence`

### Best Practices

1. **Isolate side effects** - keep action logic in dedicated queries, main transforms stay pure
2. **Make actions idempotent** - retries happen; ensure repeating won't corrupt state
3. **Don't parallelize competing resources** - `Action.Sequence` orders inside one unit, not across parallel units
4. **Handle failures** - wrap risky actions with `try ... otherwise ...`

### Rule of Thumb

- Query returns a **table/value** → normal `let` pipeline
- Query performs **writes/side effects** with guaranteed ordering → `Action.Sequence`

---

## Fast Copy

Fast Copy uses the pipeline Copy Activity backend for faster ingestion.

### Enable Fast Copy

```m
[StagingDefinition = [Kind = "FastCopy"]]
section Section1;

shared MyQuery = let
    ...
in
    Result;
```

### Supported Transformations ONLY

Fast Copy **only** supports:
- ✅ Combine files
- ✅ Select columns
- ✅ Change data types
- ✅ Rename columns
- ✅ Remove columns

### NOT Supported by Fast Copy

- ❌ `Table.Group` (aggregations)
- ❌ Complex joins
- ❌ Custom transformations
- ❌ Filters with complex logic

**If your query uses unsupported transformations (like Group By), DO NOT use Fast Copy.**

### Splitting Queries for Performance

For large data with complex transformations:

```
Query 1 (Fast Copy)     → Ingest raw data (simple transforms only)
         ↓                 Enable staging
Query 2 (Standard)      → References Query 1
         ↓                 Apply complex transforms (Group By, etc.)
Output                  → Write to destination
```

This gives you:
- Fast Copy speed for ingestion
- SQL DW compute for transformations

### Fast Copy Limitations

| Limitation | Details |
|------------|---------|
| Destinations | Lakehouse only (direct) |
| Transforms | Simple only (select, rename, types) |
| Joins | Not supported |
| Aggregations | Not supported |

---

## Modern Evaluator (Preview)

The Modern Query Evaluation Engine is for **Dataflow Gen2 with CI/CD**. Runs on .NET Core 8.

### Fast Copy vs Modern Evaluator

| Feature | Fast Copy | Modern Evaluator |
|---------|-----------|------------------|
| Purpose | Faster **ingestion** | Faster **query evaluation** |
| Limitation | Limited transformations | Limited connectors |
| Complex transforms | ❌ Not supported | ✅ Supported |
| Enable via | `[StagingDefinition = [Kind = "FastCopy"]]` | UI: Options > Scale tab |

### When to Use Each

| Scenario | Recommendation |
|----------|----------------|
| Large data, simple transforms | **Fast Copy** |
| Complex transforms (Group By, joins) | **Modern Evaluator** |
| Complex transforms + unsupported connectors | **Standard engine** |

### Supported Connectors (Modern Evaluator)

- ✅ Azure Blob Storage / ADLS Gen2
- ✅ Fabric Lakehouse
- ✅ Fabric Warehouse
- ✅ OData
- ✅ Power Platform Dataflows
- ✅ SharePoint Online List / Folder
- ✅ Web

### Enabling Modern Evaluator

**Via Fabric UI:**
1. Open dataflow
2. Go to Options → Scale tab
3. Enable "Modern query evaluation engine (Preview)"

**In queryMetadata.json:**
```json
{
    "computeEngineSettings": {
        "allowModernEvaluationEngine": true
    }
}
```

---

## Decision Tree: Which Engine?

```
Start
  │
  ├─ Simple transforms only (select, rename, types)?
  │    │
  │    ├─ YES → Use Fast Copy
  │    │
  │    └─ NO → Continue
  │
  ├─ Using supported connector?
  │    │
  │    ├─ YES → Use Modern Evaluator
  │    │
  │    └─ NO → Use Standard Engine
  │
  └─ Need side-effect writes (files)?
       │
       └─ YES → Use Action.Sequence
```

---

## Common Patterns

### Pattern 1: Fast Copy + Complex Transform

```m
[StagingDefinition = [Kind = "FastCopy"]]
section Section1;

// Query 1: Fast Copy ingestion
shared RawData = let
    Source = AzureStorage.BlobContents("https://..."),
    Csv = Csv.Document(Source),
    Typed = Table.TransformColumnTypes(Csv, {{"Amount", type number}})
in
    Typed;

// Query 2: Complex transform (references RawData)
shared Aggregated = let
    Source = RawData,
    Grouped = Table.Group(Source, {"Category"}, {{"Total", each List.Sum([Amount])}})
in
    Grouped;
```

### Pattern 2: Parallel File Export

```m
section Section1;

shared sourceData = let
    Source = Lakehouse.Contents(null),
    // ... get data
in
    Data;

shared exportFile = (partition as number) => Action.Sequence({
    ValueAction.Replace(
        AzureStorage.BlobContents("https://storage.blob.core.windows.net/exports/part" & Text.From(partition) & ".csv"),
        Csv.FromValue(Table.SelectRows(sourceData, each [PartitionKey] = partition))
    ),
    Action.DoNothing
});

shared exportAll = ListAction.ParallelExecute({
    () => exportFile(1),
    () => exportFile(2),
    () => exportFile(3),
    () => exportFile(4)
});
```

### Pattern 3: Conditional Processing

```m
section Section1;

shared processData = let
    Source = ...,
    RowCount = Table.RowCount(Source),
    
    // Use different strategies based on data size
    Result = if RowCount > 1000000 then
        // Large dataset: chunk processing
        ProcessInChunks(Source)
    else
        // Small dataset: process all at once
        ProcessAll(Source)
in
    Result;
```

---

## Troubleshooting

| Issue | Cause | Solution |
|-------|-------|----------|
| Fast Copy fails with Group By | Unsupported transform | Remove `[StagingDefinition]` annotation |
| Modern Evaluator not working | Unsupported connector | Switch to standard engine |
| Action.Sequence order wrong | Parallel execution | Ensure actions are in same `Action.Sequence` |
| Writes not completing | Missing `Action.DoNothing` | Add at end of sequence |
