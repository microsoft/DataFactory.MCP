# Apache Airflow Job Support — Feature Summary

## What Was Built

Full MCP tool support for **Microsoft Fabric Apache Airflow Jobs** was added to DataFactory.MCP. This follows the existing 3-layer architecture (Tool → Service → Model) and exposes 7 new MCP tools.

### New MCP Tools

| Tool | Description |
|------|-------------|
| `list_airflow_jobs` | List all Airflow Jobs in a workspace (paginated) |
| `create_airflow_job` | Create a new Airflow Job |
| `get_airflow_job` | Get metadata for a specific Airflow Job |
| `update_airflow_job` | Update display name and/or description |
| `delete_airflow_job` | Delete an Airflow Job (soft or hard delete) |
| `get_airflow_job_definition` | Retrieve the job's DAG/config definition (base64-decoded) |
| `update_airflow_job_definition` | Upload a new definition as JSON (auto base64-encoded) |

### Fabric REST API Mapping

All operations target the Fabric REST API under:

```
https://api.fabric.microsoft.com/v1/workspaces/{workspaceId}/apacheAirflowJobs
```

| Operation | Method | URL |
|-----------|--------|-----|
| List | GET | `.../apacheAirflowJobs` |
| Create | POST | `.../apacheAirflowJobs` |
| Get | GET | `.../apacheAirflowJobs/{airflowJobId}` |
| Update | PATCH | `.../apacheAirflowJobs/{airflowJobId}` |
| Delete | DELETE | `.../apacheAirflowJobs/{airflowJobId}?hardDelete=true` |
| Get Definition | POST | `.../apacheAirflowJobs/{airflowJobId}/getDefinition` |
| Update Definition | POST | `.../apacheAirflowJobs/{airflowJobId}/updateDefinition` |

---

## Files Added / Modified

### New Files

```
DataFactory.MCP.Core/
  Models/AirflowJob/
    AirflowJob.cs                          — Main DTO
    CreateAirflowJobRequest.cs             — Create request model
    UpdateAirflowJobRequest.cs             — Update request model
    ListAirflowJobsResponse.cs             — Paginated list response
    Definition/
      AirflowJobDefinition.cs              — Definition container
      AirflowJobDefinitionPart.cs          — Individual definition part (base64 payload)
      GetAirflowJobDefinitionResponse.cs   — GET definition response wrapper
      UpdateAirflowJobDefinitionRequest.cs — PUT definition request wrapper
  Abstractions/Interfaces/
    IFabricAirflowJobService.cs            — Service interface
  Services/
    FabricAirflowJobService.cs             — Fabric REST API implementation
  Extensions/
    AirflowJobExtensions.cs                — ToFormattedInfo() helper
  Tools/AirflowJob/
    AirflowJobTool.cs                      — MCP tool entry points

DataFactory.MCP.Tests/
  Integration/
    AirflowJobToolIntegrationTests.cs      — 37 integration tests
```

### Modified Files

```
DataFactory.MCP.Core/Extensions/ServiceCollectionExtensions.cs
  — Registered IFabricAirflowJobService + AirflowJobTool

DataFactory.MCP.Tests/Infrastructure/McpTestFixture.cs
  — Registered AirflowJobTool and FabricAirflowJobService for test DI
```

---

## Running the Tests

### Prerequisites

- .NET 10 SDK
- Run from the repo root: `c:\Users\makromer\projects\DataFactoryMCP`

### Run All Airflow Tests (no credentials needed)

```bash
dotnet test DataFactory.MCP.Tests\DataFactory.MCP.Tests.csproj --filter "AirflowJob"
```

Expected: **31 pass, 6 skipped** (the 6 skipped are authenticated-only tests).

### Run the Full Test Suite

```bash
dotnet test
```

Expected: **136+ pass, ~35 skipped, 0 failed**.

---

## Authenticated (Live API) Tests

Six tests only run when valid Azure AD credentials are available. They are marked with `[SkippableFact]` and auto-skip when unauthenticated.

| Test | What It Verifies |
|------|-----------------|
| `ListAirflowJobsAsync_WithAuthentication` | Can list jobs in the test workspace |
| `ListAirflowJobsAsync_WithInvalidWorkspaceId` | API returns proper GUID validation error |
| `GetAirflowJobAsync_NonExistentJob` | API returns 404-style error for unknown job |
| `CreateAirflowJobAsync_ShouldCreateSuccessfully` | Creates a job and cleans it up |
| `GetAirflowJobDefinitionAsync_NonExistentJob` | API error returned for unknown job |
| `AirflowJob_FullLifecycle_CreateGetUpdateDeleteAsync` | Full Create → Get → Update → List → GetDefinition → Delete cycle |

### Running Authenticated Tests

Authenticate first using the MCP tool (requires Fabric access — work/org account only):

```
mcp_datafactorymc_authenticate_interactive
```

Then run:

```bash
dotnet test DataFactory.MCP.Tests\DataFactory.MCP.Tests.csproj --filter "AirflowJob"
```

The `TryAuthenticateAsync()` call inside each test checks for a cached token. If found, the test runs; if not, it skips cleanly.

> **Note:** Fabric Airflow Jobs require a workspace with a `capacityId` (i.e., a Fabric capacity assigned). Free/PPU workspaces may return errors.

---

## Manual Testing via MCP

With the MCP server running in VS Code, you can test all 7 tools interactively through GitHub Copilot:

### 1. Authenticate

```
mcp_datafactorymc_authenticate_interactive
```

### 2. Find a workspace with a Fabric capacity

```
mcp_datafactorymc_list_workspaces
```

Pick one with a non-null `capacityId`.

### 3. Create an Airflow Job

```
mcp_datafactorymc_create_airflow_job
  workspaceId: "<your-workspace-id>"
  displayName: "My Test Job"
  description: "Testing the MCP tool"
```

### 4. List jobs to confirm

```
mcp_datafactorymc_list_airflow_jobs
  workspaceId: "<your-workspace-id>"
```

### 5. Get the job

```
mcp_datafactorymc_get_airflow_job
  workspaceId: "<your-workspace-id>"
  airflowJobId: "<id-from-create-response>"
```

### 6. Update metadata

```
mcp_datafactorymc_update_airflow_job
  workspaceId: "<your-workspace-id>"
  airflowJobId: "<id>"
  displayName: "Renamed Job"
```

### 7. Get the definition

```
mcp_datafactorymc_get_airflow_job_definition
  workspaceId: "<your-workspace-id>"
  airflowJobId: "<id>"
```

### 8. Update the definition

Provide a JSON object representing the Airflow scheduler config. The tool validates and base64-encodes it automatically:

```
mcp_datafactorymc_update_airflow_job_definition
  workspaceId: "<your-workspace-id>"
  airflowJobId: "<id>"
  definitionJson: "{\"schedulerConfig\":{}}"
```

### 9. Delete the job

```
mcp_datafactorymc_delete_airflow_job
  workspaceId: "<your-workspace-id>"
  airflowJobId: "<id>"
  hardDelete: true
```

---

## MCP Server Config

The server is configured in `.vscode/mcp.json`. After any code change, restart the MCP server in VS Code:

1. Open the Command Palette (`Ctrl+Shift+P`)
2. Search for **MCP: Restart Server** or find the server in the MCP panel and click **Restart**

New tools will appear in Copilot after the restart.
