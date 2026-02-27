# Connection Tool Evals

Tools under test:
- `ListSupportedConnectionTypesAsync(gatewayId?)`
- `ListConnectionsAsync(continuationToken?)`
- `GetConnectionAsync(connectionId)`
- `CreateConnectionAsync(connectionName, connectionType, creationMethod?, connectionParameters?, credentialType, credentials?, privacyLevel, connectionEncryption, skipTestConnection, connectivityType, gatewayId?)`
- `create_connection_ui` — `ShowCreateConnectionForm()`

---

## Tool Selection

### EVAL-CONN-001: List existing connections

**Category:** Tool Selection
**Difficulty:** Easy

**User prompt:**
> Show me all my connections

**Expected tool call(s):**
- Tool: `ListConnectionsAsync`
  - No parameters

**Assertions:**
- Must select list connections, not list supported types
- Must not call `create_connection_ui`

---

### EVAL-CONN-002: What connection types are available

**Category:** Tool Selection
**Difficulty:** Easy

**User prompt:**
> What types of connections can I create?

**Expected tool call(s):**
- Tool: `ListSupportedConnectionTypesAsync`
  - No parameters

**Assertions:**
- Must select supported types, not list existing connections

---

### EVAL-CONN-003: Get a specific connection

**Category:** Tool Selection
**Difficulty:** Easy

**User prompt:**
> Get details for connection `a1b2c3d4-e5f6-7890-abcd-ef1234567890`

**Expected tool call(s):**
- Tool: `GetConnectionAsync`
  - `connectionId`: `a1b2c3d4-e5f6-7890-abcd-ef1234567890`

**Assertions:**
- Must use GetConnection, not ListConnections
- Must extract the GUID correctly

---

### EVAL-CONN-004: Create a connection programmatically

**Category:** Tool Selection
**Difficulty:** Medium

**User prompt:**
> Create a new SQL connection called "Prod SQL" to server `sql.contoso.com` database `SalesDB` using basic auth with username `admin` and password `p@ss`

**Expected tool call(s):**
- Tool: `CreateConnectionAsync`
  - `connectionName`: `Prod SQL`
  - `connectionType`: `SQL`
  - `connectionParameters`: `{"server":"sql.contoso.com","database":"SalesDB"}`
  - `credentialType`: `Basic`
  - `credentials`: `{"username":"admin","password":"p@ss"}`

**Assertions:**
- Must use programmatic create, not UI tool
- Must correctly serialize parameters and credentials as JSON strings
- Must not call `create_connection_ui`

---

### EVAL-CONN-005: Create a connection via UI

**Category:** Tool Selection
**Difficulty:** Medium

**User prompt:**
> I'd like to set up a new connection. Can you show me the form?

**Expected tool call(s):**
- Tool: `create_connection_ui`
  - No parameters

**Assertions:**
- "Show me the form" signals UI preference
- Must not attempt `CreateConnectionAsync` with no details

**Notes:**
> The UI tool is preferred when the user wants a visual form rather than specifying all params via chat.

---

## Parameter Extraction

### EVAL-CONN-006: SQL connection with all details

**Category:** Parameter Extraction
**Difficulty:** Hard

**User prompt:**
> Create a cloud connection named "Analytics SQL" of type SQL to server `analytics.database.windows.net`, database `AnalyticsDB`. Use OAuth2 credentials. Set privacy to Organizational and encryption to Encrypted. Skip the test connection.

**Expected tool call(s):**
- Tool: `CreateConnectionAsync`
  - `connectionName`: `Analytics SQL`
  - `connectionType`: `SQL`
  - `connectionParameters`: `{"server":"analytics.database.windows.net","database":"AnalyticsDB"}`
  - `credentialType`: `OAuth2`
  - `privacyLevel`: `Organizational`
  - `connectionEncryption`: `Encrypted`
  - `skipTestConnection`: `true`
  - `connectivityType`: `ShareableCloud`

**Assertions:**
- All parameters extracted and placed in correct fields
- `connectivityType` defaults to or is set to `ShareableCloud`
- Must not hallucinate a `credentials` value for OAuth2

---

### EVAL-CONN-007: On-premises connection with gateway

**Category:** Parameter Extraction
**Difficulty:** Hard

**User prompt:**
> Create an on-prem SQL connection called "Factory DB" through gateway `gw-001` to server `10.0.0.5` database `FactoryDB` with Windows auth

**Expected tool call(s):**
- Tool: `CreateConnectionAsync`
  - `connectionName`: `Factory DB`
  - `connectionType`: `SQL`
  - `connectionParameters`: `{"server":"10.0.0.5","database":"FactoryDB"}`
  - `credentialType`: `Windows`
  - `connectivityType`: `OnPremisesGateway`
  - `gatewayId`: `gw-001`

