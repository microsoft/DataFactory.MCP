# Data Factory Connections

Everything about discovering, creating, binding, and troubleshooting data source connections.

---

## Decision Tree

```
User mentions a data source
        │
        ▼
  list_connections
  (search all available)
        │
   ┌────┴────┐
   │         │
 Found    Not found
   │         │
   ▼         ▼
 Extract   Does user have
 DatasourceId   interactive access?
 GUID        │
   │    ┌────┴────┐
   │    │         │
   │   Yes        No (API/headless)
   │    │         │
   │    ▼         ▼
   │  create_     create_connection
   │  connection_ (programmatic,
   │  _ui         requires params)
   │  (MCP App,      │
   │   browser flow)  │
   │    │         │
   │    └────┬────┘
   │         │
   │    list_connections again
   │    (get the new DatasourceId)
   │         │
   └────┬────┘
        │
        ▼
  add_connection_to_dataflow
  (bind to dataflow)
        │
        ▼
  execute_query to validate
```

---

## Discovery: list_connections

Returns all connections the authenticated user has permission for.

**When to call:** Any time the user mentions a data source by name, type, server, path, URL, or description. Users rarely use exact source names. Treat any reference to a data source as a trigger.

**How to match:**

| User says | `connectionType` | Match field |
|-----------|------------------|-------------|
| SQL database, SQL Server, "contoso-db" | `SQL` | server + database in parameters |
| SharePoint file, Excel on SharePoint | `Web` | URL containing `.sharepoint.com` |
| Lakehouse, "sales Lakehouse" | `Lakehouse` | workspace + lakehouse name in parameters |
| Blob storage, Azure storage | `AzureBlobs` | account name in parameters |
| Web API, REST endpoint | `Web` | base URL in parameters |
| Warehouse, "analytics warehouse" | `Warehouse` | workspace + warehouse name |
| Dataverse, Dynamics | `Dataverse` | environment URL |

**Fabric-source connections** (Lakehouse, Warehouse, Eventhouse): artifact names are NOT globally unique — they're scoped to their parent workspace. Always require the workspace ID to disambiguate.

**What you extract:** The `DatasourceId` GUID. This is the only value `add_connection_to_dataflow` accepts. NOT the composite format `{"ClusterId":"...","DatasourceId":"..."}` that appears in dataflow definitions.

---

## Inspecting: get_connection

Takes a connection ID, returns full details including connection type, parameters, credential type, and gateway binding.

**When to call:**
- Verifying a connection's parameters before binding (is this the right server/database?)
- Debugging auth failures (what credential type is configured?)
- Checking if a connection is gateway-bound (on-premises sources)

---

## Discovery of Connection Types: list_supported_connection_types

Returns all connection types the server supports, along with:
- Required and optional creation parameters per type
- Supported credential types (OAuth2, Basic, Key, etc.)
- Whether a gateway is required

**When to call:**
- Before `create_connection` — to learn what parameters are needed for the target type
- When the user wants to connect to a source type not in the common mappings table above
- When you're unsure which `connectionType` string to use

**Pattern:**
```
1. list_supported_connection_types
2. Find the matching type
3. Read its required parameters
4. Ask user for any values you don't already have
5. create_connection with the correct params
```

---

## Creating: Two Paths

### Path A: create_connection_ui (Interactive / MCP App)

**What it does:** Renders a React-based UI form in the Claude interface. User fills in credentials and parameters via browser. Connection is created via OAuth/interactive flow.

**When to use:**
- User is in Claude Desktop or claude.ai (has a browser)
- Source requires OAuth2 consent flow (e.g., SharePoint, Dataverse)
- User prefers not to share credentials in chat

**How to call:** Just invoke `create_connection_ui` with no parameters. The UI handles type selection, parameter collection, and credential flow.

**After creation:** Call `list_connections` to find the newly created connection's DatasourceId.

### Path B: create_connection (Programmatic)

**When to use:**
- Headless/API scenarios (no browser)
- Source uses non-interactive auth (service principal, connection string, API key)
- Automation pipelines where UI interaction isn't possible

**Workflow:**
```
1. list_supported_connection_types     → learn required params for the target type
2. Collect parameters from user        → server, database, credentials, etc.
3. create_connection                   → pass connectionType + parameters + credentials
4. list_connections                    → verify creation, extract DatasourceId
```

**Decision matrix:**

