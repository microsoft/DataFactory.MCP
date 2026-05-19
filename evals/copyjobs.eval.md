# Copy Job Tool Evals

Tools under test:
- `ListCopyJobsAsync(workspaceId, continuationToken?)`
- `CreateCopyJobAsync(workspaceId, displayName, description?)`
- `GetCopyJobAsync(workspaceId, copyJobId)`
- `GetCopyJobDefinitionAsync(workspaceId, copyJobId)`
- `UpdateCopyJobAsync(workspaceId, copyJobId, displayName?, description?)`
- `UpdateCopyJobDefinitionAsync(workspaceId, copyJobId, definitionJson)`
- `RunCopyJobAsync(workspaceId, copyJobId)`
- `GetCopyJobRunStatusAsync(workspaceId, copyJobId, runId)`
- `CreateCopyJobScheduleAsync(workspaceId, copyJobId, scheduleConfig)`
- `ListCopyJobSchedulesAsync(workspaceId, copyJobId)`

---

## Tool Selection

### EVAL-CJ-001: List copy jobs

**Category:** Tool Selection
**Difficulty:** Easy

**User prompt:**
> Show me the copy jobs in workspace `ws-data`

**Expected tool call(s):**
- Tool: `ListCopyJobsAsync`
  - `workspaceId`: `ws-data`

**Assertions:**
- Must select copy job list, not pipeline or dataflow list
- "Copy jobs" is unambiguous

---

### EVAL-CJ-002: Create a copy job

**Category:** Tool Selection
**Difficulty:** Easy

**User prompt:**
> Create a new copy job called "Daily Sales Sync" in workspace `ws-data`

**Expected tool call(s):**
- Tool: `CreateCopyJobAsync`
  - `workspaceId`: `ws-data`
  - `displayName`: `Daily Sales Sync`

**Assertions:**
- Must use copy job create, not pipeline create
- "Copy job" and "sync" are signals for data movement

---

### EVAL-CJ-003: Get copy job metadata

**Category:** Tool Selection
**Difficulty:** Easy

**User prompt:**
> Get details for copy job `cj-100` in workspace `ws-data`

**Expected tool call(s):**
- Tool: `GetCopyJobAsync`
  - `workspaceId`: `ws-data`
  - `copyJobId`: `cj-100`

**Assertions:**
- "Details" maps to metadata, not definition

---

### EVAL-CJ-004: Get copy job definition

**Category:** Tool Selection
**Difficulty:** Medium

**User prompt:**
> Show me the configuration of copy job `cj-100` in workspace `ws-data`

**Expected tool call(s):**
- Tool: `GetCopyJobDefinitionAsync`
  - `workspaceId`: `ws-data`
  - `copyJobId`: `cj-100`

