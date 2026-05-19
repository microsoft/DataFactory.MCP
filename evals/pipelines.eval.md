# Pipeline Tool Evals

Tools under test:
- `ListPipelinesAsync(workspaceId, continuationToken?)`
- `CreatePipelineAsync(workspaceId, displayName, description?, folderId?)`
- `GetPipelineAsync(workspaceId, pipelineId)`
- `GetPipelineDefinitionAsync(workspaceId, pipelineId)`
- `UpdatePipelineAsync(workspaceId, pipelineId, displayName?, description?)`
- `UpdatePipelineDefinitionAsync(workspaceId, pipelineId, definitionJson)`
- `RunPipelineAsync(workspaceId, pipelineId)`
- `GetPipelineRunStatusAsync(workspaceId, pipelineId, runId)`
- `CreatePipelineScheduleAsync(workspaceId, pipelineId, scheduleConfig)`
- `ListPipelineSchedulesAsync(workspaceId, pipelineId)`

---

## Tool Selection

### EVAL-PL-001: List pipelines

**Category:** Tool Selection
**Difficulty:** Easy

**User prompt:**
> Show me the pipelines in workspace `ws-123`

**Expected tool call(s):**
- Tool: `ListPipelinesAsync`
  - `workspaceId`: `ws-123`

**Assertions:**
- Must select pipeline list, not dataflow list
- "Pipelines" is unambiguous

---

### EVAL-PL-002: Create a pipeline

**Category:** Tool Selection
**Difficulty:** Easy

**User prompt:**
> Create a new pipeline called "Nightly Orchestration" in workspace `ws-123`

**Expected tool call(s):**
- Tool: `CreatePipelineAsync`
  - `workspaceId`: `ws-123`
  - `displayName`: `Nightly Orchestration`

**Assertions:**
- Must use create pipeline, not create dataflow
- "Pipeline" and "orchestration" are clear signals

---

### EVAL-PL-003: Get pipeline metadata

**Category:** Tool Selection
**Difficulty:** Easy

**User prompt:**
> Get details for pipeline `pl-456` in workspace `ws-123`

**Expected tool call(s):**
- Tool: `GetPipelineAsync`
  - `workspaceId`: `ws-123`
  - `pipelineId`: `pl-456`

