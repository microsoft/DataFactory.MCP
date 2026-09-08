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
ignored. Wrong value types fail evaluation.

## Closed PartType set

`Container`, `Card`, `Header`, `KpiCard`, `Table`, `LineChart`, `AreaChart`,
`BarChart`, `StackedBarChart`, `DonutChart`, `PieChart`.

| PartType | Children | Required properties | Optional |
|----------|----------|---------------------|----------|
| `Container` | One or more | none | `Direction`: `"row"` or `"column"` |
| `Card` | Exactly one | `Title` | none |
| `Header` | None | `Header` | `FarText` |
| `KpiCard` | None | `Value`, `Label` (text) | `Sub` |
| `Table` | None | table in `Data` | none |
| `LineChart`, `AreaChart` | None | `XAxis`, `YAxis`, `Data` | none |
| `BarChart`, `DonutChart`, `PieChart` | None | `Category`, `Value`, `Data` | none |
| `StackedBarChart` | None | `Category`, `Value`, `Series`, `Data` | none |

Chart properties hold **column names**, not data. `Value` and `YAxis` must name
numeric columns. Charts have no title, so nest each one in a `Card`.

## Rules

- Exactly one row has `Parent = null`.
- Names are unique and non-null; duplicates don't error but break descendants.
- Every other `Parent` matches an existing `Name`; unresolved parents render nothing.
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
        {"sales-trend", "sales-card", "LineChart", [XAxis = "Month", YAxis = "Revenue"], SalesData}
    })
```

## Triage

| Symptom | Cause |
|---------|-------|
| Renders as a plain table | Required column missing or renamed |
| `Visual not recognized: "<value>"` | PartType outside the closed set |
| One `undefined` bucket | Property names a column missing from `Data` |
| `must contain exactly one root row` | Zero or multiple `Parent = null` rows |
| `Unexpected number of cells` | `Card` has other than one child |

## Limitations

- Preview; subject to change.
- Static only — no slicers, date pickers, or cross-filtering.
- Renders in the authoring canvas only; never part of refresh output or destinations.
- Many visuals or large `Data` tables slow authoring.
- Line and area axes don't fill missing dates.

Full reference: [Create data visuals in Dataflow Gen2](https://learn.microsoft.com/fabric/data-factory/dataflow-gen2-data-visuals).
