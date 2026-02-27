# Integration Evals: M Code Quality

Tests whether the LLM generates valid, idiomatic M (Power Query) code.
Each scenario is run **twice**: once without skills (baseline) and once with skills (full system).

**Validation method:** Model output parsed by `MDocumentValidator` and `MDocumentParser` rules (regex-based, no Fabric connection needed).

**Skills tested:**
- `datafactory-core.md` — M basics, tool usage, rolling dates
- `datafactory-destinations.md` — DataDestination patterns, AllowCombine
- `datafactory-performance.md` — Query folding, chunking, connector choice
- `datafactory-advanced.md` — Fast Copy limits, Action.Sequence

---

## Section Document Structure

### EVAL-INT-M-001: Basic section document

**Category:** M Syntax
**Difficulty:** Easy
**Skills:** none → datafactory-core

**User prompt:**
> Write an M section document with a query called "GetCustomers" that reads from a SQL database server "sql.contoso.com" database "Sales" and selects only rows where Status = "Active"

**Validation rules:**
- [ ] Starts with `section Section1;` (or `section <name>;`)
- [ ] Contains `shared GetCustomers =`
- [ ] Contains `let ... in` structure
- [ ] Contains `Sql.Database`
- [ ] Contains `Table.SelectRows` with filter logic
- [ ] Ends in `;` after the query
- [ ] Passes MDocumentValidator (no errors)
- [ ] MDocumentParser extracts exactly 1 query named "GetCustomers"

**Common failure without skills:**
- Missing `section` header (model writes bare `let...in`)
- Missing `shared` keyword
- Using Python/SQL syntax instead of M

---

### EVAL-INT-M-002: Multiple queries in one document

**Category:** M Syntax
**Difficulty:** Medium
**Skills:** none → datafactory-core

**User prompt:**
> Write an M section document with two queries:
> 1. "Source" that reads from Lakehouse
> 2. "Filtered" that references Source and filters where [Amount] > 1000

**Validation rules:**
- [ ] Valid section header
- [ ] Two `shared` declarations: `Source` and `Filtered`
- [ ] `Filtered` references `Source` (not a separate data source call)
- [ ] Both queries end with `;`
- [ ] Passes MDocumentValidator
- [ ] MDocumentParser extracts exactly 2 queries

---

### EVAL-INT-M-003: Quoted query names

**Category:** M Syntax
**Difficulty:** Medium
**Skills:** none → datafactory-core

**User prompt:**
> Write an M section document with a query called "Get Sales Data" (with spaces) that returns a simple value of 42

**Validation rules:**
- [ ] Uses `shared #"Get Sales Data" =` syntax for name with spaces
- [ ] Valid section document
- [ ] Passes MDocumentValidator

**Common failure without skills:**
- Model writes `shared Get Sales Data =` (no quoting, syntax error)

---

### EVAL-INT-M-004: Balanced brackets in complex queries

**Category:** M Syntax
**Difficulty:** Hard
**Skills:** none → datafactory-core

**User prompt:**
> Write an M section document with a query "Sales" that:
> 1. Reads from SQL database "server1" / "SalesDB"
> 2. Navigates to dbo.Orders table
> 3. Filters where [OrderDate] >= #date(2025, 1, 1)
> 4. Groups by [Region] summing [Amount]
> 5. Sorts by total descending

**Validation rules:**
- [ ] Balanced parentheses `()`
- [ ] Balanced braces `{}`
- [ ] Balanced square brackets `[]`
- [ ] Contains `Table.SelectRows`, `Table.Group`, `Table.Sort`
- [ ] Uses `#date(2025, 1, 1)` M date literal (not SQL or Python date format)
- [ ] Passes MDocumentValidator

---

## Data Destinations

### EVAL-INT-M-005: New table destination configuration

**Category:** Destinations
**Difficulty:** Hard
**Skills:** none → datafactory-destinations