| Scenario | Use |
|----------|-----|
| User in Claude Desktop/claude.ai, OAuth source | `create_connection_ui` |
| User in Claude Desktop/claude.ai, non-OAuth source | Either — UI is easier |
| API/headless, any source | `create_connection` |
| User explicitly asks to create programmatically | `create_connection` |

---

## Binding: add_connection_to_dataflow

Attaches connection(s) to a dataflow so queries can authenticate against their data sources.

**Operations:**

| Intent | Call |
|--------|------|
| Add one connection | `connectionIds = "GUID"` |
| Add multiple | `connectionIds = ["GUID1", "GUID2"]` |
| Replace all | `connectionIds = "GUID"` + `clearExisting = true` |
| Clear all | `clearExisting = true` (no connectionIds) |

**Critical: The re-add-after-save bug.** `save_dataflow_definition` may wipe connection bindings. Always re-add connections after saving. This is not optional — it's the most common cause of credential errors on refresh.

**Binding workflow:**
```
1. add_connection_to_dataflow    → bind before save
2. save_dataflow_definition      → save M document (may wipe connections)
3. add_connection_to_dataflow    → re-bind after save
4. execute_query                 → validate (runs a simple query against the source)
```

**Validation:** After binding, run a lightweight `execute_query` against the dataflow to confirm the connection works. If it fails with a credentials error, the binding didn't stick — re-add and retry.

---

## Multi-Source Connection Requirements

When a dataflow combines multiple source types (e.g., Lakehouse + SharePoint):

1. **Every distinct source needs its own connection bound.** One Lakehouse connection + one Web connection = two `add_connection_to_dataflow` calls.
2. **The M document must include `[AllowCombine = true]`** as a section attribute — otherwise the privacy firewall blocks cross-source queries instantly.
3. **Consolidated Lakehouse reads.** All Lakehouse tables must come from a single `Lakehouse.Contents` call in one query. Multiple calls = multiple data source contexts = firewall failure.
4. **Fresh dataflow required.** Never convert a previously-published single-source dataflow to multi-source. Stale connection metadata persists in queryMetadata and causes instant refresh failure.

See `sources/multi-source.md` for the full multi-source M pattern.

---

## Gateways

Gateways bridge on-premises or VNet-isolated data sources to Fabric.

### list_gateways

Returns all gateways the user has permission for: on-premises, on-premises (personal), and VNet gateways.

**When to call:**
- User mentions an on-premises data source (SQL Server on-prem, file share, etc.)
- Connection creation requires a gateway ID
- Troubleshooting connectivity to sources behind a firewall

### get_gateway

Returns details for a specific gateway: type, status, member gateways (for clusters), version.

**When to call:**
- Verifying gateway health before creating a connection that depends on it
- Checking gateway cluster membership

### create_virtualnetwork_gateway

Creates a VNet gateway for accessing Azure-VNet-isolated resources from Fabric.

**When to call:**
- User needs to connect to a data source inside an Azure VNet
- No existing VNet gateway covers the target network

**Note:** On-premises gateway installation is a manual process (download + install on a machine inside the network). Only VNet gateways can be created programmatically.

---

## Troubleshooting

| Symptom | Likely Cause | Fix |
|---------|-------------|-----|
| Credentials error after save | `save_dataflow_definition` wiped bindings | Re-add via `add_connection_to_dataflow` |
| Credentials error, connections look bound | Wrong GUID format (composite vs plain) | Use plain `DatasourceId` GUID only |
| Instant refresh fail (0-3s) on multi-source | Missing connection or missing `AllowCombine` | Bind all connections + add section attribute |
| Connection exists but `list_connections` doesn't show it | User lacks permission on that connection | Check with workspace admin |
| `create_connection` fails with param errors | Wrong parameters for connection type | Call `list_supported_connection_types` first |
| Source behind firewall, connection timeout | Missing or offline gateway | `list_gateways` → check status |
| Stale connections after reverting multi→single source | `save_dataflow_definition` doesn't remove old bindings | Create a new dataflow; no `remove_connection` tool exists |

---

## What This File Does NOT Cover

- **Destination configuration** (DataDestinations annotation, _DataDestination queries) → see `destinations/` files
- **M query authoring** (source query logic, transforms) → see `datafactory-core.md`
- **OAuth token management** (token refresh, expiry) → handled by the platform, not exposed via MCP
- **Connection sharing/permissions** → managed in Fabric portal, not via MCP tools
