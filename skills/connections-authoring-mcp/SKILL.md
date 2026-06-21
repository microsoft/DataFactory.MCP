---
name: connections-authoring-mcp
description: >
  Discover, create, and manage Fabric connections and gateways using MCP tools. Uses data-
  factory-mcp server to list connections with type filtering, inspect credential and gateway
  bindings, create connections programmatically or via OAuth UI form, discover on-premises
  and VNet gateways, and provision VNet gateways. Covers the full connection lifecycle:
  discovery, type compatibility checking, creation, and gateway binding. Use when the user
  wants to: (1) find existing connections, (2) create a connection via MCP, (3) create an
  OAuth connection interactively, (4) list or inspect gateways, (5) create a VNet gateway,
  (6) troubleshoot credential errors, (7) discover supported connection types.
  Triggers: "list connections", "create connection mcp", "connection oauth", "list gateways",
  "create gateway", "vnet gateway", "connection types", "fabric connection", "gateway mcp",
  "connection credential error".
---

> **Update Check — ONCE PER SESSION (mandatory)**
> The first time this skill is used, check for data-factory-mcp updates.

## Prerequisite Knowledge

Read these companion documents:
- [DATAFACTORY-MCP-CORE.md](../../common/DATAFACTORY-MCP-CORE.md) — Authentication, workspace discovery, MCP patterns
- [CONNECTIONS-CORE.md](../../common/CONNECTIONS-CORE.md) — Connection model, types, binding lifecycle, troubleshooting

This skill adds: **how to manage connections and gateways** using MCP tools.

## MCP Tools (this skill)

| Tool | Purpose |
|------|---------|
| `list_connections` | Find existing connections (filter by type/name) |
| `get_connection` | Inspect connection details (credential type, gateway, params) |
| `list_supported_connection_types` | Discover available types and required parameters |
| `create_connection` | Create connection programmatically |
| `create_connection_ui` | Create connection via interactive OAuth UI form (MCP App) |
| `list_gateways` | Discover on-premises, personal, and VNet gateways |
| `get_gateway` | Inspect gateway details (type, status, cluster members) |
| `create_virtualnetwork_gateway` | Provision VNet gateway for Azure network isolation |

## Must / Prefer / Avoid

### MUST
- Call `list_supported_connection_types` before `create_connection` to learn required parameters
- Use plain `DatasourceId` GUID for binding — NOT composite format
- Re-add connections after `save_dataflow_definition` (save may wipe bindings)

### PREFER
- `create_connection_ui` when user has a browser and source requires OAuth
- `list_connections` before creating — the connection may already exist
- Validating connections with `execute_query` after binding

### AVOID
- Creating connections without checking if one already exists
- Assuming connection parameter formats — always check supported types first

## Decision Tree: Finding or Creating a Connection

```text
User mentions a data source
        │
        ▼
  list_connections (search all available)
        │
   ┌────┴────┐
   │         │
 Found    Not found
   │         │
   ▼         ▼
 Extract   User has browser?
 DatasourceId    │
   │    ┌────┴────┐
   │   Yes        No
   │    │         │
   │    ▼         ▼
   │  create_     create_connection
   │  connection_ (programmatic)
   │  _ui              │
   │    │         │
   │    └────┬────┘
   │         │
   │    list_connections again
   │    (get new DatasourceId)
   │         │
   └────┬────┘
        │
        ▼
  Bind to dataflow / copy job
```

## Interactive vs Programmatic Creation

| Scenario | Tool |
|----------|------|
| User in Claude Desktop/claude.ai, OAuth source | `create_connection_ui` |
| User in Claude Desktop/claude.ai, non-OAuth | Either — UI is easier |
| API/headless, any source | `create_connection` |
| User explicitly asks to create programmatically | `create_connection` |

## Programmatic Creation Workflow

```text
1. list_supported_connection_types   → Learn required params
2. Collect parameters from user      → server, database, credentials
3. create_connection                 → Pass type + params + credentials
4. list_connections                  → Verify, extract DatasourceId
```

## Supplementary References

| Reference | When |
|-----------|------|
| `references/connection-troubleshooting.md` | Credential errors, binding failures, gateway issues |
