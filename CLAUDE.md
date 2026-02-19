# Data Factory MCP Server

.NET MCP server for Microsoft Fabric Data Factory operations.

## Project Structure
- `DataFactory.MCP/` — Main MCP server
- `DataFactory.MCP.Core/` — Core library
- `DataFactory.MCP.Http/` — HTTP transport
- `DataFactory.MCP.Tests/` — Test suite
- `claude-skills/` — Skill definitions for Claude

## Development
```bash
# Build
dotnet build

# Test
dotnet test

# Run server
dotnet run --project DataFactory.MCP
```

## Skills
Read `claude-skills/SKILL.md` before working with skills. Uses RAG pattern:
- `datafactory-core.md` — Always loaded
- Other files load on-demand based on topic triggers

## Critical Rules
- **Never sample data** with `Table.FirstN` — use chunking strategies instead
- When queries timeout, chunk by date/ID/region, then aggregate
- Filter early to enable query folding
- Place expensive operations (sort) at the end

## State Files
Working state in `.claude/state/`. Read `current-task.md` to resume.
