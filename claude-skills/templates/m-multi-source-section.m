// Template: Multi-source dataflow (Lakehouse + SharePoint/Web)
// MUST use [AllowCombine = true]
// MUST consolidate all Lakehouse reads into single query
// MUST create fresh dataflow (never convert single-source)
// Replace: WORKSPACE_ID, LAKEHOUSE_ID, SHAREPOINT_URL, SHEET_NAME
// After saving: add_connection_to_dataflow (both Lakehouse + Web) → execute_query → refresh_dataflow_background with ApplyChangesIfNeeded

[AllowCombine = true]
section Section1;

// Single source query reads ALL Lakehouse tables
shared LakehouseData = let
    Source = Lakehouse.Contents(null),
    Nav = Source{[workspaceId = "WORKSPACE_ID"]}[Data],
    DB = Nav{[lakehouseId = "LAKEHOUSE_ID"]}[Data],
    Table1 = DB{[Id = "table1", ItemKind = "Table"]}[Data],
    Table2 = DB{[Id = "table2", ItemKind = "Table"]}[Data],
    // Join/combine Lakehouse tables here
    Combined = Table.NestedJoin(Table1, {"key"}, Table2, {"key"}, "joined", JoinKind.Inner)
in
    Combined;

// SharePoint/Web source
shared ExternalData = let
    WB = Excel.Workbook(Web.Contents("SHAREPOINT_URL"), null, true),
    Sheet = WB{[Item = "SHEET_NAME", Kind = "Sheet"]}[Data],
    Headers = Table.PromoteHeaders(Sheet, [PromoteAllScalars = true])
in
    Headers;

// Output query joins sources, has DataDestination
[DataDestinations = {[
  Definition = [Kind = "Reference", QueryName = "Output_DataDestination", IsNewTarget = true],
  Settings = [Kind = "Automatic", TypeSettings = [Kind = "Table"]]
]}]
shared Output = let
    Joined = Table.NestedJoin(LakehouseData, {"key"}, ExternalData, {"key"}, "ext", JoinKind.LeftOuter)
in
    Joined;

shared Output_DataDestination = let
    Pattern = Lakehouse.Contents([HierarchicalNavigation = null, CreateNavigationProperties = false, EnableFolding = false]),
    Navigation_1 = Pattern{[workspaceId = "WORKSPACE_ID"]}[Data],
    Navigation_2 = Navigation_1{[lakehouseId = "LAKEHOUSE_ID"]}[Data],
    TableNavigation = Navigation_2{[Id = "Output", ItemKind = "Table"]}?[Data]?
in
    TableNavigation;
