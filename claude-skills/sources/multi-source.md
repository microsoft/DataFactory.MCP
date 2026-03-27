# Multi-Source Dataflows: AllowCombine

When a dataflow combines multiple source types (e.g., Lakehouse + SharePoint, Lakehouse + Web), the mashup engine's privacy firewall blocks cross-source queries by default. This causes instant "Validation failure" on refresh with no detailed error.

## Fix: Section-Level AllowCombine

```m
[AllowCombine = true]
section Section1;

shared LakehouseQuery = let ... in ...;
shared SharePointQuery = let ... in ...;
shared JoinedOutput = let ... in ...;
```

## Symptom Table

| Symptom | Cause | Fix |
|---------|-------|-----|
| Instant refresh failure (0-3 seconds) | Privacy firewall blocking cross-source | Add `[AllowCombine = true]` before `section` |
| "Validation failure" with no details | Same — pre-execution metadata check | Same |
| Works in UI but fails via MCP | UI auto-prompts for privacy; API does not | Must set explicitly in M document |

**Critical:** Required for any dataflow that mixes source types. Single-source dataflows (Lakehouse-only) do not need it.

## Multi-Source via API: Requirements

Multi-source dataflows **work via API** with `[AllowCombine = true]`, but require specific conditions:

1. **Fresh dataflow** — Always create a new dataflow for multi-source. Never transition a previously-published single-source dataflow to multi-source; stale connection metadata persists and causes instant refresh failure.
2. **Consolidated Lakehouse reads** — Read all Lakehouse tables in a single source query (one `Lakehouse.Contents` call), not separate queries per table. Reduces the number of distinct data source contexts the privacy firewall must reconcile.
3. **All connections bound** — Add all connections (Lakehouse, Web/SharePoint) via `add_connection_to_dataflow` before and after `save_dataflow_definition`.
4. **`ApplyChangesIfNeeded`** — Required on first refresh of any API-created dataflow.

## Pattern That Works

```
StoreActuals     → Single query: Lakehouse.Contents → reads orders, items, stores → aggregates
StoreTargets     → Excel.Workbook(Web.Contents("...sharepoint.com/.../file.xlsx"))
StoreAnnotations → Excel.Workbook(Web.Contents("...sharepoint.com/.../file.xlsx"))
OutputQuery      → Joins StoreActuals + StoreTargets + StoreAnnotations, with DataDestination
```

## What Causes Failure

| Scenario | Result |
|----------|--------|
| Fresh dataflow + consolidated reads + AllowCombine | Works |
| Previously-published single-source converted to multi-source | Instant failure — stale metadata |
| Separate `Lakehouse.Contents` calls per source query | May fail — multiple data source contexts |
