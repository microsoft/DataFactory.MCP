---
name: copyjobs-consumption-mcp
description: >
  Inspect and monitor Fabric Copy Jobs using MCP tools for read-only exploration and run
  tracking. Uses data-factory-mcp server tools to list Copy Jobs across workspaces, decode
  Copy Job JSON definitions to examine source and destination configuration and column
  mappings, poll run status (NotStarted, InProgress, Completed, Failed, Cancelled, Deduped),
  and list configured schedules. Provides structured discovery: list workspaces, enumerate
  Copy Jobs, inspect metadata, decode definitions, check run history. Use when the user
  wants to: (1) browse Copy Jobs in a workspace, (2) inspect Copy Job configuration without
  modifying, (3) check Copy Job run status or failure reasons, (4) list Copy Job schedules,
  (5) audit Copy Job definitions, (6) compare source and destination settings across jobs.
  Triggers: "list copy jobs", "copy job run status", "inspect copy job", "copy job schedules",
  "browse copy jobs mcp", "copy job monitoring", "copy job history", "what copy jobs exist".
---

> **Update Check — ONCE PER SESSION (mandatory)**
> The first time this skill is used, check for data-factory-mcp updates.

## Prerequisite Knowledge

Read these companion documents:
- [DATAFACTORY-MCP-CORE.md](../../common/DATAFACTORY-MCP-CORE.md) — Authentication, workspace discovery, MCP patterns

This skill adds: **how to inspect and monitor Copy Jobs** using MCP tools (read-only).

## MCP Tools (this skill)

| Tool | Purpose |
|------|---------|
| `list_copy_jobs` | Discover Copy Jobs in a workspace |
| `get_copy_job` | Read Copy Job metadata |
| `get_copy_job_definition` | Read Copy Job JSON definition |
| `get_copy_job_run_status` | Poll run status |
| `list_copy_job_schedules` | List configured schedules |

## Must / Prefer / Avoid

### MUST
- Authenticate and discover workspace before inspection

### PREFER
- Discovery sequence: workspace → copy jobs → definition → run status → schedules
- Reading definition alongside run status for troubleshooting

### AVOID
- Modifying definitions — use `copyjobs-authoring-mcp` for writes

## Discovery Sequence

```text
1. list_workspaces              → Find target workspace
2. list_copy_jobs               → Enumerate Copy Jobs
3. get_copy_job                 → Read metadata
4. get_copy_job_definition      → Decode source/dest configuration
5. get_copy_job_run_status      → Check last run outcome
6. list_copy_job_schedules      → See configured schedules
```

## Run Status Interpretation

| Status | Meaning |
|--------|---------|
| `Completed` | Copy succeeded |
| `Failed` | Copy failed — check error details for schema mismatch, auth, or connectivity |
| `InProgress` | Still running |
| `Cancelled` | User or system cancelled |
| `NotStarted` | Queued |
| `Deduped` | Skipped — another run already in progress |
