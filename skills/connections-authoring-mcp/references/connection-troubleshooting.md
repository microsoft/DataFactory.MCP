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
