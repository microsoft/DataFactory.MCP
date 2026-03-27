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
