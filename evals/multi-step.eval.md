# Multi-Step Orchestration Evals

These scenarios test the model's ability to chain multiple tools in the correct order to accomplish a complex task. Each scenario involves 2+ tool calls with data flowing between them.

---

### EVAL-MS-001: End-to-end dataflow creation and setup

**Category:** Multi-Step Orchestration
**Difficulty:** Hard

**User prompt:**
> In workspace `ws-001`, create a new dataflow called "Customer ETL", connect it to my SQL connection `conn-sql-prod`, then add a query called "GetCustomers" with this M code:
> ```
> let Source = Sql.Database("prod-server", "CRM"), Customers = Source{[Schema="dbo",Item="Customers"]}[Data] in Customers
> ```

**Expected tool call sequence:**
1. Tool: `CreateDataflowAsync`
   - `workspaceId`: `ws-001`
   - `displayName`: `Customer ETL`
   - Returns: `{ "dataflowId": "df-new-123" }`

2. Tool: `AddConnectionToDataflowAsync`
   - `workspaceId`: `ws-001`
   - `dataflowId`: `df-new-123` (from step 1)
   - `connectionIds`: `conn-sql-prod`

3. Tool: `AddOrUpdateQueryInDataflowAsync`
   - `workspaceId`: `ws-001`
   - `dataflowId`: `df-new-123` (from step 1)
   - `queryName`: `GetCustomers`
   - `mCode`: the M code from the prompt

**Assertions:**
- Must execute in order: create → connect → add query
- Must use the dataflow ID returned from step 1 in steps 2 and 3
- Must not skip any step

---

### EVAL-MS-002: Authenticate then list resources

**Category:** Multi-Step Orchestration
**Difficulty:** Medium

**User prompt:**
> Sign in and show me my workspaces

**Expected tool call sequence:**
1. Tool: `AuthenticateInteractiveAsync`
2. Tool: `ListWorkspacesAsync` (after successful auth)

**Assertions:**
- Must authenticate before listing
- Must not call `ListWorkspacesAsync` before auth completes
- If auth fails, must not proceed to list workspaces

---

### EVAL-MS-003: Find and inspect a specific dataflow

**Category:** Multi-Step Orchestration
**Difficulty:** Medium

**User prompt:**
> Find the "Sales ETL" dataflow in workspace `ws-sales` and show me its M code

**Expected tool call sequence:**
1. Tool: `ListDataflowsAsync`
   - `workspaceId`: `ws-sales`
   - Returns: list including `{ "id": "df-sales-001", "displayName": "Sales ETL" }`

2. Tool: `get_dataflow_definition`
   - `workspaceId`: `ws-sales`
   - `dataflowId`: `df-sales-001` (matched by name from step 1)

**Assertions:**
- Must list first to resolve name to ID
- Must correctly match "Sales ETL" from the list results
- Must pass the matched ID to the definition tool

---

### EVAL-MS-004: Create pipeline then set its definition

**Category:** Multi-Step Orchestration
**Difficulty:** Medium

**User prompt:**
> Create a pipeline called "Daily Export" in workspace `ws-ops`, then set its definition to:
> ```json
> {"activities": [{"name": "ExportToBlob", "type": "Copy"}]}
> ```

**Expected tool call sequence:**
1. Tool: `CreatePipelineAsync`
   - `workspaceId`: `ws-ops`
   - `displayName`: `Daily Export`
   - Returns: `{ "pipelineId": "pl-new-123" }`

2. Tool: `UpdatePipelineDefinitionAsync`
   - `workspaceId`: `ws-ops`
   - `pipelineId`: `pl-new-123` (from step 1)
   - `definitionJson`: the JSON from the prompt

**Assertions:**
- Must create before updating definition
- Must use the pipeline ID from the create response

---

### EVAL-MS-005: Refresh dataflow and poll for completion

**Category:** Multi-Step Orchestration
**Difficulty:** Medium

**User prompt:**
> Refresh dataflow `df-789` in workspace `ws-123` and tell me when it's done

**Expected tool call sequence:**
1. Tool: `RefreshDataflowBackground`
   - `workspaceId`: `ws-123`
   - `dataflowId`: `df-789`
   - Returns: `{ "taskInfo": { "jobInstanceId": "job-abc" } }`

2. (After receiving notification or waiting) Tool: `RefreshDataflowStatus`
   - `workspaceId`: `ws-123`
   - `dataflowId`: `df-789`
   - `jobInstanceId`: `job-abc`

**Assertions:**
- Must start the refresh first
- Must use status check to verify completion
- Must extract jobInstanceId from the refresh response

---

### EVAL-MS-006: Create VNet gateway, then create connection through it

**Category:** Multi-Step Orchestration
**Difficulty:** Hard

**User prompt:**
> Set up a VNet gateway called "Analytics GW" on capacity `cap-001` in subscription `sub-001`, resource group `rg-net`, VNet `vnet-analytics`, subnet `data-subnet`. Then create a SQL connection called "Analytics DB" through that gateway to server `10.0.1.5` database `AnalyticsDB` with Windows auth.

