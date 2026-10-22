# Data Factory ChatGPT Skills

Operational tips and best practices for working with Microsoft Fabric Data Factory, optimized for ChatGPT Custom GPTs.

## Installation Options

### Option 1: Create a Custom GPT (Recommended)

1. Go to [ChatGPT](https://chat.openai.com) → **Explore GPTs** → **Create**
2. In the **Configure** tab:
   - **Name**: Data Factory Assistant
   - **Description**: Expert assistant for Microsoft Fabric Data Factory, M queries, and Dataflow Gen2
   - **Instructions**: Copy the contents of `gpt-instructions.md`
3. Under **Knowledge**, upload these files:
   - `knowledge-core.md`
   - `knowledge-performance.md`
   - `knowledge-destinations.md`
   - `knowledge-advanced.md`
   - `knowledge-data-visuals.md`
4. Under **Capabilities**, enable:
   - ✅ Code Interpreter (for M code analysis)
5. Click **Create** → **Save**

### Option 2: Use with ChatGPT Projects

1. Create a new Project in ChatGPT
2. Go to Project Settings → **Instructions**
3. Paste the contents of `gpt-instructions.md`
4. Upload the knowledge files to the project

### Option 3: Custom Instructions (Personal Use)

1. Go to ChatGPT Settings → **Personalization** → **Custom Instructions**
2. In "How would you like ChatGPT to respond?", paste a condensed version of `gpt-instructions.md`

## Files

| File | Purpose |
|------|---------|
| `gpt-instructions.md` | System instructions for the GPT |
| `knowledge-core.md` | M basics, Dataflow Gen2 overview |
| `knowledge-performance.md` | Query optimization, timeouts, chunking |
| `knowledge-destinations.md` | Output configuration, programmatic setup |
| `knowledge-advanced.md` | Fast Copy, Action.Sequence, Modern Evaluator |
| `knowledge-data-visuals.md` | Dataflow Gen2 Visual contract, workflow, and limitations |

## What's Covered

### Core
- M (Power Query) fundamentals
- Dataflow Gen2 architecture
- Common patterns and best practices

### Performance
- Handling query timeouts via chunking
- Filter early, expensive operations last
- Connector selection
- Query organization patterns

### Destinations
- Lakehouse → Lakehouse architecture
- Automatic vs Manual schema settings
- Programmatic destination configuration
- Hidden `_DataDestination` query pattern

### Advanced
- `Action.Sequence` for side-effecting writes
- Fast Copy (limited transforms, fast ingestion)
- Modern Evaluator (complex transforms, limited connectors)

### Data visuals
- Visualization document contract (flat five-column table)
- Closed PartType set and required properties
- Visual selection, validation, and Preview limitations

## Usage Examples

Once your GPT is created, you can ask:

- "My query is timing out, how do I fix it?"
- "How do I set up a data destination programmatically?"
- "What's the difference between Fast Copy and Modern Evaluator?"
- "Help me write an M query to aggregate sales by month"
- "Explain Action.Sequence and when to use it"
- "Build a dashboard from my SalesData query"

## Comparison with Claude Skills

| Feature | Claude Skills | ChatGPT Skills |
|---------|---------------|----------------|
| Format | Multiple .md files with YAML frontmatter | Instructions + Knowledge files |
| Loading | On-demand via RAG triggers | All knowledge available |
| Best for | Claude Projects | Custom GPTs or ChatGPT Projects |

## Dataflow Gen2 Visual builder

Visual requests are routed by `gpt-instructions.md`, which loads
`knowledge-data-visuals.md`. Visual is the canonical artifact name; dashboard,
report, and chart remain discovery synonyms.

Use the shared cases in `../evals/dataflow-visuals-parity.md` to run the same
prompts in ChatGPT and Claude and compare both outputs with one checklist. No
provider API keys are required.
