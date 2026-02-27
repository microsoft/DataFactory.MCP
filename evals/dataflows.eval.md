# Dataflow Tool Evals

Tools under test:
- `ListDataflowsAsync(workspaceId, continuationToken?)`
- `CreateDataflowAsync(workspaceId, displayName, description?, folderId?)`
- `AddConnectionToDataflowAsync(workspaceId, dataflowId, connectionIds?, clearExisting?)`
- `AddOrUpdateQueryInDataflowAsync(workspaceId, dataflowId, queryName, mCode)`
- `get_dataflow_definition(workspaceId, dataflowId)` — GetDecodedDataflowDefinitionAsync
- `save_dataflow_definition(workspaceId, dataflowId, mDocument, validateOnly?)` — SaveDataflowDefinitionAsync
- `ExecuteQueryAsync(workspaceId, dataflowId, queryName, customMashupDocument)`
- `RefreshDataflowBackground(workspaceId, dataflowId, displayName?, executeOption?)`
- `RefreshDataflowStatus(workspaceId, dataflowId, jobInstanceId)`

---

## Tool Selection

### EVAL-DF-001: List dataflows in a workspace

**Category:** Tool Selection
**Difficulty:** Easy

**User prompt:**
> Show me the dataflows in workspace `ws-abc`

**Expected tool call(s):**
- Tool: `ListDataflowsAsync`
  - `workspaceId`: `ws-abc`

**Assertions:**
- Must select dataflow list, not pipeline list
- Must extract workspace ID from prompt

---

### EVAL-DF-002: Create a new dataflow

**Category:** Tool Selection
**Difficulty:** Easy

**User prompt:**
> Create a new dataflow called "Sales ETL" in workspace `ws-123`

**Expected tool call(s):**
- Tool: `CreateDataflowAsync`
  - `workspaceId`: `ws-123`
  - `displayName`: `Sales ETL`

**Assertions:**
- Must use create dataflow, not create pipeline
- "ETL" and "dataflow" are strong signals

---

### EVAL-DF-003: Get dataflow definition (M code)

**Category:** Tool Selection
**Difficulty:** Medium

**User prompt:**
> Show me the M code for dataflow `df-456` in workspace `ws-123`

**Expected tool call(s):**
- Tool: `get_dataflow_definition`
  - `workspaceId`: `ws-123`
  - `dataflowId`: `df-456`

**Assertions:**
- "M code" / "Power Query code" / "definition" all map to this tool
- Must not call `ExecuteQueryAsync`

---

### EVAL-DF-004: Execute a query

**Category:** Tool Selection
**Difficulty:** Medium

**User prompt:**
> Run this M query against dataflow `df-789` in workspace `ws-123`:
> ```
> let Source = Sql.Database("server", "db"), Result = Table.SelectRows(Source, each [Status] = "Active") in Result
> ```

**Expected tool call(s):**
- Tool: `ExecuteQueryAsync`
  - `workspaceId`: `ws-123`
  - `dataflowId`: `df-789`
  - `queryName`: any reasonable name (e.g., `Query`, `Result`, `CustomQuery`)
  - `customMashupDocument`: the M code from the prompt

**Assertions:**
- Must select execute query, not save definition
- Must pass the M code as the `customMashupDocument`

---

### EVAL-DF-005: Refresh a dataflow

**Category:** Tool Selection
**Difficulty:** Easy

**User prompt:**
> Refresh dataflow `df-456` in workspace `ws-123`

**Expected tool call(s):**
- Tool: `RefreshDataflowBackground`
  - `workspaceId`: `ws-123`
  - `dataflowId`: `df-456`

**Assertions:**
- Must select background refresh, not execute query
- Must not use status check tool (no existing job to check)

---

### EVAL-DF-006: Check refresh status

**Category:** Tool Selection
**Difficulty:** Medium

**User prompt:**
> Is the refresh done yet?

**Context:**
> Previous `RefreshDataflowBackground` returned:
> ```json
> { "taskInfo": { "workspaceId": "ws-123", "dataflowId": "df-456", "jobInstanceId": "job-789" } }
> ```

**Expected tool call(s):**
- Tool: `RefreshDataflowStatus`
  - `workspaceId`: `ws-123`
  - `dataflowId`: `df-456`
  - `jobInstanceId`: `job-789`

**Assertions:**
- Must extract all three IDs from prior context
- Must NOT start a new refresh

---

### EVAL-DF-007: Add a connection to a dataflow

**Category:** Tool Selection
**Difficulty:** Medium

**User prompt:**
> Connect dataflow `df-123` in workspace `ws-001` to connection `conn-abc`

**Expected tool call(s):**
- Tool: `AddConnectionToDataflowAsync`
  - `workspaceId`: `ws-001`
  - `dataflowId`: `df-123`
  - `connectionIds`: `conn-abc`

**Assertions:**
- Must use the dataflow connection tool, not create connection tool
- Must pass the connection ID

---

### EVAL-DF-008: Add a query to a dataflow