**User prompt:**
> Write a complete M section document for a dataflow that reads from SQL, aggregates sales by region, and writes the output to a NEW Lakehouse table. The destination workspace ID is "ws-123" and lakehouse ID is "lh-456". The output query is called "RegionalSales".

**Validation rules:**
- [ ] Contains `shared RegionalSales =` with `[DataDestinations = ...]` attribute
- [ ] Contains `shared RegionalSales_DataDestination =` companion query
- [ ] DataDestination uses `IsNewTarget = true`
- [ ] DataDestination uses `Kind = "Automatic"` (not Manual for new tables)
- [ ] Companion query uses `?[Data]?` null-safe navigation
- [ ] Companion query has `EnableFolding = false`
- [ ] Contains workspace and lakehouse IDs
- [ ] Valid section document, passes MDocumentValidator

**Common failure without skills:**
- Missing `_DataDestination` companion query entirely
- Using `Kind = "Manual"` with `ColumnSettings` (causes `DestinationColumnNotFound`)
- Using direct `[Data]` instead of `?[Data]?` for new table
- Missing `IsNewTarget = true`

---

### EVAL-INT-M-006: Existing table destination

**Category:** Destinations
**Difficulty:** Hard
**Skills:** none → datafactory-destinations

**User prompt:**
> Write the M section document for a dataflow writing to an EXISTING Lakehouse table. The output query is "MonthlySales", workspace "ws-abc", lakehouse "lh-def". Use replace update method.

**Validation rules:**
- [ ] `IsNewTarget = false` (existing table)
- [ ] `Kind = "Manual"` with `UpdateMethod = [Kind = "Replace"]`
- [ ] Companion query uses direct `[Data]` navigation (no `?`)
- [ ] Valid section document

---

### EVAL-INT-M-007: Multi-source with AllowCombine

**Category:** Destinations
**Difficulty:** Hard
**Skills:** none → datafactory-destinations

**User prompt:**
> Write an M section document that joins data from a Lakehouse source and a SharePoint Excel file. Include proper privacy settings for cross-source access.

**Validation rules:**
- [ ] Contains `[AllowCombine = true]` before the section declaration
- [ ] Contains Lakehouse source query (`Lakehouse.Contents`)
- [ ] Contains SharePoint/Web source query (`Web.Contents` with `.sharepoint.com`)
- [ ] Contains a join/merge query referencing both sources
- [ ] Valid section document

**Common failure without skills:**
- Missing `[AllowCombine = true]` entirely (causes instant refresh failure)
- Placing `AllowCombine` in wrong position

---

## Performance Patterns

### EVAL-INT-M-008: Filter-first query design

**Category:** Performance
**Difficulty:** Medium
**Skills:** none → datafactory-performance

**User prompt:**
> Write M code to read from a large SQL table "Orders" (100M rows), filter to only 2025 orders, select columns [OrderId, CustomerId, Amount, OrderDate], then sort by Amount descending. Make it performant.

**Validation rules:**
- [ ] `Table.SelectRows` (filter) appears BEFORE `Table.Sort` (sort)
- [ ] `Table.SelectColumns` appears BEFORE `Table.Sort`
- [ ] Sort is the LAST operation (expensive, requires full data scan)
- [ ] Uses `Sql.Database` or `Sql.Databases` (native connector)
- [ ] Does NOT use `Table.FirstN` for sampling

**Common failure without skills:**
- Sorting before filtering (processes all 100M rows)
- Using `Table.FirstN` to "sample" data

---

### EVAL-INT-M-009: Rolling date window

**Category:** Performance
**Difficulty:** Medium
**Skills:** none → datafactory-core

**User prompt:**
> Write M code that filters to the last 3 months of data using a rolling window (not hardcoded dates). Use it on a table with a [TransactionDate] column.

**Validation rules:**
- [ ] Uses `DateTime.LocalNow()` or `DateTimeZone.LocalNow()` for current date
- [ ] Uses `Date.AddMonths` for relative calculation
- [ ] Does NOT contain hardcoded year/month values like `2025` or `"2025-01-01"`
- [ ] Filter references `[TransactionDate]` column

