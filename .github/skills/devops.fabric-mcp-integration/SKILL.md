---
name: devops.fabric-mcp-integration
description: "DevOps and CI integration with microsoft/mcp (Fabric.Mcp.Server). Use when publishing packages, managing feed availability, or troubleshooting CI failures."
---

# DevOps Integration with Fabric.Mcp.Server

## Overview

The `microsoft/mcp` repo consumes `Microsoft.DataFactory.MCP.Core` as a NuGet package
in `Fabric.Mcp.Tools.DataFactory`. CI builds use a DevOps feed with upstream caching
from nuget.org — packages are **not** immediately available after publishing.

## Feed Configuration

The Fabric.Mcp.Server `NuGet.config` uses a single feed:

```xml
<packageSources>
    <clear />
    <add key="azure-sdk-for-net"
         value="https://pkgs.dev.azure.com/azure-sdk/public/_packaging/azure-sdk-for-net/nuget/v3/index.json" />
</packageSources>
```

- **Feed UI:** https://dev.azure.com/azure-sdk/public/_artifacts/feed/azure-sdk-for-net
- **No direct nuget.org access** — all packages must come through this feed
- The feed has an **upstream** to nuget.org that caches packages on-demand

## How Upstream Caching Works

1. A new version is published to **nuget.org**
2. It is **NOT** immediately available on the DevOps feed
3. A Collaborator runs `dotnet restore` against the DevOps feed
4. The feed fetches and caches the package from nuget.org
5. Subsequent Fabric.Mcp.Server CI builds can then resolve it

> **Important:** The `nuget list` search index can lag behind actual availability.
> If `dotnet restore` succeeds, the package IS available for CI even if `nuget list` doesn't show it yet.

## After Publishing a New Version

### Step 1: Verify on nuget.org

```bash
nuget list Microsoft.DataFactory.MCP.Core \
  -Source "https://api.nuget.org/v3/index.json" \
  -PreRelease -AllVersions
```

### Step 2: Trigger the DevOps feed to cache it

Run from the `microsoft/mcp` repo root:

```bash
dotnet restore tools/Fabric.Mcp.Tools.DataFactory/src/Fabric.Mcp.Tools.DataFactory.csproj \
  --source "https://pkgs.dev.azure.com/azure-sdk/public/_packaging/azure-sdk-for-net/nuget/v3/index.json"
```

If restore succeeds, the package is cached and Fabric.Mcp.Server CI will find it.

### Step 3: Verify (optional)

```bash
nuget list Microsoft.DataFactory.MCP.Core \
  -Source "https://pkgs.dev.azure.com/azure-sdk/public/_packaging/azure-sdk-for-net/nuget/v3/index.json" \
  -PreRelease -AllVersions
```

> The search index may lag — restore success is the definitive proof.

## Version Management

### DataFactory.MCP side (this repo)

Version is in `Directory.Build.props`:

```xml
<MajorVersion>0</MajorVersion>
<MinorVersion>20</MinorVersion>
<PatchVersion>0</PatchVersion>
<PreReleaseLabel>beta</PreReleaseLabel>
```

### Fabric.Mcp.Server side (consumer repo)

Version is in `Directory.Packages.props` (Central Package Management):

```xml
<PackageVersion Include="Microsoft.DataFactory.MCP.Core" Version="0.20.0-beta" />
```

The project reference is in:
`tools/Fabric.Mcp.Tools.DataFactory/src/Fabric.Mcp.Tools.DataFactory.csproj`

## Credential Provider Setup

To authenticate to the DevOps feed locally, install the credential provider:
https://go.microsoft.com/fwlink/?linkid=2099625

Once installed, `dotnet restore` triggers an auth challenge and grants Collaborator access.
The feed is public — anyone can pull packages that are already cached.
Collaborators can also trigger the feed to ingest new packages from nuget.org.

## Trim Safety Requirements

Fabric.Mcp.Server publishes with IL trimming enabled:

```bash
dotnet publish servers/Fabric.Mcp.Server/src/Fabric.Mcp.Server.csproj \
  --runtime linux-x64 --self-contained \
  /p:PublishTrimmed=true /p:PublishSingleFile=true /p:TreatWarningsAsErrors=true
```

All code consumed from `Microsoft.DataFactory.MCP.Core` must be trim-safe:
- Use source-generated JSON (`DataFactoryJsonContext`) instead of reflection-based serialization
- Mark reflection-dependent APIs with `[RequiresUnreferencedCode]`
- Test with the publish command above before submitting version bumps

## Troubleshooting

| Issue | Cause | Fix |
|-------|-------|-----|
| `NU1102: Unable to find package` | Version not cached on DevOps feed | Run `dotnet restore` locally to trigger upstream cache |
| `nuget list` doesn't show new version | Search index lag | Check with `dotnet restore` instead — success = available |
| 401 Unauthorized on restore | Missing credential provider | Install cred provider (link above) |
| IL2026 trim warnings on publish | Reflection-based JSON in consumed code | Use `DataFactoryJsonContext` source-gen JSON |