**Category:** Tool Selection
**Difficulty:** Medium

**User prompt:**
> Add a new query called "GetCustomers" to dataflow `df-123` in workspace `ws-001` with this M code: `let Source = Sql.Database("server", "db") in Source`

**Expected tool call(s):**
- Tool: `AddOrUpdateQueryInDataflowAsync`
  - `workspaceId`: `ws-001`
  - `dataflowId`: `df-123`
  - `queryName`: `GetCustomers`
  - `mCode`: `let Source = Sql.Database("server", "db") in Source`

**Assertions:**
- Must select add/update query, not save definition (which replaces the entire document)
- Must pass the M code as the `mCode` parameter

---

### EVAL-DF-009: Save entire dataflow definition

**Category:** Tool Selection
**Difficulty:** Medium

**User prompt:**
> Replace the entire dataflow definition for `df-123` in workspace `ws-001` with this M document:
> ```
> section Section1;
> shared GetCustomers = let Source = Sql.Database("server", "db") in Source;
> shared GetOrders = let Source = Sql.Database("server", "db"), Orders = Source{[Schema="dbo",Item="Orders"]}[Data] in Orders;
> ```

**Expected tool call(s):**
- Tool: `save_dataflow_definition`
  - `workspaceId`: `ws-001`
  - `dataflowId`: `df-123`
  - `mDocument`: the full section document
  - `validateOnly`: `false`

**Assertions:**
- "Replace entire definition" maps to save_dataflow_definition, not AddOrUpdateQuery
- The full section document format (starting with `section Section1;`) is the signal

---

## Parameter Extraction

### EVAL-DF-010: Create dataflow with description and folder

**Category:** Parameter Extraction
**Difficulty:** Medium

**User prompt:**
> Create a dataflow named "HR Data Pipeline" in workspace `ws-hr-001` with description "Loads employee data from SQL to Lakehouse" in folder `folder-hr`

**Expected tool call(s):**
- Tool: `CreateDataflowAsync`
  - `workspaceId`: `ws-hr-001`
  - `displayName`: `HR Data Pipeline`
  - `description`: `Loads employee data from SQL to Lakehouse`
  - `folderId`: `folder-hr`

**Assertions:**
- All four parameters correctly extracted
- Description must be the verbatim user text

---

### EVAL-DF-011: Multiple connections at once

**Category:** Parameter Extraction
**Difficulty:** Hard

**User prompt:**
> Add connections `conn-1`, `conn-2`, and `conn-3` to dataflow `df-500` in workspace `ws-100`, replacing any existing connections

**Expected tool call(s):**
- Tool: `AddConnectionToDataflowAsync`
  - `workspaceId`: `ws-100`
  - `dataflowId`: `df-500`
  - `connectionIds`: `["conn-1", "conn-2", "conn-3"]` (array or comma-separated)
  - `clearExisting`: `true`

**Assertions:**
- Must pass all three connection IDs
- "Replacing any existing" maps to `clearExisting: true`

---

### EVAL-DF-012: Refresh with apply changes option

**Category:** Parameter Extraction
**Difficulty:** Medium

**User prompt:**
> Refresh dataflow `df-789` in workspace `ws-456` and apply pending changes first

**Expected tool call(s):**
- Tool: `RefreshDataflowBackground`
  - `workspaceId`: `ws-456`
  - `dataflowId`: `df-789`
  - `executeOption`: `ApplyChangesIfNeeded`

**Assertions:**
- "Apply pending changes" maps to `ApplyChangesIfNeeded`
- Must not use default `SkipApplyChanges`

---

### EVAL-DF-013: Validate definition without saving

**Category:** Parameter Extraction
**Difficulty:** Medium

**User prompt:**
> Validate this M document without saving it to dataflow `df-123` in workspace `ws-001`:
> ```
> section Section1;
> shared Query1 = let Source = 1 in Source;
> ```

**Expected tool call(s):**
- Tool: `save_dataflow_definition`
  - `workspaceId`: `ws-001`
  - `dataflowId`: `df-123`
  - `mDocument`: the M document
  - `validateOnly`: `true`

**Assertions:**
- "Validate without saving" maps to `validateOnly: true`
- Same tool is used for both validate and save

---

### EVAL-DF-014: Query name from M code

**Category:** Parameter Extraction
**Difficulty:** Medium

**User prompt:**
> Execute the "GetProducts" query on dataflow `df-100` in workspace `ws-200`. The M code is:
> ```
> let Source = Sql.Database("myserver", "mydb"), Products = Source{[Schema="dbo",Item="Products"]}[Data] in Products
> ```

**Expected tool call(s):**
- Tool: `ExecuteQueryAsync`
  - `workspaceId`: `ws-200`
  - `dataflowId`: `df-100`
  - `queryName`: `GetProducts`
  - `customMashupDocument`: the M code

**Assertions:**
- Query name must match what the user specified ("GetProducts"), not extracted from M code

---

### EVAL-DF-015: Workspace and dataflow from context

**Category:** Parameter Extraction
**Difficulty:** Hard