**Common failure without skills:**
- Hardcoded dates (`#date(2025, 10, 1)`) instead of rolling window
- Using Python `datetime` patterns

---

### EVAL-INT-M-010: Avoid Table.FirstN for data sampling

**Category:** Performance
**Difficulty:** Easy
**Skills:** none → datafactory-core + datafactory-performance

**User prompt:**
> I have a dataflow that times out when loading a large Lakehouse table. How should I handle this? Write the M code.

**Validation rules:**
- [ ] Does NOT use `Table.FirstN` to limit rows
- [ ] Uses chunking strategy (filter by date/ID ranges)
- [ ] Or uses `Table.SelectRows` with a date/ID filter
- [ ] Mentions or implements iterative processing approach

**Expected with skills:**
- Chunking by date column or ID range
- `Table.SelectRows(Source, each [DateCol] >= window_start and [DateCol] < window_end)`

**Common failure without skills:**
- `Table.FirstN(Source, 5)` — critical anti-pattern, violates MUST NOT rules

---

## Fast Copy & Advanced

### EVAL-INT-M-011: Fast Copy section attribute

**Category:** Advanced
**Difficulty:** Medium
**Skills:** none → datafactory-advanced

**User prompt:**
> Write an M section document using Fast Copy for a simple ingestion dataflow that reads from SQL and loads to Lakehouse. Only select and rename columns, no complex transforms.

**Validation rules:**
- [ ] Contains `[StagingDefinition = [Kind = "FastCopy"]]` before section declaration
- [ ] Query uses only supported transforms: select columns, rename, type change
- [ ] Does NOT contain `Table.Group`, `Table.NestedJoin`, or other unsupported transforms
- [ ] Valid section document

---

### EVAL-INT-M-012: Fast Copy with unsupported transforms — should NOT use Fast Copy

**Category:** Advanced
**Difficulty:** Hard
**Skills:** none → datafactory-advanced

**User prompt:**
> Write an M dataflow that reads from SQL, groups by Region summing Amount, and loads to Lakehouse. Use the fastest approach.

**Validation rules:**
- [ ] Does NOT contain `[StagingDefinition = [Kind = "FastCopy"]]`
- [ ] Contains `Table.Group` (which is incompatible with Fast Copy)
- [ ] Valid section document

**Common failure without skills:**
- Applying Fast Copy annotation despite using `Table.Group` (causes runtime failure)

---

### EVAL-INT-M-013: Action.Sequence for side effects only

**Category:** Advanced
**Difficulty:** Hard
**Skills:** none → datafactory-advanced

**User prompt:**
> Write M code to aggregate sales data by region from a SQL source. Should I use Action.Sequence?

**Validation rules:**
- [ ] Does NOT use `Action.Sequence` (this is pure transformation, not side effects)
- [ ] Uses standard `let ... in` pattern
- [ ] Contains `Table.Group` for aggregation
- [ ] Response explains that Action.Sequence is for writes/side effects only

**Common failure without skills:**
- Wrapping normal transforms in `Action.Sequence` unnecessarily

---

## Pipeline JSON

### EVAL-INT-M-014: Dataflow activity JSON

**Category:** Pipeline
**Difficulty:** Medium
**Skills:** none → datafactory-pipelines

**User prompt:**
> Write the pipeline definition JSON for a pipeline that refreshes dataflow "df-123" in workspace "ws-456"

**Validation rules:**
- [ ] Valid JSON (parseable)
- [ ] Has `properties.activities` array
- [ ] Activity has `type: "DataflowActivity"`
- [ ] `typeProperties.dataflowId` = `df-123`
- [ ] `typeProperties.workspaceId` = `ws-456`
- [ ] Has `policy` with timeout and retry settings

---

### EVAL-INT-M-015: Chained pipeline activities

**Category:** Pipeline
**Difficulty:** Hard
**Skills:** none → datafactory-pipelines

