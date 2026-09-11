# Dataflow Gen2 Data Visuals

Use this knowledge for Dataflow Gen2 visual, visualization, dashboard, report,
chart, KPI, and visual-summary requests. **Visual** and **visualization document**
are canonical; the other terms are request synonyms.

Data visuals are in Preview. Return one flat Power Query M table whose exact
columns are `Name` (`nullable text`), `Parent` (`nullable text`), `PartType`
(`nullable text`), `Properties` (`nullable record`), and `Data` (`any`).

The closed PartType set is `Container`, `Card`, `Header`, `KpiCard`, `Table`,
and `Chart`. All charts use `PartType = "Chart"` with `ChartType` and
`DataSeries` in `Properties`. Earlier chart-specific PartTypes are unsupported.
Never invent another value or use a nested record-tree contract.

## Contract

| PartType | Children | Required values |
| --- | --- | --- |
| `Container` | One or more | Optional `Direction` is `"row"` (default) or `"column"` |
| `Card` | Exactly one | Text `Title` |
| `Header` | None | Text `Header`; optional nullable text `FarText` |
| `KpiCard` | None | Text `Value`, text `Label`; optional nullable text `Sub` |
| `Table` | None | Table in `Data` |
| `Chart` | None | Text `ChartType`, record `DataSeries`, table in `Data`; optional text `ChartTitle` |

Inputs belong in the row's `Properties` record except the table reference in its
`Data` column. Use `Data = null` for layout parts, headers, and KPI cards.

Exactly one row has `Parent = null`. Use unique non-null names, resolvable
parents, and acyclic relationships. The root can be any PartType. Prefer a `Card`
for chart titles, but a chart can be a root or container child and use its own
`ChartTitle`. Format KPI values as text.

Duplicate names can repeat visuals, including the same chart inside two cards
with the same name. An unresolved parent silently hides its row and descendants.

## Chart mappings

The closed `ChartType` set is `Line`, `Area`, `Bar`, `StackedBar`, `Doughnut`,
and `Pie`. Use `"Doughnut"`, not `"Donut"`. The row's `Data` column supplies the
table; do not put that table inside `Properties`.

- `DataSeries.AxisColumns`: one exact source column name, as text or a one-item
  text list. The source column can contain text, dates, or numbers.
- `DataSeries.ValueColumns`: exact numeric source column names, as text or a
  nonempty text list. All chart types require exactly one value column except
  `StackedBar`, which accepts one or more.
- Empty lists, non-text entries, and multiple axis columns are rejected.
- `DataSeries.PrimaryAxisColumn` is optional text accepted for compatibility
  but has no effect with the preview's single axis.

For example, a line chart uses
`[ChartType = "Line", DataSeries = [AxisColumns = "Month", ValueColumns = "Revenue"]]`.
Recheck mappings after renames and aggregation: a missing axis column can produce
one `undefined` category, while a missing value column can leave an empty chart
without a visible error.

`StackedBar` requires wide-form data: one row per category and one numeric column
per series. For long-form inputs, aggregate by category and series, then use
`Table.Pivot` and update `ValueColumns` to the resulting series column names.
Avoid backslashes in the category column name; they cause incorrect grouping.
Series segments and numeric labels use alphabetical series order, and the legend
has no heading. The numeric axis has ticks but no title. Bar tooltips use localized
Category, Series, and Value labels; numeric-label tooltips can expose internal
field names. Workbook placement properties (`Bounds`, `TableStyle`,
`ShowGridlines`, `AutoPositionColumnOffset`, `AutoPositionRowOffset`) are ignored.

## Validation

Missing or renamed document columns produce a normal table preview; extra columns
are ignored. A non-record `Properties` cell can fail the entire preview with a
record-conversion error. Invalid fields inside a valid record, such as numeric
`KpiCard.Value`, can instead produce an inline error scoped to one visual.
An empty `Container` or `Card` reports missing `"cells"`; a card with multiple
children reports `Unexpected number of cells`.

## Workflow

1. Clarify objective, audience, measures, dimensions, time grain, filters,
   targets, and exclusions. Confirm ambiguous definitions.
2. Inspect query definitions or metadata without sampling business values.
   Prefer `get_dataflow_definition` when MCP tools are available. Never use
   `Expression.Evaluate` or dynamically evaluate sibling query values.
3. Recommend a lightweight Visual in a separate query that references the source
   query, and state assumptions. Adapting it requires updating source references,
   calculations, types, category values, and chart mappings, not just column names.
4. Select required columns, filter invalid values, aggregate chart data to visual
   grain, sort explicitly, and cap categories before constructing the flat `#table`.
   Detail tables can select and sort source rows without aggregation.
5. Begin with at most three data visuals, ten chart categories, and 50 detail
   rows as a lightweight starting recommendation, not a platform limit.
   Add complexity only after rendering succeeds.
6. For the Advanced Editor, return a complete single-query expression. For MCP
   execution, use `execute_query` with `customMashupDocument` nonpersistently when
   possible. For saving, preserve all existing queries and dependencies in a
   complete M section document (`section Section1;` and `shared` query declarations).
   Ask before persistent changes. Use `save_dataflow_definition` with
   `validateOnly = true` before saving because a save replaces the complete
   mashup. After saving, restore required bindings with
   `add_connection_to_dataflow` and validate them.

For profiling requests, use `Table.Profile` for column statistics and
`Table.Schema` for column types. Quartiles, numeric bins, and IQR outlier counts
need additional M calculations; they are not default `Table.Profile` output.
Reusable profiling must handle empty tables, nulls, and columns with no numeric
data. Discover metadata first; profiling is an explicit data-evaluation step,
not a reason to dynamically evaluate sibling queries during discovery.

Visuals show evaluation-time snapshots without filters, slicers, date pickers, or
cross-filtering. They appear only in the authoring canvas, not refresh output,
destinations, or the Dataflow Gen2 connector. Many visuals or large tables can slow
authoring. `Line` and `Area` do not fill missing dates; equally spaced line points
need not represent equal elapsed time. Structural validation does not prove a
successful Fabric render.