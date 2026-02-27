# Gateway Tool Evals

Tools under test:
- `ListGatewaysAsync(continuationToken?)`
- `GetGatewayAsync(gatewayId)`
- `create_virtualnetwork_gateway(displayName, capacityId, subscriptionId, resourceGroupName, virtualNetworkName, subnetName, inactivityMinutesBeforeSleep?, numberOfMemberGateways?)`

---

## Tool Selection

### EVAL-GW-001: List gateways

**Category:** Tool Selection
**Difficulty:** Easy

**User prompt:**
> Show me all gateways

**Expected tool call(s):**
- Tool: `ListGatewaysAsync`
  - No parameters

**Assertions:**
- Must select gateway list, not connection or capacity tools
- "Gateways" maps specifically to the gateway tool

---

### EVAL-GW-002: Get a specific gateway

**Category:** Tool Selection
**Difficulty:** Easy

**User prompt:**
> Get details for gateway `gw-12345678-abcd-1234-ef56-123456789abc`

**Expected tool call(s):**
- Tool: `GetGatewayAsync`
  - `gatewayId`: `gw-12345678-abcd-1234-ef56-123456789abc`

**Assertions:**
- Must use GetGateway, not ListGateways

---

### EVAL-GW-003: Create a VNet gateway

**Category:** Tool Selection
**Difficulty:** Medium

**User prompt:**
> Create a virtual network gateway

**Expected behavior:**
- Select `create_virtualnetwork_gateway`
- Ask for required parameters (displayName, capacityId, subscriptionId, resourceGroupName, virtualNetworkName, subnetName)

**Assertions:**
- Must select the create gateway tool
- Must ask for the many required parameters before calling

---

## Parameter Extraction

### EVAL-GW-004: VNet gateway with all parameters

**Category:** Parameter Extraction
**Difficulty:** Hard

**User prompt:**
> Create a VNet gateway called "Prod VNet GW" on capacity `cap-123` in Azure subscription `sub-456`, resource group `rg-networking`, VNet `vnet-prod`, subnet `default`. Set inactivity timeout to 60 minutes with 2 member gateways.

**Expected tool call(s):**
- Tool: `create_virtualnetwork_gateway`
  - `displayName`: `Prod VNet GW`
  - `capacityId`: `cap-123`
  - `subscriptionId`: `sub-456`
  - `resourceGroupName`: `rg-networking`
  - `virtualNetworkName`: `vnet-prod`
  - `subnetName`: `default`
  - `inactivityMinutesBeforeSleep`: `60`
  - `numberOfMemberGateways`: `2`

**Assertions:**
- All 8 parameters correctly extracted
- Inactivity minutes must be numeric, not a string

---

### EVAL-GW-005: VNet gateway with defaults

**Category:** Parameter Extraction
**Difficulty:** Medium

**User prompt:**
> Create a VNet gateway "Test GW" on capacity `cap-test` in subscription `sub-test`, resource group `rg-test`, VNet `vnet-test`, subnet `subnet-1`

**Expected tool call(s):**
- Tool: `create_virtualnetwork_gateway`
  - `displayName`: `Test GW`
  - `capacityId`: `cap-test`
  - `subscriptionId`: `sub-test`
  - `resourceGroupName`: `rg-test`
  - `virtualNetworkName`: `vnet-test`
  - `subnetName`: `subnet-1`
  - `inactivityMinutesBeforeSleep`: `120` (default)
  - `numberOfMemberGateways`: `1` (default)

**Assertions:**
- Must use defaults for unspecified optional parameters
- Must not hallucinate values for inactivity or member count

---

### EVAL-GW-006: Gateway ID extraction from context

**Category:** Parameter Extraction
**Difficulty:** Medium

**User prompt:**
> Tell me more about the first gateway in the list

**Context:**
> Previous `ListGatewaysAsync` returned:
> ```json
> { "totalCount": 3, "gateways": [
>   { "id": "gw-aaa", "displayName": "Production GW", "type": "OnPremises" },
>   { "id": "gw-bbb", "displayName": "Dev GW", "type": "VirtualNetwork" }
> ]}
> ```

**Expected tool call(s):**
- Tool: `GetGatewayAsync`
  - `gatewayId`: `gw-aaa`

**Assertions:**
- Must extract the ID of the first gateway from prior response
- Must not use the display name as the ID

---

## Edge Cases

### EVAL-GW-007: Invalid inactivity timeout value

**Category:** Edge Case
**Difficulty:** Medium

**User prompt:**
> Create a VNet gateway "Test" on capacity `cap-1` in sub `sub-1`, rg `rg-1`, VNet `vnet-1`, subnet `s1` with 45 minute timeout

**Expected behavior:**
- The tool validates inactivity must be one of: 30, 60, 90, 120, 150, 240, 360, 480, 720, 1440
- Model should either pick nearest valid value (30 or 60) or inform the user of valid options

**Assertions:**
- Must not pass `45` to the tool (it will fail validation)
- Should suggest a valid value to the user

---

### EVAL-GW-008: Gateway vs connection confusion

**Category:** Edge Case
**Difficulty:** Medium

**User prompt:**
> I need to set up a gateway connection to my on-premises SQL server

**Expected behavior:**
- "Gateway connection" is ambiguous — could mean:
  - Create a gateway (`create_virtualnetwork_gateway`)
  - Create a connection through a gateway (`CreateConnectionAsync` with `connectivityType: OnPremisesGateway`)
- Model should ask for clarification or explain both options

**Assertions:**
- Must not assume one interpretation without context
- If the user has an existing gateway, creating a connection through it is more likely

---

### EVAL-GW-009: Gateway list with no results

**Category:** Edge Case
**Difficulty:** Easy

**User prompt:**
> Do I have any gateways?

**Context:**
> `ListGatewaysAsync` returns the "No gateways found" message.

**Expected behavior:**
- Call `ListGatewaysAsync`
- Explain that no gateways were found
- Optionally suggest creating one if the user needs one

**Assertions:**
- Must not treat "no gateways found" as an error
- Must present it as a valid empty result
