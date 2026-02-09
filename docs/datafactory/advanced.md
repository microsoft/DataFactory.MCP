# Advanced: Action.Sequence, Fast Copy, Modern Evaluator

## Action.Sequence (Side Effects / Writes)

`Action.Sequence` is for **side-effecting operations** (writes, mutations) - NOT for normal query transformations.

### When to Use

| Scenario | Use Action.Sequence? |
|----------|---------------------|
| Filters, joins, transforms | NO - use normal `let ... in` |
| Write files to blob/ADLS | YES |
| Parallel file outputs | YES |
| Calculated columns, type changes | NO |

### Key Primitives

- `Action.Sequence({...})` - forces ordered execution of actions
- `Action.DoNothing` - no-op (often ends a sequence)
- `ValueAction.Replace(target, source)` - write/replace to storage
- `ListAction.ParallelExecute({...})` - run action units concurrently

### Pattern: Parallel File Writes

```m
// Function that writes a single file
shared go = (i) => Action.Sequence({
  ValueAction.Replace(
    AzureStorage.BlobContents("https://storage.blob.core.windows.net/container/output" & Text.From(i) & ".csv"),
    sourceData
  ),
  Action.DoNothing
});

// Execute 4 writes in parallel
shared writeAll = ListAction.ParallelExecute({
  () => go(0),
  () => go(1),
  () => go(2),
  () => go(3)
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

Fast copy uses pipeline Copy Activity backend for faster ingestion. Enable with:
```m
[StagingDefinition = [Kind = "FastCopy"]]
section Section1;
```

### Supported Transformations Only

Fast copy **only** supports:
- Combine files
- Select columns
- Change data types
- Rename/remove columns

### NOT Supported by Fast Copy

- `Table.Group` (aggregations)
- Complex joins
- Custom transformations

**If your query uses unsupported transformations (like Group By), omit the StagingDefinition annotation entirely.**

### Splitting Queries for Performance

For large data with complex transformations:
1. Create first query with fast copy (ingestion only, supported transforms)
2. Enable staging on first query
3. Create second query that references first query
4. Apply complex transformations (Group By, etc.) in second query

This gives you fast copy speed for ingestion + SQL DW compute for transformations.

### Output Destination Limitation

Fast copy only supports Lakehouse destinations directly. For other destinations, stage first then reference.

---

## Modern Evaluator (Preview)

The Modern Query Evaluation Engine is for **Dataflow Gen2 with CI/CD** specifically. Runs on .NET Core 8.

### Fast Copy vs Modern Evaluator

| Feature | Fast Copy | Modern Evaluator |
|---------|-----------|------------------|
| Purpose | Faster **ingestion** | Faster **query evaluation** |
| Limitation | Limited transformations | Limited connectors |
| Complex transforms | Not supported | Supported |
| Enable via | `[StagingDefinition = [Kind = "FastCopy"]]` | UI: Options > Scale tab |

### When to Use Each
- **Fast Copy**: Large data, simple transforms (select/rename columns only)
- **Modern Evaluator**: Complex transforms (Group By, joins), supported connectors
- **Neither**: Complex transforms with unsupported connectors - use standard engine

### Supported Connectors (Modern Evaluator)
- Azure Blob Storage / ADLS Gen2
- Fabric Lakehouse / Warehouse
- OData
- Power Platform Dataflows
- SharePoint Online List / Folder
- Web

### Enabling
Via Fabric UI: Options > Scale tab > "Modern query evaluation engine (Preview)"

In queryMetadata.json (observed in working dataflows):
```json
"computeEngineSettings": {
  "allowModernEvaluationEngine": true
}
```

Note: MCP `validate_and_save_m_document` updates M code but may not set computeEngineSettings. Enable via UI if needed.
