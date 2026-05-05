---
name: dataflows-authoring-mcp
description: >
  Create, update, and manage Fabric Dataflows Gen2 using MCP tools with Power Query M mashup
  definitions. Uses data-factory-mcp server tools to author M section documents, bind connections,
  trigger refresh jobs with ApplyChangesIfNeeded, and run ad-hoc M queries returning Apache Arrow
  results. Covers destination configuration for Lakehouse/Warehouse targets with DataDestinations
  annotations, multi-source patterns with AllowCombine, connection re-binding after save, and
  rolling date windows. Use when the user wants to: (1) create a new Dataflow Gen2 via MCP,
  (2) update queries in an existing dataflow, (3) trigger and monitor refresh jobs, (4) bind
  connections to dataflows, (5) run ad-hoc M queries, (6) configure staging destinations.
  Triggers: "create dataflow mcp", "dataflow M query", "refresh dataflow", "bind connection
  dataflow", "execute M query", "dataflow gen2 mcp", "add query to dataflow", "dataflow arrow
  results", "fast copy dataflow", "action sequence", "dataflow destination".
---

> **Update Check — ONCE PER SESSION (mandatory)**
> The first time this skill is used, check for data-factory-mcp updates.

## Prerequisite Knowledge

Read these companion documents:
- [DATAFACTORY-MCP-CORE.md](../../common/DATAFACTORY-MCP-CORE.md) — Authentication, workspace discovery, MCP patterns
- [CONNECTIONS-CORE.md](../../common/CONNECTIONS-CORE.md) — Connection model, binding lifecycle, troubleshooting

This skill adds: **how to author Dataflow Gen2 items** using MCP tools.

## MCP Tools (this skill)

| Tool | Purpose |
|------|---------|
| `create_dataflow` | Create new empty dataflow in a workspace |
| `save_dataflow_definition` | Save complete M section document (queries + destinations) |
| `add_connection_to_dataflow` | Bind connections to dataflow (before AND after save) |
| `add_or_update_query_in_dataflow` | Add or update individual queries |
| `execute_query` | Run M queries ad-hoc, returns Apache Arrow results |
| `refresh_dataflow_background` | Trigger async refresh job |
| `refresh_dataflow_status` | Poll refresh job for completion |
| `get_dataflow_definition` | Read current M section document (read-modify-write) |
| `list_dataflows` | Discover dataflows in a workspace |

## Must / Prefer / Avoid

### MUST
- Use `executeOption = "ApplyChangesIfNeeded"` on first refresh of API-created dataflows
- Re-add connections via `add_connection_to_dataflow` after `save_dataflow_definition` (save may wipe them)
- Use `list_workspaces` and filter by name — there is no `find_workspace` tool
- Validate each connection with `execute_query` after binding
- Create a new dataflow for multi-source — never convert a published single-source dataflow

### PREFER
- Filter early in M queries to enable query folding (push filters to source)
- Expensive operations (sort, aggregate) last — streaming operations (filter, select) first
- `execute_query` with `customMashupDocument` to iterate on M code before saving
- Standard engine for complex transforms; Fast Copy only for simple ingestion
- Rolling date windows over hardcoded dates for scheduled dataflows

### AVOID
- `get_authoring_guidance` — deprecated, author M directly
- Sorting early in query chains (reads all data before returning)
- `SELECT *` without row limits during development — use "Keep First Rows" then remove
- `IsNewTarget = false` on API-created dataflows — always use `IsNewTarget = true` with `?[Data]?`

## End-to-End Workflow: New Dataflow with Lakehouse Destination

```text
1. list_workspaces              → Find workspace, extract workspaceId
2. create_dataflow              → Create empty dataflow
3. list_connections             → Find connection for your source
4. add_connection_to_dataflow   → Bind source connection(s)
5. execute_query                → Discover target lakehouse ID
6. save_dataflow_definition     → Save M document with source + DataDestinations
7. add_connection_to_dataflow   → Re-bind connections (save may wipe them)
8. get_dataflow_definition      → Verify connections + destination config
9. refresh_dataflow_background  → Materialize; MUST use ApplyChangesIfNeeded
10. refresh_dataflow_status     → Poll until Completed/Failed
```

## Destination Configuration

### New Table (Most Common Path)

Place `[DataDestinations]` annotation before the source query:

```m
[DataDestinations = {[
  Definition = [Kind = "Reference", QueryName = "MyQuery_DataDestination", IsNewTarget = true],
  Settings = [Kind = "Automatic", TypeSettings = [Kind = "Table"]]
]}]
shared MyQuery = let ... in Result;

shared MyQuery_DataDestination = let
  Pattern = Lakehouse.Contents([HierarchicalNavigation = null, CreateNavigationProperties = false, EnableFolding = false]),
  Nav1 = Pattern{[workspaceId = "..."]}[Data],
  Nav2 = Nav1{[lakehouseId = "..."]}[Data],
  TableNav = Nav2{[Id = "MyQuery", ItemKind = "Table"]}?[Data]?
in TableNav;
```

Key requirements:
- `IsNewTarget = true` always for API-created dataflows (even for existing tables)
- `?[Data]?` null-safe operators (table doesn't exist on first refresh)
- `Kind = "Automatic"` (Manual validates columns before table exists)
- `_DataDestination` suffix naming convention for hidden query

### Discovering Lakehouse IDs

```m
let
  Source = Lakehouse.Contents(null),
  Navigation = Source{[workspaceId = "your-workspace-id"]}[Data]
in Navigation
```

Returns `lakehouseId`, `lakehouseName`, `databaseId` for each lakehouse.

## M Language Basics

M (Power Query Formula Language) is functional, case-sensitive, lazy-evaluated.

```m
let
    Source = Lakehouse.Contents(null),
    Filtered = Table.SelectRows(Source, each [Status] = "Active"),
    Result = Table.SelectColumns(Filtered, {"ID", "Name"})
in Result
```

## Rolling Date Windows

Use for scheduled dataflows instead of hardcoded dates:

| Window | Start | End |
|--------|-------|-----|
| Last N months | `Date.StartOfMonth(Date.AddMonths(today, -N))` | `Date.EndOfMonth(Date.AddMonths(today, -1))` |
| Current quarter | `Date.StartOfQuarter(today)` | `today` |
| Year to date | `#date(Date.Year(today), 1, 1)` | `today` |

## What-If Query Pattern

Create derivative queries that reference a materialized output to model scenarios without duplicating source logic:

```m
shared WhatIf_Reclassify = let
  Source = MaterializedQuery,
  Reclassed = Table.AddColumn(Source, "new_tier",
    each if [store_name] = "Tacoma" then "C" else [tier], type text)
in Reclassed;
```

## Supplementary References

Load on demand based on the task:

| Reference | When |
|-----------|------|
| `references/performance-patterns.md` | Query timeouts, chunking, query folding strategies |
| `references/advanced-features.md` | Fast Copy, Action.Sequence, Modern Evaluator |
| `references/source-patterns.md` | Multi-source with AllowCombine, SharePoint Excel |
| `references/destination-patterns.md` | Destination troubleshooting, existing table patterns |
