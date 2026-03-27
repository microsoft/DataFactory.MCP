---
name: coder.datafactory-style
description: "C# coding standards and conventions for DataFactory.MCP. Use when writing or reviewing C# code."
---

# Coding Style — DataFactory.MCP

## Project Settings

- **Target:** net10.0
- **Nullable:** enabled (project-wide)
- **Implicit usings:** enabled
- **License:** MIT

## Conventions

### Naming

| Element | Style | Example |
|---------|-------|---------|
| Classes | PascalCase | `FabricGatewayService` |
| Interfaces | `I` prefix | `IFabricGatewayService` |
| Methods | PascalCase | `ListGatewaysAsync` |
| Parameters | camelCase | `gatewayClusterId` |
| Private fields | `_` prefix | `_httpClient` |
| Constants | PascalCase | `MaxRetryCount` |
| Async methods | `Async` suffix | `GetGatewayAsync` |

### File Organization

- One primary class per file
- File name matches class name
- Tools in `Tools/`, Services in `Services/`, Models in `Models/`
- Interfaces in `Abstractions/`

### Null Safety

- Nullable is ON — respect nullable annotations
- Use `?` for nullable types, `!` only when provably non-null
- Prefer pattern matching: `if (value is not null)`
- Never suppress `#pragma warning disable` for nullable without justification

### Error Handling

- Catch specific exceptions, not `Exception` broadly
- Translate API errors to meaningful MCP error responses
- Always check auth state before API calls
- Log errors with context (operation name, resource ID)

### Async/Await

- All I/O operations must be async
- Use `ConfigureAwait(false)` in library code (Core project)
- Never use `.Result` or `.Wait()` — always await

### Dependency Injection

- All services registered in DI container via extension methods in `Extensions/`
- Constructor injection only
- Depend on interfaces (`Abstractions/`), not concrete types

## Key Patterns

### Tool → Service → Model

```
Tool (MCP schema + validation) → Service (API call) → Model (data)
```

- Tools handle: parameter parsing, validation, MCP response formatting
- Services handle: HTTP calls, auth tokens, error translation
- Models handle: data shape (DTOs, no logic)

### Configuration

- Feature flags control tool availability
- API versions managed in `Configuration/` classes
- Environment variables for auth settings
