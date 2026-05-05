# Connections & Gateways — Core Reference

Shared reference for connection model, gateway types, and binding lifecycle. Used by `connections-authoring-mcp` and referenced by dataflow/pipeline/copy job skills.

## Connection Model

A Fabric connection represents authenticated access to a data source.

| Field | Description |
|-------|-------------|
| `connectionId` | Unique identifier |
| `DatasourceId` | The GUID used for binding (plain format, not composite) |
| `connectionType` | Source type: `SQL`, `Web`, `Lakehouse`, `Warehouse`, `AzureBlobs`, `Dataverse`, etc. |
| `credentialType` | Auth method: `OAuth2`, `ServicePrincipal`, `Key`, `Anonymous`, `Windows` |
| `gatewayId` | Gateway binding (null for cloud-only sources) |

## Connection Type Mapping

| User Says | `connectionType` | Match Field |
|-----------|------------------|-------------|
| SQL database, SQL Server | `SQL` | server + database in parameters |
| SharePoint file, Excel on SharePoint | `Web` | URL containing `.sharepoint.com` |
| Lakehouse | `Lakehouse` | workspace + lakehouse name |
| Blob storage, Azure storage | `AzureBlobs` | account name |
| Web API, REST endpoint | `Web` | base URL |
| Warehouse | `Warehouse` | workspace + warehouse name |
| Dataverse, Dynamics | `Dataverse` | environment URL |

**Fabric-source connections** (Lakehouse, Warehouse, Eventhouse): artifact names are NOT globally unique — scoped to parent workspace. Always require workspace ID to disambiguate.

## Binding Lifecycle

Connections must be bound to items (dataflows) before queries can authenticate:

```text
1. add_connection_to_dataflow    → bind before save
2. save_dataflow_definition      → save M document (MAY WIPE connections)
3. add_connection_to_dataflow    → re-bind after save
4. execute_query                 → validate binding works
```

**Critical:** `save_dataflow_definition` may wipe connection bindings. Always re-add after save. This is the #1 cause of credential errors.

### Binding Operations

| Intent | Parameters |
|--------|-----------|
| Add one connection | `connectionIds = "GUID"` |
| Add multiple | `connectionIds = ["GUID1", "GUID2"]` |
| Replace all | `connectionIds = "GUID"` + `clearExisting = true` |
| Clear all | `clearExisting = true` (no connectionIds) |

## Gateway Types

| Type | Created Via | Purpose |
|------|-----------|---------|
| On-premises | Manual install (download agent) | Access on-prem sources behind firewall |
| Personal | Manual install (single user) | Personal development gateway |
| VNet | `create_virtualnetwork_gateway` API | Access Azure VNet-isolated resources |

Only VNet gateways can be created programmatically.

## Multi-Source Requirements

When combining multiple source types (e.g., Lakehouse + SharePoint):

1. **Every source needs its own connection bound** — separate `add_connection_to_dataflow` calls
2. **M document needs `[AllowCombine = true]`** section attribute — otherwise privacy firewall blocks cross-source queries
3. **Consolidated Lakehouse reads** — single `Lakehouse.Contents` call, not one per table
4. **Fresh dataflow required** — never convert a published single-source to multi-source

## Troubleshooting Matrix

| Symptom | Likely Cause | Fix |
|---------|-------------|-----|
| Credentials error after save | `save_dataflow_definition` wiped bindings | Re-add via `add_connection_to_dataflow` |
| Credentials error, bindings look correct | Wrong GUID format (composite vs plain) | Use plain `DatasourceId` only |
| Instant refresh fail (0-3s) on multi-source | Missing connection or `AllowCombine` | Bind all + add section attribute |
| `list_connections` doesn't show known connection | User lacks permission | Check with workspace admin |
| `create_connection` param errors | Wrong parameters for type | Call `list_supported_connection_types` first |
| Connection timeout on-prem source | Missing/offline gateway | `list_gateways` → check status |
| Stale connections after revert | `save_dataflow_definition` doesn't remove old bindings | Create new dataflow — no `remove_connection` tool |
