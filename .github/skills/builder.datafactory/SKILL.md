---
name: builder.datafactory
description: "Building, testing, and running the DataFactory.MCP project. Use when building source, running tests, or troubleshooting build errors."
---

# Building DataFactory.MCP

## Prerequisites

- .NET 10 SDK installed
- NuGet feeds configured (see `nuget.config` and `nuget.private.config`)

## Commands

All commands run from repo root: `Q:\Repos\DataFactory.MCP`

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

## Troubleshooting

| Issue | Fix |
|-------|-----|
| Missing packages | `dotnet restore` (check `nuget.config` for feed URLs) |
| Framework not found | Install .NET 10 SDK |
| Private feed auth | Check `nuget.private.config` credentials |
| Build warnings | Nullable is enabled project-wide; fix nullable warnings |
