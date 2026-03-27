// Template: Read Excel from SharePoint via Web.Contents
// Requires Web connection to SharePoint site URL
// Replace: TENANT, SITE, FILE, SHEET_NAME, column types
// After saving: add_connection_to_dataflow (Web connection) → execute_query to validate

shared ExcelSource = let
    WB = Excel.Workbook(
        Web.Contents("https://TENANT.sharepoint.com/teams/SITE/Shared Documents/FILE.xlsx"),
        null,
        true
    ),
    Sheet = WB{[Item = "SHEET_NAME", Kind = "Sheet"]}[Data],
    Headers = Table.PromoteHeaders(Sheet, [PromoteAllScalars = true]),
    Typed = Table.TransformColumnTypes(Headers, {
        {"col1", type text},
        {"col2", type number}
    })
in
    Typed;
