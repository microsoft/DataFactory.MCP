# Data Visuals (Preview)

Dataflow Gen2 can render a query as visuals instead of a table. The artifact is a
**Visual** (visualization document). Dashboard, report, and chart are request
synonyms only.

## Contract

A Visual is one flat M table. These exact column names trigger rendering:

```m
type table [
    Name = nullable text,
    Parent = nullable text,
    PartType = nullable text,
    Properties = nullable record,
    Data = any
]
```

Missing or renamed columns fall back to a normal table preview. Extra columns are
ignored. A non-record `Properties` cell can fail the entire preview with a
record-conversion error. An invalid field inside a valid record can instead
produce an inline error scoped to one visual. `VisualDocumentType` is just a
convenient variable name, not a required identifier.

## Closed PartType set

`Container`, `Card`, `Header`, `KpiCard`, `Table`, `Chart`.

All charts use `PartType = "Chart"`; earlier chart-specific PartTypes are
unsupported. `ChartType` selects the renderer, and `DataSeries` maps source
columns. All inputs below belong in `Properties` except the row's `Data` column.
Use `Data = null` for layout parts, headers, and KPI cards.

| PartType | Children | Required inputs | Optional |
|----------|----------|---------------------|----------|
| `Container` | One or more | none | `Direction`: `"row"` (default) or `"column"` |
| `Card` | Exactly one | `Title` (text) | none |
| `Header` | None | `Header` (text) | `FarText` (nullable text) |
| `KpiCard` | None | `Value`, `Label` (text) | `Sub` (nullable text) |
| `Table` | None | table in `Data` | none |
| `Chart` | None | `ChartType` (text), `DataSeries` (record), table in `Data` | `ChartTitle` (text) |

## Chart contract

The closed `ChartType` set is `Line`, `Area`, `Bar`, `StackedBar`, `Doughnut`,
and `Pie`. A doughnut chart uses `"Doughnut"`, not `"Donut"`.

`DataSeries` holds **exact column names**, not data:

| Field | Accepted values |
| --- | --- |
| `AxisColumns` | Text or a one-item text list naming a text, date, or numeric source column |
| `ValueColumns` | Text or a nonempty text list naming numeric source columns; exactly one except for `StackedBar`, which accepts one or more |
| `PrimaryAxisColumn` | Optional text; accepted for compatibility but ignored with the preview's single axis |

Empty lists, non-text entries, and multiple axis columns are rejected. A text
column name and its one-item text list are equivalent for every chart type.
Recheck mappings after renames, aggregation, or removal: a missing axis column can
produce an `undefined` category, while a missing value column can leave an empty
chart without a visible error. The missing name can still appear as an axis title.

Prefer nesting a chart in a `Card` for its title, but a chart can be a root or
container child with its own `ChartTitle`. Workbook placement properties
(`Bounds`, `TableStyle`, `ShowGridlines`, `AutoPositionColumnOffset`,
`AutoPositionRowOffset`) are accepted but ignored.

`StackedBar` requires wide-form data: one row per category, one numeric column
per series. Aggregate long-form inputs by category and series, then use
`Table.Pivot` to turn series values into columns. Set `ValueColumns` to the
resulting column names and update those mappings when adapting the source.

## Rules

- Exactly one row has `Parent = null`; any PartType can be the root.
- Names are unique and non-null; duplicates can repeat visuals, such as the same
  chart appearing inside two cards with the same name.
- Every other `Parent` matches an existing `Name`; unresolved parents render nothing.
- Relationships are acyclic.
- `Container` needs at least one child; `Card` needs exactly one. Everything else is a leaf.
- Format KPI numbers as text: `"$" & Number.ToText(Number.Round(x, 0))`.

## Minimal example

```m
let
    SalesData = #table(
        type table [Month = text, Revenue = number],
        {{"2026-01", 12000}, {"2026-02", 15500}}
    ),
    VisualDocumentType = type table [
        Name = nullable text, Parent = nullable text, PartType = nullable text,
        Properties = nullable record, Data = any
    ]
in
    #table(VisualDocumentType, {
        {"sales-card", null, "Card", [Title = "Monthly sales"], null},
        {"sales-trend", "sales-card", "Chart",
            [ChartType = "Line", DataSeries = [AxisColumns = "Month", ValueColumns = "Revenue"]], SalesData}
    })
```

## Triage

| Symptom | Cause |
|---------|-------|
| Renders as a plain table | Required column missing or renamed |
| `Visual not recognized: "<value>"` | PartType outside the closed set |
| One `undefined` category | `DataSeries.AxisColumns` names a column missing from `Data` |
| Empty chart without a visible error | `DataSeries.ValueColumns` can name a missing column |
| `must contain exactly one root row` | Zero or multiple `Parent = null` rows |
| Missing required visual property `"cells"` | Empty `Container` or `Card` |
| `Unexpected number of cells` | `Card` has more than one child |
| Record-conversion error prevents the preview | Non-record `Properties` cell |
| Inline `Unexpected result type` | Wrong field type inside a valid record, such as numeric `KpiCard.Value` |

## Source and profiling guidance

Create a separate Visual query that references the source table query. Adapt
source references, calculations, types, category values, and chart mappings,
not just column names. Select columns, filter invalid values, aggregate to chart
grain, and sort explicitly. A detail table can select and sort source rows without
aggregating them.

For profiling, `Table.Profile` supplies column statistics and `Table.Schema`
supplies column types. Numeric bins, quartiles, and IQR outlier counts require
additional M calculations; they are not default `Table.Profile` output. Reusable
profiling must handle empty tables, nulls, and columns with no numeric data.
Inspect query definitions or metadata first; do not use `Expression.Evaluate` or
dynamically evaluate sibling queries for discovery. Evaluate profiling data only
as part of the requested profiling work.

## Authoring and persistence

For the Advanced Editor, return a complete single-query expression like the
minimal example. For MCP execution, use `execute_query` with
`customMashupDocument` nonpersistently when available. For saving, first inspect
`get_dataflow_definition` and preserve all queries and dependencies in a complete
M section document (`section Section1;` and `shared` query declarations).
Ask before persistent changes and call `save_dataflow_definition` with
`validateOnly = true` before saving; a save replaces the complete mashup. Restore
required bindings afterward with `add_connection_to_dataflow` and validate them.

As a lightweight starting recommendation, use at most three data visuals, ten
chart categories, and 50 detail rows. These are not platform limits; add complexity
after rendering succeeds.

## Limitations

- Preview; subject to change.
- Evaluation-time snapshots; no filters, slicers, date pickers, or cross-filtering.
- Renders in the authoring canvas only; never part of refresh output, destinations,
  or the Dataflow Gen2 connector.
- Many visuals or large `Data` tables slow authoring.
- `Line` and `Area` don't fill missing dates; equally spaced line points need not
  represent equal elapsed time.
- `StackedBar` segments and numeric labels use alphabetical series order. The
  legend has no heading; the numeric axis has ticks but no title. Bar tooltips use
  localized Category, Series, and Value labels, but numeric-label tooltips can show
  internal field names. Avoid backslashes in the category column name because
  stacks group incorrectly.
- Structural validation does not prove successful Fabric rendering.

Full reference: [Create data visuals in Dataflow Gen2](https://learn.microsoft.com/fabric/data-factory/dataflow-gen2-data-visuals).
