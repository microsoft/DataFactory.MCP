---
name: builder.datafactory
description: "Building, testing, and running the DataFactory.MCP project. Use when building source, running tests, or troubleshooting build errors."
---

# Building DataFactory.MCP

## Prerequisites

- .NET 10 SDK installed
- NuGet feeds configured (see `nuget.config` and `nuget.private.config`)

## Commands

All commands run from the repo root.

| Task | Command |
|------|---------|
| Build all | `dotnet build` |
| Build release | `dotnet build -c Release` |
| Build specific project | `dotnet build DataFactory.MCP.Core/` |
| Run all tests | `dotnet test` |
| Run tests verbose | `dotnet test -v normal` |
| Run specific test | `dotnet test --filter "FullyQualifiedName~TestName"` |
| Run with coverage | `dotnet test --collect:"XPlat Code Coverage"` |
| Run MCP server | `dotnet run --project DataFactory.MCP` |
| Clean + build | `dotnet clean && dotnet build` |
| Restore packages | `dotnet restore` |

## Versioning

Version is defined in `Directory.Build.props`:
```xml
<MajorVersion>0</MajorVersion>
<MinorVersion>19</MinorVersion>
<PatchVersion>0</PatchVersion>
<PreReleaseLabel>beta</PreReleaseLabel>
```

To bump version, use `Update-ServerVersion.ps1` or edit `Directory.Build.props` directly.

## NuGet Packages Produced

- `Microsoft.DataFactory.MCP` — Main server package
- `Microsoft.DataFactory.MCP.Core` — Core library
- `Microsoft.DataFactory.MCP.Http` — HTTP transport

## CI Feed Configuration (mcp repo integration)

The `microsoft/mcp` repo uses a single DevOps NuGet feed with an upstream to nuget.org.
CI builds **only** pull from this feed — no direct nuget.org access.

**Feed URL:** `https://pkgs.dev.azure.com/azure-sdk/public/_packaging/azure-sdk-for-net/nuget/v3/index.json`
**Feed UI:** https://dev.azure.com/azure-sdk/public/_artifacts/feed/azure-sdk-for-net

### How package caching works

The DevOps feed caches packages **on-demand** from its nuget.org upstream:
1. A new version is published to nuget.org
2. It is **NOT** immediately available on the DevOps feed
3. A Collaborator must trigger a `dotnet restore` against the DevOps feed
4. The feed fetches and caches the package from nuget.org
5. Subsequent CI builds can then resolve it

### After publishing a new version

```bash
# 1. Verify the package exists on nuget.org
nuget list Microsoft.DataFactory.MCP.Core -Source "https://api.nuget.org/v3/index.json" -PreRelease -AllVersions

# 2. Trigger the DevOps feed to cache it (run from mcp repo root)
dotnet restore tools/Fabric.Mcp.Tools.DataFactory/src/Fabric.Mcp.Tools.DataFactory.csproj \
  --source "https://pkgs.dev.azure.com/azure-sdk/public/_packaging/azure-sdk-for-net/nuget/v3/index.json"

# 3. Verify it's cached (search index may lag behind — restore success is the real proof)
nuget list Microsoft.DataFactory.MCP.Core -Source "https://pkgs.dev.azure.com/azure-sdk/public/_packaging/azure-sdk-for-net/nuget/v3/index.json" -PreRelease -AllVersions
```

> **Note:** The `nuget list` search index can lag behind actual availability.
> If `dotnet restore` succeeds, the package IS available for CI even if `nuget list` doesn't show it yet.

### DevOps feed credential provider

If you haven't authenticated to the DevOps feed locally, install the credential provider:
https://go.microsoft.com/fwlink/?linkid=2099625

Once installed, `dotnet restore` will trigger an auth challenge and let you query the feed as a Collaborator.

### npm feed (McpApps UI)

The `DataFactory.MCP.Core/Resources/McpApps/` project uses the `Gateway-AdminPortal` DevOps npm feed.
Same caching behavior applies — new npm versions may not be immediately available.

If a package version isn't cached yet (404 on `npm ci`), pin to a version that exists on the feed
or wait for the upstream to sync.

## Troubleshooting

| Issue | Fix |
|-------|-----|
| Missing packages | `dotnet restore` (check `nuget.config` for feed URLs) |
| Framework not found | Install .NET 10 SDK |
| Private feed auth | Check `nuget.private.config` credentials |
| Build warnings | Nullable is enabled project-wide; fix nullable warnings |
| Package not on DevOps feed | Run `dotnet restore` locally to trigger upstream cache (see above) |
| npm 404 in CI | Pin to a version available on Gateway-AdminPortal feed |