**Assertions:**
- "Details" / "metadata" maps to `GetPipelineAsync`
- Must not call `GetPipelineDefinitionAsync` (that's for the JSON config)

---

### EVAL-PL-004: Get pipeline definition (JSON config)

**Category:** Tool Selection
**Difficulty:** Medium

**User prompt:**
> Show me the JSON definition of pipeline `pl-456` in workspace `ws-123`

**Expected tool call(s):**
- Tool: `GetPipelineDefinitionAsync`
  - `workspaceId`: `ws-123`
  - `pipelineId`: `pl-456`

**Assertions:**
- "JSON definition" / "configuration" maps to definition tool
- Must not call `GetPipelineAsync` (that's metadata only)

---

### EVAL-PL-005: Update pipeline name

**Category:** Tool Selection
**Difficulty:** Easy

**User prompt:**
> Rename pipeline `pl-456` in workspace `ws-123` to "Weekly Orchestration"

**Expected tool call(s):**
- Tool: `UpdatePipelineAsync`
  - `workspaceId`: `ws-123`
  - `pipelineId`: `pl-456`
  - `displayName`: `Weekly Orchestration`

**Assertions:**
- "Rename" maps to metadata update, not definition update

---

### EVAL-PL-006: Update pipeline definition

**Category:** Tool Selection
**Difficulty:** Medium

**User prompt:**
> Update the definition of pipeline `pl-456` in workspace `ws-123` with this JSON:
> ```json
> {"activities": [{"name": "CopyData", "type": "Copy"}]}
> ```

**Expected tool call(s):**
- Tool: `UpdatePipelineDefinitionAsync`
  - `workspaceId`: `ws-123`
  - `pipelineId`: `pl-456`
  - `definitionJson`: `{"activities": [{"name": "CopyData", "type": "Copy"}]}`

**Assertions:**
- Must use definition update, not metadata update
- Must pass the JSON verbatim

---

## Parameter Extraction

### EVAL-PL-007: Create with description and folder

**Category:** Parameter Extraction
**Difficulty:** Medium

**User prompt:**
> Create pipeline "Export to Blob" in workspace `ws-export` with description "Exports daily data to Azure Blob Storage" in folder `folder-exports`

**Expected tool call(s):**
- Tool: `CreatePipelineAsync`
  - `workspaceId`: `ws-export`
  - `displayName`: `Export to Blob`
  - `description`: `Exports daily data to Azure Blob Storage`
  - `folderId`: `folder-exports`

**Assertions:**
- All four parameters extracted correctly

---

### EVAL-PL-008: Update both name and description

**Category:** Parameter Extraction
**Difficulty:** Medium

**User prompt:**
> Update pipeline `pl-789` in workspace `ws-ops`: change the name to "Daily ETL v2" and set description to "Updated pipeline with incremental refresh"

**Expected tool call(s):**
- Tool: `UpdatePipelineAsync`
  - `workspaceId`: `ws-ops`
  - `pipelineId`: `pl-789`
  - `displayName`: `Daily ETL v2`
  - `description`: `Updated pipeline with incremental refresh`

**Assertions:**
- Both displayName and description must be provided
- Must use the exact values from the prompt

---

### EVAL-PL-009: Pipeline ID from previous list

**Category:** Parameter Extraction
**Difficulty:** Medium

**User prompt:**
> Get the definition of the "Nightly Sync" pipeline

**Context:**
> Previous `ListPipelinesAsync` returned:
> ```json
> { "workspaceId": "ws-main", "pipelines": [
>   { "id": "pl-001", "displayName": "Nightly Sync" },
>   { "id": "pl-002", "displayName": "Weekly Report" }
> ]}
> ```

**Expected tool call(s):**
- Tool: `GetPipelineDefinitionAsync`
  - `workspaceId`: `ws-main`
  - `pipelineId`: `pl-001`

**Assertions:**
- Must resolve "Nightly Sync" to `pl-001` from context
- Must infer the workspace from the prior list call

---

### EVAL-PL-010: Complex pipeline definition JSON

**Category:** Parameter Extraction
**Difficulty:** Hard

**User prompt:**
> Update pipeline `pl-100` in workspace `ws-100` with this definition:
> ```json
> {
>   "activities": [
>     {"name": "LookupConfig", "type": "Lookup", "dataset": "ConfigDS"},
>     {"name": "ForEachRegion", "type": "ForEach", "items": "@activity('LookupConfig').output.value"},
>     {"name": "CopyRegionData", "type": "Copy", "source": "SQLSource", "sink": "LakehouseSink"}
>   ]
> }
> ```

**Expected tool call(s):**
- Tool: `UpdatePipelineDefinitionAsync`
  - `workspaceId`: `ws-100`
  - `pipelineId`: `pl-100`
  - `definitionJson`: the complete JSON (preserved exactly)

**Assertions:**
- JSON must be passed verbatim, not reformatted or simplified
- Must not strip or modify the `@activity(...)` expression

---

### EVAL-PL-011: Pagination — next page of pipelines

**Category:** Parameter Extraction
**Difficulty:** Medium

**User prompt:**
> Load more pipelines

**Context:**
> Previous `ListPipelinesAsync` returned:
> ```json
> { "workspaceId": "ws-main", "pipelineCount": 25, "continuationToken": "ct-next", "hasMoreResults": true }
> ```

**Expected tool call(s):**
- Tool: `ListPipelinesAsync`
  - `workspaceId`: `ws-main`
  - `continuationToken`: `ct-next`

**Assertions:**
- Must extract both workspace ID and continuation token from context
- Must not list without the token (would repeat page 1)

---

## Edge Cases

### EVAL-PL-012: Get metadata vs get definition

**Category:** Edge Case
**Difficulty:** Medium

**User prompt:**
> Tell me about pipeline `pl-456` in workspace `ws-123`

**Expected behavior:**
- "Tell me about" is ambiguous — could be metadata or definition
- `GetPipelineAsync` (metadata) is the safer default
- Could call both and combine the information

**Assertions:**
- Must call at least `GetPipelineAsync`
- Calling both GetPipeline and GetPipelineDefinition is acceptable

---

### EVAL-PL-013: Update with no changes

**Category:** Edge Case
**Difficulty:** Medium

**User prompt:**
> Update pipeline `pl-789` in workspace `ws-123`

**Expected behavior:**
- No displayName or description was provided
- Tool will return a validation error: "At least one of displayName or description must be provided"
- Model should ask what the user wants to change

**Assertions:**
- Must not call `UpdatePipelineAsync` without at least one field
- Must ask the user what they want to update

---

### EVAL-PL-014: Invalid JSON in definition update

**Category:** Edge Case
**Difficulty:** Medium

**User prompt:**
> Update the definition of pipeline `pl-456` in workspace `ws-123` with: `{broken json`

**Expected behavior:**
- The tool validates JSON format and returns a validation error
- Model should relay the error and help fix the JSON

**Assertions:**
- Must attempt the tool call (tool handles validation)
- Must explain the JSON parsing error from the response

---

### EVAL-PL-015: Pipeline vs dataflow — "data pipeline"

**Category:** Edge Case
**Difficulty:** Hard

**User prompt:**
> List all data pipelines in workspace `ws-001`

**Expected tool call(s):**
- Tool: `ListPipelinesAsync`
  - `workspaceId`: `ws-001`

**Assertions:**
- "Data pipeline" should map to Pipeline, not Dataflow
- In Fabric terminology, "pipeline" specifically refers to the orchestration artifact
- If unsure, calling both `ListPipelinesAsync` and `ListDataflowsAsync` is acceptable

---

## Pipeline Run & Schedule (PR #94+)

### EVAL-PL-016: Run a pipeline on demand

**Category:** Tool Selection
**Difficulty:** Easy

**User prompt:**
> Run pipeline `pl-456` in workspace `ws-123`

**Expected tool call(s):**
- Tool: `RunPipelineAsync`
  - `workspaceId`: `ws-123`
  - `pipelineId`: `pl-456`

**Assertions:**
- Must select run tool, not update or create
- "Run" is unambiguous for on-demand execution

---

### EVAL-PL-017: Check pipeline run status

**Category:** Tool Selection
**Difficulty:** Easy

**User prompt:**
> Check the status of pipeline run `run-789` for pipeline `pl-456` in workspace `ws-123`

**Expected tool call(s):**
- Tool: `GetPipelineRunStatusAsync`
  - `workspaceId`: `ws-123`
  - `pipelineId`: `pl-456`
  - `runId`: `run-789`

**Assertions:**
- Must select run status tool
- Must extract all three IDs from prompt

---

### EVAL-PL-018: Schedule a pipeline

**Category:** Tool Selection
**Difficulty:** Medium

**User prompt:**
> Set up a daily schedule for pipeline `pl-456` in workspace `ws-123`

**Expected tool call(s):**
- Tool: `CreatePipelineScheduleAsync`
  - `workspaceId`: `ws-123`
  - `pipelineId`: `pl-456`
  - `scheduleConfig`: contains daily frequency

**Assertions:**
- Must select schedule creation tool
- "Daily schedule" maps to schedule, not run

---

### EVAL-PL-019: List pipeline schedules

**Category:** Tool Selection
**Difficulty:** Easy

**User prompt:**
> What schedules are configured for pipeline `pl-456` in workspace `ws-123`?

**Expected tool call(s):**
- Tool: `ListPipelineSchedulesAsync`
  - `workspaceId`: `ws-123`
  - `pipelineId`: `pl-456`

**Assertions:**
- "Schedules configured" maps to list schedules
- Must not call run status

---

### EVAL-PL-020: Run then poll status

**Category:** Parameter Extraction
**Difficulty:** Medium

**User prompt:**
> Check if my pipeline run finished

**Context:**
> Previous `RunPipelineAsync` returned:
> ```json
> { "workspaceId": "ws-main", "pipelineId": "pl-001", "runId": "run-abc-123" }
> ```

**Expected tool call(s):**
- Tool: `GetPipelineRunStatusAsync`
  - `workspaceId`: `ws-main`
  - `pipelineId`: `pl-001`
  - `runId`: `run-abc-123`

**Assertions:**
- Must extract all three IDs from prior run context
- Must not re-run the pipeline

---

### EVAL-PL-021: Run pipeline by name from list

**Category:** Parameter Extraction
**Difficulty:** Medium

**User prompt:**
> Run the "Nightly Sync" pipeline

**Context:**
> Previous `ListPipelinesAsync` returned:
> ```json
> { "workspaceId": "ws-main", "pipelines": [
>   { "id": "pl-001", "displayName": "Nightly Sync" },
>   { "id": "pl-002", "displayName": "Weekly Report" }
> ]}
> ```

**Expected tool call(s):**
- Tool: `RunPipelineAsync`
  - `workspaceId`: `ws-main`
  - `pipelineId`: `pl-001`

**Assertions:**
- Must resolve name to ID from context
- Must infer workspace from prior list

---

### EVAL-PL-022: Schedule vs run disambiguation

**Category:** Edge Case
**Difficulty:** Hard

**User prompt:**
> I want this pipeline to run every Monday at 6am

**Context:**
> Working with pipeline `pl-456` in workspace `ws-123`

**Expected tool call(s):**
- Tool: `CreatePipelineScheduleAsync`
  - `workspaceId`: `ws-123`
  - `pipelineId`: `pl-456`
  - `scheduleConfig`: weekly, Monday, 6:00 AM

**Assertions:**
- "Run every Monday" is a schedule request, not an on-demand run
- Must use schedule creation, not run tool

---

### EVAL-PL-023: Pipeline failed — what happened?

**Category:** Edge Case
**Difficulty:** Hard

**User prompt:**
> My pipeline run failed, what went wrong?

**Context:**
> Working with pipeline `pl-456` in workspace `ws-123`, last run was `run-999`

**Expected behavior:**
- Call `GetPipelineRunStatusAsync` to get error details
- If the failure is from a Dataflow activity, suggest checking `RefreshDataflowStatus`

**Assertions:**
- Must check run status first
- Must not attempt to fix without diagnosing
