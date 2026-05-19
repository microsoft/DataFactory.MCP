# Data Factory MCP — Common Patterns

Cross-cutting prerequisites for all Data Factory MCP skills. Read this before any item-specific skill.

## Authentication

The MCP server requires authentication before any operation. Three paths:

| Tool | When |
|------|------|
| `authenticate_interactive` | User has a browser (Claude Desktop, claude.ai) |
| `authenticate_device_code` | Headless/terminal — user copies a code to browser |
| `authenticate_service_principal` | CI/CD, automation — no user interaction |

**Always check first:** Call `get_authentication_status` before attempting auth. If already authenticated, skip.

**Session lifecycle:**
```text
get_authentication_status → (if not authed) authenticate_* → list_workspaces → operate → logout
```

## Workspace Discovery

`list_workspaces` returns all workspaces the authenticated user can access.

**Pattern:** Every operation requires a `workspaceId`. Discover it first:
```text
list_workspaces → filter by name → extract workspaceId → use in subsequent calls
```

There is no `find_workspace` tool — always list and filter by name.

## Capacity Discovery

`list_capacities` returns available Fabric capacities with SKU, state, and region.

**When to call:**
- Before creating items — verify the target workspace has an active capacity
- Troubleshooting "insufficient capacity" errors

## MCP Tool Invocation Patterns

### Error Handling

| HTTP Status | Meaning | Action |
|-------------|---------|--------|
| 401 | Token expired or invalid | Re-authenticate |
| 403 | Insufficient permissions | Check workspace role (need Contributor+) |
| 429 | Rate limited | Wait and retry (exponential backoff) |
| 404 | Item not found | Verify workspaceId + itemId |

### Long-Running Operations

Some operations (refresh, pipeline run) are async:
1. Trigger: returns immediately with a job/run ID
2. Poll: call status tool repeatedly until terminal state
3. Terminal states: `Completed`, `Failed`, `Cancelled`

### Common Mistake: GUID Formats

- **Workspace/item IDs**: Plain GUIDs (`xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx`)
- **Connection IDs for binding**: Plain `DatasourceId` GUID only — NOT the composite `{"ClusterId":"...","DatasourceId":"..."}` format from definitions

## Must / Prefer / Avoid

### MUST
- Authenticate before any operation
- Use `list_workspaces` to resolve workspace names to IDs
- Check `get_authentication_status` before re-authenticating

### PREFER
- Interactive auth when user has a browser
- Device code for headless environments
- Service principal for automation

### AVOID
- Calling `authenticate_*` without checking status first
- Assuming workspace names are unique — verify by listing
- Hardcoding workspace or item IDs in reusable workflows
