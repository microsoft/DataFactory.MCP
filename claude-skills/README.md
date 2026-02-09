# Data Factory MCP Skills

Operational tips and best practices for working with Microsoft Fabric Data Factory MCP tools.

## Installation

Upload all 5 `.md` files to your Claude Project:

1. Go to your Claude Project settings
2. Add these files to Project Knowledge:
   - `datafactory-SKILL.md`
   - `datafactory-core.md`
   - `datafactory-performance.md`
   - `datafactory-destinations.md`
   - `datafactory-advanced.md`

## Files

| File | Purpose | When Claude Loads |
|------|---------|-------------------|
| `datafactory-SKILL.md` | Index file | Always (tells Claude when to load others) |
| `datafactory-core.md` | M basics, MCP tools overview | Always |
| `datafactory-performance.md` | Query optimization, timeouts, chunking | On-demand |
| `datafactory-destinations.md` | Output configuration, programmatic setup | On-demand |
| `datafactory-advanced.md` | Fast Copy, Action.Sequence, Modern Evaluator | On-demand |

## What's Covered

### Core
- M (Power Query) fundamentals
- Dataflow Gen2 overview
- MCP tool reference

### Performance
- Handling query timeouts via chunking
- Filter early, expensive operations last
- Connector selection
- Query organization patterns

### Destinations
- Lakehouse → Lakehouse architecture
- Automatic vs Manual schema settings
- **Programmatic destination configuration via `validate_and_save_m_document`**
- Hidden `_DataDestination` query pattern
- Connection ID formats

### Advanced
- `Action.Sequence` for side-effecting writes
- Fast Copy (limited transforms, fast ingestion)
- Modern Evaluator (complex transforms, limited connectors)

## Usage

Once installed, Claude will automatically reference these files when you ask about Data Factory topics:

- "My query is timing out" → loads `datafactory-performance.md`
- "How do I set the output destination programmatically?" → loads `datafactory-destinations.md`
- "What's Fast Copy?" → loads `datafactory-advanced.md`

## Requirements

- Claude Project with Data Factory MCP tools enabled
- MCP tools: `DataFactory.MCP:*`

## Contributing

To update the skills:
1. Edit the relevant `.md` file
2. Re-upload to your Claude Project
3. Changes take effect in new conversations
