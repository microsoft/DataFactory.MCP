---
name: datafactory-operations-mcp
description: >
  Diagnose and triage operational issues across Fabric Data Factory items using MCP tools.
  Provides cross-item troubleshooting for dataflow refresh failures, pipeline run failures,
  copy job errors, and connection credential problems. Uses data-factory-mcp server to check
  status across item types, validate connection bindings, inspect errors, and identify common
  failure patterns (DataflowNeverPublishedError, credentials, timeout, schema mismatch).
  Covers symptom-based triage with decision trees. Use when the user wants to:
  (1) troubleshoot a failed dataflow refresh, (2) diagnose a pipeline run failure,
  (3) debug credential errors, (4) triage copy job failures, (5) understand why a scheduled
  job didn't run, (6) check health across items. Triggers: "dataflow failed", "pipeline
  failed", "copy job error", "credential error", "refresh failed", "troubleshoot data
  factory", "why did my dataflow fail", "DataflowNeverPublishedError", "diagnostics".
---

> **Update Check — ONCE PER SESSION (mandatory)**
> The first time this skill is used, check for data-factory-mcp updates.

## Prerequisite Knowledge

Read these companion documents:
- [DATAFACTORY-MCP-CORE.md](../../common/DATAFACTORY-MCP-CORE.md) — Authentication, workspace discovery, error handling
- [CONNECTIONS-CORE.md](../../common/CONNECTIONS-CORE.md) — Connection troubleshooting matrix

This skill adds: **cross-item operational triage** for Data Factory.

## Symptom Triage

| Symptom | Likely Cause | Investigation |
|---------|-------------|---------------|
| `DataflowNeverPublishedError` | Default `SkipApplyChanges` on first run | Use `ApplyChangesIfNeeded` |
| `DestinationColumnNotFound` | Manual mappings for new table | Switch to `Kind = "Automatic"` |
| Credentials error on refresh | Connection binding wiped by save | Re-add via `add_connection_to_dataflow` |
| Instant refresh fail (0-3s) | Privacy firewall or unpublished draft | Add `[AllowCombine = true]` and/or `ApplyChangesIfNeeded` |
| FastCopy fails with transforms | Unsupported transform in Fast Copy mode | Remove `[StagingDefinition]` |
| Pipeline shows Failed | Underlying activity failed | Check item-level status (dataflow/copy job) |
| Copy Job schema mismatch | Source schema changed | Inspect definition, update column mappings |
| Scheduled job didn't run | Schedule not configured or paused | `list_*_schedules` to verify |
| Multi-source instant fail | Dirty dataflow or separate Lakehouse.Contents | Create fresh dataflow |
| Stale connections after revert | save_dataflow_definition doesn't remove old bindings | Create new dataflow |

## Triage Decision Tree

```text
Item failed
    │
    ├── Dataflow?
    │     └── refresh_dataflow_status → check error
    │           ├── Credentials → re-bind connections
    │           ├── NeverPublished → ApplyChangesIfNeeded
    │           ├── Timeout → chunk query (see performance patterns)
    │           └── M syntax → inspect definition, fix code
    │
    ├── Pipeline?
    │     └── get_pipeline_run_status → find failed activity
    │           └── Check underlying item status
    │
    └── Copy Job?
          └── get_copy_job_run_status → check error
                ├── Schema mismatch → inspect definition
                ├── Auth failure → check connection
                └── Timeout → check data volume
```

## Connection Health Check

When a credential error occurs across any item type:

```text
1. list_connections                 → Verify connection exists
2. get_connection                  → Check credential type, gateway binding
3. add_connection_to_dataflow      → Re-bind (if dataflow)
4. execute_query                   → Validate with lightweight query
```

## Must / Prefer / Avoid

### MUST
- Check item-specific status before diagnosing (don't guess)
- Start from symptoms, not assumptions
- Verify connection bindings as first step for auth errors

### PREFER
- Cross-referencing run status with definition state
- Checking schedule configuration when "job didn't run"
- Using the symptom triage table above as the starting point

### AVOID
- Modifying items during triage — diagnose first, then fix via authoring skills
- Assuming the pipeline is the root cause when an underlying activity failed
