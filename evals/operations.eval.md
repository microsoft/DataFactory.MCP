# Operations / Cross-Item Triage Evals

Tests the model's ability to diagnose failures across Data Factory item types and route to the correct status/inspection tools.

Tools referenced:
- `RefreshDataflowStatus(workspaceId, dataflowId, jobInstanceId)`
- `GetPipelineRunStatusAsync(workspaceId, pipelineId, runId)`
- `GetCopyJobRunStatusAsync(workspaceId, copyJobId, runId)`
- `AddConnectionToDataflowAsync(workspaceId, dataflowId, connectionIds?, clearExisting?)`
- `ListConnectionsAsync(workspaceId?)`
- `GetConnectionAsync(connectionId)`
- `get_dataflow_definition(workspaceId, dataflowId)`

---

## Tool Selection

### EVAL-OPS-001: Dataflow refresh failed — diagnose

**Category:** Tool Selection
**Difficulty:** Medium

**User prompt:**
> My dataflow refresh failed. The dataflow is `df-100` in workspace `ws-prod` and the job ID is `job-456`

**Expected tool call(s):**
- Tool: `RefreshDataflowStatus`
  - `workspaceId`: `ws-prod`
  - `dataflowId`: `df-100`
  - `jobInstanceId`: `job-456`

**Assertions:**
- Must check dataflow refresh status, not pipeline status
- Must extract all three IDs

---

### EVAL-OPS-002: Pipeline failed — drill into activity

**Category:** Tool Selection
**Difficulty:** Medium

**User prompt:**
> Pipeline `pl-200` in workspace `ws-prod` failed on run `run-abc`. The failed activity is a Dataflow activity. How do I find the root cause?

**Expected behavior:**
- Call `GetPipelineRunStatusAsync` to get activity-level details
- Suggest checking `RefreshDataflowStatus` for the underlying dataflow error

**Assertions:**
- Must check pipeline run status first
- Must advise looking at dataflow refresh status for root cause
- Pipeline failure on a Dataflow activity → root cause is always in the dataflow

---

### EVAL-OPS-003: Credential error — re-bind connection

**Category:** Tool Selection
**Difficulty:** Medium

**User prompt:**
> I'm getting a credentials error when refreshing dataflow `df-100` in workspace `ws-prod`. The connection ID is `conn-xyz`.

**Expected tool call(s):**
- Tool: `AddConnectionToDataflowAsync`
  - `workspaceId`: `ws-prod`
  - `dataflowId`: `df-100`
  - `connectionIds`: `conn-xyz`

**Assertions:**
- Credential errors after save → re-bind connection
- Must use add connection tool, not create connection

---

### EVAL-OPS-004: DataflowNeverPublishedError

**Category:** Tool Selection
**Difficulty:** Hard

**User prompt:**
> I created a dataflow via MCP and tried to refresh it but got `DataflowNeverPublishedError`

**Context:**
> Dataflow `df-new` in workspace `ws-dev`

**Expected behavior:**
- Explain that API-created dataflows need `executeOption = "ApplyChangesIfNeeded"` on first refresh
- Suggest calling `RefreshDataflowBackground` with `executeOption = "ApplyChangesIfNeeded"`

**Assertions:**
- Must identify the fix: `ApplyChangesIfNeeded`
- Must not suggest deleting and recreating the dataflow

---

## Parameter Extraction

### EVAL-OPS-005: Diagnose from vague error

**Category:** Parameter Extraction
**Difficulty:** Hard

**User prompt:**
> Something's wrong with my dataflow — it fails instantly, takes about 2 seconds

**Context:**
> Working with dataflow `df-multi` in workspace `ws-analytics`, last refresh job was `job-999`

**Expected tool call(s):**
- Tool: `RefreshDataflowStatus`
  - `workspaceId`: `ws-analytics`
  - `dataflowId`: `df-multi`
  - `jobInstanceId`: `job-999`

**Assertions:**
- "Fails instantly" (0-3s) → likely privacy firewall or unpublished draft
- Must check status before suggesting a fix
- After seeing error, should suggest: AllowCombine (if multi-source) or ApplyChangesIfNeeded (if first refresh)

---

### EVAL-OPS-006: Connection health check

**Category:** Parameter Extraction
**Difficulty:** Medium

**User prompt:**
> Check if connection `conn-sql-01` is still working

**Expected tool call(s):**
- Tool: `GetConnectionAsync`
  - `connectionId`: `conn-sql-01`

**Assertions:**
- Must inspect the connection details (credential type, gateway status)
- Must not try to create or modify the connection

---

## Edge Cases

### EVAL-OPS-007: Ambiguous failure — which item type?

**Category:** Edge Case
**Difficulty:** Hard

**User prompt:**
> Something in workspace `ws-prod` failed last night. I'm not sure if it was a dataflow, pipeline, or copy job.

**Expected behavior:**
- Need to discover which items exist and check their status
- Suggest listing all three item types: `ListDataflowsAsync`, `ListPipelinesAsync`, `ListCopyJobsAsync`
- Then check run/refresh status for each

**Assertions:**
- Must not guess the item type — discover first
- Acceptable to call all three list tools in parallel

---

### EVAL-OPS-008: Scheduled job didn't run

**Category:** Edge Case
**Difficulty:** Medium

**User prompt:**
> My pipeline was supposed to run at 6am but it didn't execute

**Context:**
> Pipeline `pl-nightly` in workspace `ws-prod`

**Expected behavior:**
- Check if a schedule exists: `ListPipelineSchedulesAsync`
- Check if a run was created: `GetPipelineRunStatusAsync` or list recent runs

**Assertions:**
- Must verify schedule exists before assuming it's a platform issue
- "Didn't run" could be: no schedule configured, schedule paused, or run was Deduped