**User prompt:**
> Write pipeline JSON with two dataflow activities: "Refresh Source" (dataflow df-1) runs first, then "Refresh Aggregation" (dataflow df-2) runs after it succeeds. Both in workspace ws-100.

**Validation rules:**
- [ ] Valid JSON
- [ ] Two activities in `properties.activities`
- [ ] Second activity has `dependsOn` referencing first activity name
- [ ] `dependencyConditions` includes `"Succeeded"`
- [ ] Both have correct `dataflowId` values
- [ ] Both have `type: "DataflowActivity"`

---

## First Refresh Pattern

### EVAL-INT-M-016: ApplyChangesIfNeeded on first refresh

**Category:** Workflow
**Difficulty:** Medium
**Skills:** none → datafactory-core

**User prompt:**
> I just created a dataflow via the API and saved its definition. Now I want to refresh it. What executeOption should I use?

**Validation rules:**
- [ ] Specifies `executeOption = "ApplyChangesIfNeeded"` (not default `SkipApplyChanges`)
- [ ] Explains WHY: API-created dataflows start in unpublished draft state
- [ ] Mentions that subsequent refreshes can use either option

**Common failure without skills:**
- Using default `SkipApplyChanges` (causes `DataflowNeverPublishedError`)

---

### EVAL-INT-M-017: Re-add connections after save_dataflow_definition

**Category:** Workflow
**Difficulty:** Medium
**Skills:** none → datafactory-core

**User prompt:**
> I just saved a dataflow definition with save_dataflow_definition. What should I do next before refreshing?

**Validation rules:**
- [ ] Mentions re-adding connections via `add_connection_to_dataflow`
- [ ] Explains that `save_dataflow_definition` may wipe connections
- [ ] Suggests verifying with `get_dataflow_definition`

**Common failure without skills:**
- Going straight to refresh without re-adding connections (causes credential errors)

---

### EVAL-INT-M-018: Connection discovery workflow

**Category:** Workflow
**Difficulty:** Medium
**Skills:** none → datafactory-core

**User prompt:**
> I need to connect my dataflow to a SQL database called "contoso-analytics". How do I find the right connection ID?

**Validation rules:**
- [ ] Suggests calling `list_connections`
- [ ] Mentions matching by `connectionType: "SQL"` and server/database in parameters
- [ ] Extracts the `DatasourceId` GUID
- [ ] Does NOT suggest hardcoding a connection ID

---

## Full Lifecycle

### EVAL-INT-M-019: Complete dataflow setup with destination

**Category:** Lifecycle
**Difficulty:** Hard
**Skills:** none → datafactory-core + datafactory-destinations

**User prompt:**
> Walk me through creating a complete dataflow that reads from SQL, transforms data, and writes to a new Lakehouse table. Give me the tool calls and M code.

**Validation rules:**
- [ ] Correct tool sequence: create_dataflow → add_connection → save_dataflow_definition → add_connection (re-add) → refresh with ApplyChangesIfNeeded
- [ ] M document has valid section structure
- [ ] Includes `DataDestinations` annotation with `IsNewTarget = true`, `Kind = "Automatic"`
- [ ] Includes `_DataDestination` companion query with `?[Data]?`
- [ ] Uses `executeOption = "ApplyChangesIfNeeded"` for first refresh

---

### EVAL-INT-M-020: Multi-source dataflow lifecycle

**Category:** Lifecycle
**Difficulty:** Hard
**Skills:** none → datafactory-core + datafactory-destinations

**User prompt:**
> Create a dataflow that joins Lakehouse data with an Excel file on SharePoint, then writes to a new Lakehouse table. Give me the complete plan.

**Validation rules:**
- [ ] Creates a FRESH dataflow (not reusing existing)
- [ ] Adds ALL connections before saving
- [ ] M document includes `[AllowCombine = true]`
- [ ] Consolidates Lakehouse reads into single query
- [ ] Re-adds connections after save
- [ ] Uses `ApplyChangesIfNeeded` on first refresh
- [ ] Mentions that multi-source cannot revert to single-source
