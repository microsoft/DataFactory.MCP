# Workspace & Capacity Tool Evals

Tools under test:
- `ListWorkspacesAsync(roles?, continuationToken?, preferWorkspaceSpecificEndpoints?)`
- `ListCapacitiesAsync(continuationToken?)`

---

## Tool Selection

### EVAL-WC-001: List workspaces

**Category:** Tool Selection
**Difficulty:** Easy

**User prompt:**
> Show me my workspaces

**Expected tool call(s):**
- Tool: `ListWorkspacesAsync`
  - No parameters (all optional)

**Assertions:**
- Must select workspace tool, not capacity tool
- Should not pass any optional filters unless user specified them

---

### EVAL-WC-002: List capacities

**Category:** Tool Selection
**Difficulty:** Easy

**User prompt:**
> What Fabric capacities do I have access to?

**Expected tool call(s):**
- Tool: `ListCapacitiesAsync`
  - No parameters

**Assertions:**
- Must select capacity tool, not workspace tool
- "Fabric capacities" maps unambiguously to this tool

---

### EVAL-WC-003: Workspace vs capacity disambiguation

**Category:** Tool Selection
**Difficulty:** Medium

**User prompt:**
> What resources do I have in Fabric?

**Expected behavior:**
- Could call `ListWorkspacesAsync` and/or `ListCapacitiesAsync`
- Both are reasonable interpretations

**Assertions:**
- Must call at least one of the two tools
- Calling both is acceptable and arguably better

---

## Parameter Extraction

### EVAL-WC-004: Workspaces filtered by role

**Category:** Parameter Extraction
**Difficulty:** Medium

**User prompt:**
> Show me workspaces where I'm an admin or contributor

**Expected tool call(s):**
- Tool: `ListWorkspacesAsync`
  - `roles`: `Admin,Contributor`

**Assertions:**
- Must correctly map "admin" to "Admin" and "contributor" to "Contributor"
- Must use comma-separated format in a single string
- Must not pass roles as an array (parameter is a single string)

---

### EVAL-WC-005: Workspaces with API endpoints

**Category:** Parameter Extraction
**Difficulty:** Medium

**User prompt:**
> List all workspaces and include their API endpoints

**Expected tool call(s):**
- Tool: `ListWorkspacesAsync`
  - `preferWorkspaceSpecificEndpoints`: `true`

**Assertions:**
- Must set the endpoints flag to true
- Must not filter by roles (user asked for "all")

---

### EVAL-WC-006: Pagination — second page of capacities

**Category:** Parameter Extraction
**Difficulty:** Medium

**User prompt:**
> Load more capacities

**Context:**
> Previous `ListCapacitiesAsync` returned:
> ```json
> { "totalCount": 25, "continuationToken": "eyJza2lwIjoyNX0=", "hasMoreResults": true }
> ```

**Expected tool call(s):**
- Tool: `ListCapacitiesAsync`
  - `continuationToken`: `eyJza2lwIjoyNX0=`

**Assertions:**
- Must extract the continuation token from previous response
- Must not call without the token (would just return the first page again)

---

## Edge Cases

### EVAL-WC-007: Role name variations

**Category:** Edge Case
**Difficulty:** Medium

**User prompt:**
> Show workspaces I can view

**Expected tool call(s):**
- Tool: `ListWorkspacesAsync`
  - `roles`: `Viewer`

**Assertions:**
- Must map "can view" to the `Viewer` role
- Must not use a literal string like "view" or "read"

---

### EVAL-WC-008: All roles requested

**Category:** Edge Case
**Difficulty:** Easy

**User prompt:**
> Give me all workspaces regardless of my role

**Expected tool call(s):**
- Tool: `ListWorkspacesAsync`
  - `roles`: `null` / omitted

**Assertions:**
- "All workspaces regardless of role" means no role filter
- Must not pass `Admin,Member,Contributor,Viewer` — just omit the parameter

---

### EVAL-WC-009: Workspace search by name — not supported

**Category:** Edge Case
**Difficulty:** Hard

**User prompt:**
> Find the workspace called "Sales Analytics"

**Expected behavior:**
- Call `ListWorkspacesAsync` with no name filter (no such parameter exists)
- Filter the results client-side to find matching workspace
- Present the match to the user

**Assertions:**
- Must not hallucinate a `name` or `filter` parameter
- Must call the list tool and scan results
- Must inform user if no match is found
