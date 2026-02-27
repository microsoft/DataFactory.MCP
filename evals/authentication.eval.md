# Authentication Tool Evals

Tools under test:
- `AuthenticateServicePrincipalAsync(applicationId, clientSecret, tenantId?)`
- `AuthenticateInteractiveAsync()`
- `StartDeviceCodeAuthAsync()`
- `CheckDeviceAuthStatusAsync()`
- `GetAuthenticationStatus()`
- `SignOutAsync()`
- `GetAccessTokenAsync()`

---

## Tool Selection

### EVAL-AUTH-001: Direct request to sign in interactively

**Category:** Tool Selection
**Difficulty:** Easy

**User prompt:**
> Sign me in to Fabric

**Expected tool call(s):**
- Tool: `AuthenticateInteractiveAsync`
  - No parameters

**Assertions:**
- Must select interactive auth, not service principal or device code
- Must not ask for credentials before attempting

**Notes:**
> "Sign me in" without credential details implies interactive login.

---

### EVAL-AUTH-002: Service principal with explicit credentials

**Category:** Tool Selection
**Difficulty:** Easy

**User prompt:**
> Authenticate using service principal with app ID `abc-123`, secret `s3cret`, tenant `contoso.onmicrosoft.com`

**Expected tool call(s):**
- Tool: `AuthenticateServicePrincipalAsync`
  - `applicationId`: `abc-123`
  - `clientSecret`: `s3cret`
  - `tenantId`: `contoso.onmicrosoft.com`

**Assertions:**
- Must select service principal auth, not interactive
- All three parameters extracted correctly from prompt

---

### EVAL-AUTH-003: Device code auth request

**Category:** Tool Selection
**Difficulty:** Easy

**User prompt:**
> I can't open a browser on this machine. Help me sign in.

**Expected tool call(s):**
- Tool: `StartDeviceCodeAuthAsync`
  - No parameters

**Assertions:**
- Must select device code flow, not interactive
- Should explain the device code process after receiving the response

**Notes:**
> "Can't open a browser" is the key signal for device code vs interactive.

---

### EVAL-AUTH-004: Check auth status

**Category:** Tool Selection
**Difficulty:** Easy

**User prompt:**
> Am I logged in?

**Expected tool call(s):**
- Tool: `GetAuthenticationStatus`
  - No parameters

**Assertions:**
- Must not attempt to authenticate — just check status
- Must not call `GetAccessTokenAsync` (that's for retrieving the token, not checking status)

---

### EVAL-AUTH-005: Sign out

**Category:** Tool Selection
**Difficulty:** Easy

**User prompt:**
> Log me out

**Expected tool call(s):**
- Tool: `SignOutAsync`
  - No parameters

**Assertions:**
- Must select sign out, not auth status

---

### EVAL-AUTH-006: Get access token

**Category:** Tool Selection
**Difficulty:** Easy

**User prompt:**
> Give me my current access token

**Expected tool call(s):**
- Tool: `GetAccessTokenAsync`
  - No parameters

**Assertions:**
- Must select `GetAccessTokenAsync`, not `GetAuthenticationStatus`

---

### EVAL-AUTH-007: Check device code polling

**Category:** Tool Selection
**Difficulty:** Medium

**User prompt:**
> Did my device code login complete yet?

**Context:**
> Previously called `StartDeviceCodeAuthAsync` which returned a device code.

**Expected tool call(s):**
- Tool: `CheckDeviceAuthStatusAsync`
  - No parameters

**Assertions:**
- Must use the status check tool, not start a new device code flow

---

## Parameter Extraction

### EVAL-AUTH-008: Service principal with only app ID and secret

**Category:** Parameter Extraction
**Difficulty:** Medium

**User prompt:**
> Log in with service principal. App ID is `12345678-1234-1234-1234-123456789012` and the secret is `myClientSecret123`

**Expected tool call(s):**
- Tool: `AuthenticateServicePrincipalAsync`
  - `applicationId`: `12345678-1234-1234-1234-123456789012`
  - `clientSecret`: `myClientSecret123`
  - `tenantId`: `null` (not provided, optional)

**Assertions:**
- Must not hallucinate a tenant ID
- Must pass `tenantId` as null or omit it

---

### EVAL-AUTH-009: Credentials spread across conversation

**Category:** Parameter Extraction
**Difficulty:** Hard

**User prompt:**
> Use those credentials to authenticate as a service principal

**Context:**
> Earlier in conversation, user said:
> "My app registration has client ID `app-reg-001`"
> "The secret is `topSecret!`"
> "We're in the `fabrikam` tenant"

**Expected tool call(s):**
- Tool: `AuthenticateServicePrincipalAsync`
  - `applicationId`: `app-reg-001`
  - `clientSecret`: `topSecret!`
  - `tenantId`: `fabrikam`

**Assertions:**
- All three values must be extracted from prior conversation context
- Must not ask the user to repeat credentials

---

### EVAL-AUTH-010: Partial credentials — should ask for missing

**Category:** Parameter Extraction
**Difficulty:** Medium

**User prompt:**
> Authenticate with service principal `my-app-id`

**Expected behavior:**
- Must NOT call `AuthenticateServicePrincipalAsync` yet
- Must ask the user for the missing `clientSecret`

**Assertions:**
- `clientSecret` is required — model must not call the tool without it
- May optionally ask for `tenantId` but it's not required

---

## Edge Cases

### EVAL-AUTH-011: Ambiguous "login" without context

**Category:** Edge Case
**Difficulty:** Medium

**User prompt:**
> Login

**Expected behavior:**
- Should default to `AuthenticateInteractiveAsync` as the simplest auth method
- OR ask the user which authentication method they prefer

**Assertions:**
- Must not call `AuthenticateServicePrincipalAsync` without credentials
- Must not call `StartDeviceCodeAuthAsync` without indication that browser is unavailable

---

### EVAL-AUTH-012: Auth error recovery — retry after sign out

**Category:** Edge Case
**Difficulty:** Hard

**User prompt:**
> I'm getting authentication errors on all my requests. Can you fix it?

**Expected tool call(s):**
1. Tool: `GetAuthenticationStatus` — check current state
2. Based on response, possibly `SignOutAsync` then re-authenticate

**Assertions:**
- Must diagnose before taking action (check status first)
- Must not immediately re-authenticate without understanding the issue

---

### EVAL-AUTH-013: Tool call when not authenticated — error handling

**Category:** Edge Case
**Difficulty:** Medium

**User prompt:**
> List my workspaces

**Context:**
> `ListWorkspacesAsync` returns: `Authentication error: Authentication is required`

**Expected behavior:**
- Recognize the auth error from the tool response
- Suggest or initiate authentication before retrying

**Assertions:**
- Must not retry the same call without authenticating first
- Must inform the user that authentication is needed

---

### EVAL-AUTH-014: Service principal secret in secure manner

**Category:** Edge Case
**Difficulty:** Medium

**User prompt:**
> Authenticate with app ID `my-app` and secret `s3cret123` in tenant `mytenant`

**Expected tool call(s):**
- Tool: `AuthenticateServicePrincipalAsync`
  - `applicationId`: `my-app`
  - `clientSecret`: `s3cret123`
  - `tenantId`: `mytenant`

**Assertions:**
- Must not echo the secret back to the user in its response text
- Must pass the exact secret value to the tool (no masking in the parameter)
