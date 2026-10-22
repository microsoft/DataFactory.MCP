# Dataflow Gen2 Visual parity checks

Use these cases to compare the Claude and ChatGPT skill variants. They do not
require provider API keys and are not executed by the MCP server.

## Setup

- Claude: load `claude-skills/SKILL.md` and
  `claude-skills/datafactory-data-visuals.md`.
- ChatGPT: use `chatgpt-skills/gpt-instructions.md`
  with `chatgpt-skills/knowledge-data-visuals.md`.
- Start a new conversation in each client.
- Submit the same prompt without adding client-specific guidance.

## Shared acceptance criteria

For cases that generate M, both clients should meet the following criteria.
For explanation-only and persistence cases, assess the relevant guidance rather
than requiring code or a new visual:

- Call the artifact a Visual or visualization document.
- Return a flat Power Query M table with `Name`, `Parent`, `PartType`,
  `Properties`, and `Data`.
- Use only `Container`, `Card`, `Header`, `KpiCard`, `Table`, and `Chart` as
  PartTypes, and only `Line`, `Area`, `Bar`, `StackedBar`, `Doughnut`, and `Pie`
  as chart types. Never emit earlier chart-specific PartTypes.
- Produce exactly one root and valid parent relationships.
- Prefer a `Card` for chart titles; honor requests for a root chart with its own
  `ChartTitle` instead of requiring a card.
- Map `DataSeries.AxisColumns` and `ValueColumns` to exact source column names.
  Text and one-item text lists are equivalent. Require exactly one axis column
  and one value column except for `StackedBar`, which accepts multiple values.
- Use wide-form data for `StackedBar`, pivoting long-form inputs when needed.
- Format KPI values as text.
- Avoid source mutation and dynamic sibling-query evaluation.
- State that data visuals are a Preview feature when limitations matter.

Equivalent output does not require identical row names, local variable names,
formatting, prose, or visual ordering. Compare the selected PartTypes, data
aggregations, chart mappings, hierarchy validity, and safety behavior.

## Case 1: KPI synonym routing

> Create a minimal Dataflow Gen2 dashboard that shows a KPI with the text value
> 42 and label Active customers. Return only runnable Power Query M.

Expected in both outputs: one valid `KpiCard` root with text `Value` and `Label`.

## Case 2: Report synonym routing

> Create a Dataflow Gen2 report from an existing SalesData query with Month text
> and Revenue number columns. Build one line chart titled Monthly revenue. Keep
> it inside a card, keep it lightweight, and return only runnable Power Query M.

Expected in both outputs: one `Card` containing one `Chart`, with
`ChartType = "Line"`, `DataSeries` mapping `AxisColumns` to `"Month"` and
`ValueColumns` to `"Revenue"`, and an aggregated, sorted data table. Equivalent
one-item text lists are valid.

## Case 3: Multi-visual summary

> Create a Dataflow Gen2 visual from an existing SalesData query with Category
> text and Revenue number columns. Build a header, a revenue KPI, a bar chart
> titled Revenue by category, and a detail table limited to 50 rows. Return only
> runnable Power Query M.

Expected in both outputs: `Container`, `Header`, `KpiCard`, `Card`, `Chart` with
`ChartType = "Bar"`, and `Table`; valid hierarchy; bounded detail data; numeric
category aggregation.

## Case 4: Persistence safety

> Add this visual query to my existing dataflow and save it.

Expected in both responses: inspect the existing definition, preserve the full
mashup, validate before saving, obtain confirmation before the persistent save,
and restore required connection bindings afterward.

## Case 5: Wide-form stacked bar

> SalesData contains Region, ProductCategory, and numeric Revenue. ProductCategory
> contains Hardware and Software. Create a stacked bar chart by region and product
> category. Return only runnable Power Query M.

Expected in both outputs: aggregate by region and category, pivot ProductCategory
into numeric Hardware and Software columns, and return a `Chart` with
`ChartType = "StackedBar"`. `DataSeries.AxisColumns` names Region and
`ValueColumns` lists both pivoted columns. No legacy Category/Value/Series mapping.

## Case 6: Root chart and one-item lists

> SalesByRegion has one row per Region and numeric Revenue. Create one root
> doughnut chart with its own title Revenue share, without a card or container.
> Use one-item lists for both column mappings. Return only runnable Power Query M.

Expected in both outputs: one `Chart` root, `Parent = null`,
`ChartType = "Doughnut"`, `ChartTitle = "Revenue share"`, and
`DataSeries = [AxisColumns = {"Region"}, ValueColumns = {"Revenue"}]`.

## Case 7: Mapping cardinality

> Can a Line chart use AxisColumns = {"Month", "Region"} and
> ValueColumns = {"Revenue", "Orders"}? What about empty lists or numeric entries?

Expected in both responses: reject all of those invalid mappings; every chart has
exactly one axis column, and only `StackedBar` permits multiple value columns.
Offer separate line charts instead. Do not imply that `PrimaryAxisColumn` enables
multiple axes; it has no effect in the preview.

## Case 8: Preview errors and duplicate names

> Explain the difference between a number in a Properties cell and a number in
> KpiCard.Value inside a valid record. What happens with duplicate card names,
> missing parents, an empty card, two children in a card, or missing chart columns?

Expected in both responses: distinguish whole-preview record conversion from a
row-scoped field error. Duplicate card names can repeat their chart; missing
parents hide descendants. Empty cards report missing cells; multiple children
report an unexpected count. Missing axis columns can show an undefined category;
missing value columns can leave an empty chart without a visible error.

## Case 9: Reusable profiling

> Explain how to build a reusable profiling Visual with column types, numeric
> distributions, quartiles, and IQR outlier counts. Does Table.Profile provide
> all of those automatically?

Expected in both responses: use a separate query referencing the source,
`Table.Profile` for statistics, and `Table.Schema` for types; additional M
calculations for bins, quartiles, and IQR outlier counts. Handle empty tables,
nulls, and columns without numeric data; no dynamic sibling-query evaluation
during discovery.

## Case 10: Preview rendering caveats

> Does a Line chart's date spacing measure elapsed time? Can I control chart
> placement with Bounds? What StackedBar rendering caveats should I account for?

Expected in both responses: missing dates are not filled and equal line-point
spacing need not mean equal elapsed time. Workbook placement properties are
ignored. Stacked series and numeric labels use alphabetical series order; the
legend has no heading and the numeric axis no title. Numeric-label tooltips can
expose internal names even though bar tooltips use localized labels. Avoid
backslashes in category column names because grouping is incorrect.

## Recording results

For each case record `Pass`, `Different but equivalent`, `Fail`, or
`Not applicable` (for example, hierarchy in an explanation-only case) for:

| Check | Claude | ChatGPT | Notes |
| --- | --- | --- | --- |
| Request routed to Visual workflow | | | |
| Required PartTypes present | | | |
| Valid hierarchy and cardinality | | | |
| Equivalent aggregation and mappings | | | |
| Preview and safety guidance aligned | | | |

A release is ready when neither client has a `Fail`. Review any
`Different but equivalent` result to ensure the difference is presentation-only.