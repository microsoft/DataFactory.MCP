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

For each case, both clients should:

- Call the artifact a Visual or visualization document.
- Return a flat Power Query M table with `Name`, `Parent`, `PartType`,
  `Properties`, and `Data`.
- Use only documented PartTypes.
- Produce exactly one root and valid parent relationships.
- Put every chart inside a `Card`.
- Map chart properties to exact source column names.
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
> it lightweight and return only runnable Power Query M.

Expected in both outputs: one `Card` containing one `LineChart`, with `XAxis =
"Month"`, `YAxis = "Revenue"`, and an aggregated, sorted data table.

## Case 3: Multi-visual summary

> Create a Dataflow Gen2 visual from an existing SalesData query with Category
> text and Revenue number columns. Build a header, a revenue KPI, a bar chart
> titled Revenue by category, and a detail table limited to 50 rows. Return only
> runnable Power Query M.

Expected in both outputs: `Container`, `Header`, `KpiCard`, `Card`, `BarChart`,
and `Table`; valid hierarchy; bounded detail data; numeric category aggregation.

## Case 4: Persistence safety

> Add this visual query to my existing dataflow and save it.

Expected in both responses: inspect the existing definition, preserve the full
mashup, validate before saving, obtain confirmation before the persistent save,
and restore required connection bindings afterward.

## Recording results

For each case record `Pass`, `Different but equivalent`, or `Fail` for:

| Check | Claude | ChatGPT | Notes |
| --- | --- | --- | --- |
| Request routed to Visual workflow | | | |
| Required PartTypes present | | | |
| Valid hierarchy and cardinality | | | |
| Equivalent aggregation and mappings | | | |
| Preview and safety guidance aligned | | | |

A release is ready when neither client has a `Fail`. Review any
`Different but equivalent` result to ensure the difference is presentation-only.