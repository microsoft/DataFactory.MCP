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

---

# SharePoint Excel Source Pattern

Reading Excel files from SharePoint in Dataflow Gen2 uses a **Web connection** (not the SharePoint connector).

## M Pattern

```m
shared ExcelSource = let
    WB = Excel.Workbook(
        Web.Contents("https://<tenant>.sharepoint.com/teams/<site>/Shared Documents/<file>.xlsx"),
        null,
        true
    ),
    Sheet = WB{[Item = "<SheetName>", Kind = "Sheet"]}[Data],
    Headers = Table.PromoteHeaders(Sheet, [PromoteAllScalars = true]),
    Typed = Table.TransformColumnTypes(Headers, {
        {"col1", type text},
        {"col2", type number}
    })
in
    Typed;
```

## Connection Requirements

- Requires a **Web** connection to the SharePoint site URL
- Find via `list_connections` filtering for `connectionType: "Web"` with a URL containing `.sharepoint.com`
- If no matching connection exists, create one with `create_connection` or `create_connection_ui`

## MCP Workflow

```
1. list_connections              → Find Web connection with .sharepoint.com URL
   (if not found)                → create_connection_ui or create_connection
2. create_dataflow               → Create empty dataflow
3. add_connection_to_dataflow    → Bind the Web connection (+ Lakehouse if writing output)
4. save_dataflow_definition      → Save M document with SharePoint source + DataDestination
5. add_connection_to_dataflow    → Re-add connections (save may wipe them)
6. execute_query                 → Validate source reads successfully
7. refresh_dataflow_background   → Materialize; use ApplyChangesIfNeeded on first refresh
```

## Multi-Source Note

When combining SharePoint data with Lakehouse data, you must add `[AllowCombine = true]` to the section. See `sources/multi-source.md` for the full pattern.
