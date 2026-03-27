// Template: New table destination (single source, Lakehouse → Lakehouse)
// Replace: QUERY_NAME, WORKSPACE_ID, LAKEHOUSE_ID
// Replace query body with actual transform logic
// After saving: add_connection_to_dataflow → execute_query to validate → refresh_dataflow_background with ApplyChangesIfNeeded

section Section1;

[DataDestinations = {[
  Definition = [Kind = "Reference", QueryName = "QUERY_NAME_DataDestination", IsNewTarget = true],
  Settings = [Kind = "Automatic", TypeSettings = [Kind = "Table"]]
]}]
shared QUERY_NAME = let
    Source = Lakehouse.Contents(null),
    Nav = Source{[workspaceId = "WORKSPACE_ID"]}[Data],
    DB = Nav{[lakehouseId = "LAKEHOUSE_ID"]}[Data],
    // TODO: your transform logic here
    Result = DB
in
    Result;

shared QUERY_NAME_DataDestination = let
    Pattern = Lakehouse.Contents([HierarchicalNavigation = null, CreateNavigationProperties = false, EnableFolding = false]),
    Navigation_1 = Pattern{[workspaceId = "WORKSPACE_ID"]}[Data],
    Navigation_2 = Navigation_1{[lakehouseId = "LAKEHOUSE_ID"]}[Data],
    TableNavigation = Navigation_2{[Id = "QUERY_NAME", ItemKind = "Table"]}?[Data]?
in
    TableNavigation;