**Expected tool call sequence:**
1. Tool: `create_virtualnetwork_gateway`
   - `displayName`: `Analytics GW`
   - `capacityId`: `cap-001`
   - `subscriptionId`: `sub-001`
   - `resourceGroupName`: `rg-net`
   - `virtualNetworkName`: `vnet-analytics`
   - `subnetName`: `data-subnet`
   - Returns: `{ "gateway": { "id": "gw-new-456" } }`

2. Tool: `CreateConnectionAsync`
   - `connectionName`: `Analytics DB`
   - `connectionType`: `SQL`
   - `connectionParameters`: `{"server":"10.0.1.5","database":"AnalyticsDB"}`
   - `credentialType`: `Windows`
   - `connectivityType`: `VirtualNetworkGateway`
   - `gatewayId`: `gw-new-456` (from step 1)

**Assertions:**
- Must create gateway before creating connection
- Must use the gateway ID from step 1 in the connection's `gatewayId`
- Must set `connectivityType` to `VirtualNetworkGateway`

---

### EVAL-MS-007: Discover workspace → list dataflows → execute query

**Category:** Multi-Step Orchestration
**Difficulty:** Hard

**User prompt:**
> Find my workspaces where I'm an admin, then list the dataflows in the first workspace, and run this query on the first dataflow:
> ```
> let Source = 1 in Source
> ```

**Expected tool call sequence:**
1. Tool: `ListWorkspacesAsync`
   - `roles`: `Admin`
   - Returns: `{ "workspaces": [{ "id": "ws-first", "displayName": "Admin WS" }] }`

2. Tool: `ListDataflowsAsync`
   - `workspaceId`: `ws-first` (from step 1)
   - Returns: `{ "dataflows": [{ "id": "df-first", "displayName": "Main ETL" }] }`

3. Tool: `ExecuteQueryAsync`
   - `workspaceId`: `ws-first` (from step 1)
   - `dataflowId`: `df-first` (from step 2)
   - `queryName`: any reasonable name
   - `customMashupDocument`: `let Source = 1 in Source`

**Assertions:**
- Must chain all three calls in order
- Must use "first" result from each step to feed the next
- Must not skip any intermediate step

---

### EVAL-MS-008: Check auth, authenticate if needed, then operate

**Category:** Multi-Step Orchestration
**Difficulty:** Medium

**User prompt:**
> List my gateways

**Context:**
> Model is unsure whether the user is authenticated.

**Expected tool call sequence:**
1. Tool: `GetAuthenticationStatus` — check first
   - Returns: `"Not authenticated. Please sign in."`

2. Inform user that authentication is needed first
   - User says: "Ok, sign me in"

3. Tool: `AuthenticateInteractiveAsync`
   - Returns: success

4. Tool: `ListGatewaysAsync`

**Assertions:**
- Must check auth or attempt the call before prompting for auth
- Must not proceed to list gateways without authentication
- Recovery flow (auth → retry) must work smoothly

---

### EVAL-MS-009: Get dataflow definition, modify query, save back

**Category:** Multi-Step Orchestration
**Difficulty:** Hard

**User prompt:**
> In dataflow `df-100` workspace `ws-100`, change the "GetOrders" query to filter only orders from 2025

**Expected tool call sequence:**
1. Tool: `get_dataflow_definition`
   - `workspaceId`: `ws-100`
   - `dataflowId`: `df-100`
   - Returns: definition with existing M code including GetOrders query

2. Tool: `AddOrUpdateQueryInDataflowAsync`
   - `workspaceId`: `ws-100`
   - `dataflowId`: `df-100`
   - `queryName`: `GetOrders`
   - `mCode`: modified M code with date filter for 2025

**Assertions:**
- Must read the definition first to understand the existing query
- Must use `AddOrUpdateQuery` (not `save_dataflow_definition`) for a single query change
- Modified M code must add a filter for year 2025

---

### EVAL-MS-010: Full pipeline workflow — create, define, check

**Category:** Multi-Step Orchestration
**Difficulty:** Hard

**User prompt:**
> In workspace `ws-cicd`:
> 1. Create a pipeline called "CI Validation"
> 2. Set its definition to `{"activities": [{"name": "RunTests", "type": "Script"}]}`
> 3. Verify the definition was saved correctly

**Expected tool call sequence:**
1. Tool: `CreatePipelineAsync`
   - `workspaceId`: `ws-cicd`
   - `displayName`: `CI Validation`
   - Returns: `{ "pipelineId": "pl-new" }`

2. Tool: `UpdatePipelineDefinitionAsync`
   - `workspaceId`: `ws-cicd`
   - `pipelineId`: `pl-new`
   - `definitionJson`: `{"activities": [{"name": "RunTests", "type": "Script"}]}`

3. Tool: `GetPipelineDefinitionAsync`
   - `workspaceId`: `ws-cicd`
   - `pipelineId`: `pl-new`

**Assertions:**
- Must follow the numbered sequence exactly
- Must verify by reading back the definition in step 3
- Must use the ID from step 1 consistently