**User prompt:**
> Run a query on that dataflow: `let Source = 1 in Source`

**Context:**
> Earlier in conversation, user listed dataflows in workspace `ws-main`:
> ```json
> { "workspaceId": "ws-main", "dataflows": [
>   { "id": "df-only", "displayName": "Sales Dataflow" }
> ]}
> ```

**Expected tool call(s):**
- Tool: `ExecuteQueryAsync`
  - `workspaceId`: `ws-main`
  - `dataflowId`: `df-only`
  - `queryName`: any reasonable name
  - `customMashupDocument`: `let Source = 1 in Source`

**Assertions:**
- Must resolve "that dataflow" from context
- Must infer workspace from when the dataflow was listed

---

### EVAL-DF-016: Refresh with display name

**Category:** Parameter Extraction
**Difficulty:** Easy

**User prompt:**
> Refresh the "Sales ETL" dataflow `df-sales` in workspace `ws-123`

**Expected tool call(s):**
- Tool: `RefreshDataflowBackground`
  - `workspaceId`: `ws-123`
  - `dataflowId`: `df-sales`
  - `displayName`: `Sales ETL`

**Assertions:**
- Must pass the display name for notifications
- Must not confuse display name with dataflow ID

---

## Edge Cases

### EVAL-DF-017: Dataflow vs pipeline disambiguation

**Category:** Edge Case
**Difficulty:** Medium

**User prompt:**
> I want to create an ETL process in workspace `ws-001`

**Expected behavior:**
- "ETL process" is ambiguous — could be a dataflow or a pipeline
- Should ask the user which type they want
- If forced to choose, dataflow is more natural for ETL (data transformation)

**Assertions:**
- Must not assume without asking
- If it proceeds, dataflow is a better default than pipeline for "ETL"

---

### EVAL-DF-018: Clear all connections

**Category:** Edge Case
**Difficulty:** Medium

**User prompt:**
> Remove all connections from dataflow `df-123` in workspace `ws-001`

**Expected tool call(s):**
- Tool: `AddConnectionToDataflowAsync`
  - `workspaceId`: `ws-001`
  - `dataflowId`: `df-123`
  - `connectionIds`: null / omitted
  - `clearExisting`: `true`

**Assertions:**
- Must pass `clearExisting: true` with no connection IDs to clear all
- Must not try to look up connections first

---

### EVAL-DF-019: Save definition — invalid M document

**Category:** Edge Case
**Difficulty:** Hard

**User prompt:**
> Save this to dataflow `df-123` in workspace `ws-001`:
> ```
> shared Query1 = let Source = 1 in Source;
> ```

**Expected behavior:**
- The tool's validator will reject this (missing `section Section1;` header)
- Model should relay the validation error and suggest fixing the document

**Assertions:**
- Must attempt the save (tool handles validation internally)
- Must explain the validation failure from the response
- Should suggest adding the section header

---

### EVAL-DF-020: Refresh when already refreshing

**Category:** Edge Case
**Difficulty:** Medium

**User prompt:**
> Refresh dataflow `df-456` in workspace `ws-123`

**Context:**
> A refresh is already in progress for this dataflow (previous `RefreshDataflowBackground` is still running).

**Expected behavior:**
- Call `RefreshDataflowBackground` — the API will handle the conflict
- If API returns "already refreshing", relay that to user
- Could alternatively check status first with `RefreshDataflowStatus`

**Assertions:**
- Must not silently ignore the request
- Must relay the API's response about the existing refresh

---

### EVAL-DF-021: AddOrUpdateQuery vs save_dataflow_definition

**Category:** Edge Case
**Difficulty:** Hard

**User prompt:**
> Update the "GetCustomers" query in dataflow `df-001`, workspace `ws-001` to:
> ```
> let Source = Sql.Database("newserver", "newdb") in Source
> ```

**Expected tool call(s):**
- Tool: `AddOrUpdateQueryInDataflowAsync`
  - `workspaceId`: `ws-001`
  - `dataflowId`: `df-001`
  - `queryName`: `GetCustomers`
  - `mCode`: `let Source = Sql.Database("newserver", "newdb") in Source`

**Assertions:**
- "Update a single query" maps to `AddOrUpdateQuery`, not `save_dataflow_definition`
- `save_dataflow_definition` replaces the ENTIRE document — overkill for a single query update

**Notes:**
> This tests the model's ability to distinguish between partial update (one query) vs full replace (entire section document).

---

### EVAL-DF-022: Execute query without explicit M code

**Category:** Edge Case
**Difficulty:** Hard

**User prompt:**
> Run the "GetSalesData" query from dataflow `df-100` in workspace `ws-200`

**Expected behavior:**
- `ExecuteQueryAsync` requires `customMashupDocument` (the M code to execute)
- Without it, the model should either:
  1. First call `get_dataflow_definition` to retrieve the M code, then execute
  2. Ask the user for the M code

**Assertions:**
- Must not call `ExecuteQueryAsync` without M code
- Retrieving the definition first is the ideal approach
