# Dataflow Gen2 Data Visuals

Use this knowledge when a user asks for a Dataflow Gen2 visual, visualization,
dashboard, report, chart, KPI, or visual summary. Call the generated artifact a
**Visual** or **visualization document**; the other terms are request synonyms.

Data visuals are a Preview feature. A Visual is a flat Power Query M table with
these exact columns and types:

```powerquery-m
type table [
    Name = nullable text,
    Parent = nullable text,
    PartType = nullable text,
    Properties = nullable record,
    Data = any
]
```

Only these PartTypes exist: `Container`, `Card`, `Header`, `KpiCard`, `Table`,
`LineChart`, `AreaChart`, `BarChart`, `StackedBarChart`, `DonutChart`, and
`PieChart`.

## Contract

| PartType | Children | Required values |
| --- | --- | --- |
| `Container` | One or more | Optional `Direction` is `"row"` or `"column"` |
| `Card` | Exactly one | Text `Title` |
| `Header` | None | Text `Header`; optional text `FarText` |
| `KpiCard` | None | Text `Value`, text `Label`; optional text `Sub` |
| `Table` | None | Table in `Data` |
| `LineChart`, `AreaChart` | None | `XAxis`, numeric `YAxis`, table in `Data` |
| `BarChart`, `DonutChart`, `PieChart` | None | `Category`, numeric `Value`, table in `Data` |
| `StackedBarChart` | None | `Category`, numeric `Value`, `Series`, table in `Data` |

Exactly one row has `Parent = null`. Every `Name` is unique and non-null. Every
other `Parent` resolves to a `Name`, and relationships are acyclic. Charts go
inside cards. Chart properties contain exact column names from their `Data`
table; a bad name can silently produce one `undefined` bucket. KPI values are
formatted as text.

Duplicate names don't raise an error, but any row parented to an ambiguous name
fails to render, so keep names unique.

## Workflow

1. Clarify objective, audience, measures, dimensions, time grain, filters,
   targets, and exclusions. Confirm ambiguous definitions.
2. Inspect query definitions or metadata without sampling business values.
   Prefer `get_dataflow_definition` when MCP tools are available. Never use
   `Expression.Evaluate` or dynamically evaluate sibling query values.
3. Recommend a lightweight Visual and state assumptions.
4. Select required columns, filter invalid values, aggregate to visual grain,
   sort explicitly, and cap categories before building the flat `#table`.
5. Start with at most three data visuals, ten chart categories, and 50 detail
   rows. Add complexity only after rendering succeeds.
6. Test nonpersistently with `execute_query` when possible. Ask before changing
   a dataflow. Use `save_dataflow_definition` with `validateOnly = true` before
   saving because a save replaces the complete mashup. After saving, restore
   required bindings with `add_connection_to_dataflow` and validate them.

Visuals are static, render only in the authoring canvas, do not become refresh
output, and can slow authoring when numerous or backed by large tables. Line and
area charts do not fill missing dates. Structural validation cannot prove that
Fabric rendered the Visual successfully.