**Assertions:**
- Must set `connectivityType` to `OnPremisesGateway`
- Must include `gatewayId`
- "On-prem" maps to `OnPremisesGateway`

---

### EVAL-CONN-008: VNet connection with gateway

**Category:** Parameter Extraction
**Difficulty:** Medium

**User prompt:**
> Create a virtual network connection to Azure Blobs using gateway `vnet-gw-123`. Name it "VNet Blob Storage".

**Expected tool call(s):**
- Tool: `CreateConnectionAsync`
  - `connectionName`: `VNet Blob Storage`
  - `connectionType`: `AzureBlobs`
  - `connectivityType`: `VirtualNetworkGateway`
  - `gatewayId`: `vnet-gw-123`

**Assertions:**
- "Virtual network" maps to `VirtualNetworkGateway`
- Must include the gateway ID

---

### EVAL-CONN-009: Supported types filtered by gateway

**Category:** Parameter Extraction
**Difficulty:** Medium

**User prompt:**
> What connection types does gateway `gw-456` support?

**Expected tool call(s):**
- Tool: `ListSupportedConnectionTypesAsync`
  - `gatewayId`: `gw-456`

**Assertions:**
- Must pass the gateway ID to filter supported types
- Must not call `ListGatewaysAsync` or other gateway tools

---

### EVAL-CONN-010: Service principal credentials

**Category:** Parameter Extraction
**Difficulty:** Hard

**User prompt:**
> Create a Web connection named "API Endpoint" to `https://api.example.com`. Authenticate with a service principal: client ID `sp-client-123`, secret `sp-secret-456`, tenant `my-tenant-id`.

**Expected tool call(s):**
- Tool: `CreateConnectionAsync`
  - `connectionName`: `API Endpoint`
  - `connectionType`: `Web`
  - `connectionParameters`: `{"url":"https://api.example.com"}` (parameter name may vary)
  - `credentialType`: `ServicePrincipal`
  - `credentials`: `{"servicePrincipalClientId":"sp-client-123","servicePrincipalSecret":"sp-secret-456","tenantId":"my-tenant-id"}`

**Assertions:**
- Must map service principal fields to the correct JSON credential keys
- Must not confuse connection parameters with credentials

---

## Edge Cases

### EVAL-CONN-011: Create without specifying all required details

**Category:** Edge Case
**Difficulty:** Medium

**User prompt:**
> Create a new SQL connection

**Expected behavior:**
- Must ask for at minimum: connection name and server/database parameters
- Should NOT call `CreateConnectionAsync` with just defaults

**Assertions:**
- Must ask the user for missing required information
- Calling the tool with just a name and "SQL" type would create a broken connection

---

### EVAL-CONN-012: Ambiguous — create via UI or programmatic

**Category:** Edge Case
**Difficulty:** Medium

**User prompt:**
> I want to add a new data source connection

**Expected behavior:**
- Could call `create_connection_ui` (show form) or ask for details to use `CreateConnectionAsync`
- Asking the user which approach they prefer is acceptable

**Assertions:**
- Must not call `CreateConnectionAsync` with no parameters
- Either showing the UI form or asking for details is valid

---

### EVAL-CONN-013: Connection ID in wrong format

**Category:** Edge Case
**Difficulty:** Easy

**User prompt:**
> Get connection details for `my-sql-connection`

**Expected behavior:**
- Call `GetConnectionAsync` with the provided string
- The tool's validation will return a GUID format error
- Model should relay the error to the user

**Assertions:**
- Must attempt the tool call (not pre-validate GUID format)
- Must clearly explain the validation error from the response

---

### EVAL-CONN-014: Pagination for connections

**Category:** Edge Case
**Difficulty:** Medium

**User prompt:**
> Show me the next page of connections

**Context:**
> Previous `ListConnectionsAsync` returned:
> ```json
> { "totalCount": 50, "continuationToken": "page2token", "hasMoreResults": true }
> ```

**Expected tool call(s):**
- Tool: `ListConnectionsAsync`
  - `continuationToken`: `page2token`

**Assertions:**
- Must use the continuation token from the previous response
- Must not call without the token

---

### EVAL-CONN-015: Key-based credential type

**Category:** Edge Case
**Difficulty:** Medium

**User prompt:**
> Create a connection to Azure Blobs named "Data Lake Raw" with account key `myAccountKey12345`

**Expected tool call(s):**
- Tool: `CreateConnectionAsync`
  - `connectionName`: `Data Lake Raw`
  - `connectionType`: `AzureBlobs`
  - `credentialType`: `Key`
  - `credentials`: `{"key":"myAccountKey12345"}`

**Assertions:**
- Must map "account key" to credential type `Key`
- Must place the key value in the correct credential JSON field