**Assertions:**
- "Configuration" maps to definition (source/dest/mappings), not metadata
- Must not call `GetCopyJobAsync` (that's metadata only)

---

### EVAL-CJ-005: Run a copy job

**Category:** Tool Selection
**Difficulty:** Easy

**User prompt:**
> Run copy job `cj-100` in workspace `ws-data`

**Expected tool call(s):**
- Tool: `RunCopyJobAsync`
  - `workspaceId`: `ws-data`
  - `copyJobId`: `cj-100`

**Assertions:**
- "Run" is unambiguous for on-demand execution

---

### EVAL-CJ-006: Check copy job run status

**Category:** Tool Selection
**Difficulty:** Easy

**User prompt:**
> Check the status of copy job run `run-555` for copy job `cj-100` in workspace `ws-data`

**Expected tool call(s):**
- Tool: `GetCopyJobRunStatusAsync`
  - `workspaceId`: `ws-data`
  - `copyJobId`: `cj-100`
  - `runId`: `run-555`

**Assertions:**
- Must extract all three IDs

---

## Parameter Extraction

### EVAL-CJ-007: Create with description

**Category:** Parameter Extraction
**Difficulty:** Medium

**User prompt:**
> Create a copy job called "Customer Replication" in workspace `ws-crm` with description "Replicates customer data from SQL Server to Lakehouse nightly"

**Expected tool call(s):**
- Tool: `CreateCopyJobAsync`
  - `workspaceId`: `ws-crm`
  - `displayName`: `Customer Replication`
  - `description`: `Replicates customer data from SQL Server to Lakehouse nightly`

**Assertions:**
- All three parameters extracted correctly

---

### EVAL-CJ-008: Update copy job definition

**Category:** Parameter Extraction
**Difficulty:** Medium

**User prompt:**
> Update the definition of copy job `cj-200` in workspace `ws-data` with this config:
> ```json
> {"source": {"type": "SQL", "table": "dbo.Customers"}, "destination": {"type": "Lakehouse", "table": "customers_raw"}}
> ```

**Expected tool call(s):**
- Tool: `UpdateCopyJobDefinitionAsync`
  - `workspaceId`: `ws-data`
  - `copyJobId`: `cj-200`
  - `definitionJson`: the complete JSON

**Assertions:**
- JSON passed verbatim
- Must use definition update, not metadata update

---

### EVAL-CJ-009: Copy job ID from previous list

**Category:** Parameter Extraction
**Difficulty:** Medium

**User prompt:**
> Run the "Customer Replication" copy job

**Context:**
> Previous `ListCopyJobsAsync` returned:
> ```json
> { "workspaceId": "ws-crm", "copyJobs": [
>   { "id": "cj-201", "displayName": "Customer Replication" },
>   { "id": "cj-202", "displayName": "Product Sync" }
> ]}
> ```

**Expected tool call(s):**
- Tool: `RunCopyJobAsync`
  - `workspaceId`: `ws-crm`
  - `copyJobId`: `cj-201`

**Assertions:**
- Must resolve name to ID from context
- Must infer workspace from prior list

---

### EVAL-CJ-010: Schedule a copy job

**Category:** Parameter Extraction
**Difficulty:** Medium

**User prompt:**
> Schedule copy job `cj-100` in workspace `ws-data` to run every day at midnight

**Expected tool call(s):**
- Tool: `CreateCopyJobScheduleAsync`
  - `workspaceId`: `ws-data`
  - `copyJobId`: `cj-100`
  - `scheduleConfig`: daily, 00:00

**Assertions:**
- "Every day at midnight" maps to daily schedule
- Must use schedule creation, not run

---

### EVAL-CJ-011: List copy job schedules

**Category:** Parameter Extraction
**Difficulty:** Easy

**User prompt:**
> What schedules are set for copy job `cj-100` in workspace `ws-data`?

**Expected tool call(s):**
- Tool: `ListCopyJobSchedulesAsync`
  - `workspaceId`: `ws-data`
  - `copyJobId`: `cj-100`

**Assertions:**
- Must select list schedules, not create schedule

---

## Edge Cases

### EVAL-CJ-012: Copy job vs pipeline — "copy data"

**Category:** Edge Case
**Difficulty:** Hard

**User prompt:**
> I need to copy data from SQL Server to Lakehouse in workspace `ws-data`

**Expected behavior:**
- "Copy data" could map to Copy Job or a pipeline with Copy activity
- Copy Job is the simpler, preferred path for direct source→dest movement
- Model should suggest creating a Copy Job

**Assertions:**
- Should prefer Copy Job over pipeline for simple data movement
- Must not create a Dataflow (no transforms mentioned)

---

### EVAL-CJ-013: Copy job run failed

**Category:** Edge Case
**Difficulty:** Medium

**User prompt:**
> My copy job run failed, what went wrong?

**Context:**
> Working with copy job `cj-100` in workspace `ws-data`, last run was `run-888`

**Expected behavior:**
- Call `GetCopyJobRunStatusAsync` to get error details
- Present error information to user

**Assertions:**
- Must check run status first
- Must not attempt to modify without diagnosing

---

### EVAL-CJ-014: Rename a copy job

**Category:** Edge Case
**Difficulty:** Easy

**User prompt:**
> Rename copy job `cj-100` in workspace `ws-data` to "Customer Sync v2"

**Expected tool call(s):**
- Tool: `UpdateCopyJobAsync`
  - `workspaceId`: `ws-data`
  - `copyJobId`: `cj-100`
  - `displayName`: `Customer Sync v2`

**Assertions:**
- "Rename" maps to metadata update, not definition update

---

### EVAL-CJ-015: Poll after run

**Category:** Edge Case
**Difficulty:** Medium

**User prompt:**
> Is my copy job done yet?

**Context:**
> Previous `RunCopyJobAsync` returned:
> ```json
> { "workspaceId": "ws-data", "copyJobId": "cj-100", "runId": "run-999" }
> ```

**Expected tool call(s):**
- Tool: `GetCopyJobRunStatusAsync`
  - `workspaceId`: `ws-data`
  - `copyJobId`: `cj-100`
  - `runId`: `run-999`

**Assertions:**
- Must extract all IDs from prior run context
- Must not re-run the copy job
